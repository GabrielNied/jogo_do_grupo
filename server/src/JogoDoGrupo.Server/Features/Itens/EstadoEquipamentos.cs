using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

public sealed class EstadoEquipamentos
{
    public long PersonagemId { get; set; }
    public int ConjuntoAtivo { get; set; } = 1;
    public int AbasBolsa { get; set; } = RegrasItens.AbasBolsaInicial;
}
