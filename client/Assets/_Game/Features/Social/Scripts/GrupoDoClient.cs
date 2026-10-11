using UnityEngine;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O <see cref="EstadoDoGrupo"/> do jogo, criado na inicialização para já estar ouvindo o
    /// servidor quando um convite chegar com a janela fechada. Uma instância por execução.
    /// </summary>
    internal static class GrupoDoClient
    {
        private static EstadoDoGrupo _estado;

        /// <summary>O grupo do jogador local; criado sob demanda se a inicialização ainda não rodou.</summary>
        public static EstadoDoGrupo Estado
        {
            get
            {
                if (_estado == null)
                {
                    _estado = new EstadoDoGrupo(new ConexaoDoGrupoReal());
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
    }
}
