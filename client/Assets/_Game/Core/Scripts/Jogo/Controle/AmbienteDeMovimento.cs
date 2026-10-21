using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Controle
{
    /// <summary>
    /// O que o movimento do jogador local precisa saber do mundo para nadar e escalar (#349):
    /// os volumes marcados que valem (os de <see cref="VolumesMarcados"/>, que o servidor também
    /// confere, mais os marcadores da cena de validação) e o estado atual do movimento, que o
    /// combate lê para não atacar a nado nem na parede. O servidor decide; isto é só a previsão
    /// do client e a ponte com a conexão, que fica na funcionalidade Personagem.
    /// </summary>
    public static class AmbienteDeMovimento
    {
        private static readonly List<VolumeMarcado> _daCena = new List<VolumeMarcado>();
        private static readonly List<VolumeMarcado> _todos = new List<VolumeMarcado>();
        private static bool _atualizado;

        /// <summary>O jogador está nadando (previsão local, igual à regra do servidor).</summary>
        public static bool Nadando { get; private set; }

        /// <summary>O jogador está escalando uma via.</summary>
        public static bool Escalando { get; private set; }

        /// <summary>O servidor disse que a stamina não cobre o nado ou a escalada.</summary>
        public static bool SemStamina { get; private set; }

        /// <summary>Nado e escalada não deixam atacar (o servidor também recusa).</summary>
        public static bool ImpedeAtaque => Nadando || Escalando;

        /// <summary>Quem controla a conexão envia a intenção de começar (true) ou soltar (false) a via.</summary>
        public static event Action<bool> EscaladaPedida;

        /// <summary>Volumes que valem agora: os do catálogo e os marcadores presentes na cena.</summary>
        public static IReadOnlyList<VolumeMarcado> Volumes
        {
            get
            {
                if (!_atualizado)
                {
                    _todos.Clear();
                    _todos.AddRange(VolumesMarcados.Todos);
                    _todos.AddRange(_daCena);
                    _atualizado = true;
                }

                return _todos;
            }
        }

        /// <summary>Um marcador da cena entrou: o volume passa a valer só no client (validação).</summary>
        public static void Registrar(VolumeMarcado volume)
        {
            if (volume != null && !_daCena.Contains(volume))
            {
                _daCena.Add(volume);
                _atualizado = false;
            }
        }

        /// <summary>Um marcador da cena saiu.</summary>
        public static void Remover(VolumeMarcado volume)
        {
            if (_daCena.Remove(volume))
            {
                _atualizado = false;
            }
        }

        /// <summary>O controle do jogador diz o que está fazendo agora.</summary>
        internal static void DefinirEstadoLocal(bool nadando, bool escalando)
        {
            Nadando = nadando;
            Escalando = escalando;
        }

        /// <summary>O controle começou ou soltou a escalada: avisa quem fala com o servidor.</summary>
        internal static void PedirEscalada(bool escalar) => EscaladaPedida?.Invoke(escalar);

        /// <summary>
        /// O servidor mandou o estado. Se ele diz que não escala e o jogador achava que sim, o
        /// controle solta a via no próximo quadro (<see cref="ConsumirSoltarEscalada"/>).
        /// </summary>
        public static void AplicarEstadoDoServidor(bool nadando, bool escalando, bool semStamina)
        {
            SemStamina = semStamina;
            if (!escalando && Escalando)
            {
                _soltarEscalada = true;
            }
        }

        private static bool _soltarEscalada;

        /// <summary>Se o servidor mandou soltar a via; zera ao ler.</summary>
        internal static bool ConsumirSoltarEscalada()
        {
            var soltar = _soltarEscalada;
            _soltarEscalada = false;
            return soltar;
        }

        /// <summary>Volta tudo ao estado inicial (ao sair do mundo).</summary>
        public static void Limpar()
        {
            Nadando = false;
            Escalando = false;
            SemStamina = false;
            _soltarEscalada = false;
            _daCena.Clear();
            _atualizado = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Limpar();
            EscaladaPedida = null;
        }
    }
}
