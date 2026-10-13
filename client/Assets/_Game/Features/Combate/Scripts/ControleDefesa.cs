using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// A defesa do próprio personagem: segurar o botão direito sobe a guarda (andar a 40%,
    /// stamina pago por golpe, o servidor decide quanto e quando quebra) e a reação ao acerto
    /// (o golpe em curso para e a próxima ação espera 0,3 s). O client só informa que a guarda
    /// está segurada; stamina e quebra voltam em <c>RecursosMudaram</c> e <c>GuardaMudou</c>.
    /// </summary>
    internal sealed class ControleDefesa : MonoBehaviour
    {
        public const string TextoGuardaQuebrou = "Guarda quebrou!";
        public const string TextoGolpeBloqueado = "Bloqueou!";

        /// <summary>Botão direito do mouse (docs/features/interface.md, Teclas e atalhos: bloqueio).</summary>
        public const string BotaoPadrao = "<Mouse>/rightButton";

        // Depois de uma recusa ou quebra o client espera um pouco antes de pedir de novo, para
        // não repetir o pedido a cada quadro enquanto o botão continua apertado.
        private const double EsperaDepoisDaRecusaSegundos = 0.4;

        // Abaixo disso de stamina o indicador avisa que a guarda está perto de quebrar.
        private const int StaminaBaixo = 20;

        // Inclinação do corpo para trás enquanto dura a reação ao acerto.
        private const float InclinacaoDaReacaoGraus = 14f;

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private const string FonteDoTeto = "guarda";

        private long _personagemId;
        private ProntidaoLocal _prontidao;
        private PainelDefesa _painel;
        private InputAction _guarda;
        private double _proximaTentativa;
        private double _reagindoAte;

        public void Configurar(long personagemId, ProntidaoLocal prontidao, PainelDefesa painel)
        {
            _personagemId = personagemId;
            _prontidao = prontidao;
            _painel = painel;

            _guarda = new InputAction("Guarda", InputActionType.Button, BotaoPadrao);
            if (isActiveAndEnabled)
            {
                _guarda.Enable();
            }

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<GuardaMudou>(CombateMessageIds.GuardaMudou, AoMudarGuarda)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<GolpeBloqueado>(
                    CombateMessageIds.GolpeBloqueado,
                    AoBloquear
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AcaoInterrompida>(
                    CombateMessageIds.AcaoInterrompida,
                    AoInterromper
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDeQueda>(
                    CombateMessageIds.EstadoDeQueda,
                    AoMudarQueda
                )
            );
        }

        private void OnEnable() => _guarda?.Enable();

        private void OnDisable()
        {
            _guarda?.Disable();
            _prontidao?.Teto.Soltar(FonteDoTeto);
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            _guarda?.Dispose();
        }

        /// <summary>
        /// Antes de atacar ou esquivar: a guarda desce e o servidor é avisado antes do pedido
        /// da ação (as mensagens chegam na ordem em que saem).
        /// </summary>
        public static void LargarParaAgir(ProntidaoLocal prontidao)
        {
            if (prontidao == null || !prontidao.Guardando)
            {
                return;
            }

            ConexaoTempoReal.Enviar(
                CombateMessageIds.IntencaoDeGuarda,
                new IntencaoDeGuarda { Segurando = false }
            );
            prontidao.MarcarGuarda(false, Time.unscaledTimeAsDouble);
        }

        private void Update()
        {
            var agora = Time.unscaledTimeAsDouble;
            var jogador = EntradaNoJogo.Jogador;
            var noJogo = EntradaNoJogo.NoJogo && jogador != null && ConexaoTempoReal.Conectado;
            var segurando =
                noJogo
                && _guarda != null
                && _guarda.IsPressed()
                && Cursor.lockState == CursorLockMode.Locked;

            if (segurando && !_prontidao.Guardando)
            {
                if (agora >= _proximaTentativa && _prontidao.PodeGuardar(agora))
                {
                    Subir(jogador, agora);
                }
            }
            else if (!segurando && _prontidao.Guardando)
            {
                Descer(jogador, agora, avisarOServidor: noJogo);
            }

            var staminaBaixo = _prontidao.Stamina is int stamina && stamina < StaminaBaixo;
            _painel?.Mostrar(_prontidao.Guardando, staminaBaixo);
        }

        // Depois do Update do ControleTerceiraPessoa (que vira o corpo para onde anda): a
        // postura de guarda olha para onde a câmera mira e anda a 40%; a reação inclina o corpo.
        private void LateUpdate()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null)
            {
                _prontidao.Teto.Soltar(FonteDoTeto);
                return;
            }

            if (_prontidao.Guardando)
            {
                _prontidao.Teto.Prender(FonteDoTeto, jogador, ApresentacaoDefesa.FatorDaGuarda);
                var camera = Camera.main;
                if (camera != null)
                {
                    jogador.transform.rotation = Quaternion.Euler(
                        0f,
                        camera.transform.eulerAngles.y,
                        0f
                    );
                }
            }
            else
            {
                _prontidao.Teto.Soltar(FonteDoTeto);
            }

            if (Time.unscaledTimeAsDouble < _reagindoAte)
            {
                jogador.transform.rotation *= Quaternion.Euler(-InclinacaoDaReacaoGraus, 0f, 0f);
            }
        }

        private void Subir(GameObject jogador, double agora)
        {
            ConexaoTempoReal.Enviar(
                CombateMessageIds.IntencaoDeGuarda,
                new IntencaoDeGuarda { Segurando = true }
            );
            _prontidao.MarcarGuarda(true, agora);
            AnimadorDeDefesa.DefinirGuarda(jogador.GetComponentInChildren<Animator>(), true);
        }

        private void Descer(GameObject jogador, double agora, bool avisarOServidor)
        {
            if (avisarOServidor)
            {
                ConexaoTempoReal.Enviar(
                    CombateMessageIds.IntencaoDeGuarda,
                    new IntencaoDeGuarda { Segurando = false }
                );
            }

            _prontidao.MarcarGuarda(false, agora);
            if (jogador != null)
            {
                AnimadorDeDefesa.DefinirGuarda(jogador.GetComponentInChildren<Animator>(), false);
            }
        }

        private void AoMudarGuarda(GuardaMudou guarda)
        {
            if (guarda.PersonagemId != _personagemId)
            {
                return; // A postura dos outros jogadores é do visual deles (Mundo/Personagem).
            }

            var jogador = EntradaNoJogo.Jogador;
            if (guarda.Segurando)
            {
                return; // O client já subiu a guarda ao apertar (ADR 0024).
            }

            // Recusada, quebrada ou largada: a guarda desce aqui sem repetir o aviso ao servidor.
            Descer(jogador, Time.unscaledTimeAsDouble, avisarOServidor: false);
            _proximaTentativa = Time.unscaledTimeAsDouble + EsperaDepoisDaRecusaSegundos;
            if (guarda.Quebrou)
            {
                AvisosDoJogo.Mostrar(TextoGuardaQuebrou, TipoDeAviso.Erro);
            }
        }

        private void AoBloquear(GolpeBloqueado bloqueio)
        {
            if (bloqueio.PersonagemId == _personagemId)
            {
                AvisosDoJogo.Mostrar(TextoGolpeBloqueado, TipoDeAviso.Aviso);
            }
        }

        // Caído deita o personagem; levantado (ou de volta à cidade) o põe de pé. A tela do caído
        // é de Personagem; aqui só a pose.
        private void AoMudarQueda(EstadoDeQueda queda)
        {
            if (queda.PersonagemId != _personagemId)
            {
                return;
            }

            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null)
            {
                return;
            }

            var animator = jogador.GetComponentInChildren<Animator>();
            var caido = queda.Estado == FaseDeQueda.Caido || queda.Estado == FaseDeQueda.Levantando;
            if (caido)
            {
                AnimadorDeDefesa.DefinirGuarda(animator, false);
            }

            AnimadorDeDefesa.DefinirCaido(animator, caido);
        }

        private void AoInterromper(AcaoInterrompida interrupcao)
        {
            // Os inimigos interrompidos são do visual de Mobs; aqui só o próprio personagem.
            if (!interrupcao.Jogador || interrupcao.AlvoId != _personagemId)
            {
                return;
            }

            var agora = Time.unscaledTimeAsDouble;
            var duracao = interrupcao.DuracaoMs / 1000.0;
            _prontidao.MarcarReacao(agora, duracao);
            _reagindoAte = agora + duracao;
            _proximaTentativa = Math.Max(_proximaTentativa, agora + duracao);

            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                var animator = jogador.GetComponentInChildren<Animator>();
                AnimadorDeDefesa.DefinirGuarda(animator, false);
                AnimadorDeDefesa.Interromper(animator);
            }
        }
    }
}
