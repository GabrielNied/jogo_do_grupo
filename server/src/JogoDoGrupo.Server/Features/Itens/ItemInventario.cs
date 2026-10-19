using System.Text.Json;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Um espaço ocupado do inventário de um personagem (uma linha por item, sem pilha). A ordem
/// do inventário é a ordem do <see cref="Id"/>.
/// </summary>
public sealed class ItemInventario
{
    public long Id { get; set; }

    /// <summary>Personagem dono do item (<c>Personagens.Id</c>).</summary>
    public long PersonagemId { get; set; }

    /// <summary>Id do item em <see cref="CatalogoItens"/>.</summary>
    public int ItemId { get; set; }

    public PosicaoEquipamento Posicao { get; set; }

    /// <summary>Contêiner onde o item está guardado (só a bolsa funciona por enquanto).</summary>
    public ConteinerItem Conteiner { get; set; } = ConteinerItem.Bolsa;

    /// <summary>Aba do contêiner; nula com o item equipado.</summary>
    public int? Aba { get; set; }

    /// <summary>Casa da aba; nula com o item equipado.</summary>
    public int? Casa { get; set; }

    public string? EquipamentoJson { get; set; }

    /// <summary>
    /// O exemplar gravado. Um exemplar na escala antiga sai já convertido para a escala de 0 a 100
    /// (<see cref="MigracaoDeTier"/>; a conta é determinística, então ler de novo dá o mesmo
    /// resultado). A gravação definitiva é do <see cref="MigradorDeEquipamentos"/>, na subida do
    /// servidor, ou da próxima vez que o exemplar for gravado.
    /// </summary>
    public Equipamento? LerEquipamento()
    {
        if (EquipamentoJson == null)
            return null;
        var item = JsonSerializer.Deserialize<Equipamento>(EquipamentoJson, Json);
        if (item != null)
        {
            MigracaoDeTier.Migrar(item);
        }
        return item;
    }

    public void GravarEquipamento(Equipamento item) =>
        EquipamentoJson = JsonSerializer.Serialize(item, Json);

    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

    public EntradaInventario ParaEntrada() =>
        new()
        {
            InstanciaId = Id,
            Posicao = Posicao,
            Conteiner = Conteiner,
            Aba = Aba ?? RegrasItens.SemCasa,
            Casa = Casa ?? RegrasItens.SemCasa,
            Equipamento = LerEquipamento(),
            ItemId = ItemId,
            Nome =
                CatalogoItens.ObterNome(ItemId) ?? CatalogoEquipamentos.Obter(ItemId)?.Nome ?? "",
        };
}
