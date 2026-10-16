using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Temporizador do clique de ataque no client: soltar antes de
    /// <see cref="RegrasAtaque.SegurarParaPesadoSegundos"/> (0,3 s) emite ataque leve; segurar até lá
    /// emite ataque pesado imediatamente (docs/features/fatia-jogavel.md e combate.md).
    /// Descarta o clique se a condição de ataque for perdida durante o hold.
    /// </summary>
    internal sealed class ProcessadorDeCliqueAtaque
    {
        private double _segurandoDesde = -1d;
        private bool _pesadoDoClique;

        /// <summary>Verdadeiro enquanto o botão estiver sendo segurado em um clique válido.</summary>
        public bool Segurando => _segurandoDesde >= 0d;

        /// <summary>Momento em que o clique começou (-1 se nenhum clique ativo).</summary>
        public double SegurandoDesde => _segurandoDesde;

        /// <summary>Se o pesado já foi disparado durante o clique atual.</summary>
        public bool PesadoDoClique => _pesadoDoClique;

        /// <summary>
        /// Atualiza o temporizador a cada quadro. Retorna true se um ataque deve ser disparado neste quadro.
        /// </summary>
        public bool Atualizar(
            bool apertouNesteQuadro,
            bool segurandoBotao,
            bool podeAtacar,
            double agora,
            out TipoDeAtaque? ataqueDisparado
        )
        {
            ataqueDisparado = null;

            if (apertouNesteQuadro && podeAtacar)
            {
                _segurandoDesde = agora;
                _pesadoDoClique = false;
            }

            if (_segurandoDesde < 0d)
            {
                return false;
            }

            if (!podeAtacar)
            {
                _segurandoDesde = -1d; // Abriu um menu no meio do clique: o clique é descartado.
                return false;
            }

            if (
                !_pesadoDoClique
                && segurandoBotao
                && agora - _segurandoDesde >= RegrasAtaque.SegurarParaPesadoSegundos
            )
            {
                _pesadoDoClique = true;
                ataqueDisparado = TipoDeAtaque.Pesado;
                return true;
            }

            if (!segurandoBotao)
            {
                // Soltou: antes dos 0,3 s é o leve; o pesado já saiu enquanto segurava.
                if (!_pesadoDoClique)
                {
                    ataqueDisparado = TipoDeAtaque.Leve;
                }

                _segurandoDesde = -1d;
                return ataqueDisparado.HasValue;
            }

            return false;
        }

        /// <summary>Descarta qualquer clique em andamento.</summary>
        public void Resetar()
        {
            _segurandoDesde = -1d;
            _pesadoDoClique = false;
        }
    }
}
