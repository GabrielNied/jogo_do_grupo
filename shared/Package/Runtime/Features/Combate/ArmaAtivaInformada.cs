using System;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor informa ao dono qual tipo de arma está empunhado no conjunto ativo.
    /// Enviado ao entrar no mundo ou quando o conjunto ativo é atualizado.
    /// Id: <see cref="CombateMessageIds.ArmaAtivaInformada"/> (6053).
    /// </summary>
    [Serializable]
    public class ArmaAtivaInformada
    {
        public TipoEquipamento TipoArma = TipoEquipamento.Espada;
        public int Conjunto = 1;
    }
}
