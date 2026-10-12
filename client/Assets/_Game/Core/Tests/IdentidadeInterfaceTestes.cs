using System;
using System.Collections;
using JogoDoGrupo.Client.Core.Contas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class IdentidadeInterfaceTestes
    {
        [UnityTest]
        public IEnumerator FontesResolvidas_EConteudoLongoUsamOPadraoNaRaizEstreita()
        {
            var objeto = new GameObject("Teste de identidade");
            objeto.SetActive(false);
            var configuracao = ScriptableObject.CreateInstance<PanelSettings>();
            // Sem tela no modo batch, a escala pela resolução não dá largura à raiz; aqui só
            // importam as fontes e a raiz estreita (a escala tem teste próprio abaixo).
            configuracao.scaleMode = PanelScaleMode.ConstantPixelSize;
            configuracao.themeStyleSheet = Resources.Load<ThemeStyleSheet>(EstiloCore.CaminhoTema);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = configuracao;
            objeto.SetActive(true);
            try
            {
                var raiz = documento.rootVisualElement;
                raiz.style.width = 640;
                raiz.style.height = 720;
                EstiloCore.Aplicar(raiz);
                var tela = new VisualElement();
                tela.AddToClassList(EstiloCore.Tela);
                var painel = EstiloCore.CriarPainel();
                var titulo = new Label("Sua expedição");
                titulo.AddToClassList(EstiloCore.Titulo);
                var botao = new Button { text = "Continuar" };
                botao.AddToClassList(EstiloCore.Botao);
                painel.Add(titulo);
                painel.Add(botao);
                for (var i = 0; i < 50; i++)
                {
                    var linha = new Label(
                        "Descrição longa com acentos: expedição, essência e construção."
                    );
                    linha.AddToClassList(EstiloCore.Texto);
                    painel.Add(linha);
                }
                tela.Add(painel);
                raiz.Add(tela);
                yield return null;
                yield return null;
                Assert.AreSame(
                    EstiloVisual.CarregarFonte(ConfigVisual.Fontes.Titulo),
                    titulo.resolvedStyle.unityFontDefinition.fontAsset
                );
                Assert.AreSame(
                    EstiloVisual.CarregarFonte(ConfigVisual.Fontes.Controle),
                    botao.resolvedStyle.unityFontDefinition.fontAsset
                );
                Assert.IsTrue(raiz.ClassListContains("lumora-estreito"));
                Assert.LessOrEqual(painel.layout.width, raiz.layout.width);
                Assert.LessOrEqual(painel.layout.height, raiz.layout.height);
                Assert.Greater(
                    painel.verticalScroller.highValue,
                    0,
                    "Conteúdo longo precisa rolar, sem sair da tela."
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objeto);
                UnityEngine.Object.DestroyImmediate(configuracao);
            }
        }

        [Test]
        public void Aplicar_SoCarregaUmaVezENaoBloqueiaCliquesNoMundo()
        {
            var raiz = new VisualElement();
            EstiloCore.Aplicar(raiz);
            var quantidade = raiz.styleSheets.count;
            EstiloCore.Aplicar(raiz);
            Assert.AreEqual(quantidade, raiz.styleSheets.count);
            Assert.IsTrue(raiz.ClassListContains("lumora-ui"));
            Assert.AreEqual(PickingMode.Ignore, raiz.pickingMode);
            Assert.Throws<ArgumentNullException>(() => EstiloCore.Aplicar(null));
        }

        [Test]
        public void Escala_AcompanhaAResolucaoPelaAlturaDeReferencia()
        {
            var painel = ScriptableObject.CreateInstance<PanelSettings>();
            try
            {
                EstiloCore.ConfigurarEscala(painel);
                Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, painel.scaleMode);
                Assert.AreEqual(PanelScreenMatchMode.MatchWidthOrHeight, painel.screenMatchMode);
                Assert.AreEqual(new Vector2Int(1920, 1080), painel.referenceResolution);
                Assert.AreEqual(1f, painel.match, "Segue a altura: a janela cabe em 21:9 e 16:10.");
                Assert.Throws<ArgumentNullException>(() => EstiloCore.ConfigurarEscala(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(painel);
            }
        }

        [Test]
        public void ConfiguracaoDoPainel_SaiPronta_ComEscalaTemaEOrdem()
        {
            var painel = EstiloCore.CriarConfiguracaoDoPainel("PainelTeste", -2f);
            try
            {
                Assert.AreEqual("PainelTeste", painel.name);
                Assert.AreEqual(-2f, painel.sortingOrder);
                Assert.AreEqual(HideFlags.DontSave, painel.hideFlags);
                Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, painel.scaleMode);
                Assert.IsNotNull(painel.themeStyleSheet);
                Assert.Throws<ArgumentException>(() =>
                    EstiloCore.CriarConfiguracaoDoPainel(" ", 0f)
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(painel);
            }
        }

        [Test]
        public void PainelRolavel_PreservaConteudoEControlesAcessiveis()
        {
            var painel = EstiloCore.CriarPainel(social: true);
            var botao = new Button { name = "teste.acao" };
            painel.contentContainer.Add(botao);
            Assert.AreSame(painel.contentContainer, botao.parent);
            Assert.AreSame(botao, painel.Q<Button>("teste.acao"));
            Assert.AreEqual(ScrollerVisibility.Auto, painel.verticalScrollerVisibility);
            Assert.AreEqual(ScrollerVisibility.Hidden, painel.horizontalScrollerVisibility);
            Assert.IsTrue(painel.ClassListContains("ui-painel-social"));
        }

        [Test]
        public void Login_MantemCamposSenhaProtegidaEBotoesComAMarcaAprovada()
        {
            var raiz = new VisualElement();
            var login = new TelaLogin();
            login.Abrir(raiz);
            try
            {
                var campos = raiz.Query<TextField>().ToList();
                Assert.AreEqual(2, campos.Count);
                Assert.IsFalse(campos[0].isPasswordField);
                Assert.IsTrue(campos[1].isPasswordField);
                Assert.AreEqual(2, raiz.Query<Button>(null, EstiloCore.Botao).ToList().Count);
                Assert.IsNotNull(raiz.Q("lumora.marca"));
                Assert.IsNotNull(raiz.Q<ScrollView>(null, EstiloCore.Painel));
            }
            finally
            {
                login.Fechar();
            }
        }
    }
}
