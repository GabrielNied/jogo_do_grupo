using System;

namespace JogoDoGrupo.Shared.Features.Montarias
{
    /// <summary>
    /// Lista de tipos de montarias desbloqueadas pela conta do jogador.
    /// </summary>
    [Serializable]
    public class ColecaoMontarias
    {
        public int[] MontariasDesbloqueadas = Array.Empty<int>();
    }
}
