using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

public sealed class ConfigInfusao
{
    public long[] CustosPorRaridade { get; set; } = new long[] { 10, 20, 40, 80, 160 };
    public const long CreditoDeTeste = 1000;

    public long Custo(RaridadeItem raridade)
    {
        if (CustosPorRaridade.Length != 5 || CustosPorRaridade.Any(v => v <= 0))
            throw new InvalidOperationException("Custos de infusão inválidos.");
        return CustosPorRaridade[(int)raridade];
    }
}

public sealed class AssinadorInfusao
{
    private readonly byte[] _chave = RandomNumberGenerator.GetBytes(32);

    public string Assinar(string dados) =>
        Convert.ToHexString(HMACSHA256.HashData(_chave, Encoding.UTF8.GetBytes(dados)));

    public bool Confere(string recebido, string esperado) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(recebido ?? ""),
            Encoding.UTF8.GetBytes(esperado)
        );
}

internal static class InfusaoRotas
{
    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        var caminho = RegrasEquipamentos.Rota + "/infusao";
        rotas
            .MapPost(
                caminho + "/estado",
                (
                    string personagemId,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    ICarteiraCoroas carteira,
                    CancellationToken c
                ) =>
                    Executar(
                        personagemId,
                        usuario,
                        db,
                        carteira,
                        null,
                        null,
                        null,
                        null,
                        false,
                        false,
                        c
                    )
            )
            .RequireAuthorization();
        rotas
            .MapPost(
                caminho + "/previa",
                (
                    string personagemId,
                    PedidoInfusao pedido,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    ICarteiraCoroas carteira,
                    ConfigInfusao config,
                    AssinadorInfusao assinador,
                    CancellationToken c
                ) =>
                    Executar(
                        personagemId,
                        usuario,
                        db,
                        carteira,
                        pedido,
                        null,
                        config,
                        assinador,
                        false,
                        false,
                        c
                    )
            )
            .RequireAuthorization();
        rotas
            .MapPost(
                caminho + "/confirmar",
                (
                    string personagemId,
                    PedidoInfusao pedido,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    ICarteiraCoroas carteira,
                    ConfigInfusao config,
                    AssinadorInfusao assinador,
                    CancellationToken c
                ) =>
                    Executar(
                        personagemId,
                        usuario,
                        db,
                        carteira,
                        pedido,
                        null,
                        config,
                        assinador,
                        true,
                        false,
                        c
                    )
            )
            .RequireAuthorization();
        rotas
            .MapPost(
                caminho + "/favorito",
                (
                    string personagemId,
                    PedidoFavorito pedido,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    ICarteiraCoroas carteira,
                    CancellationToken c
                ) =>
                    Executar(
                        personagemId,
                        usuario,
                        db,
                        carteira,
                        null,
                        pedido,
                        null,
                        null,
                        false,
                        false,
                        c
                    )
            )
            .RequireAuthorization();
        if (rotas.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
            rotas
                .MapPost(
                    caminho + "/coroas-teste",
                    (
                        string personagemId,
                        ClaimsPrincipal usuario,
                        JogoDbContext db,
                        ICarteiraCoroas carteira,
                        CancellationToken c
                    ) =>
                        Executar(
                            personagemId,
                            usuario,
                            db,
                            carteira,
                            null,
                            null,
                            null,
                            null,
                            false,
                            true,
                            c
                        )
                )
                .RequireAuthorization();
    }

    private static IResult Erro(string codigo, int status = 409) =>
        RespostasApi.Erro(status, codigo);

