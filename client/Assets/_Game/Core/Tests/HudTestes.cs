using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// A moldura do HUD: as regiões que a <see cref="EntradaNoJogo"/> cria ao entrar no mundo
    /// e remove ao sair, e a convivência com o menu do Esc.
    /// </summary>
    public class HudTestes
    {
        private VisualElement _containerAnterior;
        private VisualElement _container;
        private double _agora;

        [SetUp]
        public void Preparar()
        {
            _containerAnterior = Navegacao.Container;
            _container = new VisualElement();
            Navegacao.Inicializar(_container);
            _agora = 100.0;
        }

        [TearDown]
        public void Limpar()
        {
            Hud.Remover();
            Navegacao.Inicializar(_containerAnterior);
        }

        private void Montar()
        {
            Hud.Montar(_container, () => _agora);
        }

        [Test]
        public void ForaDoMundo_NaoHaRegioes()
        {
            Assert.IsFalse(Hud.Montado);
            foreach (RegiaoDoHud regiao in Enum.GetValues(typeof(RegiaoDoHud)))
            {
                Assert.IsNull(Hud.Regiao(regiao), regiao.ToString());
            }
        }

        [Test]
        public void Montar_CriaTodasAsRegioesNoContainer_SemReceberClique()
        {
            Montar();

            Assert.IsTrue(Hud.Montado);
            Assert.AreSame(_container, Hud.Raiz.parent);
            Assert.IsTrue(Hud.Raiz.ClassListContains(EstiloCore.Hud));
            Assert.AreEqual(PickingMode.Ignore, Hud.Raiz.pickingMode);
            foreach (RegiaoDoHud regiao in Enum.GetValues(typeof(RegiaoDoHud)))
            {
                var elemento = Hud.Regiao(regiao);
                Assert.IsNotNull(elemento, regiao.ToString());
                Assert.AreSame(Hud.Raiz, elemento.parent);
                Assert.AreEqual(Hud.NomeDaRegiao(regiao), elemento.name);
                Assert.IsTrue(elemento.ClassListContains(EstiloCore.HudRegiao));
                Assert.IsTrue(elemento.ClassListContains(Hud.ClasseDaRegiao(regiao)));
                Assert.AreEqual(PickingMode.Ignore, elemento.pickingMode, regiao.ToString());
            }
        }

        [Test]
        public void CadaRegiaoTemClassePropria()
        {
            var vistas = new System.Collections.Generic.HashSet<string>();
            foreach (RegiaoDoHud regiao in Enum.GetValues(typeof(RegiaoDoHud)))
            {
                Assert.IsTrue(vistas.Add(Hud.ClasseDaRegiao(regiao)), regiao.ToString());
            }
        }

        [Test]
        public void Montar_FicaAtrasDoQueJaEstavaNaTela()
        {
            var janela = new VisualElement();
            _container.Add(janela);

            Montar();

            Assert.AreEqual(0, _container.IndexOf(Hud.Raiz));
            Assert.AreEqual(1, _container.IndexOf(janela));
        }

        [Test]
        public void Remover_TiraOHudETodasAsRegioes()
        {
            Montar();
            var regiao = Hud.Regiao(RegiaoDoHud.InferiorEsquerda);
            var painelDaFuncionalidade = new VisualElement();
            regiao.Add(painelDaFuncionalidade);

            Hud.Remover();

            Assert.IsFalse(Hud.Montado);
            Assert.AreEqual(0, _container.childCount);
            Assert.IsNull(regiao.parent);
            foreach (RegiaoDoHud r in Enum.GetValues(typeof(RegiaoDoHud)))
            {
                Assert.IsNull(Hud.Regiao(r), r.ToString());
            }
        }

        [Test]
        public void MontarDeNovo_SubstituiOHudAnterior()
        {
            Montar();
            var antigo = Hud.Raiz;

            Montar();

            Assert.AreNotSame(antigo, Hud.Raiz);
            Assert.IsNull(antigo.parent);
            Assert.AreEqual(3, _container.childCount);
        }

        [Test]
        public void SemInterface_NaoMonta()
        {
            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Warning,
                new System.Text.RegularExpressions.Regex("HUD não foi montado")
            );

            Hud.Montar(null);

            Assert.IsFalse(Hud.Montado);
            Assert.IsNull(Hud.Regiao(RegiaoDoHud.InferiorCentro));
        }

        [Test]
        public void MenuDoEscAberto_NaoApagaOHud()
        {
            Montar();
            AvisosDoJogo.Mostrar("Inventário cheio", TipoDeAviso.Aviso);
            var menu = new MenuJogo(null);

            menu.Abrir();

            Assert.IsTrue(menu.Aberto);
            Assert.IsTrue(Hud.Montado);
            Assert.AreSame(_container, Hud.Raiz.parent);
            Assert.Less(_container.IndexOf(Hud.Raiz), _container.childCount - 1);
            Assert.AreEqual(1, Hud.Avisos.Raiz.childCount);

            menu.Fechar();

            Assert.AreSame(_container, Hud.Raiz.parent);
            Assert.AreEqual(1, Hud.Avisos.Raiz.childCount);
        }

        [Test]
        public void MenuDoEscAberto_AvisoVencidoNaoVoltaAoFechar()
        {
            Montar();
            AvisosDoJogo.Mostrar("Ana entrou no grupo");
            var menu = new MenuJogo(null);
            menu.Abrir();

            _agora += ListaDeAvisos.Duracao + 1;
            Hud.Avisos.Atualizar();
            menu.Fechar();

            Assert.AreEqual(0, Hud.Avisos.Raiz.childCount);
        }

        [Test]
        public void AvisosDoJogo_ForaDoMundo_NaoFazNada()
        {
            Assert.DoesNotThrow(() => AvisosDoJogo.Mostrar("Inventário cheio", TipoDeAviso.Erro));
            Assert.IsNull(Hud.Avisos);
        }

        [Test]
        public void AvisosDoJogo_AparecemNaRegiaoInferiorCentro()
        {
            Montar();

            AvisosDoJogo.Mostrar("Vigor insuficiente", TipoDeAviso.Erro);

            var regiao = Hud.Regiao(RegiaoDoHud.InferiorCentro);
            var linha = regiao.Q<Label>(null, EstiloCore.Aviso);
            Assert.IsNotNull(linha);
            Assert.AreEqual("Vigor insuficiente", linha.text);
            Assert.IsTrue(linha.ClassListContains(EstiloCore.AvisoErro));
            Assert.AreEqual(PickingMode.Ignore, linha.pickingMode);
        }

        [Test]
        public void AlternarVisivel_OcultaEMostraOHud_ERespeitaAlternancia()
        {
            Montar();
            Assert.IsFalse(Hud.OcultoPeloUsuario);

            // Alternar oculta
            Hud.AlternarVisivel();
            Assert.IsTrue(Hud.OcultoPeloUsuario);
            Assert.AreEqual(DisplayStyle.None, Hud.Raiz.style.display.value);

            // DefinirVisivel(true) não força se o usuário ocultou
            Hud.DefinirVisivel(true);
            Assert.IsTrue(Hud.OcultoPeloUsuario);
            Assert.AreEqual(DisplayStyle.None, Hud.Raiz.style.display.value);

            // Alternar novamente volta a mostrar
            Hud.AlternarVisivel();
            Assert.IsFalse(Hud.OcultoPeloUsuario);
            Assert.AreEqual(DisplayStyle.Flex, Hud.Raiz.style.display.value);
        }
    }
}
