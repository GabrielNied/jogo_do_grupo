using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Menu do Esc no mundo: sobreposto ao jogo (sem abrir uma <see cref="Tela"/>), com
    /// "Continuar", "Opções", "Voltar à seleção" (<see cref="EntradaNoJogo.Sair"/>),
    /// "Sair da conta" e "Sair do jogo".
    /// </summary>
    internal sealed class MenuJogo
    {
        private const string NomeElemento = "core.menu-jogo";

        private readonly Action _aoContinuar;
        private VisualElement _raiz;
        private VisualElement _conteudoOpcoes;
        private Button _abaAudio;
        private Button _abaVideo;
        private Button _abaJogo;
        private Button _abaTeclas;
        private string _capturando;
        private int _capturaCanceladaNoFrame = -1;
        private Label _avisoTeclas;
        private Button _aplicarTela;
        private List<Vector2Int> _resolucoes;
        private Vector2Int _resolucaoPendente;
        private bool _telaCheiaPendente;
        private bool _opcoesAbertas;

        private enum AbaOpcoes
        {
            Audio,
            Video,
            Jogo,
            Teclas,
        }

        public MenuJogo(Action aoContinuar)
        {
            _aoContinuar = aoContinuar;
        }

        public bool Aberto => _raiz != null;

        public bool OpcoesAbertas => _opcoesAbertas;

        public bool CapturaAtiva => _capturando != null;

        public bool CancelouCapturaNesteFrame => _capturaCanceladaNoFrame == Time.frameCount;

        public void Abrir()
        {
            var container = Navegacao.Container;
            if (Aberto || container == null)
            {
                return;
            }

            _raiz = new VisualElement { name = NomeElemento };
            _raiz.AddToClassList(EstiloCore.Tela);
            _raiz.AddToClassList(EstiloCore.TelaSobreJogo);

            var painel = EstiloCore.CriarPainel();

            var titulo = new Label("Menu do jogo");
            titulo.AddToClassList(EstiloCore.Titulo);

            var aviso = new Label("O mundo continua ativo enquanto este menu está aberto.");
            aviso.AddToClassList(EstiloCore.Texto);
            aviso.AddToClassList("menu-jogo-aviso");

            var continuar = new Button(() => _aoContinuar?.Invoke()) { text = "Continuar" };
            continuar.AddToClassList(EstiloCore.Botao);
            continuar.AddToClassList(EstiloCore.BotaoPrincipal);
            continuar.AddToClassList("menu-jogo-acao");

            var opcoes = new Button(MostrarOpcoes) { text = "Opções" };
            opcoes.AddToClassList(EstiloCore.Botao);
            opcoes.AddToClassList("menu-jogo-acao");

            var voltar = new Button(EntradaNoJogo.Sair) { text = "Voltar à seleção" };
            voltar.AddToClassList(EstiloCore.Botao);
            voltar.AddToClassList("menu-jogo-acao");

            var sairConta = new Button(SairDaConta) { text = "Sair da conta" };
            sairConta.AddToClassList(EstiloCore.Botao);
            sairConta.AddToClassList("menu-jogo-acao");

            var sairJogo = new Button(SairDoJogo) { text = "Sair do jogo" };
            sairJogo.AddToClassList(EstiloCore.Botao);
            sairJogo.AddToClassList("menu-jogo-acao");

            var saidas = new VisualElement();
            saidas.AddToClassList("menu-jogo-saidas");
            var tituloSaidas = new Label("Sair");
            tituloSaidas.AddToClassList(EstiloCore.Subtitulo);
            saidas.Add(tituloSaidas);
            saidas.Add(voltar);
            saidas.Add(sairConta);
            saidas.Add(sairJogo);

            var dica = new Label("Esc fecha este menu");
            dica.AddToClassList(EstiloCore.Mensagem);
            dica.AddToClassList("menu-jogo-dica");

            painel.Add(titulo);
            painel.Add(aviso);
            painel.Add(continuar);
            painel.Add(opcoes);
            painel.Add(saidas);
            painel.Add(dica);
            _raiz.Add(painel);
            container.Add(_raiz);
            continuar.schedule.Execute(() => continuar.Focus());
        }

        private void MostrarOpcoes()
        {
            if (_raiz == null)
            {
                return;
            }

            _opcoesAbertas = true;
            _telaCheiaPendente = OpcoesLocais.Atual.TelaCheia;
            _resolucaoPendente = new Vector2Int(
                OpcoesLocais.Atual.Largura,
                OpcoesLocais.Atual.Altura
            );
            _raiz.Clear();
            var painel = EstiloCore.CriarPainel();
            painel.AddToClassList("opcoes-painel");
            var titulo = new Label("Opções");
            titulo.AddToClassList(EstiloCore.Titulo);
            painel.Add(titulo);

            var abas = new VisualElement();
            abas.AddToClassList("opcoes-abas");
            _abaAudio = CriarAba("Áudio", AbaOpcoes.Audio);
            _abaVideo = CriarAba("Vídeo", AbaOpcoes.Video);
            _abaJogo = CriarAba("Jogo", AbaOpcoes.Jogo);
            _abaTeclas = CriarAba("Teclas", AbaOpcoes.Teclas);
            abas.Add(_abaAudio);
            abas.Add(_abaVideo);
            abas.Add(_abaJogo);
            abas.Add(_abaTeclas);
            painel.Add(abas);

            _conteudoOpcoes = new ScrollView();
            _conteudoOpcoes.AddToClassList("opcoes-conteudo");
            painel.Add(_conteudoOpcoes);
            _raiz.RegisterCallback<KeyDownEvent>(AoCapturarTecla);
            _raiz.RegisterCallback<PointerDownEvent>(AoCapturarMouse);

            var voltar = new Button(VoltarDoSubmenu) { text = "Voltar ao menu" };
            voltar.AddToClassList(EstiloCore.Botao);
            voltar.AddToClassList("opcoes-voltar");
            painel.Add(voltar);
            _raiz.Add(painel);
            MostrarAba(AbaOpcoes.Audio);
            _abaAudio.schedule.Execute(() => _abaAudio.Focus());
        }

        private Button CriarAba(string nome, AbaOpcoes aba)
        {
            var botao = new Button(() => MostrarAba(aba)) { text = nome };
            botao.AddToClassList(EstiloCore.Botao);
            botao.AddToClassList("opcoes-aba");
            return botao;
        }

        private void MostrarAba(AbaOpcoes aba)
        {
            _capturando = null;
            _abaAudio.EnableInClassList("opcoes-aba-ativa", aba == AbaOpcoes.Audio);
            _abaVideo.EnableInClassList("opcoes-aba-ativa", aba == AbaOpcoes.Video);
            _abaJogo.EnableInClassList("opcoes-aba-ativa", aba == AbaOpcoes.Jogo);
            _abaTeclas.EnableInClassList("opcoes-aba-ativa", aba == AbaOpcoes.Teclas);
            _conteudoOpcoes.Clear();
            switch (aba)
            {
                case AbaOpcoes.Audio:
                    MontarAudio();
                    break;
                case AbaOpcoes.Video:
                    MontarVideo();
                    break;
                case AbaOpcoes.Jogo:
                    MontarJogo();
                    break;
                case AbaOpcoes.Teclas:
                    MontarTeclas();
                    break;
            }
        }

        private void MontarTeclas()
        {
            var subtitulo = new Label("Controles do mundo");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            _conteudoOpcoes.Add(subtitulo);
            CriarLinhaTecla("Andar para frente", TeclasDoJogo.Frente);
            CriarLinhaTecla("Andar para trás", TeclasDoJogo.Tras);
            CriarLinhaTecla("Andar à esquerda", TeclasDoJogo.Esquerda);
            CriarLinhaTecla("Andar à direita", TeclasDoJogo.Direita);
            CriarLinhaTecla("Andar devagar", TeclasDoJogo.AndarDevagar);
            CriarLinhaTecla("Pular", TeclasDoJogo.Pular);
            CriarLinhaTecla("Esquiva", TeclasDoJogo.Esquiva);
            CriarLinhaTecla("Ataque leve / pesado", TeclasDoJogo.Ataque);
            CriarLinhaTecla("Bloqueio", TeclasDoJogo.Defesa);
            CriarLinhaTecla("Interagir", TeclasDoJogo.Interagir);
            CriarLinhaTecla("Pegar item", TeclasDoJogo.Coletar);
            CriarLinhaTecla("Esconder HUD", TeclasDoJogo.EsconderHud);

            var janelas = new Label("Janelas");
            janelas.AddToClassList(EstiloCore.Subtitulo);
            _conteudoOpcoes.Add(janelas);
            foreach (var definicao in JanelasDoJogo.Registro.Definicoes)
            {
                if (!RegistroDeJanelas.TeclaValida(definicao.Tecla))
                {
                    continue;
                }

                CriarLinhaTecla(definicao.Titulo, TeclasDoJogo.Janela(definicao.Chave));
            }

            _avisoTeclas = new Label("Clique em Trocar e aperte uma tecla. Esc cancela.");
            _avisoTeclas.AddToClassList(EstiloCore.Mensagem);
            _conteudoOpcoes.Add(_avisoTeclas);
            Button restaurar = null;
            restaurar = new Button(() =>
            {
                if (restaurar.text == "Restaurar padrão")
                {
                    restaurar.text = "Confirmar restauração";
                    _avisoTeclas.text = "Clique novamente para restaurar todas as teclas.";
                    return;
                }
                TeclasDoJogo.RestaurarTudo();
                OpcoesGravadas.Salvar(GrupoDeOpcoes.Teclas);
                MontarAbaTeclasNovamente();
            })
            {
                text = "Restaurar padrão",
            };
            restaurar.AddToClassList(EstiloCore.Botao);
            _conteudoOpcoes.Add(restaurar);
        }

        private void CriarLinhaTecla(string nome, string id)
        {
            var linha = new VisualElement();
            linha.AddToClassList("opcoes-tecla-linha");
            var rotulo = new Label(nome);
            rotulo.AddToClassList("opcoes-tecla-nome");
            var atual = new Label(TeclasDoJogo.Rotulo(id));
            atual.AddToClassList("opcoes-tecla-atual");
            Button trocar = null;
            trocar = new Button(() =>
            {
                _capturando = id;
                _avisoTeclas.text = "Aperte uma tecla para " + nome + " · Esc cancela";
                trocar.Focus();
            })
            {
                text = "Trocar",
            };
            trocar.AddToClassList(EstiloCore.Botao);
            var padrao = new Button(() =>
            {
                TeclasDoJogo.Restaurar(id);
                OpcoesGravadas.Salvar(GrupoDeOpcoes.Teclas);
                MontarAbaTeclasNovamente();
            })
            {
                text = "Padrão",
            };
            padrao.AddToClassList(EstiloCore.Botao);
            linha.Add(rotulo);
            linha.Add(atual);
            linha.Add(trocar);
            linha.Add(padrao);
            _conteudoOpcoes.Add(linha);
        }

        private void MontarAbaTeclasNovamente()
        {
            _capturando = null;
            _conteudoOpcoes.Clear();
            MontarTeclas();
        }

        private void AoCapturarTecla(KeyDownEvent evt)
        {
            if (_capturando == null)
                return;
            evt.StopImmediatePropagation();
            if (evt.keyCode == KeyCode.Escape)
            {
                _capturaCanceladaNoFrame = Time.frameCount;
                _capturando = null;
                _avisoTeclas.text = "Troca cancelada.";
                return;
            }
            var caminho = CaminhoDaTecla(evt.keyCode);
            AplicarTecla(caminho);
        }

        private void AoCapturarMouse(PointerDownEvent evt)
        {
            if (_capturando == null)
                return;
            evt.StopImmediatePropagation();
            var caminho = evt.button switch
            {
                0 => "<Mouse>/leftButton",
                1 => "<Mouse>/rightButton",
                2 => "<Mouse>/middleButton",
                _ => null,
            };
            AplicarTecla(caminho);
        }

        private void AplicarTecla(string caminho)
        {
            if (TeclasDoJogo.TentarTrocar(_capturando, caminho, out var aviso))
            {
                OpcoesGravadas.Salvar(GrupoDeOpcoes.Teclas);
                MontarAbaTeclasNovamente();
                _avisoTeclas.text = aviso ?? "Tecla atualizada.";
            }
            else
                _avisoTeclas.text = aviso;
        }

        private static string CaminhoDaTecla(KeyCode tecla)
        {
            if (tecla >= KeyCode.A && tecla <= KeyCode.Z)
                return "<Keyboard>/" + tecla.ToString().ToLowerInvariant();
            if (tecla >= KeyCode.Alpha0 && tecla <= KeyCode.Alpha9)
                return "<Keyboard>/" + (int)(tecla - KeyCode.Alpha0);
            switch (tecla)
            {
                case KeyCode.Space:
                    return "<Keyboard>/space";
                case KeyCode.LeftShift:
                    return "<Keyboard>/leftShift";
                case KeyCode.RightShift:
                    return "<Keyboard>/rightShift";
                case KeyCode.LeftAlt:
                    return "<Keyboard>/leftAlt";
                case KeyCode.RightAlt:
                    return "<Keyboard>/rightAlt";
                case KeyCode.LeftControl:
                    return "<Keyboard>/leftCtrl";
                case KeyCode.RightControl:
                    return "<Keyboard>/rightCtrl";
                case KeyCode.UpArrow:
                    return "<Keyboard>/upArrow";
                case KeyCode.DownArrow:
                    return "<Keyboard>/downArrow";
                case KeyCode.LeftArrow:
                    return "<Keyboard>/leftArrow";
                case KeyCode.RightArrow:
                    return "<Keyboard>/rightArrow";
                case KeyCode.Tab:
                    return "<Keyboard>/tab";
                default:
                    return null;
            }
        }

        private void MontarAudio()
        {
            _conteudoOpcoes.Add(
                CriarVolume(
                    "Volume geral",
                    OpcoesLocais.Atual.VolumeGeral,
                    valor =>
                    {
                        OpcoesLocais.Atual.DefinirVolumeGeral(valor);
                        OpcoesGravadas.Salvar(GrupoDeOpcoes.Audio);
                    }
                )
            );
            _conteudoOpcoes.Add(
                CriarVolume(
                    "Música",
                    OpcoesLocais.Atual.VolumeMusica,
                    valor =>
                    {
                        OpcoesLocais.Atual.DefinirVolumeMusica(valor);
                        OpcoesGravadas.Salvar(GrupoDeOpcoes.Audio);
                    }
                )
            );
            _conteudoOpcoes.Add(
                CriarVolume(
                    "Efeitos",
                    OpcoesLocais.Atual.VolumeEfeitos,
                    valor =>
                    {
                        OpcoesLocais.Atual.DefinirVolumeEfeitos(valor);
                        OpcoesGravadas.Salvar(GrupoDeOpcoes.Audio);
                    }
                )
            );
        }

        private static VisualElement CriarVolume(string nome, int valor, Action<int> definir)
        {
            var linha = new VisualElement();
            linha.AddToClassList("opcoes-linha");
            var controle = new SliderInt(nome, 0, 100) { value = valor };
            controle.AddToClassList("opcoes-slider");
            var numero = new Label(valor + "%");
            numero.AddToClassList("opcoes-valor");
            controle.RegisterValueChangedCallback(evt =>
            {
                var ajustado = OpcoesLocais.LimitarVolume(evt.newValue);
                controle.SetValueWithoutNotify(ajustado);
                numero.text = ajustado + "%";
                definir(ajustado);
            });
            linha.Add(controle);
            linha.Add(numero);
            return linha;
        }

        private void MontarVideo()
        {
            var qualidades = new List<string> { "Baixa", "Média", "Alta" };
            var qualidade = new DropdownField("Qualidade")
            {
                choices = qualidades,
                value = OpcoesLocais.Atual.Qualidade,
            };
            qualidade.AddToClassList("opcoes-campo");
            qualidade.RegisterValueChangedCallback(evt =>
            {
                OpcoesLocais.Atual.DefinirQualidade(evt.newValue);
                OpcoesGravadas.Salvar(GrupoDeOpcoes.Qualidade);
            });
            _conteudoOpcoes.Add(qualidade);

            var telaCheia = new Toggle("Tela cheia") { value = _telaCheiaPendente };
            telaCheia.AddToClassList("opcoes-campo");
            telaCheia.RegisterValueChangedCallback(evt =>
            {
                _telaCheiaPendente = evt.newValue;
                AtualizarAplicarTela();
            });
            _conteudoOpcoes.Add(telaCheia);

            var disponiveis = new List<Vector2Int>();
            var catalogo = Screen.resolutions;
            if (catalogo != null)
            {
                foreach (var item in catalogo)
                {
                    disponiveis.Add(new Vector2Int(item.width, item.height));
                }
            }
            _resolucoes = ResolucoesSemDuplicata(disponiveis);
            if (_resolucaoPendente != Vector2Int.zero && !_resolucoes.Contains(_resolucaoPendente))
            {
                _resolucaoPendente = Vector2Int.zero;
            }
            var escolhas = new List<string> { "A do monitor" };
            var indiceAtual = 0;
            for (var i = 0; i < _resolucoes.Count; i++)
            {
                var tamanho = _resolucoes[i];
                escolhas.Add(tamanho.x + " × " + tamanho.y);
                if (tamanho == _resolucaoPendente)
                {
                    indiceAtual = i + 1;
                }
            }

            var resolucao = new DropdownField("Resolução")
            {
                choices = escolhas,
                value = escolhas[indiceAtual],
            };
            resolucao.AddToClassList("opcoes-campo");
            resolucao.RegisterValueChangedCallback(evt =>
            {
                var indice = escolhas.IndexOf(evt.newValue);
                _resolucaoPendente = indice > 0 ? _resolucoes[indice - 1] : Vector2Int.zero;
                AtualizarAplicarTela();
            });
            _conteudoOpcoes.Add(resolucao);

            _aplicarTela = new Button(() =>
            {
                OpcoesLocais.Atual.DefinirTelaCheia(_telaCheiaPendente);
                OpcoesLocais.Atual.DefinirResolucao(_resolucaoPendente.x, _resolucaoPendente.y);
                OpcoesGravadas.Salvar(GrupoDeOpcoes.Tela);
                AtualizarAplicarTela();
            })
            {
                text = "Aplicar vídeo",
            };
            _aplicarTela.AddToClassList(EstiloCore.Botao);
            _aplicarTela.AddToClassList(EstiloCore.BotaoPrincipal);
            _aplicarTela.AddToClassList("opcoes-aplicar");
            _conteudoOpcoes.Add(_aplicarTela);
            AtualizarAplicarTela();
        }

        private void AtualizarAplicarTela()
        {
            _aplicarTela?.SetEnabled(
                _telaCheiaPendente != OpcoesLocais.Atual.TelaCheia
                    || _resolucaoPendente.x != OpcoesLocais.Atual.Largura
                    || _resolucaoPendente.y != OpcoesLocais.Atual.Altura
            );
        }

        private void MontarJogo()
        {
            var linha = new VisualElement();
            linha.AddToClassList("opcoes-linha");
            var sensibilidade = new Slider("Sensibilidade da câmera", 0.5f, 2f)
            {
                value = OpcoesLocais.Atual.Sensibilidade,
            };
            sensibilidade.AddToClassList("opcoes-slider");
            var numero = new Label(OpcoesLocais.Atual.Sensibilidade.ToString("0.0") + "×");
            numero.AddToClassList("opcoes-valor");
            sensibilidade.RegisterValueChangedCallback(evt =>
            {
                var ajustado = OpcoesLocais.LimitarSensibilidade(evt.newValue);
                sensibilidade.SetValueWithoutNotify(ajustado);
                numero.text = ajustado.ToString("0.0") + "×";
                OpcoesLocais.Atual.DefinirSensibilidade(ajustado);
                OpcoesGravadas.Salvar(GrupoDeOpcoes.Jogo);
            });
            linha.Add(sensibilidade);
            linha.Add(numero);
            _conteudoOpcoes.Add(linha);

            var idioma = new DropdownField("Idioma")
            {
                choices = new List<string> { OpcoesLocais.IdiomaPadrao },
                value = OpcoesLocais.IdiomaPadrao,
            };
            idioma.AddToClassList("opcoes-campo");
            idioma.SetEnabled(false);
            _conteudoOpcoes.Add(idioma);
        }

        internal static List<Vector2Int> ResolucoesSemDuplicata(IEnumerable<Vector2Int> resolucoes)
        {
            var unicas = new List<Vector2Int>();
            var vistas = new HashSet<Vector2Int>();
            if (resolucoes == null)
            {
                return unicas;
            }

            foreach (var tamanho in resolucoes)
            {
                if (tamanho.x > 0 && tamanho.y > 0 && vistas.Add(tamanho))
                {
                    unicas.Add(tamanho);
                }
            }

            return unicas;
        }

        public void VoltarDoSubmenu()
        {
            if (!_opcoesAbertas)
            {
                return;
            }

            Fechar();
            Abrir();
        }

        private static void SairDoJogo()
        {
            UnityEngine.Application.Quit();
        }

        private static void SairDaConta()
        {
            EntradaNoJogo.Desmontar();
            Navegacao.VoltarParaLogin();
        }

        public void Fechar()
        {
            _capturando = null;
            _raiz?.RemoveFromHierarchy();
            _raiz = null;
            _opcoesAbertas = false;
            _conteudoOpcoes = null;
            _aplicarTela = null;
        }
    }
}