    private static async Task<IResult> Executar(
        string personagemId,
        ClaimsPrincipal usuario,
        JogoDbContext db,
        ICarteiraCoroas carteira,
        PedidoInfusao? pedido,
        PedidoFavorito? favorito,
        ConfigInfusao? config,
        AssinadorInfusao? assinador,
        bool confirmar,
        bool credito,
        CancellationToken c
    )
    {
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            c
        );
        if (!await ItensRotas.PersonagemDaContaAsync(personagemId, usuario.ObterContaId(), db, c))
            return Erro(CodigosErroItens.PersonagemNaoEncontrado, 404);
        var id = long.Parse(personagemId, CultureInfo.InvariantCulture);
        var itens = await db.Set<ItemInventario>()
            .Where(i => i.PersonagemId == id)
            .OrderBy(i => i.Id)
            .ToListAsync(c);
        PreviaInfusao? previa = null;
        var aviso = "";
        if (favorito != null)
        {
            var entrada = itens.SingleOrDefault(i => i.Id == favorito.InstanciaId);
            var equipamento = entrada?.LerEquipamento();
            if (entrada == null || equipamento == null)
                return Erro("ItemNaoEncontrado", 404);
            equipamento.Favorito = favorito.Favorito;
            entrada.GravarEquipamento(equipamento);
            aviso = favorito.Favorito
                ? "Item protegido como favorito."
                : "Proteção de favorito removida.";
        }
        if (credito)
            await carteira.CreditarAsync(id, ConfigInfusao.CreditoDeTeste, c);
        if (pedido != null)
        {
            if (pedido.ReceptorId == pedido.DoadorId)
                return Erro("MesmoItem", 400);
            var receptor = itens.SingleOrDefault(i => i.Id == pedido.ReceptorId);
            var doador = itens.SingleOrDefault(i => i.Id == pedido.DoadorId);
            if (receptor == null || doador == null)
                return Erro("ItemNaoEncontrado", 404);
            if (doador.Posicao != PosicaoEquipamento.Inventario)
                return Erro("DoadorEquipado");
            var recebe = receptor.LerEquipamento();
            var doa = doador.LerEquipamento();
            var invalido = RegrasInfusao.Validar(recebe!, doa!);
            if (invalido != null)
                return Erro(invalido);
            var atributo = recebe!.Atributos[recebe.IndiceInfusivel];
            var valorBase = recebe.ValorBaseInfusivel;
            if (valorBase == 0 && recebe.InfusoesUsadas == 0)
                valorBase = atributo.Valor;
            if (
                !double.IsFinite(valorBase)
                || valorBase <= 0
                || !double.IsFinite(atributo.Valor)
                || atributo.Valor < 0
            )
                return Erro("ItemInvalido");
            double depois;
            try
            {
                depois = RegrasInfusao.Depois(atributo.Valor, valorBase);
            }
            catch (OverflowException)
            {
                return Erro("ItemInvalido");
            }
            // A fusão (#616) vai limitar pela faixa; enquanto a infusão existe, também não passa do máximo.
            if (ConfigTier.Maximo(recebe.Tipo, atributo.Status) is { } maximo)
                depois = Math.Min(depois, maximo);
            var custo = config!.Custo(recebe.Raridade);
            var assinatura = assinador!.Assinar(
                string.Join(
                    "\n",
                    id.ToString(CultureInfo.InvariantCulture),
                    receptor.Id,
                    receptor.EquipamentoJson,
                    (int)receptor.Posicao,
                    doador.Id,
                    doador.EquipamentoJson,
                    (int)doador.Posicao,
                    custo
                )
            );
            previa = new PreviaInfusao
            {
                ReceptorId = receptor.Id,
                DoadorId = doador.Id,
                Receptor = recebe.Nome,
                Doador = doa!.Nome,
                Status = atributo.Status,
                Antes = atributo.Valor,
                Depois = depois,
                SlotsDepois = recebe.InfusoesUsadas + 1,
                Limite = RegrasEquipamentos.LimiteInfusoes(recebe.Raridade),
                Custo = custo,
                Confirmacao = assinatura,
            };
            if (confirmar)
            {
                if (!assinador.Confere(pedido.Confirmacao, assinatura))
                    return Erro("PreviaDesatualizada");
                if (!await carteira.DebitarAsync(id, custo, c))
                    return Erro("CoroasInsuficientes");
                recebe.ValorBaseInfusivel = valorBase;
                recebe.InfusoesUsadas++;
                atributo.Valor = depois;
                receptor.GravarEquipamento(recebe);
                db.Remove(doador);
                itens.Remove(doador);
                previa = null;
                aviso = "Infusão concluída. O doador foi consumido e o atributo aumentou.";
            }
        }
        await db.SaveChangesAsync(c);
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { id }, c);
        var resposta = EquipamentosRotas.Resposta(itens, estado?.ConjuntoAtivo ?? 1);
        resposta.ModoInfusao = true;
        resposta.Coroas = await carteira.SaldoAsync(id, c);
        resposta.Previa = previa!;
        resposta.Aviso = aviso;
        await transacao.CommitAsync(c);
        return Results.Ok(resposta);
    }
}
