using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Mostra o estado de queda recebido de Combate para o próprio personagem. A contagem local é
    /// apenas apresentação; toda saída e retorno continua sendo decidida pelo servidor.
    /// </summary>
    internal sealed class ObservadorDaQueda : MonoBehaviour
    {
        private const string NomeObservador = "Personagem.ObservadorDaQueda";
        private const string NomeRaiz = "Personagem.Queda";

        private static ObservadorDaQueda _instancia;
        private GameObject _jogadorAtendido;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDaQueda>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null || jogador == _jogadorAtendido)
            {
                return;
            }

            _jogadorAtendido = jogador;
            try
            {
                Montar(jogador, EntradaNoJogo.PersonagemId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void Montar(GameObject jogador, long personagemId)
        {
            var raiz = new GameObject(NomeRaiz);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, jogador.scene);

            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel("PainelQueda", 4f);
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);
            EstiloPersonagem.Aplicar(documento.rootVisualElement);

            var painel = new PainelQueda(documento.rootVisualElement);
            raiz.AddComponent<ControladorDaQueda>().Configurar(personagemId, painel);
        }
    }

    /// <summary>Assina o contrato de Combate e apresenta o estado caído do personagem local.</summary>
    internal sealed class ControladorDaQueda : MonoBehaviour
    {
        private const double DuracaoDoHoldSegundos = 1.0;
        private const double AtrasoParaVoltarSegundos = RegrasQueda.MinimoParaVoltarSegundos;

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private long _personagemId;
        private PainelQueda _painel;
        private FaseDeQueda _fase;
        private int _segundosRecebidos;
        private double _recebidoEm;
        private double _caidoDesde;
        private double _holdDesde;
        private bool _segurando;
        private bool _pedidoEnviado;
        private string _aviso = "";

        internal void Configurar(long personagemId, PainelQueda painel)
        {
            _personagemId = personagemId;
            _painel = painel;
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDeQueda>(
                    CombateMessageIds.EstadoDeQueda,
                    AoMudarEstado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AcaoRecusada>(
                    CombateMessageIds.AcaoRecusada,
                    AoRecusarAcao
                )
            );
            _painel.ConfigurarHold(IniciarHold, EncerrarHold);
            _painel.Mostrar(false);
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            var documento = GetComponent<UIDocument>();
            if (documento != null && documento.panelSettings != null)
            {
                Destroy(documento.panelSettings);
            }
        }

        private void Update()
        {
            if (_fase != FaseDeQueda.Caido && _fase != FaseDeQueda.Levantando)
            {
                return;
            }

            if (
                _segurando
                && !_pedidoEnviado
                && Time.unscaledTimeAsDouble - _holdDesde >= DuracaoDoHoldSegundos
            )
            {
                _pedidoEnviado = ConexaoTempoReal.Enviar(
                    CombateMessageIds.VoltarParaACidade,
                    new VoltarParaACidade()
                );
                _segurando = false;
                _painel.DefinirHold(0f, false);
                _aviso = _pedidoEnviado ? TextosQueda.SolicitandoRetorno : TextosQueda.SemConexao;
            }

            AtualizarPainel();
        }

        private void AoMudarEstado(EstadoDeQueda estado)
        {
            if (!EntradaNoJogo.NoJogo || estado == null || estado.PersonagemId != _personagemId)
            {
                return;
            }

            var anterior = _fase;
            _fase = estado.Estado;
            _segundosRecebidos = Math.Max(0, estado.SegundosRestantes);
            _recebidoEm = Time.unscaledTimeAsDouble;
            _aviso = "";
            _pedidoEnviado = false;
            EncerrarHold();

            if (
                _fase == FaseDeQueda.Caido
                && anterior != FaseDeQueda.Caido
                && anterior != FaseDeQueda.Levantando
            )
            {
                _caidoDesde = _recebidoEm;
            }
            else if (_fase == FaseDeQueda.Levantando && anterior != FaseDeQueda.Levantando)
            {
                // Ao retomar uma interação, o servidor pode ter enviado a contagem pausada.
                _caidoDesde =
                    _recebidoEm
                    - Math.Max(0.0, RegrasQueda.DuracaoCaidoSegundos - _segundosRecebidos);
            }

            AtualizarPainel(estado);
        }

        private void AoRecusarAcao(AcaoRecusada recusada)
        {
            if (recusada == null)
            {
                return;
            }

            if (recusada.Codigo == CodigosErroCombate.AindaNaoPodeVoltar)
            {
                _aviso = TextosQueda.AguardeAntesDoRetorno;
                _pedidoEnviado = false;
                AtualizarPainel();
            }
        }

        private void IniciarHold()
        {
            if (_pedidoEnviado || _segurando || !PodeVoltar())
            {
                return;
            }

            _segurando = true;
            _holdDesde = Time.unscaledTimeAsDouble;
            _aviso = TextosQueda.SegureParaVoltar;
        }

        private void EncerrarHold()
        {
            if (_segurando && !_pedidoEnviado)
            {
                _aviso = "";
            }

            _segurando = false;
            _painel?.DefinirHold(0f, false);
        }

        private bool PodeVoltar() =>
            Time.unscaledTimeAsDouble - _caidoDesde >= AtrasoParaVoltarSegundos;

        private void AtualizarPainel(EstadoDeQueda estado = null)
        {
            var caido = _fase == FaseDeQueda.Caido || _fase == FaseDeQueda.Levantando;
            _painel.Mostrar(caido);
            if (!caido)
            {
                return;
            }

            var restante = _segundosRecebidos;
            if (_fase == FaseDeQueda.Caido)
            {
                restante = Math.Max(
                    0,
                    _segundosRecebidos - (int)(Time.unscaledTimeAsDouble - _recebidoEm)
                );
            }

            var temAjudante = _fase == FaseDeQueda.Levantando;
            var progresso = temAjudante
                ? Mathf.Clamp01((estado?.ProgressoMs ?? 0) / (float)RegrasQueda.TempoParaLevantarMs)
                : 0f;
            if (temAjudante && progresso <= 0f)
            {
                progresso = Mathf.Clamp01(
                    (float)(
                        (Time.unscaledTimeAsDouble - _recebidoEm)
                        / RegrasQueda.TempoParaLevantarSegundos
                    )
                );
            }

            var liberado = PodeVoltar();
            _painel.Atualizar(
                restante,
                temAjudante,
                progresso,
                liberado,
                _pedidoEnviado,
                _aviso,
                _segurando
                    ? Mathf.Clamp01(
                        (float)((Time.unscaledTimeAsDouble - _holdDesde) / DuracaoDoHoldSegundos)
                    )
                    : 0f
            );
        }
    }

    /// <summary>Constrói o painel de queda com UI Toolkit e os estilos Lumora.</summary>
    internal sealed class PainelQueda
    {
        private readonly VisualElement _raiz;
        private readonly Label _estado;
        private readonly Label _contagem;
        private readonly Label _aviso;
        private readonly VisualElement _trilho;
        private readonly VisualElement _preenchimento;
        private readonly Button _voltar;
        private bool _visivel;
        private Action _iniciarHold;
        private Action _encerrarHold;

        internal PainelQueda(VisualElement raiz)
        {
            _raiz = new VisualElement { name = "TelaQueda" };
            _raiz.AddToClassList(EstiloCore.Tela);
            _raiz.AddToClassList(EstiloCore.TelaSobreJogo);

            var caixa = new VisualElement { name = "PainelQueda" };
            caixa.AddToClassList(EstiloCore.Painel);
            caixa.AddToClassList(EstiloPersonagem.PainelQueda);

            var titulo = new Label(TextosQueda.Titulo);
            titulo.AddToClassList(EstiloCore.Titulo);
            _contagem = new Label();
            _contagem.AddToClassList(EstiloPersonagem.QuedaContagem);
            _estado = new Label();
            _estado.AddToClassList(EstiloCore.Subtitulo);

            _trilho = new VisualElement();
            _trilho.AddToClassList(EstiloCore.BarraCarregamento);
            _trilho.AddToClassList(EstiloPersonagem.QuedaTrilho);
            _preenchimento = new VisualElement();
            _preenchimento.AddToClassList(EstiloCore.BarraCarregamentoPreenchimento);
            _preenchimento.AddToClassList(EstiloPersonagem.QuedaPreenchimento);
            _trilho.Add(_preenchimento);

            _voltar = new Button { text = TextosQueda.BotaoVoltar };
            _voltar.AddToClassList(EstiloCore.Botao);
            _voltar.AddToClassList(EstiloCore.BotaoPrincipal);
            _voltar.RegisterCallback<PointerDownEvent>(AoPressionar);
            _voltar.RegisterCallback<PointerUpEvent>(AoSoltar);
            _voltar.RegisterCallback<PointerLeaveEvent>(AoSoltar);
            _voltar.RegisterCallback<KeyDownEvent>(AoPressionarTecla);
            _voltar.RegisterCallback<KeyUpEvent>(AoSoltarTecla);

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.MensagemAviso);
            caixa.Add(titulo);
            caixa.Add(_contagem);
            caixa.Add(_estado);
            caixa.Add(_trilho);
            caixa.Add(_voltar);
            caixa.Add(_aviso);
            _raiz.Add(caixa);
            _raiz.style.display = DisplayStyle.None;
            raiz.Add(_raiz);
        }

        internal void ConfigurarHold(Action iniciar, Action encerrar)
        {
            _iniciarHold = iniciar;
            _encerrarHold = encerrar;
        }

        internal void Mostrar(bool visivel)
        {
            if (_visivel == visivel)
            {
                return;
            }

            _visivel = visivel;
            _raiz.style.display = visivel ? DisplayStyle.Flex : DisplayStyle.None;
            if (visivel)
            {
                _voltar.Focus();
            }
        }

        internal void Atualizar(
            int segundos,
            bool temAjudante,
            float progresso,
            bool retornoLiberado,
            bool solicitando,
            string aviso,
            float hold
        )
        {
            _contagem.text = segundos + " s";
            _estado.text = temAjudante
                ? TextosQueda.EstaoLevantandoVoce
                : TextosQueda.AguardandoAliado;
            _estado.EnableInClassList(EstiloPersonagem.QuedaEstadoAguardando, !temAjudante);
            _estado.EnableInClassList(EstiloPersonagem.QuedaEstadoLevantando, temAjudante);
            EstiloPersonagem.Mostrar(_trilho, temAjudante || hold > 0f);
            _preenchimento.style.width = Length.Percent(progresso * 100f);
            _voltar.SetEnabled(retornoLiberado && !solicitando);
            _voltar.text = retornoLiberado
                ? TextosQueda.BotaoVoltar
                : TextosQueda.BotaoVoltarBloqueado;
            _aviso.text = aviso;
            EstiloPersonagem.Mostrar(_aviso, !string.IsNullOrEmpty(aviso));
            _voltar.EnableInClassList(EstiloPersonagem.QuedaHoldAtivo, hold > 0f);
            _preenchimento.style.width = Length.Percent(
                temAjudante ? progresso * 100f : hold * 100f
            );
        }

        internal void DefinirHold(float progresso, bool visivel)
        {
            _preenchimento.style.width = Length.Percent(progresso * 100f);
            EstiloPersonagem.Mostrar(_trilho, visivel);
        }

        private void AoPressionar(PointerDownEvent evento)
        {
            if (evento.button != 0)
            {
                return;
            }

            _voltar.CapturePointer(evento.pointerId);
            _iniciarHold?.Invoke();
        }

        private void AoSoltar(PointerUpEvent evento)
        {
            if (_voltar.HasPointerCapture(evento.pointerId))
            {
                _voltar.ReleasePointer(evento.pointerId);
            }

            _encerrarHold?.Invoke();
        }

        private void AoPressionarTecla(KeyDownEvent evento)
        {
            if (
                evento.keyCode == KeyCode.Space
                || evento.keyCode == KeyCode.Return
                || evento.keyCode == KeyCode.KeypadEnter
            )
            {
                _iniciarHold?.Invoke();
                evento.StopPropagation();
            }
        }

        private void AoSoltarTecla(KeyUpEvent evento)
        {
            if (
                evento.keyCode == KeyCode.Space
                || evento.keyCode == KeyCode.Return
                || evento.keyCode == KeyCode.KeypadEnter
            )
            {
                _encerrarHold?.Invoke();
                evento.StopPropagation();
            }
        }
    }

    internal static class TextosQueda
    {
        internal const string Titulo = "Você caiu";
        internal const string AguardandoAliado = "Aguardando um aliado";
        internal const string EstaoLevantandoVoce = "Um aliado está levantando você";
        internal const string BotaoVoltar = "Segure para voltar à cidade";
        internal const string BotaoVoltarBloqueado = "Voltar à cidade (disponível após 5 s)";
        internal const string SegureParaVoltar = "Continue segurando por 1 s para confirmar.";
        internal const string SolicitandoRetorno = "Solicitando retorno à cidade…";
        internal const string SemConexao = "Sem conexão com o servidor.";
        internal const string AguardeAntesDoRetorno = "Aguarde 5 s caído antes de voltar.";
    }
}
