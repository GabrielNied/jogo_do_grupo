using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>Valores iniciais de teste; ajuste de balanceamento sem mudar o sorteio.</summary>
public sealed class ConfigEquipamentos
{
    /// <summary>Quantos atributos extras, além dos do modelo-base, cada exemplar sorteia.</summary>
    public int QuantidadeAleatorios { get; set; } = 2;

    /// <summary>
    /// De onde saem os atributos extras. Só entram os que o modelo ainda não tem e que são
    /// elegíveis na categoria (<see cref="ConfigTier"/>); os valores saem da faixa, não daqui.
    /// </summary>
    public StatusItem[] StatusAleatorios { get; set; } =
    [
        StatusItem.Forca,
        StatusItem.Destreza,
        StatusItem.Intelecto,
        StatusItem.Vitalidade,
        StatusItem.Espirito,
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
        if (
            config.QuantidadeAleatorios < 0
            || config.QuantidadeAleatorios > candidatos.Count
            || candidatos.Any(c => !Enum.IsDefined(c))
        )
            throw new InvalidOperationException("Pool de atributos aleatórios inválido.");
        for (var i = 0; i < config.QuantidadeAleatorios; i++)
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
        return item;
    }
}
