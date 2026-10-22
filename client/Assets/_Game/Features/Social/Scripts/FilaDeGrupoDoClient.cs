using JogoDoGrupo.Client.Core.TempoReal;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O <see cref="EstadoDaFilaDeGrupo"/> do jogo, mantido ativo para receber as atualizações da
    /// fila de dungeon mesmo quando a janela estiver fechada. Uma instância por execução.
    /// </summary>
    internal static class FilaDeGrupoDoClient
    {
        private static EstadoDaFilaDeGrupo _estado;

        /// <summary>O estado da fila de grupo do jogador local.</summary>
        public static EstadoDaFilaDeGrupo Estado
        {
            get
            {
                if (_estado == null)
                {
                    _estado = new EstadoDaFilaDeGrupo(new ConexaoDoGrupoReal());
                }

                return _estado;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Iniciar()
        {
            _ = Estado;
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, o campo estático sobrevive e o
        // roteador do Core é limpo: recomeça do zero.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _estado?.Dispose();
            _estado = null;
        }

        /// <summary>Substitui a instância ativa para fins de teste.</summary>
        internal static void DefinirInstanciaParaTestes(EstadoDaFilaDeGrupo instancia)
        {
            _estado?.Dispose();
            _estado = instancia;
        }
    }
}
