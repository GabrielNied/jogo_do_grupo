using System.Collections.Generic;
using System.Linq;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// As últimas <see cref="Maximo"/> linhas do chat (todos os canais) enquanto o jogo está
    /// aberto. Ignorar esconde na hora de mostrar, não de guardar: ao deixar de ignorar, as
    /// linhas voltam.
    /// </summary>
    internal sealed class HistoricoDeChat
    {
        public const int Maximo = 100;

        private readonly List<LinhaDeChat> _linhas = new List<LinhaDeChat>();

        public IReadOnlyList<LinhaDeChat> Linhas => _linhas;

        public void Adicionar(LinhaDeChat linha)
        {
            if (linha == null)
            {
                return;
            }

            _linhas.Add(linha);
            Aparar();
        }

        /// <summary>
        /// Junta o histórico que o servidor manda ao entrar na cidade: pula o que já está aqui
        /// (reentrar não duplica) e mantém a ordem pela hora. Devolve quantas linhas entraram.
        /// </summary>
        public int AdicionarHistorico(IEnumerable<LinhaDeChat> novas)
        {
            var entraram = 0;
            foreach (var linha in novas)
            {
                if (linha == null || _linhas.Any(existente => Igual(existente, linha)))
                {
                    continue;
                }

                _linhas.Add(linha);
                entraram++;
            }

            if (entraram > 0)
            {
                var ordenadas = _linhas.OrderBy(l => l.HoraUnixMs).ToList(); // estável
                _linhas.Clear();
                _linhas.AddRange(ordenadas);
                Aparar();
            }

            return entraram;
        }

        /// <summary>
        /// As últimas <paramref name="quantidade"/> linhas que podem aparecer (sem as de nomes
        /// ignorados), da mais antiga para a mais nova.
        /// </summary>
        public List<LinhaDeChat> Ultimas(int quantidade, ListaDeIgnorados ignorados)
        {
            var resultado = new List<LinhaDeChat>();
            for (var i = _linhas.Count - 1; i >= 0 && resultado.Count < quantidade; i--)
            {
                var linha = _linhas[i];
                var escondida =
                    ignorados != null
                    && !linha.Propria
                    && linha.Canal != CanalDaLinha.Sistema
                    && ignorados.Contem(linha.Nome);
                if (!escondida)
                {
                    resultado.Add(linha);
                }
            }

            resultado.Reverse();
            return resultado;
        }

        private static bool Igual(LinhaDeChat a, LinhaDeChat b) =>
            a.Canal == b.Canal
            && a.RemetenteId == b.RemetenteId
            && a.HoraUnixMs == b.HoraUnixMs
            && a.Texto == b.Texto;

        private void Aparar()
        {
            if (_linhas.Count > Maximo)
            {
                _linhas.RemoveRange(0, _linhas.Count - Maximo);
            }
        }
    }
}
