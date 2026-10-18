using System.Text.Json;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Um item guardado no banco pessoal da conta (issue #177).
/// Compartilhado entre todos os personagens da mesma conta.
/// </summary>
public sealed class ItemBanco
{
    public long Id { get; set; }

    /// <summary>Conta dona do banco.</summary>
    public long ContaId { get; set; }

    /// <summary>Id do item em <see cref="CatalogoItens"/>.</summary>
    public int ItemId { get; set; }

    /// <summary>Aba do banco (0 a 3); nula durante remanejamento temporário.</summary>
    public int? Aba { get; set; }

    /// <summary>Casa da aba no banco (0 a 39); nula durante remanejamento temporário.</summary>
    public int? Casa { get; set; }

    public string? EquipamentoJson { get; set; }

    public Equipamento? LerEquipamento()
    {
        if (EquipamentoJson == null)
        {
            return null;
        }

        var item = JsonSerializer.Deserialize<Equipamento>(EquipamentoJson, Json);
        MigracaoDeTier.Migrar(item);
        return item;
    }

    public void GravarEquipamento(Equipamento item) =>
        EquipamentoJson = JsonSerializer.Serialize(item, Json);

    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

    public EntradaInventario ParaEntrada() =>
        new()
        {
            InstanciaId = Id,
            Posicao = PosicaoEquipamento.Inventario,
            Conteiner = ConteinerItem.Banco,
            Aba = Aba ?? RegrasItens.SemCasa,
            Casa = Casa ?? RegrasItens.SemCasa,
            Equipamento = LerEquipamento(),
            ItemId = ItemId,
            Nome =
                CatalogoItens.ObterNome(ItemId) ?? CatalogoEquipamentos.Obter(ItemId)?.Nome ?? "",
        };
}
