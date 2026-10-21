using System.Data;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Economia;

public sealed record ResultadoBancoGuilda(
    bool Sucesso,
    string? CodigoErro = null,
    EstadoDoBancoGuilda? Estado = null
);

/// <summary>
/// Lógica autoritativa do Banco de Guilda (#338).
/// Permite que todos os membros depositem itens e coroas, enquanto apenas Líder e Oficial
/// podem retirá-los, conforme as permissões de <see cref="RegrasGuilda"/>.
/// </summary>
public sealed class ServicoDeBancoGuilda(
    JogoDbContext db,
    IConsultaDeGuilda consultaGuilda,
    ICustodiaDeItens custodiaItens,
    ICarteiraCoroas carteiraCoroas,
    IDiretorioDePersonagens diretorio,
    Func<long, (float X, float Z)?>? obterPosicao = null,
    Action<long>? aoMovimentarCoroas = null
)
{
    private static readonly Lock _travaOperacoes = new();

    public async Task<ResultadoBancoGuilda> ConsultarAsync(long personagemId, CancellationToken ct)
    {
        var guildaId = consultaGuilda.ObterGuildaId(personagemId);
        if (guildaId <= 0)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SemGuilda);
        }

        if (EstaLongeDoBanco(personagemId))
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.LongeDoBancoGuilda);
        }

        var estado = await MontarEstadoAsync(personagemId, guildaId, ct);
        return new ResultadoBancoGuilda(true, Estado: estado);
    }

    public async Task<ResultadoBancoGuilda> DepositarItemAsync(
        long personagemId,
        long instanciaId,
        int abaDesejada,
        int casaDesejada,
        CancellationToken ct
    )
    {
        var guildaId = consultaGuilda.ObterGuildaId(personagemId);
        if (guildaId <= 0)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SemGuilda);
        }

        var cargo = consultaGuilda.ObterCargo(personagemId);
        if (cargo == null || !RegrasGuilda.PodeDepositarNoBanco(cargo.Value))
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SemPermissaoBancoGuilda);
        }

        if (EstaLongeDoBanco(personagemId))
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.LongeDoBancoGuilda);
        }

        lock (_travaOperacoes) { }

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var itensAtuais = await db.Set<ItemBancoGuildaEntidade>()
            .Where(i => i.GuildaId == guildaId)
            .ToListAsync(ct);

        if (itensAtuais.Count >= ConfigBancoGuilda.CapacidadeMaxima)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.BancoGuildaCheio);
        }

        var ocupados = new HashSet<(int Aba, int Casa)>(itensAtuais.Select(i => (i.Aba, i.Casa)));

        int abaFinal = -1;
        int casaFinal = -1;

        if (
            abaDesejada >= 1
            && abaDesejada <= ConfigBancoGuilda.AbasDisponiveis
            && casaDesejada >= 0
            && casaDesejada < ConfigBancoGuilda.CasasPorAba
            && !ocupados.Contains((abaDesejada, casaDesejada))
        )
        {
            abaFinal = abaDesejada;
            casaFinal = casaDesejada;
        }
        else
        {
            for (var a = 1; a <= ConfigBancoGuilda.AbasDisponiveis; a++)
            {
                for (var c = 0; c < ConfigBancoGuilda.CasasPorAba; c++)
                {
                    if (!ocupados.Contains((a, c)))
                    {
                        abaFinal = a;
                        casaFinal = c;
                        break;
                    }
                }
                if (abaFinal != -1)
                    break;
            }
        }

        if (abaFinal == -1 || casaFinal == -1)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.BancoGuildaCheio);
        }

        var itemRetirado = await custodiaItens.RetirarAsync(personagemId, instanciaId, ct);
        if (itemRetirado == null)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.ItemNaoNegociavel);
        }

        diretorio.TentarObterNome(personagemId, out var nomePersonagem);

        var entidade = new ItemBancoGuildaEntidade
        {
            GuildaId = guildaId,
            Aba = abaFinal,
            Casa = casaFinal,
            ItemId = itemRetirado.ItemId,
            EquipamentoJson = itemRetirado.EquipamentoJson,
            DepositadoPorNome = nomePersonagem ?? "",
            DataDepositoUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        db.Set<ItemBancoGuildaEntidade>().Add(entidade);
        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        var estado = await MontarEstadoAsync(personagemId, guildaId, ct);
        return new ResultadoBancoGuilda(true, Estado: estado);
    }

    public async Task<ResultadoBancoGuilda> RetirarItemAsync(
        long personagemId,
        long itemBancoId,
        CancellationToken ct
    )
    {
        var guildaId = consultaGuilda.ObterGuildaId(personagemId);
        if (guildaId <= 0)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SemGuilda);
        }

        var cargo = consultaGuilda.ObterCargo(personagemId);
        if (cargo == null || !RegrasGuilda.PodeRetirarDoBanco(cargo.Value))
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SemPermissaoBancoGuilda);
        }

        if (EstaLongeDoBanco(personagemId))
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.LongeDoBancoGuilda);
        }

        lock (_travaOperacoes) { }

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var item = await db.Set<ItemBancoGuildaEntidade>()
            .FirstOrDefaultAsync(i => i.Id == itemBancoId && i.GuildaId == guildaId, ct);

        if (item == null)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.ItemNaoEncontradoNoBanco);
        }

        var entregou = await custodiaItens.DevolverAsync(
            personagemId,
            new ItemEmCustodia(item.ItemId, item.EquipamentoJson),
            ct
        );

        if (!entregou)
        {
            await transacao.RollbackAsync(ct);
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.InventarioCheio);
        }

        db.Set<ItemBancoGuildaEntidade>().Remove(item);
        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        var estado = await MontarEstadoAsync(personagemId, guildaId, ct);
        return new ResultadoBancoGuilda(true, Estado: estado);
    }

    public async Task<ResultadoBancoGuilda> DepositarCoroasAsync(
        long personagemId,
        long quantidade,
        CancellationToken ct
    )
    {
        if (quantidade <= 0)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.QuantidadeInvalida);
        }

        var guildaId = consultaGuilda.ObterGuildaId(personagemId);
        if (guildaId <= 0)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SemGuilda);
        }

        if (EstaLongeDoBanco(personagemId))
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.LongeDoBancoGuilda);
        }

        lock (_travaOperacoes) { }

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var debitou = await carteiraCoroas.DebitarAsync(personagemId, quantidade, ct);
        if (!debitou)
        {
            await transacao.RollbackAsync(ct);
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SaldoInsuficiente);
        }

        var saldo = await db.Set<SaldoBancoGuildaEntidade>()
            .FirstOrDefaultAsync(s => s.GuildaId == guildaId, ct);

        if (saldo == null)
        {
            saldo = new SaldoBancoGuildaEntidade { GuildaId = guildaId, Coroas = quantidade };
            db.Set<SaldoBancoGuildaEntidade>().Add(saldo);
        }
        else
        {
            saldo.Coroas += quantidade;
        }

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        aoMovimentarCoroas?.Invoke(personagemId);

        var estado = await MontarEstadoAsync(personagemId, guildaId, ct);
        return new ResultadoBancoGuilda(true, Estado: estado);
    }

    public async Task<ResultadoBancoGuilda> RetirarCoroasAsync(
        long personagemId,
        long quantidade,
        CancellationToken ct
    )
    {
        if (quantidade <= 0)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.QuantidadeInvalida);
        }

        var guildaId = consultaGuilda.ObterGuildaId(personagemId);
        if (guildaId <= 0)
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SemGuilda);
        }

        var cargo = consultaGuilda.ObterCargo(personagemId);
        if (cargo == null || !RegrasGuilda.PodeRetirarDoBanco(cargo.Value))
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.SemPermissaoBancoGuilda);
        }

        if (EstaLongeDoBanco(personagemId))
        {
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.LongeDoBancoGuilda);
        }

        lock (_travaOperacoes) { }

        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct
        );

        var saldo = await db.Set<SaldoBancoGuildaEntidade>()
            .FirstOrDefaultAsync(s => s.GuildaId == guildaId, ct);

        if (saldo == null || saldo.Coroas < quantidade)
        {
            await transacao.RollbackAsync(ct);
            return new ResultadoBancoGuilda(false, CodigosErroEconomia.CoroasInsuficientesNoBanco);
        }

        saldo.Coroas -= quantidade;
        await carteiraCoroas.CreditarAsync(personagemId, quantidade, ct);

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        aoMovimentarCoroas?.Invoke(personagemId);

        var estado = await MontarEstadoAsync(personagemId, guildaId, ct);
        return new ResultadoBancoGuilda(true, Estado: estado);
    }

    private async Task<EstadoDoBancoGuilda> MontarEstadoAsync(
        long personagemId,
        long guildaId,
        CancellationToken ct
    )
    {
        var cargo = consultaGuilda.ObterCargo(personagemId);
        var podeRetirar = cargo.HasValue && RegrasGuilda.PodeRetirarDoBanco(cargo.Value);
        var nomeGuilda = consultaGuilda.ObterNomeGuilda(guildaId);

        var saldo = await db.Set<SaldoBancoGuildaEntidade>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.GuildaId == guildaId, ct);

        var itensEntidades = await db.Set<ItemBancoGuildaEntidade>()
            .AsNoTracking()
            .Where(i => i.GuildaId == guildaId)
            .OrderBy(i => i.Aba)
            .ThenBy(i => i.Casa)
            .ToListAsync(ct);

        var itensDto = itensEntidades
            .Select(i =>
            {
                var detalhes = custodiaItens.Descrever(
                    new ItemEmCustodia(i.ItemId, i.EquipamentoJson),
                    i.Id
                );
                return new ItemBancoGuildaDto
                {
                    Id = i.Id,
                    Aba = i.Aba,
                    Casa = i.Casa,
                    ItemId = i.ItemId,
                    EquipamentoJson = i.EquipamentoJson ?? "",
                    DepositadoPorNome = i.DepositadoPorNome,
                    DataDepositoUnix = i.DataDepositoUnix,
                    Detalhes = detalhes,
                };
            })
            .ToArray();

        return new EstadoDoBancoGuilda
        {
            GuildaId = guildaId,
            NomeGuilda = nomeGuilda,
            SaldoCoroas = saldo?.Coroas ?? 0,
            PodeRetirar = podeRetirar,
            Itens = itensDto,
        };
    }

    private bool EstaLongeDoBanco(long personagemId)
    {
        if (obterPosicao == null)
            return false;

        var pos = obterPosicao(personagemId);
        if (!pos.HasValue)
            return false;

        return !ConfigBancoGuilda.PertoDoBancoGuilda(pos.Value.X, pos.Value.Z);
    }
}
