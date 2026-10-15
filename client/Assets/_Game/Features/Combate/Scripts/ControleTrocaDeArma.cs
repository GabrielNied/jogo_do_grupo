using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// A troca de arma do próprio personagem: a tecla R envia a intenção e, na hora, bloqueia
    /// golpe, esquiva e guarda por 0,4 s (<see cref="RegrasTrocaDeArma"/>), como o servidor fará.
    /// Quem decide é o servidor: se recusar, o bloqueio acaba e o motivo aparece no aviso. A arma
    /// nova, o visual e as skills 1 a 3 chegam do servidor (<c>BarraDeSkillsMudou</c>); a recarga é
    /// de cada arma, então nada aqui mexe nela.
    /// </summary>
    internal sealed class ControleTrocaDeArma : MonoBehaviour
    {
        /// <summary>R (docs/features/interface.md, Teclas e atalhos: troca de arma).</summary>
        public const string BotaoPadrao = "<Keyboard>/r";

        public const string TextoTrocouDeArma = "Arma trocada";

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private ProntidaoLocal _prontidao;
        private InputAction _troca;
        private int _sequencia;

        public void Configurar(ProntidaoLocal prontidao)
        {
            _prontidao = prontidao;
            _troca = new InputAction("TrocaDeArma", InputActionType.Button, BotaoPadrao);
            if (isActiveAndEnabled)
            {
                _troca.Enable();
            }

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<TrocaDeArmaIniciada>(
                    CombateMessageIds.TrocaDeArmaIniciada,
                    AoIniciar
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<TrocaDeArmaRecusada>(
                    CombateMessageIds.TrocaDeArmaRecusada,
                    AoRecusar
                )
            );
        }

        private void OnEnable() => _troca?.Enable();

        private void OnDisable() => _troca?.Disable();

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            _troca?.Dispose();
        }

        private void Update()
        {
            if (
                _troca == null
                || !_troca.WasPressedThisFrame()
                || !EntradaNoJogo.NoJogo
                || EntradaNoJogo.Jogador == null
                || !ConexaoTempoReal.Conectado
                || Cursor.lockState != CursorLockMode.Locked
            )
            {
                return;
            }

            var agora = Time.unscaledTimeAsDouble;
            // Segurando a guarda: ela desce e o servidor é avisado antes do pedido da troca.
            ControleDefesa.LargarParaAgir(_prontidao);
            if (!_prontidao.PodeTrocarDeArma(agora))
            {
                return;
            }

            _prontidao.MarcarTrocaDeArma(agora);
            ConexaoTempoReal.Enviar(
                CombateMessageIds.IntencaoDeTrocaDeArma,
                new IntencaoDeTrocaDeArma { Sequencia = ++_sequencia }
            );
        }

        private static void AoIniciar(TrocaDeArmaIniciada troca)
        {
            AvisosDoJogo.Mostrar(TextoTrocouDeArma, TipoDeAviso.Aviso, CategoriaDeAviso.Combate);
        }

        private void AoRecusar(TrocaDeArmaRecusada recusa)
        {
            _prontidao.DesfazerTrocaDeArma(Time.unscaledTimeAsDouble);
            AvisosDoJogo.Mostrar(
                ApresentacaoAtaque.TextoDaRecusa(recusa.Codigo),
                TipoDeAviso.Erro,
                CategoriaDeAviso.Combate
            );
        }
    }
}
