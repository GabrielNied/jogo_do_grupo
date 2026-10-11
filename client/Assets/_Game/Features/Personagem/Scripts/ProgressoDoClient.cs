using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// O <see cref="EstadoDeProgresso"/> do jogo. Ouve experiência e subida de nível desde o
    /// começo, para a barra existir mesmo com a janela fechada.
    /// </summary>
    internal static class ProgressoDoClient
    {
        private static EstadoDeProgresso _estado;

        public static EstadoDeProgresso Estado
        {
            get
            {
                if (_estado == null)
                {
                    _estado = new EstadoDeProgresso();
                }

                return _estado;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Iniciar()
        {
            ConexaoTempoReal.Assinar<ExperienciaGanha>(
                PersonagemMessageIds.ExperienciaGanha,
                ganho => Estado.Aplicar(ganho)
            );
            ConexaoTempoReal.Assinar<SubiuDeNivel>(
                PersonagemMessageIds.SubiuDeNivel,
                aviso => Estado.Aplicar(aviso)
            );
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _estado = null;
        }
    }
}
