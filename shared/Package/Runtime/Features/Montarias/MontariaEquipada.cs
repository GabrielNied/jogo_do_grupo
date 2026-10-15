using System;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Servidor → client: confirmação da montaria equipada pelo personagem.
    /// Id: <see cref="MontariasMessageIds.MontariaEquipada"/>.
    /// </summary>
    [Serializable]
    public class MontariaEquipada
    {
        public int TipoMontaria;
    }
}
