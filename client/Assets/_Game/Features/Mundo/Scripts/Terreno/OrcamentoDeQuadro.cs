using System.Diagnostics;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Relógio do orçamento de tempo de um quadro: quem monta o mundo aos poucos pergunta
    /// <see cref="Esgotou"/> a cada peça e, quando é <c>true</c>, cede a vez ao próximo quadro.
    /// O orçamento padrão é o da vegetação (<see cref="ConfigVegetacao"/>).
    /// </summary>
    public sealed class OrcamentoDeQuadro
    {
        private readonly float _ms;
        private readonly Stopwatch _relogio = Stopwatch.StartNew();

        public OrcamentoDeQuadro(float ms = ConfigVegetacao.OrcamentoDeMontagemPorQuadroMs)
        {
            _ms = ms;
        }

        /// <summary>O orçamento, em milissegundos por quadro.</summary>
        public float Ms => _ms;

        /// <summary>
        /// <c>true</c> se o quadro já gastou o orçamento; nesse caso o relógio recomeça para o
        /// próximo quadro.
        /// </summary>
        public bool Esgotou()
        {
            if (_relogio.Elapsed.TotalMilliseconds < _ms)
            {
                return false;
            }

            _relogio.Restart();
            return true;
        }
    }
}
