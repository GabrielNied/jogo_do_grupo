using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>Valores iniciais de teste; ajuste de balanceamento sem mudar o sorteio.</summary>
public sealed class ConfigEquipamentos
{
    /// <summary>Quantos atributos extras, além dos do modelo-base, cada exemplar sorteia.</summary>
    public int QuantidadeAleatorios { get; set; } = 2;

    /// <summary>
    /// De onde saem os atributos extras. Só entram os que o modelo ainda não tem e que são
    /// elegíveis na categoria (<see cref="ConfigAtributosItem"/>); os valores saem da faixa, não
    /// daqui. O dano da arma não entra: quem o tem é o modelo da arma.
    /// </summary>
    public StatusItem[] StatusAleatorios { get; set; } =
    [
        StatusItem.DanoFisico,
        StatusItem.DanoMagico,
        StatusItem.VidaMaxima,
        StatusItem.DefesaFisica,
        StatusItem.DefesaMagica,
        StatusItem.RegeneracaoVida,
        StatusItem.RegeneracaoStamina,
        StatusItem.RegeneracaoMana,
        StatusItem.ChanceCritica,
        StatusItem.DanoCritico,
        StatusItem.VelocidadeAtaqueFisico,
        StatusItem.VelocidadeAtaqueMagico,
        StatusItem.PenetracaoFisica,
        StatusItem.PenetracaoMagica,
    ];
}

/// <summary>
/// Gera o exemplar (issue #615): os atributos do modelo-base mais alguns extras sorteados, cada
/// um com um inteiro dentro da faixa da categoria (<see cref="ConfigTier.Faixas"/>). O grau
/// escolhe a janela da faixa e a origem do inimigo dá mais rolagens (valor melhor); o tier é
/// consequência dos valores e fica gravado como <see cref="Equipamento.TierOrigem"/>.
/// </summary>
public sealed class GeradorEquipamentos(ConfigEquipamentos config)
{
    public Equipamento? Gerar(
        int modeloId,
        Random aleatorio,
        OrigemDoLoot origem = OrigemDoLoot.Comum,
        GrauEquipamento grau = GrauEquipamento.Inicial
    )
    {
        if (!Enum.IsDefined(origem) || !Enum.IsDefined(grau))
            throw new ArgumentOutOfRangeException(nameof(origem));
        var item = CatalogoEquipamentos.Obter(modeloId);
        if (item == null)
            return null;
        var atributos = item.Atributos.ToList();
        var candidatos = config
            .StatusAleatorios.Where(s =>
                atributos.All(a => a.Status != s) && ConfigTier.Faixa(item.Tipo, s) != null
            )
            .Distinct()
            .ToList();
        if (config.QuantidadeAleatorios < 0 || candidatos.Any(c => !Enum.IsDefined(c)))
            throw new InvalidOperationException("Pool de atributos aleatórios inválido.");
        // Categoria com poucos elegíveis (ex.: arma) sorteia só os que existem.
        var quantidade = Math.Min(config.QuantidadeAleatorios, candidatos.Count);
        for (var i = 0; i < quantidade; i++)
        {
            var indice = aleatorio.Next(candidatos.Count);
            atributos.Add(new AtributoItem { Status = candidatos[indice] });
            candidatos.RemoveAt(indice);
        }
        var rolagens = TabelaDeLoot.RolagensDe(origem);
        foreach (var atributo in atributos)
        {
            // O catálogo diz quais atributos o modelo tem; o valor é sorteado na faixa. Atributo
            // sem faixa na categoria (legado sem efeito no tier) fica como o catálogo o definiu.
            if (ConfigTier.Faixa(item.Tipo, atributo.Status) is { } faixa)
                atributo.Valor = RegrasTier.SortearValor(aleatorio, faixa, grau, rolagens);
        }
        item.Atributos = atributos.ToArray();
        item.Grau = grau;
        item.NivelPersonagemMinimo = TabelaDeLoot.NivelMinimoDoGrau(grau);
        item.VersaoDosAtributos = RegrasTier.VersaoAtual;
        item.TierOrigem = RegrasTier.TierDe(item);
        // O sorteio do poder vem por último: não muda nenhum valor sorteado antes e não entra no
        // tier (DEC-004). Só o modelo que declara categorias de poder recebe um (PODER-007).
        item.PoderId = CatalogoDePoderes.Sortear(item, aleatorio.Next);
        return item;
    }
}
