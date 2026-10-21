using System.Data;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Economia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed class ServicoDeCorreio(
    JogoDbContext db,
    ICarteiraCoroas carteira,
    ICustodiaDeItens custodia,
    IDiretorioDePersonagens diretorio,
    RelogioDoMercado relogio
)
{
    private static readonly SemaphoreSlim Trava = new(1, 1);

    public async Task<EstadoDoCorreio> ConsultarAsync(long destinatarioId, CancellationToken c)
    {
        var cartas = await db.Set<CartaCorreio>()
            .AsNoTracking()
            .Include(carta => carta.Itens)
            .Where(carta => carta.DestinatarioId == destinatarioId)
            .OrderByDescending(carta => carta.EnviadaEm)
            .Take(100)
            .Select(carta => new ResumoCarta
            {
                Id = carta.Id,
                Remetente = carta.RemetenteNome,
                Assunto = carta.Assunto,
                Lida = carta.Lida,
                TemAnexos = !carta.AnexosColetados && (carta.Coroas > 0 || carta.Itens.Count > 0),
                Coroas = carta.AnexosColetados ? 0 : carta.Coroas,
                TotalItens = carta.AnexosColetados ? 0 : carta.Itens.Count,
                EnviadaEm = carta.EnviadaEm,
                ExpiraEm = carta.ExpiraEm,
                DoSistema = carta.DoSistema,
            })
            .ToArrayAsync(c);

        var naoLidas = cartas.Count(carta => !carta.Lida);

        return new EstadoDoCorreio { Cartas = cartas, NaoLidas = naoLidas };
    }

    public async Task<(string? Erro, DetalhesDaCarta? Detalhes)> LerCartaAsync(
        long destinatarioId,
        long cartaId,
        CancellationToken c
    )
    {
        var carta = await db.Set<CartaCorreio>()
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == cartaId && c.DestinatarioId == destinatarioId, c);

        if (carta == null)
            return (CodigosErroEconomia.CartaNaoEncontrada, null);

        if (!carta.Lida)
        {
            carta.Lida = true;
            await db.SaveChangesAsync(c);
        }

        var anexosDescritos = new List<AnexoItemCorreio>(carta.Itens.Count);
        foreach (var anexo in carta.Itens)
        {
            var entrada = custodia.Descrever(
                new ItemEmCustodia(anexo.ItemId, anexo.EquipamentoJson)
            );
            anexosDescritos.Add(
                new AnexoItemCorreio
                {
                    AnexoId = anexo.Id,
                    ItemId = anexo.ItemId,
                    Item = entrada,
                }
            );
        }

        var detalhes = new DetalhesDaCarta
        {
            Id = carta.Id,
            Remetente = carta.RemetenteNome,
            Assunto = carta.Assunto,
            Mensagem = carta.Mensagem,
            Lida = carta.Lida,
            Coroas = carta.AnexosColetados ? 0 : carta.Coroas,
            Itens = anexosDescritos.ToArray(),
            EnviadaEm = carta.EnviadaEm,
            ExpiraEm = carta.ExpiraEm,
            AnexosColetados = carta.AnexosColetados,
            DoSistema = carta.DoSistema,
        };

        return (null, detalhes);
    }

    public async Task<(string? Erro, long CartaId, long DestinatarioId)> EnviarCartaAsync(
        long remetenteId,
        string destinatarioNome,
        string assunto,
        string mensagem,
        long coroas,
        long[] itensInstancias,
        bool exigirProximidade,
        float x,
        float z,
        CancellationToken c
    )
    {
        if (string.IsNullOrWhiteSpace(assunto))
            return (CodigosErroEconomia.AssuntoVazio, 0, 0);

        assunto = assunto.Trim();
        if (assunto.Length > ConfigCorreio.MaximoTamanhoAssunto)
            assunto = assunto.Substring(0, ConfigCorreio.MaximoTamanhoAssunto);

        mensagem ??= "";
        if (mensagem.Length > ConfigCorreio.MaximoTamanhoCorpo)
            mensagem = mensagem.Substring(0, ConfigCorreio.MaximoTamanhoCorpo);

        if (coroas < 0)
            return (CodigosErroEconomia.QuantidadeInvalida, 0, 0);

        itensInstancias ??= [];
        if (itensInstancias.Length > ConfigCorreio.MaximoItensAnexados)
            return (CodigosErroEconomia.LimiteDeAnexos, 0, 0);

        if (exigirProximidade && !ConfigCorreio.DentroDoRaioCorreio(x, z))
            return (CodigosErroEconomia.LongeDoCorreio, 0, 0);

        destinatarioNome = (destinatarioNome ?? "").Trim();
        if (!diretorio.TentarObterPorNome(destinatarioNome, out var destId))
            return (CodigosErroEconomia.DestinatarioNaoEncontrado, 0, 0);

        if (destId == remetenteId)
            return (CodigosErroEconomia.DestinatarioInvalido, 0, 0);

        if (!diretorio.TentarObterNome(remetenteId, out var remetenteNome))
            remetenteNome = "Desconhecido";

        var custoTotal = ConfigCorreio.TaxaEnvioCoroas + coroas;

        await Trava.WaitAsync(c);
        try
        {
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                c
            );

            var debitou = await carteira.DebitarAsync(remetenteId, custoTotal, c);
            if (!debitou)
                return (CodigosErroEconomia.SaldoInsuficiente, 0, 0);

            var agora = relogio.AgoraSegundos;
            var carta = new CartaCorreio
            {
                DestinatarioId = destId,
                RemetenteId = remetenteId,
                RemetenteNome = remetenteNome,
                Assunto = assunto,
                Mensagem = mensagem,
                Coroas = coroas,
                Lida = false,
                AnexosColetados = false,
                Devolvida = false,
                DoSistema = false,
                EnviadaEm = agora,
                ExpiraEm = agora + ConfigCorreio.ExpiracaoSegundos,
            };

            foreach (var instId in itensInstancias)
            {
                var itemCustodia = await custodia.RetirarAsync(remetenteId, instId, c);
                if (itemCustodia == null)
                {
                    await transacao.RollbackAsync(c);
                    return (CodigosErroEconomia.ItemNaoEncontrado, 0, 0);
                }

                carta.Itens.Add(
                    new AnexoItemCorreioEntidade
                    {
                        ItemId = itemCustodia.ItemId,
                        EquipamentoJson = itemCustodia.EquipamentoJson,
                    }
                );
            }

            db.Add(carta);
            await db.SaveChangesAsync(c);
            await transacao.CommitAsync(c);

            return (null, carta.Id, destId);
        }
        finally
        {
            Trava.Release();
        }
    }

    public async Task<long> EnviarCartaDoSistemaAsync(
        long destinatarioId,
        string remetenteNome,
        string assunto,
        string mensagem,
        long coroas,
        IEnumerable<ItemEmCustodia>? itens,
        CancellationToken c
    )
    {
        await Trava.WaitAsync(c);
        try
        {
            var agora = relogio.AgoraSegundos;
            var carta = new CartaCorreio
            {
                DestinatarioId = destinatarioId,
                RemetenteId = null,
                RemetenteNome = string.IsNullOrWhiteSpace(remetenteNome)
                    ? "Sistema"
                    : remetenteNome,
                Assunto = string.IsNullOrWhiteSpace(assunto) ? "Mensagem do Sistema" : assunto,
                Mensagem = mensagem ?? "",
                Coroas = Math.Max(0, coroas),
                Lida = false,
                AnexosColetados = false,
                Devolvida = false,
                DoSistema = true,
                EnviadaEm = agora,
                ExpiraEm = agora + ConfigCorreio.ExpiracaoSegundos,
            };

            if (itens != null)
            {
                foreach (var item in itens)
                {
                    carta.Itens.Add(
                        new AnexoItemCorreioEntidade
                        {
                            ItemId = item.ItemId,
                            EquipamentoJson = item.EquipamentoJson,
                        }
                    );
                }
            }

            db.Add(carta);
            await db.SaveChangesAsync(c);
            return carta.Id;
        }
        finally
        {
            Trava.Release();
        }
    }

    public async Task<(string? Erro, long Coroas, int ItensColetados)> ColetarAnexosAsync(
        long destinatarioId,
        long cartaId,
        bool exigirProximidade,
        float x,
        float z,
        CancellationToken c
    )
    {
        if (exigirProximidade && !ConfigCorreio.DentroDoRaioCorreio(x, z))
            return (CodigosErroEconomia.LongeDoCorreio, 0, 0);

        await Trava.WaitAsync(c);
        try
        {
            await using var transacao = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                c
            );

            var carta = await db.Set<CartaCorreio>()
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Id == cartaId && c.DestinatarioId == destinatarioId, c);

            if (carta == null)
                return (CodigosErroEconomia.CartaNaoEncontrada, 0, 0);

            if (carta.AnexosColetados || (carta.Coroas <= 0 && carta.Itens.Count == 0))
                return (CodigosErroEconomia.SemAnexos, 0, 0);

            if (carta.Coroas > 0)
            {
                await carteira.CreditarAsync(destinatarioId, carta.Coroas, c);
            }

            foreach (var anexo in carta.Itens)
            {
                var entregue = await custodia.DevolverAsync(
                    destinatarioId,
                    new ItemEmCustodia(anexo.ItemId, anexo.EquipamentoJson),
                    c
                );

                if (!entregue)
                {
                    await transacao.RollbackAsync(c);
                    return (CodigosErroEconomia.InventarioCheio, 0, 0);
                }
            }

            carta.AnexosColetados = true;
            carta.Lida = true;
            await db.SaveChangesAsync(c);
            await transacao.CommitAsync(c);

            return (null, carta.Coroas, carta.Itens.Count);
        }
        finally
        {
            Trava.Release();
        }
    }

    public async Task<string?> ExcluirCartaAsync(
        long destinatarioId,
        long cartaId,
        CancellationToken c
    )
    {
        await Trava.WaitAsync(c);
        try
        {
            var carta = await db.Set<CartaCorreio>()
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Id == cartaId && c.DestinatarioId == destinatarioId, c);

            if (carta == null)
                return CodigosErroEconomia.CartaNaoEncontrada;

            var temAnexos = !carta.AnexosColetados && (carta.Coroas > 0 || carta.Itens.Count > 0);
            if (temAnexos)
                return CodigosErroEconomia.CartaComAnexos;

            db.Remove(carta);
            await db.SaveChangesAsync(c);
            return null;
        }
        finally
        {
            Trava.Release();
        }
    }

    public async Task<int> ProcessarExpiracoesAsync(CancellationToken c)
    {
        await Trava.WaitAsync(c);
        try
        {
            var agora = relogio.AgoraSegundos;
            var expiradas = await db.Set<CartaCorreio>()
                .Include(c => c.Itens)
                .Where(c => !c.Devolvida && !c.AnexosColetados && c.ExpiraEm <= agora)
                .ToListAsync(c);

            var processadas = 0;
            foreach (var carta in expiradas)
            {
                if (carta.RemetenteId.HasValue && (carta.Coroas > 0 || carta.Itens.Count > 0))
                {
                    // Devolve ao remetente original
                    var devolucao = new CartaCorreio
                    {
                        DestinatarioId = carta.RemetenteId.Value,
                        RemetenteId = null,
                        RemetenteNome = "Correio da Cidade",
                        Assunto = $"[Devolução] {carta.Assunto}",
                        Mensagem = "Carta não recolhida a tempo e devolvida ao remetente original.",
                        Coroas = carta.Coroas,
                        Lida = false,
                        AnexosColetados = false,
                        Devolvida = false,
                        DoSistema = true,
                        EnviadaEm = agora,
                        ExpiraEm = agora + ConfigCorreio.ExpiracaoSegundos,
                    };

                    foreach (var anexo in carta.Itens)
                    {
                        devolucao.Itens.Add(
                            new AnexoItemCorreioEntidade
                            {
                                ItemId = anexo.ItemId,
                                EquipamentoJson = anexo.EquipamentoJson,
                            }
                        );
                    }

                    db.Add(devolucao);
                }

                carta.Devolvida = true;
                processadas++;
            }

            if (processadas > 0)
                await db.SaveChangesAsync(c);

            return processadas;
        }
        finally
        {
            Trava.Release();
        }
    }
}
