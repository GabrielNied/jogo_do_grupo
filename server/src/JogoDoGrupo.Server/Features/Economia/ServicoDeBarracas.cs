using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed record BarracaPresencialInfo(
    long PersonagemId,
    string NomeDono,
    string Titulo,
    float PosicaoX,
    float PosicaoZ,
    long MontadaEm
);

/// <summary>
public sealed class ServicoDeBarracas(
    JogoDbContext db,
    ICarteiraCoroas carteira,
    ServicoDoMercado mercado,
    RelogioDoMercado relogio,
    ControleDeBarracasAtivas ativas
)
{
    public bool TentarObterNomeBarraca(long personagemId, out string nomeOuTitulo) =>
        ativas.TentarObterNomeBarraca(personagemId, out nomeOuTitulo);

    public string? MontarBarraca(
        long personagemId,
        string nomeDono,
        string titulo,
        float x,
        float z
    )
    {
        if (!RegrasBarraca.PodeMontarBarraca(x, z, out var erro))
            return erro;

        var agora = relogio.AgoraSegundos;
        ativas.RegistrarPresencial(
            new BarracaPresencialInfo(personagemId, nomeDono, (titulo ?? "").Trim(), x, z, agora)
        );

        return null;
    }

    public void DesmontarBarraca(long personagemId)
    {
        ativas.RemoverPresencial(personagemId);
    }

    public void AoDesconectar(long personagemId)
    {
        // Barraca presencial é removida quando o personagem desloga;
        // o vendedor offline (se contratado) permanece.
        DesmontarBarraca(personagemId);
    }

    public async Task<string?> ContratarVendedorOfflineAsync(
        long personagemId,
        string nomeDono,
        string titulo,
        int dias,
        float x,
        float z,
        CancellationToken c
    )
    {
        var saldo = await carteira.SaldoAsync(personagemId, c);
        if (
            !RegrasBarraca.PodeContratarVendedorOffline(
                dias,
                x,
                z,
                saldo,
                out var erro,
                out var custoTotal
            )
        )
            return erro;

        await using var transacao = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            c
        );

        if (!await carteira.DebitarAsync(personagemId, custoTotal, c))
            return CodigosErroEconomia.SaldoInsuficiente;

        var agora = relogio.AgoraSegundos;
        var duracaoSegundos = checked(dias * ConfigBarraca.DuracaoDiariaSegundos);

        var existente = await db.Set<VendedorOfflineEntidade>()
            .SingleOrDefaultAsync(v => v.PersonagemId == personagemId, c);

        if (existente == null)
        {
            existente = new VendedorOfflineEntidade
            {
                PersonagemId = personagemId,
                NomeDono = nomeDono,
                Titulo = (titulo ?? "").Trim(),
                PosicaoX = x,
                PosicaoZ = z,
                ContratadoEm = agora,
                ExpiraEm = agora + duracaoSegundos,
                DiariasPagas = dias,
                Ativo = true,
            };
            db.Set<VendedorOfflineEntidade>().Add(existente);
        }
        else
        {
            existente.NomeDono = nomeDono;
            existente.Titulo = (titulo ?? "").Trim();
            existente.PosicaoX = x;
            existente.PosicaoZ = z;
            existente.ContratadoEm = agora;
            existente.ExpiraEm = Math.Max(existente.ExpiraEm, agora) + duracaoSegundos;
            existente.DiariasPagas += dias;
            existente.Ativo = true;
        }

        await db.SaveChangesAsync(c);
        await transacao.CommitAsync(c);
        return null;
    }

    public async Task<string?> EncerrarVendedorOfflineAsync(long personagemId, CancellationToken c)
    {
        var existente = await db.Set<VendedorOfflineEntidade>()
            .SingleOrDefaultAsync(v => v.PersonagemId == personagemId, c);

        if (existente == null || !existente.Ativo)
            return CodigosErroEconomia.VendedorOfflineNaoAtivo;

        existente.Ativo = false;
        await db.SaveChangesAsync(c);
        return null;
    }

    public async Task<int> VerificarExpiracoesAsync(CancellationToken c)
    {
        var agora = relogio.AgoraSegundos;
        var expirados = await db.Set<VendedorOfflineEntidade>()
            .Where(v => v.Ativo && v.ExpiraEm <= agora)
            .ToListAsync(c);

        if (expirados.Count == 0)
            return 0;

        foreach (var vendedor in expirados)
        {
            vendedor.Ativo = false;
        }

        await db.SaveChangesAsync(c);
        return expirados.Count;
    }

    public async Task<List<BarracaResumo>> ListarBarracasAtivasAsync(CancellationToken c)
    {
        var lista = new List<BarracaResumo>();
        var agora = relogio.AgoraSegundos;

        // 1. Barracas presenciais
        foreach (var p in ativas.BarracasPresenciais)
        {
            var ordens = await mercado.ListarOrdensDaBarracaAsync(p.PersonagemId, c);
            lista.Add(
                new BarracaResumo
                {
                    PersonagemId = p.PersonagemId,
                    NomeDono = p.NomeDono,
                    TituloBarraca = p.Titulo,
                    PosicaoX = p.PosicaoX,
                    PosicaoZ = p.PosicaoZ,
                    EhVendedorOffline = false,
                    QuantidadeItens = ordens.Count,
                }
            );
        }

        // 2. Vendedores offline ativos
        var offlineAtivos = await db.Set<VendedorOfflineEntidade>()
            .AsNoTracking()
            .Where(v => v.Ativo && v.ExpiraEm > agora)
            .ToListAsync(c);

        foreach (var v in offlineAtivos)
        {
            if (ativas.TentarObterPresencial(v.PersonagemId, out _))
                continue; // Presencial tem precedência se a pessoa estiver logada

            var ordens = await mercado.ListarOrdensDaBarracaAsync(v.PersonagemId, c);
            lista.Add(
                new BarracaResumo
                {
                    PersonagemId = v.PersonagemId,
                    NomeDono = v.NomeDono,
                    TituloBarraca = v.Titulo,
                    PosicaoX = v.PosicaoX,
                    PosicaoZ = v.PosicaoZ,
                    EhVendedorOffline = true,
                    QuantidadeItens = ordens.Count,
                }
            );
        }

        return lista;
    }

    public async Task<DadosDaBarraca?> ConsultarBarracaAsync(long personagemId, CancellationToken c)
    {
        var agora = relogio.AgoraSegundos;
        string nomeDono;
        string titulo;
        bool ehOffline;

        if (ativas.TentarObterPresencial(personagemId, out var p) && p != null)
        {
            nomeDono = p.NomeDono;
            titulo = p.Titulo;
            ehOffline = false;
        }
        else
        {
            var offline = await db.Set<VendedorOfflineEntidade>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    v => v.PersonagemId == personagemId && v.Ativo && v.ExpiraEm > agora,
                    c
                );

            if (offline == null)
                return null;

            nomeDono = offline.NomeDono;
            titulo = offline.Titulo;
            ehOffline = true;
        }

        var itens = await mercado.ListarOrdensDaBarracaAsync(personagemId, c);
        return new DadosDaBarraca
        {
            PersonagemId = personagemId,
            NomeDono = nomeDono,
            Titulo = titulo,
            EhVendedorOffline = ehOffline,
            TaxaPercentual = ConfigBarraca.TaxaVendaBarracaPercentual,
            Itens = itens.ToArray(),
        };
    }

    public async Task<ResultadoDoMercado> ComprarDaBarracaAsync(
        long compradorId,
        long ordemId,
        long vendedorId,
        CancellationToken c
    )
    {
        var agora = relogio.AgoraSegundos;
        var ativa =
            ativas.TentarObterPresencial(vendedorId, out _)
            || await db.Set<VendedorOfflineEntidade>()
                .AnyAsync(v => v.PersonagemId == vendedorId && v.Ativo && v.ExpiraEm > agora, c);

        if (!ativa)
            return ResultadoDoMercado.Falha(CodigosErroEconomia.BarracaNaoEncontrada);

        return await mercado.ComprarOrdemNaBarracaAsync(compradorId, ordemId, vendedorId, c);
    }
}
