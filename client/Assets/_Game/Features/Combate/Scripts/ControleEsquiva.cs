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
    /// Esquiva no client: o Alt esquerdo envia a intenção com a direção do movimento e o
    /// personagem se desloca na hora (predição, no ritmo de <see cref="RegrasEsquiva"/>). A
    /// animação toca quando o servidor confirma e, se ele recusar, o movimento antecipado é
    /// desfeito e o motivo aparece no aviso do jogo. Vigor, posição final e recusa são decididos
    /// pelo servidor; a posição dele continua prevalecendo (correção da presença do Mundo).
    /// </summary>
    internal sealed class ControleEsquiva : MonoBehaviour
    {
        private readonly GeradorDeEsquivas _gerador = new GeradorDeEsquivas();
        private readonly PredicaoDeEsquiva _predicao = new PredicaoDeEsquiva();
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private long _personagemId;
        private InputAction _esquiva;
        private InputAction _mover;
        private CharacterController _corpo;
        private ControleTerceiraPessoa _controle;
        private bool _inputAnterior = true;

        public void Configurar(long personagemId)
        {
            _personagemId = personagemId;

            _esquiva = new InputAction(
                "Esquiva",
                InputActionType.Button,
                ApresentacaoEsquiva.TeclaPadrao
            );
            _mover = new InputAction("EsquivaDirecao", InputActionType.Value);
            _mover
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
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

        private void OnDisable()
        {
            _esquiva?.Disable();
            _mover?.Disable();
        }

        private void OnDestroy()
        {
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
                anguloCamera,
                jogador.transform.eulerAngles.y
            );

            var intencao = _gerador.Criar(direcao);
            ConexaoTempoReal.Enviar(CombateMessageIds.IntencaoDeEsquiva, intencao);

            _corpo = jogador.GetComponent<CharacterController>();
            _controle = jogador.GetComponent<ControleTerceiraPessoa>();
            var posicao = jogador.transform.position;
            _predicao.Iniciar(intencao.Sequencia, posicao.x, posicao.z, intencao.DirecaoY);
            jogador.transform.rotation = Quaternion.Euler(0f, intencao.DirecaoY, 0f);

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
            if (!ApresentacaoEsquiva.EhMinha(esquiva.PersonagemId, _personagemId))
            {
                // Animação dos outros jogadores é do visual deles (Mundo/Personagem).
                return;
            }

            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                AnimadorDeEsquiva.Tocar(jogador.GetComponentInChildren<Animator>());
            }
        }

        private void AoRecusarEsquiva(EsquivaRecusada recusa)
        {
            AvisosDoJogo.Mostrar(
                ApresentacaoEsquiva.TextoDaRecusa(recusa.Codigo),
                TipoDeAviso.Erro
            );

            if (!_predicao.Recusar(recusa.Sequencia, out var voltarX, out var voltarZ))
            {
                return;
            }

            DevolverControle();
            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null)
            {
                return;
            }

            // CharacterController só aceita teleporte com ele desligado.
            var posicao = jogador.transform.position;
            var corpo = jogador.GetComponent<CharacterController>();
            if (corpo != null)
            {
                corpo.enabled = false;
            }

            jogador.transform.position = new Vector3((float)voltarX, posicao.y, (float)voltarZ);
            if (corpo != null)
            {
                corpo.enabled = true;
            }
        }
    }
}
