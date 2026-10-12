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
    /// segurá-lo por 0,3 s, o pesado, com a direção da mira; mostra o que o servidor responde (animação
    /// no início do ataque, número de dano sobre o alvo no acerto, aviso de recusa e barras de
    /// recursos). Um clique durante outra ação entra na fila do servidor ou fica guardado
    /// (<see cref="ProntidaoLocal.ClicarAtaque"/>); um golpe que o servidor recusa é desfeito. Nada
    /// de dano, custo ou acerto é decidido aqui.
    /// </summary>
    internal sealed class ControleAtaque : MonoBehaviour
    {
        /// <summary>Afastamento, em pixels de referência, entre os números de um golpe em vários alvos.</summary>
        public const float EspacoEntreNumeros = 70f;

        private readonly GeradorDeIntencoes _gerador = new GeradorDeIntencoes();
        private readonly FilaDeNumeros _numeros = new FilaDeNumeros();
        private readonly AvisoTemporario _aviso = new AvisoTemporario();
        private readonly GolpesEmAberto _golpes = new GolpesEmAberto();
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private long _personagemId;
        private PainelAtaque _painelAtaque;
        private ControleRecursos _recursos;
        private ControleImpacto _impacto;
        private ProntidaoLocal _prontidao;
        private InputAction _leve;

        // A cadeia de leves como o servidor a conta (RegrasAtaque.PassoDoGolpe), para escolher a
        // animação sem esperar a resposta; o AtaqueIniciado do servidor corrige.
        private int _proximoPasso;
        private int _proximoPassoAntes;

        // O golpe em curso e o que espera na fila do servidor, para a recusa desfazer o certo.
        private int _sequenciaEmCurso;
        private int _sequenciaNaFila;
        private float _direcaoNaFila;
        private double _reacaoVista = double.NegativeInfinity;

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
            ControleRecursos recursos,
            ControleImpacto impacto,
            ProntidaoLocal prontidao
        )
        {
            _personagemId = personagemId;
            _painelAtaque = painelAtaque;
            _recursos = recursos;
            _impacto = impacto;
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
            var agora = Time.unscaledTimeAsDouble;
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

            var agora = Time.unscaledTimeAsDouble;
            AvancarFilaEBuffer(agora);
            ConferirGolpesSemResposta(agora);

            if (_leve == null)
            {
                return;
            }

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
                Pedir(TipoDeAtaque.Pesado, agora);
            }
            else if (!_leve.IsPressed())
            {
                // Soltou: antes dos 0,3 s é o leve; o pesado já saiu enquanto segurava.
                if (!_pesadoDoClique)
                {
                    Pedir(TipoDeAtaque.Leve, agora);
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

        // O que a fila e o buffer pedem a cada quadro: o golpe da fila começa quando a ação em
        // curso acaba, e o clique guardado sai quando cabe na fila do servidor.
        private void AvancarFilaEBuffer(double agora)
        {
            if (_prontidao == null)
            {
                return;
            }

            if (_prontidao.IniciarDaFila(agora, out var daFila))
            {
                IniciarGolpe(daFila, _direcaoNaFila, _sequenciaNaFila, agora);
            }

            if (_prontidao.LiberarAtaqueGuardado(agora, out var guardado))
            {
                Pedir(guardado, agora);
            }
        }

        private void Pedir(TipoDeAtaque tipo, double agora)
        {
            if (_prontidao == null)
            {
                EnviarEIniciar(tipo, agora);
                return;
            }

            switch (_prontidao.ClicarAtaque(tipo, agora))
            {
                case ResultadoDoClique.EnviarAgora:
                    EnviarEIniciar(tipo, agora);
                    break;
                case ResultadoDoClique.EnviarNaFila:
                    EnviarNaFila(tipo);
                    break;
                case ResultadoDoClique.SemVigor:
                    _aviso.Mostrar(ApresentacaoAtaque.TextoVigorInsuficiente);
                    break;
                case ResultadoDoClique.Descartado:
                    _aviso.Mostrar(ApresentacaoAtaque.TextoAcaoEmAndamento);
                    break;
            }
        }

        private void EnviarEIniciar(TipoDeAtaque tipo, double agora)
        {
            var intencao = Enviar(tipo);
            IniciarGolpe(tipo, intencao.DirecaoY, intencao.Sequencia, agora);
        }

        private void EnviarNaFila(TipoDeAtaque tipo)
        {
            // Entra na fila do servidor agora; a animação começa quando a ação em curso acabar.
            var intencao = Enviar(tipo);
            _sequenciaNaFila = intencao.Sequencia;
            _direcaoNaFila = intencao.DirecaoY;
        }

        private IntencaoDeAtaque Enviar(TipoDeAtaque tipo)
        {
            ControleDefesa.LargarParaAgir(_prontidao); // Atacar solta a guarda, antes do pedido.
            var intencao = _gerador.Criar(tipo, DirecaoDaMira());
            ConexaoTempoReal.Enviar(CombateMessageIds.IntencaoDeAtaque, intencao);
            return intencao;
        }

        // O golpe começa na tela. Anima na hora, sem esperar o servidor (ADR 0024). O acerto e o
        // dano continuam sendo dele: o número de dano só aparece com AtaqueAcertou.
        private void IniciarGolpe(TipoDeAtaque tipo, float direcao, int sequencia, double agora)
        {
            var passo = RegrasAtaque.PassoDoGolpe(
                tipo,
                _proximoPasso,
                _prontidao != null ? agora - _prontidao.LivreEm : double.MaxValue
            );
            _proximoPassoAntes = _proximoPasso;
            _proximoPasso = RegrasAtaque.ProximoPasso(tipo, passo);

            _prontidao?.MarcarAtaque(tipo, agora);
            _tipoEmCurso = tipo;
            _giroDoCorpo = direcao;
            _sequenciaEmCurso = sequencia;
            _golpes.Registrar(sequencia, tipo, direcao, agora);

            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                // O corpo começa virado para a mira; dali só gira até o teto do ataque.
                jogador.transform.rotation = Quaternion.Euler(0f, _giroDoCorpo, 0f);
                AnimadorDeAtaque.Tocar(jogador.GetComponentInChildren<Animator>(), tipo, passo);
            }
        }

        // Golpe que o servidor não confirmou no prazo errou o alvo: rastro e som no vazio.
        private void ConferirGolpesSemResposta(double agora)
        {
            if (_prontidao != null && _prontidao.UltimaReacaoEm > _reacaoVista)
            {
                // Levou um acerto que interrompeu a ação: o golpe cancelado não chega ao contato.
                _reacaoVista = _prontidao.UltimaReacaoEm;
                _golpes.Limpar();
            }

            while (_golpes.TirarVencido(agora, out var golpe))
            {
                var jogador = EntradaNoJogo.Jogador;
                if (jogador != null)
                {
                    _impacto?.GolpeNoVazio(jogador.transform.position, golpe.DirecaoY);
                }
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

            // A animação própria já tocou ao apertar (ADR 0024); o passo do servidor corrige a
            // cadeia daqui em diante.
            _proximoPasso = RegrasAtaque.ProximoPasso(ataque.Tipo, ataque.Passo);
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

            _golpes.Confirmar(ataque.Sequencia, out _);

            // O pesado e o 3º leve da cadeia aparecem maiores: o tipo e o passo vêm no acerto.
            var forte = ApresentacaoAtaque.GolpeForte(ataque.Tipo, ataque.Passo);
            foreach (var acerto in ataque.Acertos)
            {
                if (acerto == null)
                {
                    continue;
                }

                // Um número por alvo, sobre a cabeça dele; sem o alvo na cena, em torno da mira.
                var alvo = RegistroDeAlvos.Achar(acerto.AlvoId);
                Vector3? origem = alvo != null ? RegistroDeAlvos.AcimaDaCabeca(alvo) : null;
                _numeros.Adicionar(ApresentacaoAtaque.TextoDoDano(acerto.Dano), forte, 0f, origem);
            }
        }

        private void AoRecusarAtaque(AtaqueRecusado recusa)
        {
            _aviso.Mostrar(ApresentacaoAtaque.TextoDaRecusa(recusa.Codigo));
            var agora = Time.unscaledTimeAsDouble;

            if (recusa.Sequencia == _sequenciaNaFila && _prontidao?.NaFila != null)
            {
                // Ainda nem começou: nada a desfazer na tela.
                _prontidao.CancelarFila();
                _sequenciaNaFila = 0;
                return;
            }

            if (recusa.Sequencia != _sequenciaEmCurso || _sequenciaEmCurso == 0)
            {
                return; // Pedido de outro momento (o servidor recusou o 3º da fila, por exemplo).
            }

            // O golpe já tocou e gastou vigor previsto (ADR 0024): desfaz os dois.
            _golpes.Confirmar(recusa.Sequencia, out _);
            _prontidao?.DesfazerAtaque(agora);
            _proximoPasso = _proximoPassoAntes;
            _sequenciaEmCurso = 0;
            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                AnimadorDeAtaque.Cancelar(jogador.GetComponentInChildren<Animator>());
            }
        }

        private void AoMudarRecursos(RecursosMudaram recursos) =>
            _recursos?.AplicarDoServidor(recursos);
    }
}
