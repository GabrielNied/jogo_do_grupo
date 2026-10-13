using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Api;
using JogoDoGrupo.Server.Core.Autenticacao;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Itens;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Assina a prévia da fusão (HMAC com chave em memória, ADR 0032): a confirmação só vale se o
/// estado atual das peças, o atributo e o custo ainda forem os da prévia.
/// </summary>
public sealed class AssinadorFusao
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

/// <summary>
/// Fusão de equipamentos (issue #616, FUSAO-001 a FUSAO-015): prévia que não altera nada e
/// confirmação que cobra as coroas, consome o material e grava o novo valor e o nível na mesma
/// transação serializável (ADR 0032). Tudo ou nada.
/// </summary>
internal static class FusaoRotas
{
    /// <summary>Crédito da rota de teste (só Development).</summary>
    public const long CreditoDeTeste = 20000;

    public static void Mapear(IEndpointRouteBuilder rotas)
    {
        var caminho = RegrasEquipamentos.Rota + "/fusao";
        rotas
            .MapPost(
                caminho + "/estado",
                (
                    string personagemId,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    ICarteiraCoroas carteira,
                    CancellationToken c
                ) => Executar(personagemId, usuario, db, carteira, c)
            )
            .RequireAuthorization();
        rotas
            .MapPost(
                caminho + "/previa",
                (
                    string personagemId,
                    PedidoFusao pedido,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    ICarteiraCoroas carteira,
                    AssinadorFusao assinador,
                    CancellationToken c
                ) => Executar(personagemId, usuario, db, carteira, c, pedido, assinador, false)
            )
            .RequireAuthorization();
        rotas
            .MapPost(
                caminho + "/confirmar",
                (
                    string personagemId,
                    PedidoFusao pedido,
                    ClaimsPrincipal usuario,
                    JogoDbContext db,
                    ICarteiraCoroas carteira,
                    AssinadorFusao assinador,
                    EventosDoServidor eventos,
                    CancellationToken c
                ) =>
                    Executar(
                        personagemId,
                        usuario,
                        db,
                        carteira,
                        c,
                        pedido,
                        assinador,
                        true,
                        eventos: eventos
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
                ) => Executar(personagemId, usuario, db, carteira, c, favorito: pedido)
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
                    ) => Executar(personagemId, usuario, db, carteira, c, credito: true)
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
        CancellationToken c,
        PedidoFusao? pedido = null,
        AssinadorFusao? assinador = null,
        bool confirmar = false,
        PedidoFavorito? favorito = null,
        bool credito = false,
        EventosDoServidor? eventos = null
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
        PreviaFusao? previa = null;
        var aviso = "";
        var mudouOVestido = false;
        if (favorito != null)
        {
            var entrada = itens.SingleOrDefault(i => i.Id == favorito.InstanciaId);
            var equipamento = entrada?.LerEquipamento();
            if (entrada == null || equipamento == null)
                return Erro(CodigosErroItens.ItemNaoEncontrado, 404);
            equipamento.Favorito = favorito.Favorito;
            entrada.GravarEquipamento(equipamento);
            aviso = favorito.Favorito
                ? "Item protegido como favorito."
                : "Proteção de favorito removida.";
        }
        if (credito)
            await carteira.CreditarAsync(id, CreditoDeTeste, c);
        if (pedido != null)
        {
            var resultado = Avaliar(id, pedido, itens, assinador!);
            if (resultado.Erro != null)
                return resultado.Erro;
            var calculo = resultado.Calculo!;
            previa = calculo.Previa;
            if (confirmar)
            {
                if (!assinador!.Confere(pedido.Confirmacao, previa.Confirmacao))
                    return Erro(CodigosErroItens.PreviaDesatualizada);
                // Materiais de dungeon e raid são a #617: sem eles a fusão de +6 em diante não
                // acontece (FUSAO-009). Conferido antes de cobrar.
                if (!previa.MateriaisSuficientes)
                    return Erro(CodigosErroItens.MateriaisInsuficientes);
                if (!await carteira.DebitarAsync(id, previa.Custo, c))
                    return Erro(CodigosErroItens.CoroasInsuficientes);
                calculo.Atributo.Valor = previa.ValorDepois;
                calculo.Dados.NivelFusao = previa.NivelDepois;
                calculo.Principal.GravarEquipamento(calculo.Dados);
                // Fundir uma peça vestida muda o que o combate soma.
                mudouOVestido = calculo.Principal.Posicao != PosicaoEquipamento.Inventario;
                db.Remove(calculo.Material);
                itens.Remove(calculo.Material);
                previa = null;
                aviso = "Fusão concluída. O material foi consumido e o atributo mudou.";
            }
        }
        await db.SaveChangesAsync(c);
        var estado = await db.Set<EstadoEquipamentos>().FindAsync(new object[] { id }, c);
        var resposta = EquipamentosRotas.Resposta(
            itens,
            estado?.ConjuntoAtivo ?? 1,
            await ItensRotas.NivelAsync(id, db, c)
        );
        resposta.ModoFusao = true;
        resposta.Coroas = await carteira.SaldoAsync(id, c);
        resposta.Previa = previa!;
        resposta.Aviso = aviso;
        await transacao.CommitAsync(c);
        if (mudouOVestido)
            eventos?.Publicar(new EquipamentoMudou(id));
        return Results.Ok(resposta);
    }

