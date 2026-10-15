namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Servidor avisa o dono que a troca valeu: o conjunto ativo mudou e, por
    /// <see cref="DuracaoMs"/>, golpe, esquiva e guarda são recusados. A barra de skills da arma
    /// nova chega em <c>BarraDeSkillsMudou</c>. Id: <see cref="CombateMessageIds.TrocaDeArmaIniciada"/>.
    /// </summary>
    [System.Serializable]
    public class TrocaDeArmaIniciada
    {
        /// <summary>O conjunto de armas (1 ou 2) que ficou ativo.</summary>
        public int Conjunto = 0;

        /// <summary>Quanto dura o bloqueio, em milissegundos (<see cref="RegrasTrocaDeArma.DuracaoMs"/>).</summary>
        public int DuracaoMs = 0;
    }
}
