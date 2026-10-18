using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Desenvolvimento;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Admin
{
    /// <summary>
    /// A tela de admin (F8): escolhe item, personagem, inimigo e inventário e manda a ação ao
    /// servidor, que só executa com a chave de desenvolvimento. Montada em código por cima de
    /// tudo; aberta, o personagem fica parado e o cursor livre, como no F10. Esc fecha.
    /// </summary>
    internal sealed class PainelDeAdmin
    {
        private const float OrdemDoPainel = 55f;
        private const float LarguraDoPainel = 620f;
        private static readonly string[] Abas = { "Item", "Personagem", "Mobs", "Inventário" };

        private readonly List<VisualElement> _secoes = new List<VisualElement>();
        private readonly List<Button> _botoes = new List<Button>();
        private readonly List<Button> _botoesDeAba = new List<Button>();

        private GameObject _objeto;
        private VisualElement _caixa;
        private Label _status;
        private Button _limpar;
        private IDisposable _assinatura;
        private bool _esperando;
        private float _esperaAte;
        private bool _limparArmado;

        private TipoEquipamento _tipo = TipoEquipamento.Espada;
        private List<int> _modelos = new List<int>();
        private int _modelo;
        private GrauEquipamento _grau = GrauEquipamento.Forte;
        private OrigemDoLoot _origem = OrigemDoLoot.Comum;
        private int _quantidade = 1;
        private int _especie = CatalogoCriaturas.JavaliDoCampo;
        private int _quantidadeDeInimigos = 1;
        private long _xp = 1000;
        private long _coroas = 1000;
        private long _essencias = 10;
        private float _x;
        private float _y;
        private float _z;

        public bool Aberto => _caixa != null && _caixa.style.display == DisplayStyle.Flex;

        public void Alternar()
        {
            if (Aberto)
            {
                Fechar();
                return;
            }

            if (JanelasDoJogo.AlgoAberto || BloqueioDeEntradaPorTexto.AtalhosDoJogoBloqueados())
            {
                return;
            }

            Montar();
            _caixa.style.display = DisplayStyle.Flex;
            PrenderJogo(true);
        }

        /// <summary>Todo quadro: Esc fecha, e a espera por resposta expira.</summary>
        public void Atualizar(bool escApertado)
        {
            if (escApertado && Aberto)
            {
                Fechar();
                return;
            }

            if (_esperando && Time.realtimeSinceStartup >= _esperaAte)
            {
                Mostrar(PedidosDeAdmin.SemResposta, false);
                DefinirEsperando(false);
            }
        }

        public void Destruir()
        {
            _assinatura?.Dispose();
            _assinatura = null;
            if (_objeto != null)
            {
                UnityEngine.Object.Destroy(_objeto);
            }
        }

        private void Fechar()
        {
            if (_caixa != null)
            {
                _caixa.style.display = DisplayStyle.None;
            }

            PrenderJogo(false);
        }

        private void Montar()
        {
            if (_caixa != null)
            {
                return;
            }

            _objeto = new GameObject("Desenvolvimento.Admin.Painel");
            UnityEngine.Object.DontDestroyOnLoad(_objeto);
            _objeto.SetActive(false);
            var documento = _objeto.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelAdmin",
                OrdemDoPainel
            );
            _objeto.SetActive(true);
            EstiloCore.Aplicar(documento.rootVisualElement);

            var raiz = new VisualElement { name = "desenvolvimento.admin" };
            raiz.AddToClassList(EstiloCore.RaizInterface);
            raiz.pickingMode = PickingMode.Ignore;
            raiz.style.alignItems = Align.Center;
            raiz.style.justifyContent = Justify.Center;
            documento.rootVisualElement.Add(raiz);

            _caixa = new VisualElement();
            _caixa.AddToClassList(EstiloCore.Painel);
            _caixa.style.width = LarguraDoPainel;
            _caixa.style.display = DisplayStyle.None;
            raiz.Add(_caixa);

            var titulo = new Label("Admin de teste (F8)");
            titulo.AddToClassList(EstiloCore.Titulo);
            _caixa.Add(titulo);
            _caixa.Add(MontarAbas());

            var corpo = new ScrollView(ScrollViewMode.Vertical);
            corpo.style.maxHeight = new Length(60, LengthUnit.Percent);
            _caixa.Add(corpo);
            AdicionarSecao(corpo, MontarItem());
            AdicionarSecao(corpo, MontarPersonagem());
            AdicionarSecao(corpo, MontarMobs());
            AdicionarSecao(corpo, MontarInventario());

            _status = new Label("Escolha uma ação.");
            _status.AddToClassList(EstiloCore.Mensagem);
            _caixa.Add(_status);

            var rodape = new VisualElement();
            rodape.AddToClassList(EstiloCore.LinhaBotoes);
            rodape.Add(Botao("Fechar", Fechar, false, false));
            _caixa.Add(rodape);

            MostrarAba(0);
            _assinatura = ConexaoTempoReal.Assinar<ResultadoDeAcaoDeAdmin>(
                DesenvolvimentoMessageIds.ResultadoDeAcaoDeAdmin,
                AoResultado
            );
        }

        private VisualElement MontarAbas()
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            for (var i = 0; i < Abas.Length; i++)
            {
                var indice = i;
                var botao = Botao(Abas[i], () => MostrarAba(indice), false, false);
                _botoesDeAba.Add(botao);
                linha.Add(botao);
            }

            return linha;
        }

        private void AdicionarSecao(VisualElement corpo, VisualElement secao)
        {
            _secoes.Add(secao);
            corpo.Add(secao);
        }

        private void MostrarAba(int indice)
        {
            for (var i = 0; i < _secoes.Count; i++)
            {
                _secoes[i].style.display = i == indice ? DisplayStyle.Flex : DisplayStyle.None;
                _botoesDeAba[i].EnableInClassList(EstiloCore.BotaoPrincipal, i == indice);
            }

            Desarmar();
        }

        private VisualElement MontarItem()
        {
            var secao = new VisualElement();
            var tipos = new List<string>();
            foreach (TipoEquipamento t in Enum.GetValues(typeof(TipoEquipamento)))
            {
                tipos.Add(t.ToString());
            }

            var modelo = new DropdownField("Modelo", new List<string>(), 0);
            void RecarregarModelos()
            {
                _modelos = RegrasAdmin.ModelosDoTipo(_tipo);
                var nomes = new List<string>();
                foreach (var id in _modelos)
                {
                    nomes.Add(PedidosDeAdmin.NomeDoModelo(id));
                }

                modelo.choices = nomes;
                _modelo = _modelos.Count > 0 ? _modelos[0] : 0;
                modelo.SetValueWithoutNotify(nomes.Count > 0 ? nomes[0] : "");
            }

            var tipo = new DropdownField("Tipo", tipos, _tipo.ToString());
            tipo.RegisterValueChangedCallback(e =>
            {
                _tipo = (TipoEquipamento)Enum.Parse(typeof(TipoEquipamento), e.newValue);
                RecarregarModelos();
            });
            modelo.RegisterValueChangedCallback(e =>
            {
                var indice = modelo.index;
                if (indice >= 0 && indice < _modelos.Count)
                {
                    _modelo = _modelos[indice];
                }
            });
            RecarregarModelos();

            var grau = Enumeracao("Grau", _grau, v => _grau = v);
            var origem = Enumeracao("Origem do inimigo", _origem, v => _origem = v);
            var quantidade = new IntegerField(
                $"Quantidade (1 a {ConfigAdmin.QuantidadeMaximaDeItens})"
            )
            {
                value = _quantidade,
            };
            quantidade.RegisterValueChangedCallback(e => _quantidade = e.newValue);

            secao.Add(tipo);
            secao.Add(modelo);
            secao.Add(grau);
            secao.Add(origem);
            secao.Add(quantidade);
            secao.Add(
                Acao(
                    "Gerar na bolsa",
                    () =>
                        PedidosDeAdmin.Item(
                            _modelo,
                            _grau,
                            _origem,
                            PedidosDeAdmin.LimitarQuantidade(
                                _quantidade,
                                ConfigAdmin.QuantidadeMaximaDeItens
                            )
                        ),
                    true
                )
            );
            return secao;
        }

        private VisualElement MontarPersonagem()
        {
            var secao = new VisualElement();
            secao.Add(Numero("Experiência (±)", _xp, v => _xp = v));
            secao.Add(
                Acao(
                    "Aplicar experiência",
                    () => PedidosDeAdmin.ComValor(AcaoDeAdmin.AjustarExperiencia, _xp),
                    false
                )
            );
            secao.Add(Numero("Coroas (±)", _coroas, v => _coroas = v));
            secao.Add(
                Acao(
                    "Aplicar coroas",
                    () => PedidosDeAdmin.ComValor(AcaoDeAdmin.AjustarCoroas, _coroas),
                    false
                )
            );
            secao.Add(
                Numero("Essências de dungeon e de raid (cada)", _essencias, v => _essencias = v)
            );
            secao.Add(
                Acao(
                    "Creditar essências",
                    () => PedidosDeAdmin.ComValor(AcaoDeAdmin.CreditarEssencias, _essencias),
                    false
                )
            );

            var x = new FloatField("X") { value = _x };
            x.RegisterValueChangedCallback(e => _x = e.newValue);
            var y = new FloatField("Y") { value = _y };
            y.RegisterValueChangedCallback(e => _y = e.newValue);
            var z = new FloatField("Z") { value = _z };
            z.RegisterValueChangedCallback(e => _z = e.newValue);
            secao.Add(x);
            secao.Add(y);
            secao.Add(z);
            secao.Add(Acao("Teleportar", () => PedidosDeAdmin.Teleporte(_x, _y, _z), false));
            return secao;
        }

        private VisualElement MontarMobs()
        {
            var secao = new VisualElement();
            var nomes = new List<string>();
            foreach (var criatura in CatalogoCriaturas.Todas)
            {
                nomes.Add($"{criatura.Nome} ({criatura.Id})");
            }

            var especie = new DropdownField("Espécie", nomes, 0);
            especie.RegisterValueChangedCallback(e =>
            {
                var indice = especie.index;
                if (indice >= 0 && indice < CatalogoCriaturas.Todas.Length)
                {
                    _especie = CatalogoCriaturas.Todas[indice].Id;
                }
            });
            var quantidade = new IntegerField(
                $"Quantidade (1 a {ConfigAdmin.QuantidadeMaximaDeInimigos})"
            )
            {
                value = _quantidadeDeInimigos,
            };
            quantidade.RegisterValueChangedCallback(e => _quantidadeDeInimigos = e.newValue);
            secao.Add(especie);
            secao.Add(quantidade);
            secao.Add(
                Acao(
                    "Spawnar perto",
                    () =>
                        PedidosDeAdmin.Inimigo(
                            _especie,
                            PedidosDeAdmin.LimitarQuantidade(
                                _quantidadeDeInimigos,
                                ConfigAdmin.QuantidadeMaximaDeInimigos
                            )
                        ),
                    true
                )
            );
            secao.Add(
                Acao(
                    "Matar inimigos da área",
                    () => PedidosDeAdmin.Simples(AcaoDeAdmin.MatarInimigosDaArea),
                    false
                )
            );
            return secao;
        }

        private VisualElement MontarInventario()
        {
            var secao = new VisualElement();
            secao.Add(
                Acao(
                    "Equipar último item gerado",
                    () => PedidosDeAdmin.Simples(AcaoDeAdmin.EquiparUltimoItem),
                    true
                )
            );
            secao.Add(
                Acao(
                    "Quebrar equipados",
                    () => PedidosDeAdmin.Simples(AcaoDeAdmin.QuebrarEquipados),
                    false
                )
            );
            secao.Add(
                Acao(
                    "Consertar equipados",
                    () => PedidosDeAdmin.Simples(AcaoDeAdmin.ConsertarEquipados),
                    false
                )
            );
            _limpar = Botao("Limpar bolsa (menos favoritos)", LimparBolsa, false, true);
            secao.Add(_limpar);
            return secao;
        }

        // Apagar a bolsa pede dois cliques: o primeiro arma, o segundo envia.
        private void LimparBolsa()
        {
            if (!_limparArmado)
            {
                _limparArmado = true;
                _limpar.text = "Clique de novo para apagar a bolsa";
                return;
            }

            Desarmar();
            Enviar(PedidosDeAdmin.Simples(AcaoDeAdmin.LimparBolsa));
        }

        private void Desarmar()
        {
            _limparArmado = false;
            if (_limpar != null)
            {
                _limpar.text = "Limpar bolsa (menos favoritos)";
            }
        }

        private void Enviar(PedirAcaoDeAdmin pedido)
        {
            if (_esperando)
            {
                return;
            }

            if (!ConexaoTempoReal.Conectado)
            {
                Mostrar(PedidosDeAdmin.SemConexao, false);
                return;
            }

            var recusa = RegrasAdmin.Validar(pedido);
            if (recusa != null)
            {
                Mostrar(recusa, false);
                return;
            }

            ConexaoTempoReal.Enviar(DesenvolvimentoMessageIds.PedirAcaoDeAdmin, pedido);
            DefinirEsperando(true);
            _status.text = "Enviado, esperando o servidor...";
        }

        private void AoResultado(ResultadoDeAcaoDeAdmin resultado)
        {
            if (resultado == null)
            {
                return;
            }

            var texto = PedidosDeAdmin.TextoDoResultado(resultado);
            Mostrar(texto, resultado.Aceito);
            ChatDoJogo.AdicionarLinhaSistema?.Invoke(texto);
            DefinirEsperando(false);
        }

        private void DefinirEsperando(bool esperando)
        {
            _esperando = esperando;
            _esperaAte = Time.realtimeSinceStartup + PedidosDeAdmin.EsperaMaximaSegundos;
            foreach (var botao in _botoes)
            {
                botao.SetEnabled(!esperando);
            }
        }

        private void Mostrar(string texto, bool aceito)
        {
            if (_status == null)
            {
                return;
            }

            _status.text = texto;
            _status.EnableInClassList(EstiloCore.Mensagem, aceito);
            _status.EnableInClassList(EstiloCore.MensagemErro, !aceito);
        }

        private Button Acao(string texto, Func<PedirAcaoDeAdmin> montar, bool principal) =>
            Botao(texto, () => Enviar(montar()), principal, true);

        private Button Botao(string texto, Action aoClicar, bool principal, bool bloqueiaNaEspera)
        {
            var botao = new Button(aoClicar) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            if (principal)
            {
                botao.AddToClassList(EstiloCore.BotaoPrincipal);
            }

            if (bloqueiaNaEspera)
            {
                _botoes.Add(botao);
            }

            return botao;
        }

        private static VisualElement Numero(string rotulo, long inicial, Action<long> aoMudar)
        {
            var campo = new LongField(rotulo) { value = inicial };
            campo.RegisterValueChangedCallback(e => aoMudar(e.newValue));
            return campo;
        }

        private static DropdownField Enumeracao<T>(string rotulo, T inicial, Action<T> aoMudar)
            where T : struct, Enum
        {
            var nomes = new List<string>(Enum.GetNames(typeof(T)));
            var campo = new DropdownField(rotulo, nomes, inicial.ToString());
            campo.RegisterValueChangedCallback(e => aoMudar((T)Enum.Parse(typeof(T), e.newValue)));
            return campo;
        }

        // Com o painel aberto o personagem e a câmera param e o cursor fica livre, como no F10.
        private static void PrenderJogo(bool painelAberto)
        {
            var jogador = EntradaNoJogo.Jogador;
            var controle = jogador != null ? jogador.GetComponent<ControleTerceiraPessoa>() : null;
            if (controle != null)
            {
                controle.InputAtivo = !painelAberto;
            }

            var camera = Camera.main;
            var controleDaCamera =
                camera != null ? camera.GetComponent<CameraTerceiraPessoa>() : null;
            if (controleDaCamera != null)
            {
                controleDaCamera.InputAtivo = !painelAberto;
            }

            if (painelAberto)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            else if (!JanelasDoJogo.AlgoAberto)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }
    }
}
