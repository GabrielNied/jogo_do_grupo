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
    /// Esquiva no client: o Alt esquerdo envia a intenção com a direção do movimento, e o
    /// personagem se desloca e anima na hora, no ritmo de <see cref="RegrasEsquiva"/>. O client é
    /// dono do próprio corpo (ADR 0024): se o servidor recusar (ex.: sem stamina), o motivo aparece
    /// no aviso do jogo e a esquiva para onde está, sem voltar no tempo. O servidor decide o
    /// stamina gasta; a posição só muda por fora com uma posição forçada do Mundo.
    /// </summary>
    internal sealed class ControleEsquiva : MonoBehaviour
    {
        private readonly GeradorDeEsquivas _gerador = new GeradorDeEsquivas();
        private readonly PredicaoDeEsquiva _predicao = new PredicaoDeEsquiva();
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private long _personagemId;
        private ProntidaoLocal _prontidao;
        private InputAction _esquiva;
        private InputAction _mover;
        private CharacterController _corpo;
        private ControleTerceiraPessoa _controle;
        private bool _inputAnterior = true;

        public void Configurar(long personagemId, ProntidaoLocal prontidao)
        {
            _personagemId = personagemId;
            _prontidao = prontidao;

            _esquiva = new InputAction(
                "Esquiva",
                InputActionType.Button,
                TeclasDoJogo.Caminho(TeclasDoJogo.Esquiva)
            );
            _mover = new InputAction("EsquivaDirecao", InputActionType.Value);
            _mover
                .AddCompositeBinding("2DVector")
                .With("Up", TeclasDoJogo.Caminho(TeclasDoJogo.Frente))
                .With("Down", TeclasDoJogo.Caminho(TeclasDoJogo.Tras))
                .With("Left", TeclasDoJogo.Caminho(TeclasDoJogo.Esquerda))
                .With("Right", TeclasDoJogo.Caminho(TeclasDoJogo.Direita));
            TeclasDoJogo.Mudou += AtualizarTeclas;
            if (isActiveAndEnabled)
            {
                _esquiva.Enable();
                _mover.Enable();
            }

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EsquivaIniciada>(
                    CombateMessageIds.EsquivaIniciada,
                    AoIniciarEsquiva
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EsquivaRecusada>(
                    CombateMessageIds.EsquivaRecusada,
                    AoRecusarEsquiva
                )
            );
        }

        private void OnEnable()
        {
            _esquiva?.Enable();
            _mover?.Enable();
        }

        private void AtualizarTeclas()
        {
            _esquiva?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Esquiva));
            _mover?.ApplyBindingOverride(1, TeclasDoJogo.Caminho(TeclasDoJogo.Frente));
            _mover?.ApplyBindingOverride(2, TeclasDoJogo.Caminho(TeclasDoJogo.Tras));
            _mover?.ApplyBindingOverride(3, TeclasDoJogo.Caminho(TeclasDoJogo.Esquerda));
            _mover?.ApplyBindingOverride(4, TeclasDoJogo.Caminho(TeclasDoJogo.Direita));
        }

        private void OnDisable()
        {
            _esquiva?.Disable();
            _mover?.Disable();
        }

        private void OnDestroy()
        {
            TeclasDoJogo.Mudou -= AtualizarTeclas;
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            _esquiva?.Dispose();
            _mover?.Dispose();
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null)
            {
                _predicao.Cancelar();
                return;
            }

            if (_predicao.Ativa)
            {
                AplicarPredicao();
                return;
            }

            if (_prontidao != null && _prontidao.LiberarEsquivaGuardada(Time.unscaledTimeAsDouble))
            {
                Pedir(jogador); // O clique guardado durante o golpe ou a esquiva anterior.
                return;
            }

            if (
                _esquiva != null
                && _esquiva.WasPressedThisFrame()
                && ApresentacaoEsquiva.PodePedir(
                    ConexaoTempoReal.Conectado,
                    Cursor.lockState == CursorLockMode.Locked,
                    _predicao.Ativa
                )
            )
            {
                Pedir(jogador);
            }
        }

        private void Pedir(GameObject jogador)
        {
            var camera = Camera.main;
            var anguloCamera =
                camera != null ? camera.transform.eulerAngles.y : jogador.transform.eulerAngles.y;
            var direcao = ApresentacaoEsquiva.DirecaoDoPedido(
                _mover.ReadValue<Vector2>(),
                anguloCamera
            );

            var agora = Time.unscaledTimeAsDouble;
            if (_prontidao != null)
            {
                var resultado = _prontidao.ClicarEsquiva(agora);
                if (resultado == ResultadoDoClique.Guardado)
                {
                    return; // Sai quando a esquiva puder começar.
                }

                if (resultado != ResultadoDoClique.EnviarAgora)
                {
                    var semStamina =
                        _prontidao.StaminaEm(agora) is int stamina
                        && !RegrasEsquiva.TemStamina(stamina);
                    AvisosDoJogo.Mostrar(
                        semStamina
                            ? ApresentacaoEsquiva.TextoStaminaInsuficiente
                            : ApresentacaoEsquiva.TextoAcaoEmAndamento,
                        TipoDeAviso.Aviso,
                        CategoriaDeAviso.Combate
                    );
                    return;
                }
            }

            ControleDefesa.LargarParaAgir(_prontidao); // Esquivar solta a guarda, antes do pedido.
            var intencao = _gerador.Criar(direcao);
            ConexaoTempoReal.Enviar(CombateMessageIds.IntencaoDeEsquiva, intencao);
            _prontidao?.MarcarEsquiva(agora);

            _corpo = jogador.GetComponent<CharacterController>();
            _controle = jogador.GetComponent<ControleTerceiraPessoa>();
            var posicao = jogador.transform.position;
            _predicao.Iniciar(intencao.Sequencia, posicao.x, posicao.z, intencao.DirecaoY);
            jogador.transform.rotation = Quaternion.Euler(0f, intencao.DirecaoY, 0f);

            // Anima na hora, sem esperar a resposta do servidor (ADR 0024).
            AnimadorDeEsquiva.Tocar(jogador.GetComponentInChildren<Animator>());

            // O andar normal fica parado durante a esquiva: ela é o único deslocamento.
            if (_controle != null)
            {
                _inputAnterior = _controle.InputAtivo;
                _controle.InputAtivo = false;
            }
        }

        private void AplicarPredicao()
        {
            _predicao.Avancar(Time.deltaTime, out var passoX, out var passoZ);
            if (_corpo != null && _corpo.enabled)
            {
                _corpo.Move(new Vector3((float)passoX, 0f, (float)passoZ));
            }

            if (!_predicao.Ativa)
            {
                DevolverControle();
            }
        }

        private void DevolverControle()
        {
            if (_controle != null)
            {
                _controle.InputAtivo = _inputAnterior;
            }
        }

        private void AoIniciarEsquiva(EsquivaIniciada esquiva)
        {
            // A própria esquiva já animou ao apertar (ADR 0024); a dos outros jogadores toca
            // no avatar deles (#481).
            AnimacaoDeOutros.TocarEsquiva(esquiva, _personagemId);
        }

        private void AoRecusarEsquiva(EsquivaRecusada recusa)
        {
            AvisosDoJogo.Mostrar(
                ApresentacaoEsquiva.TextoDaRecusa(recusa.Codigo),
                TipoDeAviso.Erro,
                CategoriaDeAviso.Combate
            );

            // Para onde está, sem voltar no tempo (ADR 0024): o client é dono do próprio corpo.
            if (_predicao.Recusar(recusa.Sequencia, out _, out _))
            {
                DevolverControle();
            }
        }
    }
}
