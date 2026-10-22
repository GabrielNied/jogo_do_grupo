#nullable enable
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>
    /// Registro de objetos interagíveis no mundo para selecionar o mais próximo ao alcance (#1195).
    /// Evita que múltiplos alvos (como um NPC e um baú próximos) abram ao mesmo tempo.
    /// </summary>
    public static class RegistroDeInteracao
    {
        private static readonly List<IInteragivelNoMundo> _interagiveis =
            new List<IInteragivelNoMundo>();
        private static readonly object _trava = new object();

        /// <summary>Registra um objeto interagível.</summary>
        public static void Registrar(IInteragivelNoMundo interagivel)
        {
            if (interagivel == null)
            {
                return;
            }

            lock (_trava)
            {
                if (!_interagiveis.Contains(interagivel))
                {
                    _interagiveis.Add(interagivel);
                }
            }
        }

        /// <summary>Remove o registro de um objeto interagível.</summary>
        public static void Desregistrar(IInteragivelNoMundo interagivel)
        {
            if (interagivel == null)
            {
                return;
            }

            lock (_trava)
            {
                _interagiveis.Remove(interagivel);
            }
        }

        /// <summary>Limpa todos os interagíveis cadastrados (usado em testes ou ao trocar de cena).</summary>
        public static void Limpar()
        {
            lock (_trava)
            {
                _interagiveis.Clear();
            }
        }

        /// <summary>
        /// Retorna o interagível mais próximo da coordenada informada que esteja ao alcance e possa interagir.
        /// Retorna null se nenhum estiver ao alcance.
        /// </summary>
        public static IInteragivelNoMundo? ObterMaisProximo(float posX, float posZ)
        {
            lock (_trava)
            {
                IInteragivelNoMundo? maisProximo = null;
                var menorDistanciaSq = float.MaxValue;

                for (var i = 0; i < _interagiveis.Count; i++)
                {
                    var item = _interagiveis[i];
                    if (item == null || !item.PodeInteragir())
                    {
                        continue;
                    }

                    var dx = posX - item.PosicaoX;
                    var dz = posZ - item.PosicaoZ;
                    var distSq = dx * dx + dz * dz;
                    var raio = item.RaioInteracao;

                    if (distSq <= raio * raio && distSq < menorDistanciaSq)
                    {
                        menorDistanciaSq = distSq;
                        maisProximo = item;
                    }
                }

                return maisProximo;
            }
        }

        /// <summary>
        /// Verifica se o interagível informado é o mais próximo da coordenada e está ao alcance.
        /// </summary>
        public static bool EhOMaisProximo(IInteragivelNoMundo interagivel, float posX, float posZ)
        {
            if (interagivel == null)
            {
                return false;
            }

            return ObterMaisProximo(posX, posZ) == interagivel;
        }
    }
}
