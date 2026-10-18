using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Client → servidor: pede o catálogo de itens à venda pelo NPC.
    /// Id: <see cref="EconomiaMessageIds.ConsultarCatalogoLoja"/>.
    /// </summary>
    [Serializable]
    public class ConsultarCatalogoLoja { }
}