    private sealed record Calculo(
        PreviaFusao Previa,
        ItemInventario Principal,
        ItemInventario Material,
        Equipamento Dados,
        AtributoItem Atributo
    );

    private sealed record Avaliacao(IResult? Erro, Calculo? Calculo);

    /// <summary>
    /// Confere o pedido contra o estado gravado e calcula a prévia. Não altera nada: o
    /// <see cref="Calculo.Dados"/> é uma cópia lida do JSON, que só vira estado se a confirmação
    /// o gravar.
    /// </summary>
    private static Avaliacao Avaliar(
        long personagemId,
        PedidoFusao pedido,
        List<ItemInventario> itens,
        AssinadorFusao assinador
    )
    {
        if (pedido.PrincipalId == pedido.MaterialId)
            return new(Erro(CodigosErroItens.MesmoItem, 400), null);
        var principal = itens.SingleOrDefault(i => i.Id == pedido.PrincipalId);
        var material = itens.SingleOrDefault(i => i.Id == pedido.MaterialId);
        if (principal == null || material == null)
            return new(Erro(CodigosErroItens.ItemNaoEncontrado, 404), null);
        var dados = principal.LerEquipamento();
        var dadosMaterial = material.LerEquipamento();
        if (dados == null || dadosMaterial == null)
            return new(Erro(CodigosErroItens.ItemNaoEncontrado, 404), null);
        if (material.Posicao != PosicaoEquipamento.Inventario)
            return new(Erro(CodigosErroItens.MaterialEquipado), null);
        var invalido = RegrasFusao.Validar(dados, dadosMaterial);
        if (invalido != null)
            return new(Erro(invalido), null);
        if (!RegrasFusao.Elegiveis(dados, dadosMaterial).Contains(pedido.Status))
            return new(Erro(CodigosErroItens.AtributoInelegivel), null);
        var atributo = RegrasFusao.Procurar(dados, pedido.Status)!;
        var valorMaterial = RegrasFusao.Procurar(dadosMaterial, pedido.Status)!.Valor;
        var faixa = ConfigTier.Faixa(dados.Tipo, pedido.Status)!.Value;
        if (!double.IsFinite(atributo.Valor) || !double.IsFinite(valorMaterial))
            return new(Erro(CodigosErroItens.ItemInvalido), null);

        var nivel = dados.NivelFusao;
        var tierAntes = RegrasTier.TierDe(dados);
        var valorAtual = (int)Math.Round(atributo.Valor, MidpointRounding.AwayFromZero);
        var depois = RegrasFusao.Resultado(atributo.Valor, valorMaterial, faixa);
        var original = atributo.Valor;
        atributo.Valor = depois;
        var tierDepois = RegrasTier.TierDe(dados);
        atributo.Valor = original;

        var custo = ConfigFusao.Custo(nivel);
        var dungeon = ConfigFusao.MateriaisDungeon(nivel);
        var raid = ConfigFusao.MateriaisRaid(nivel);
        var assinatura = assinador.Assinar(
            string.Join(
                "\n",
                personagemId.ToString(CultureInfo.InvariantCulture),
                principal.Id,
                principal.EquipamentoJson,
                (int)principal.Posicao,
                material.Id,
                material.EquipamentoJson,
                (int)material.Posicao,
                (int)pedido.Status,
                custo,
                dungeon,
                raid
            )
        );
        var previa = new PreviaFusao
        {
            PrincipalId = principal.Id,
            MaterialId = material.Id,
            Principal = dados.Nome,
            Material = dadosMaterial.Nome,
            Categoria = dados.Tipo,
            NivelAtual = nivel,
            NivelDepois = nivel + 1,
            Status = pedido.Status,
            ValorAtual = valorAtual,
            ValorMaterial = (int)Math.Round(valorMaterial, MidpointRounding.AwayFromZero),
            ValorDepois = depois,
            TierAntes = tierAntes,
            TierDepois = tierDepois,
            Custo = custo,
            MateriaisDungeon = dungeon,
            MateriaisRaid = raid,
            MateriaisSuficientes = dungeon == 0 && raid == 0,
            Confirmacao = assinatura,
        };
        return new(null, new Calculo(previa, principal, material, dados, atributo));
    }
}
