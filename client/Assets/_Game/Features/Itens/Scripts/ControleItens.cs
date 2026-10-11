using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Itens no mundo, enquanto o jogador está nele: I abre o inventário do personagem logado,
    /// E perto do baú abre o baú (<c>POST</c>) e mostra os ganhos e o inventário resultante.
    /// Só envia a intenção e mostra a resposta: o sorteio e a gravação são do servidor.
    /// Criado pelo <see cref="ObservadorDoMundo"/> na cena do mundo, então o "Sair" descarrega
    /// tudo junto.
    /// </summary>
    internal sealed class ControleItens : MonoBehaviour
    {
        private const string TextoDicaBau = "E: abrir o baú";

        private InputAction _inventario;
        private InputAction _interagir;
        private InputAction _sair;

        private PainelItens _painel;
        private PanelSettings _configuracaoDoPainel;
        private Transform _bau;
        private ControleTerceiraPessoa _controle;
        private CameraTerceiraPessoa _camera;
        private long _personagemId;
        private GameObject _jogador;

        // Cada abertura do painel é uma "geração": resposta de uma geração antiga é descartada.
        private int _geracao;
        private bool _ocupado;
        private ResultadoItens _ultimo;

        /// <summary>Liga o controle ao jogador, ao personagem e ao baú. Chamar antes de ativar.</summary>
        public void Configurar(
            GameObject jogador,
            long personagemId,
            Transform bau,
            CameraTerceiraPessoa camera,
            VisualElement raizDaInterface,
            PanelSettings configuracaoDoPainel
        )
        {
            _jogador = jogador;
            _personagemId = personagemId;
            _bau = bau;
            _controle = jogador.GetComponent<ControleTerceiraPessoa>();
            _camera = camera;
            _configuracaoDoPainel = configuracaoDoPainel;
            _painel = new PainelItens(raizDaInterface, FecharPeloBotao);

            _inventario = new InputAction("Inventario", InputActionType.Button, "<Keyboard>/i");
            _inventario.AddBinding("<Gamepad>/select");
            _interagir = new InputAction("Interagir", InputActionType.Button, "<Keyboard>/e");
            _interagir.AddBinding("<Gamepad>/buttonWest");
            _sair = new InputAction("FecharItens", InputActionType.Button, "<Keyboard>/escape");
            _sair.AddBinding("<Gamepad>/start");

            if (isActiveAndEnabled)
            {
                AtivarAcoes();
            }
        }

        private void OnEnable()
        {
            AtivarAcoes();
        }

        private void OnDisable()
        {
            _inventario?.Disable();
            _interagir?.Disable();
            _sair?.Disable();
        }

        private void OnDestroy()
        {
            _inventario?.Dispose();
            _interagir?.Dispose();
            _sair?.Dispose();
            _inventario = null;
            _interagir = null;
            _sair = null;

            if (_configuracaoDoPainel != null)
            {
                Destroy(_configuracaoDoPainel);
            }
        }

        private void AtivarAcoes()
        {
            _inventario?.Enable();
            _interagir?.Enable();
            _sair?.Enable();
        }

        private void Update()
        {
            if (_painel == null || _jogador == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            var painelAberto = _painel.Visivel;
            if (painelAberto)
            {
                // O Core devolve o controle ao fechar o menu do Esc; enquanto o painel está
                // aberto, o controle continua parado e o mouse solto.
                BloquearJogo();
            }

            // Controle parado com o painel fechado = menu do Esc aberto (só o Core faz isso):
            // não abre inventário nem baú por baixo dele.
            var menuDoCoreAberto = !painelAberto && _controle != null && !_controle.InputAtivo;

            var perto = BauNoMundo.EstaPerto(_jogador.transform.position, _bau.position);
            AtualizarDica(!painelAberto && !menuDoCoreAberto && perto);

            if (menuDoCoreAberto)
            {
                return;
            }

            if (painelAberto && _sair.WasPressedThisFrame())
            {
                // O Core abre o menu do Esc neste mesmo frame e cuida do controle e do mouse.
                FecharPainel(false);
            }
            else if (_inventario.WasPressedThisFrame())
            {
                if (painelAberto)
                {
                    FecharPainel(true);
                }
                else
                {
                    AbrirInventario();
                }
            }
            else if (_interagir.WasPressedThisFrame())
            {
                if (painelAberto)
                {
                    FecharPainel(true);
                }
                else if (perto)
                {
                    AbrirBau();
                }
            }
        }

        private void FecharPeloBotao()
        {
            FecharPainel(true);
        }

        private void FecharPainel(bool devolverControle)
        {
            _geracao++;
            _ocupado = false;
            _painel.Esconder();
            if (devolverControle)
            {
                DevolverJogo();
            }
        }

        private void AtualizarDica(bool mostrar)
        {
            if (mostrar)
            {
                _painel.MostrarDica(TextoDicaBau);
            }
            else
            {
                _painel.EsconderDica();
            }
        }

        private void BloquearJogo()
        {
            if (_controle != null)
            {
                _controle.InputAtivo = false;
            }

            if (_camera != null)
            {
                _camera.InputAtivo = false;
            }

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void DevolverJogo()
        {
            if (_controle != null)
            {
                _controle.InputAtivo = true;
            }

            if (_camera != null)
            {
                _camera.InputAtivo = true;
            }

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private async void AbrirInventario()
        {
            if (_ocupado)
            {
                return;
            }

            var geracao = ++_geracao;
            _ocupado = true;
            BloquearJogo();
            _painel.MostrarCarregando("Inventário");

            try
            {
                var resposta = await ClienteApi.Get<InventarioResponse>(
                    RotasItens.InventarioDe(_personagemId)
                );
                if (!RespostaAindaVale(geracao))
                {
                    return;
                }

                var resultado = ApresentacaoItens.InterpretarInventario(
                    resposta.Sucesso,
                    resposta.CodigoErro,
                    resposta.Corpo
                );
                MostrarResultado(resultado, false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (RespostaAindaVale(geracao))
                {
                    MostrarResultado(
                        ResultadoItens.Falha(ApresentacaoItens.MensagemDeErro(null, false), null),
                        false
                    );
                }
            }
        }

        private async void AbrirBau()
        {
            if (_ocupado)
            {
                return;
            }

            var geracao = ++_geracao;
            _ocupado = true;
            BloquearJogo();
            _painel.MostrarCarregando("Baú");

            try
            {
                var resposta = await ClienteApi.Post<AbrirBauResponse>(
                    RotasItens.BauDe(_personagemId),
                    null
                );
                if (!RespostaAindaVale(geracao))
                {
                    return;
                }

                var resultado = ApresentacaoItens.InterpretarBau(
                    resposta.Sucesso,
                    resposta.CodigoErro,
                    resposta.Corpo
                );
                MostrarResultado(resultado, true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (RespostaAindaVale(geracao))
                {
                    MostrarResultado(
                        ResultadoItens.Falha(ApresentacaoItens.MensagemDeErro(null, true), null),
                        true
                    );
                }
            }
        }

        // Depois do await o painel pode ter sido fechado, o jogador pode ter saído do mundo ou o
        // Core pode ter voltado ao login (401): nesses casos não mexe em nada.
        private bool RespostaAindaVale(int geracao)
        {
            if (this == null || !isActiveAndEnabled || !EntradaNoJogo.NoJogo)
            {
                return false;
            }

            if (geracao != _geracao)
            {
                return false;
            }

            _ocupado = false;
            return true;
        }

        private void MostrarResultado(ResultadoItens resultado, bool abrindoBau)
        {
            if (resultado.Deu)
            {
                _ultimo = resultado;
                var titulo = ApresentacaoItens.TituloDoInventario(resultado.Inventario.Count);
                var aviso =
                    abrindoBau && resultado.Ganhos.Count == 0 ? "O baú estava vazio." : null;
                _painel.Mostrar(titulo, aviso, false, resultado.Ganhos, resultado.Inventario);
                return;
            }

            if (resultado.CodigoErro == CodigosErroCliente.Cancelada)
            {
                FecharPainel(true);
                return;
            }

            // Falha: mostra o aviso e mantém a última lista que o servidor mandou (nunca inventa).
            var anterior = _ultimo != null ? _ultimo.Inventario : null;
            var tituloAnterior =
                anterior != null ? ApresentacaoItens.TituloDoInventario(anterior.Count)
                : abrindoBau ? "Baú"
                : "Inventário";
            _painel.Mostrar(tituloAnterior, resultado.Aviso, true, null, anterior);
        }
    }
}
