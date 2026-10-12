using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>Valores iniciais de teste; ajuste de balanceamento sem mudar o sorteio.</summary>
public sealed class ConfigEquipamentos
{
    public int QuantidadeAleatorios { get; set; } = 2;
    public Dictionary<StatusItem, double> ValoresAleatorios { get; set; } =
        new()
        {
            [StatusItem.Forca] = 6,
            [StatusItem.Destreza] = 6,
            [StatusItem.Intelecto] = 6,
            [StatusItem.Vitalidade] = 6,
            [StatusItem.Espirito] = 3,
        };
}

public sealed class GeradorEquipamentos(ConfigEquipamentos config)
{
    public Equipamento? Gerar(
        int modeloId,
        Random aleatorio,
        RaridadeItem raridade = RaridadeItem.Comum,
        GrauEquipamento grau = GrauEquipamento.Inicial
    )
    {
        if (!Enum.IsDefined(raridade) || !Enum.IsDefined(grau))
            throw new ArgumentOutOfRangeException(nameof(raridade));
        var item = CatalogoEquipamentos.Obter(modeloId);
        if (item == null)
            return null;
        var atributos = item.Atributos.ToList();
        var candidatos = config
            .ValoresAleatorios.Where(p => atributos.All(a => a.Status != p.Key))
            .ToList();
        if (
            config.QuantidadeAleatorios < 0
            || config.QuantidadeAleatorios > candidatos.Count
            || candidatos.Any(c =>
                c.Value < 0 || !double.IsFinite(c.Value) || !Enum.IsDefined(c.Key)
            )
        )
            throw new InvalidOperationException("Pool de atributos aleatórios inválido.");
        for (var i = 0; i < config.QuantidadeAleatorios; i++)
        {
            var indice = aleatorio.Next(candidatos.Count);
            var escolhido = candidatos[indice];
            candidatos.RemoveAt(indice);
            atributos.Add(new AtributoItem { Status = escolhido.Key, Valor = escolhido.Value });
        }
        var fator = RegrasEquipamentos.Fator(grau);
        foreach (var atributo in atributos)
            atributo.Valor *= fator;
        item.Atributos = atributos.ToArray();
        item.IndiceInfusivel = aleatorio.Next(atributos.Count);
        item.Raridade = raridade;
        item.Grau = grau;
        item.NivelPersonagemMinimo =
            grau == GrauEquipamento.Forte ? 20
            : grau == GrauEquipamento.Medio ? 10
            : 1;
        return item;
    }
}
