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

    public Equipamento? LerEquipamento() =>
        EquipamentoJson == null
            ? null
            : JsonSerializer.Deserialize<Equipamento>(EquipamentoJson, Json);

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
