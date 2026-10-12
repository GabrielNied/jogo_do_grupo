using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>Os números da defesa como o client os usa (a regra e o custo são do servidor).</summary>
    internal static class ApresentacaoDefesa
    {
        /// <summary>A fração da corrida permitida com a guarda segurada (0,4).</summary>
        public static float FatorDaGuarda => (float)RegrasGuarda.VelocidadeComGuarda;
    }
}
