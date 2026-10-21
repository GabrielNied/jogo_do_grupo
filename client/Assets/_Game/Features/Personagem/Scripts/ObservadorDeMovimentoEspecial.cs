using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Ponte entre o movimento do jogador (Core) e o servidor para nado, queda e escalada (#349):
    /// envia a intenção de escalar, recebe o estado que o servidor decidiu e avisa quando uma
    /// queda tira vida. Mesmo padrão do <see cref="ObservadorDaBarraXp"/>.
    /// </summary>
    internal sealed class ObservadorDeMovimentoEspecial : MonoBehaviour
    {
        private const string NomeObservador = "Personagem.MovimentoEspecialObservador";

        private static ObservadorDeMovimentoEspecial _instancia;

        private IDisposable _estado;
        private IDisposable _dano;
        private bool _estavaSemStamina;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDeMovimentoEspecial>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Awake()
        {
            _estado = ConexaoTempoReal.Assinar<EstadoDoMovimento>(
                PersonagemMessageIds.EstadoDoMovimento,
                AoReceberEstado
            );
            _dano = ConexaoTempoReal.Assinar<DanoDeQueda>(
                PersonagemMessageIds.DanoDeQueda,
                AoReceberDano
            );
            AmbienteDeMovimento.EscaladaPedida += AoPedirEscalada;
        }

        private void OnDestroy()
        {
            _estado?.Dispose();
            _dano?.Dispose();
            _estado = null;
            _dano = null;
            AmbienteDeMovimento.EscaladaPedida -= AoPedirEscalada;
        }

        private static void AoPedirEscalada(bool escalar)
        {
            ConexaoTempoReal.Enviar(
                PersonagemMessageIds.IntencaoDeEscalada,
                new IntencaoDeEscalada { Escalar = escalar }
            );
        }

        private void AoReceberEstado(EstadoDoMovimento estado)
        {
            if (estado == null)
            {
                return;
            }

            AmbienteDeMovimento.AplicarEstadoDoServidor(
                estado.Nadando,
                estado.Escalando,
                estado.SemStamina
            );
            if (estado.SemStamina && !_estavaSemStamina)
            {
                AvisosDoJogo.Mostrar(TextosPersonagem.AvisoSemStaminaNoMovimento);
            }

            _estavaSemStamina = estado.SemStamina;
        }

        private static void AoReceberDano(DanoDeQueda aviso)
        {
            if (aviso != null && aviso.Dano > 0)
            {
                AvisosDoJogo.Mostrar(TextosPersonagem.AvisoDanoDeQueda(aviso.Dano));
            }
        }
    }
}
