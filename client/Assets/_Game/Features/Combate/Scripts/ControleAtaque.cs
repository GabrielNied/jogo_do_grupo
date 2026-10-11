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
    /// Ataque da primeira arma no client: botão esquerdo envia o ataque leve e o direito, o
    /// pesado, com a direção da mira; mostra o que o servidor responde (animação no início do
    /// ataque, número de dano no acerto, aviso de recusa e barras de recursos). Nada de dano,
    /// custo ou acerto é decidido aqui.
    /// </summary>
    internal sealed class ControleAtaque : MonoBehaviour
    {
        /// <summary>Afastamento, em pixels de referência, entre os números de um golpe em vários alvos.</summary>
        public const float EspacoEntreNumeros = 70f;

        private readonly GeradorDeIntencoes _gerador = new GeradorDeIntencoes();
        private readonly FilaDeNumeros _numeros = new FilaDeNumeros();
        private readonly AvisoTemporario _aviso = new AvisoTemporario();
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private long _personagemId;
        private PainelAtaque _painelAtaque;
        private PainelRecursos _painelRecursos;
        private ControleRecursos _recursos;
        private InputAction _leve;
        private InputAction _pesado;
        private TipoDeAtaque _ultimoTipoProprio = TipoDeAtaque.Leve;

        public void Configurar(
            long personagemId,
            PainelAtaque painelAtaque,
            PainelRecursos painelRecursos,
            ControleRecursos recursos
        )
        {
            _personagemId = personagemId;
            _painelAtaque = painelAtaque;
            _painelRecursos = painelRecursos;
            _recursos = recursos;

            // Botões do mouse, como em docs/features/interface.md (Teclas e atalhos).
            _leve = new InputAction("AtaqueLeve", InputActionType.Button, "<Mouse>/leftButton");
            _pesado = new InputAction(
                "AtaquePesado",
                InputActionType.Button,
                "<Mouse>/rightButton"
            );
            if (isActiveAndEnabled)
            {
                _leve.Enable();
                _pesado.Enable();
            }

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AtaqueIniciado>(
                    CombateMessageIds.AtaqueIniciado,
                    AoIniciarAtaque
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AtaqueAcertou>(
                    CombateMessageIds.AtaqueAcertou,
                    AoAcertarAtaque
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AtaqueRecusado>(
                    CombateMessageIds.AtaqueRecusado,
                    AoRecusarAtaque
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<RecursosMudaram>(
                    CombateMessageIds.RecursosMudaram,
                    AoMudarRecursos
                )
            );
        }

        private void OnEnable()
        {
            _leve?.Enable();
            _pesado?.Enable();
        }

        private void OnDisable()
        {
            _leve?.Disable();
            _pesado?.Disable();
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            _leve?.Dispose();
            _pesado?.Dispose();
        }

        private void Update()
        {
            if (!EntradaNoJogo.NoJogo)
            {
                return;
            }

            _numeros.Atualizar(Time.deltaTime);
            _aviso.Atualizar(Time.deltaTime);
            _painelAtaque?.Mostrar(_numeros, _aviso);

            if (!PodeAtacar())
            {
                return;
            }

            if (_leve != null && _leve.WasPressedThisFrame())
            {
                Enviar(TipoDeAtaque.Leve);
            }
            else if (_pesado != null && _pesado.WasPressedThisFrame())
            {
                Enviar(TipoDeAtaque.Pesado);
            }
        }

        /// <summary>
        /// Só ataca com o jogo no mundo, conectado e sem janela ou menu aberto (o cursor fica
        /// preso à câmera só quando nada está aberto).
        /// </summary>
        private static bool PodeAtacar() =>
            ConexaoTempoReal.Conectado && Cursor.lockState == CursorLockMode.Locked;

        private void Enviar(TipoDeAtaque tipo)
        {
            var intencao = _gerador.Criar(tipo, DirecaoDaMira());
            ConexaoTempoReal.Enviar(CombateMessageIds.IntencaoDeAtaque, intencao);

            // Anima na hora, sem esperar o servidor (ADR 0024). O acerto e o dano continuam
            // sendo dele: o número de dano só aparece com AtaqueAcertou.
            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                AnimadorDeAtaque.Tocar(jogador.GetComponentInChildren<Animator>(), tipo);
            }
        }

        /// <summary>Para onde a câmera olha no plano do chão, em graus (0 = +Z, cresce para +X).</summary>
        private static float DirecaoDaMira()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                return camera.transform.eulerAngles.y;
            }

            var jogador = EntradaNoJogo.Jogador;
            return jogador != null ? jogador.transform.eulerAngles.y : 0f;
        }

        private void AoIniciarAtaque(AtaqueIniciado ataque)
        {
            if (!ApresentacaoAtaque.MostraODano(ataque.AtacanteId, _personagemId))
            {
                // Animação dos outros jogadores é do visual deles (Mundo/Personagem).
                return;
            }

            // A animação própria já tocou ao apertar (ADR 0024).
            _ultimoTipoProprio = ataque.Tipo;
        }

        private void AoAcertarAtaque(AtaqueAcertou ataque)
        {
            if (
                !ApresentacaoAtaque.MostraODano(ataque.AtacanteId, _personagemId)
                || ataque.Acertos == null
            )
            {
                return;
            }

            var pesado = _ultimoTipoProprio == TipoDeAtaque.Pesado;
            var total = 0;
            foreach (var acerto in ataque.Acertos)
            {
                if (acerto != null)
                {
                    total++;
                }
            }

            var posicao = 0;
            foreach (var acerto in ataque.Acertos)
            {
                if (acerto == null)
                {
                    continue;
                }

                // Um número por alvo, lado a lado em torno do centro da mira.
                var deslocamento = (posicao - (total - 1) / 2f) * EspacoEntreNumeros;
                _numeros.Adicionar(
                    ApresentacaoAtaque.TextoDoDano(acerto.Dano),
                    pesado,
                    deslocamento
                );
                posicao++;
            }
        }

        private void AoRecusarAtaque(AtaqueRecusado recusa)
        {
            _aviso.Mostrar(ApresentacaoAtaque.TextoDaRecusa(recusa.Codigo));
        }

        private void AoMudarRecursos(RecursosMudaram recursos)
        {
            if (
                _painelRecursos != null
                && ApresentacaoCombate.TentarMontar(
                    recursos,
                    out var vida,
                    out var vigor,
                    out var energia
                )
            )
            {
                _painelRecursos.Mostrar(vida, vigor, energia);
                _recursos?.RegistrarAtualizacaoDoServidor();
            }
        }
    }
}
