using System;
using JogoDoGrupo.Client.Core.UI;
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

        public MenuJogo(Action aoContinuar)
        {
            _aoContinuar = aoContinuar;
        }

        public bool Aberto => _raiz != null;

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

            var titulo = new Label("Menu");
            titulo.AddToClassList(EstiloCore.Titulo);

            var continuar = new Button(() => _aoContinuar?.Invoke()) { text = "Continuar" };
            continuar.AddToClassList(EstiloCore.Botao);
            continuar.AddToClassList(EstiloCore.BotaoPrincipal);

            var opcoes = new Button(MostrarOpcoes) { text = "Opções" };
            opcoes.AddToClassList(EstiloCore.Botao);

            var voltar = new Button(EntradaNoJogo.Sair) { text = "Voltar à seleção" };
            voltar.AddToClassList(EstiloCore.Botao);

            var sairConta = new Button(SairDaConta) { text = "Sair da conta" };
            sairConta.AddToClassList(EstiloCore.Botao);

            var sairJogo = new Button(SairDoJogo) { text = "Sair do jogo" };
            sairJogo.AddToClassList(EstiloCore.Botao);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            linha.Add(continuar);
            linha.Add(opcoes);

            painel.Add(titulo);
            painel.Add(linha);
            painel.Add(voltar);
            painel.Add(sairConta);
            painel.Add(sairJogo);
            _raiz.Add(painel);
            container.Add(_raiz);
        }

        private void MostrarOpcoes()
        {
            if (_raiz == null)
            {
                return;
            }

            _raiz.Clear();
            var painel = EstiloCore.CriarPainel();
            var titulo = new Label("Opções");
            titulo.AddToClassList(EstiloCore.Titulo);
            painel.Add(titulo);
            painel.Add(
                LinhaVolume(
                    "Volume geral",
                    OpcoesLocais.Atual.VolumeGeral,
                    valor =>
                    {
                        OpcoesLocais.Atual.DefinirVolumeGeral(valor);
                        OpcoesGravadas.Salvar();
                        ReabrirOpcoes();
                    }
                )
            );
            painel.Add(
                LinhaVolume(
                    "Música",
                    OpcoesLocais.Atual.VolumeMusica,
                    valor =>
                    {
                        OpcoesLocais.Atual.DefinirVolumeMusica(valor);
                        OpcoesGravadas.Salvar();
                        ReabrirOpcoes();
                    }
                )
            );
            painel.Add(
                LinhaVolume(
                    "Efeitos",
                    OpcoesLocais.Atual.VolumeEfeitos,
                    valor =>
                    {
                        OpcoesLocais.Atual.DefinirVolumeEfeitos(valor);
                        OpcoesGravadas.Salvar();
                        ReabrirOpcoes();
                    }
                )
            );
            var qualidade = new Button(() =>
            {
                var atual = OpcoesLocais.Atual.Qualidade;
                OpcoesLocais.Atual.DefinirQualidade(
                    atual == "Baixa" ? "Média"
                    : atual == "Média" ? "Alta"
                    : "Baixa"
                );
                OpcoesGravadas.Salvar();
                ReabrirOpcoes();
            })
            {
                text = "Qualidade: " + OpcoesLocais.Atual.Qualidade,
            };
            qualidade.AddToClassList(EstiloCore.Botao);
            painel.Add(qualidade);
            var tela = new Button(() =>
            {
                OpcoesLocais.Atual.DefinirTelaCheia(!OpcoesLocais.Atual.TelaCheia);
                OpcoesGravadas.Salvar();
                ReabrirOpcoes();
            })
            {
                text = OpcoesLocais.Atual.TelaCheia ? "Tela cheia: sim" : "Tela cheia: não",
            };
            tela.AddToClassList(EstiloCore.Botao);
            painel.Add(tela);
            var resolucao = new Button(CiclarResolucao) { text = TextoDaResolucao() };
            resolucao.AddToClassList(EstiloCore.Botao);
            painel.Add(resolucao);
            var sensibilidade = new Label(
                "Sensibilidade da câmera: " + OpcoesLocais.Atual.Sensibilidade.ToString("0.0")
            );
            painel.Add(sensibilidade);
            painel.Add(LinhaSensibilidade());
            var idioma = new Label("Idioma: " + OpcoesLocais.Atual.Idioma);
            painel.Add(idioma);
            var voltar = new Button(() =>
            {
                Fechar();
                Abrir();
            })
            {
                text = "Voltar",
            };
            voltar.AddToClassList(EstiloCore.Botao);
            voltar.AddToClassList(EstiloCore.BotaoPrincipal);
            painel.Add(voltar);
            _raiz.Add(painel);
        }

        private static VisualElement LinhaVolume(string nome, int atual, System.Action<int> definir)
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            var menos = new Button(() => definir(atual - 5)) { text = nome + " " + atual + " −" };
            menos.AddToClassList(EstiloCore.Botao);
            var mais = new Button(() => definir(atual + 5)) { text = "+" };
            mais.AddToClassList(EstiloCore.Botao);
            linha.Add(menos);
            linha.Add(mais);
            return linha;
        }

        private VisualElement LinhaSensibilidade()
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            var menos = new Button(() =>
            {
                OpcoesLocais.Atual.DefinirSensibilidade(OpcoesLocais.Atual.Sensibilidade - 0.1f);
                OpcoesGravadas.Salvar();
                ReabrirOpcoes();
            })
            {
                text = "−",
            };
            menos.AddToClassList(EstiloCore.Botao);
            var mais = new Button(() =>
            {
                OpcoesLocais.Atual.DefinirSensibilidade(OpcoesLocais.Atual.Sensibilidade + 0.1f);
                OpcoesGravadas.Salvar();
                ReabrirOpcoes();
            })
            {
                text = "+",
            };
            mais.AddToClassList(EstiloCore.Botao);
            linha.Add(menos);
            linha.Add(mais);
            return linha;
        }

        private void CiclarResolucao()
        {
            var lista = UnityEngine.Screen.resolutions;
            if (lista == null || lista.Length == 0)
            {
                OpcoesLocais.Atual.DefinirResolucao(0, 0);
            }
            else
            {
                var indice = 0;
                for (var i = 0; i < lista.Length; i++)
                {
                    if (
                        lista[i].width == OpcoesLocais.Atual.Largura
                        && lista[i].height == OpcoesLocais.Atual.Altura
                    )
                    {
                        indice = (i + 1) % lista.Length;
                        break;
                    }
                }

                if (OpcoesLocais.Atual.Largura <= 0)
                {
                    indice = 0;
                }

                OpcoesLocais.Atual.DefinirResolucao(lista[indice].width, lista[indice].height);
            }

            OpcoesGravadas.Salvar();
            ReabrirOpcoes();
        }

        private static string TextoDaResolucao()
        {
            if (OpcoesLocais.Atual.Largura <= 0)
            {
                return "Resolução: a do monitor";
            }

            return "Resolução: " + OpcoesLocais.Atual.Largura + "×" + OpcoesLocais.Atual.Altura;
        }

        private void ReabrirOpcoes()
        {
            Fechar();
            Abrir();
            MostrarOpcoes();
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
            _raiz?.RemoveFromHierarchy();
            _raiz = null;
        }
    }
}
