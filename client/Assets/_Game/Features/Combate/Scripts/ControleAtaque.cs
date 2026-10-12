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
    /// Ataque da primeira arma no client: o clique no botão esquerdo envia o ataque leve e
    /// segurá-lo por 0,3 s, o pesado, com a direção da mira; mostra o que o servidor responde (animação no início do
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
        private ProntidaoLocal _prontidao;
        private InputAction _leve;
        private TipoDeAtaque _ultimoTipoProprio = TipoDeAtaque.Leve;
        private int _ultimoPassoProprio;

        // Compromisso do golpe: corpo preso a 40% da corrida e girando só até o teto do ataque.
        private TipoDeAtaque _tipoEmCurso = TipoDeAtaque.Leve;
        private float _giroDoCorpo;
        private const string FonteDoTeto = "golpe";

        // Botão do ataque: soltar antes de SegurarParaPesadoSegundos é o leve; segurar até lá
        // começa o pesado (docs/features/interface.md, Teclas e atalhos).
        private double _segurandoDesde = -1d;
        private bool _pesadoDoClique;

        public void Configurar(
            long personagemId,
            PainelAtaque painelAtaque,
            PainelRecursos painelRecursos,
            ControleRecursos recursos,
            ProntidaoLocal prontidao
        )
        {
            _personagemId = personagemId;
            _painelAtaque = painelAtaque;
            _painelRecursos = painelRecursos;
            _recursos = recursos;
            _prontidao = prontidao;

            // Botão esquerdo, como em docs/features/interface.md (Teclas e atalhos): o clique é
            // o leve e segurar por 0,3 s é o pesado. O direito é a guarda (ControleDefesa).
            _leve = new InputAction("AtaqueLeve", InputActionType.Button, "<Mouse>/leftButton");
            if (isActiveAndEnabled)
            {
                _leve.Enable();
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
        }

        private void OnDisable()
        {
            SoltarVelocidade();
            _leve?.Disable();
        }

        // Depois do Update do ControleTerceiraPessoa (que vira o corpo para onde anda): durante
        // o preparo e o acerto o corpo acompanha a mira, só até o teto de giro do ataque.
        private void LateUpdate()
        {
            var agora = Time.unscaledTime;
            var jogador = EntradaNoJogo.Jogador;
            var comprometido =
                EntradaNoJogo.NoJogo
                && jogador != null
                && _prontidao != null
                && _prontidao.Comprometido(agora);
            if (!comprometido)
            {
                SoltarVelocidade();
                return;
            }

            PrenderVelocidade(jogador);
            _giroDoCorpo = ApresentacaoAtaque.GirarParaAMira(
                _giroDoCorpo,
                DirecaoDaMira(),
                _tipoEmCurso,
                Time.deltaTime
            );
            jogador.transform.rotation = Quaternion.Euler(0f, _giroDoCorpo, 0f);
        }

        private void PrenderVelocidade(GameObject jogador) =>
            _prontidao.Teto.Prender(FonteDoTeto, jogador, ApresentacaoAtaque.FatorDoGolpe);

        // Só devolve o que ainda é o valor preso: se o servidor mandou outra velocidade no meio
        // do golpe, vale a dele.
        private void SoltarVelocidade() => _prontidao?.Teto.Soltar(FonteDoTeto);

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            _leve?.Dispose();
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

            if (_leve == null)
            {
                return;
            }

            var agora = Time.unscaledTime;
            if (_leve.WasPressedThisFrame() && PodeAtacar())
            {
                _segurandoDesde = agora;
                _pesadoDoClique = false;
            }

            if (_segurandoDesde < 0d)
            {
                return;
            }

            if (!PodeAtacar())
            {
                _segurandoDesde = -1d; // Abriu um menu no meio do clique: o clique é descartado.
                return;
            }

            if (
                !_pesadoDoClique
                && _leve.IsPressed()
                && agora - _segurandoDesde >= RegrasAtaque.SegurarParaPesadoSegundos
            )
            {
                _pesadoDoClique = true;
                Enviar(TipoDeAtaque.Pesado);
            }
            else if (!_leve.IsPressed())
            {
                // Soltou: antes dos 0,3 s é o leve; o pesado já saiu enquanto segurava.
                if (!_pesadoDoClique)
                {
                    Enviar(TipoDeAtaque.Leve);
                }

                _segurandoDesde = -1d;
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
            var agora = Time.unscaledTime;
            if (_prontidao != null && !_prontidao.PodeAtacar(tipo, agora))
            {
                var semVigor = _prontidao.Vigor is int vigor && !RegrasAtaque.TemVigor(tipo, vigor);
                _aviso.Mostrar(
                    semVigor
                        ? ApresentacaoAtaque.TextoVigorInsuficiente
                        : ApresentacaoAtaque.TextoAcaoEmAndamento
                );
                return;
            }

            ControleDefesa.LargarParaAgir(_prontidao); // Atacar solta a guarda, antes do pedido.
            var intencao = _gerador.Criar(tipo, DirecaoDaMira());
            ConexaoTempoReal.Enviar(CombateMessageIds.IntencaoDeAtaque, intencao);
            _prontidao?.MarcarAtaque(tipo, agora);
            _tipoEmCurso = tipo;
            _giroDoCorpo = intencao.DirecaoY;

            // Anima na hora, sem esperar o servidor (ADR 0024). O acerto e o dano continuam
            // sendo dele: o número de dano só aparece com AtaqueAcertou.
            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                // O corpo começa virado para a mira; dali só gira até o teto do ataque.
                jogador.transform.rotation = Quaternion.Euler(0f, _giroDoCorpo, 0f);
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
            _ultimoPassoProprio = ataque.Passo;
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

            // O pesado e o 3º leve da cadeia aparecem maiores.
            var pesado = ApresentacaoAtaque.GolpeForte(_ultimoTipoProprio, _ultimoPassoProprio);
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
                _prontidao?.RegistrarVigor(vigor.Atual);
                _recursos?.RegistrarAtualizacaoDoServidor();
            }
        }
    }
}
