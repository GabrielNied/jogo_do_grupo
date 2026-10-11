using System;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Visual.Tests
{
    public class EstiloVisualTestes
    {
        [Test]
        public void Responsividade_AlternaNosLimiaresESaiDoModoCompacto()
        {
            var raiz = new VisualElement();
            EstiloVisual.AtualizarResponsividade(raiz, ConfigVisual.Responsividade.CompactoAte);
            Assert.IsTrue(raiz.ClassListContains("lumora-compacto"));
            Assert.IsFalse(raiz.ClassListContains("lumora-estreito"));
            EstiloVisual.AtualizarResponsividade(raiz, ConfigVisual.Responsividade.EstreitoAte);
            Assert.IsTrue(raiz.ClassListContains("lumora-estreito"));
            EstiloVisual.AtualizarResponsividade(raiz, ConfigVisual.Responsividade.CompactoAte + 1);
            Assert.IsFalse(raiz.ClassListContains("lumora-compacto"));
            Assert.IsFalse(raiz.ClassListContains("lumora-estreito"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Responsividade_NaoTrocaEstadoAntesDeLayoutValido(float largura)
        {
            var raiz = new VisualElement();
            raiz.AddToClassList("lumora-compacto");
            EstiloVisual.AtualizarResponsividade(raiz, largura);
            Assert.IsTrue(raiz.ClassListContains("lumora-compacto"));
        }

        [Test]
        public void ErrosDeIntegracao_NaoProduzemFallbackSilencioso()
        {
            Assert.Throws<ArgumentNullException>(() => EstiloVisual.Aplicar(null));
            Assert.Throws<ArgumentNullException>(() =>
                EstiloVisual.AtualizarResponsividade(null, 1280)
            );
            Assert.Throws<InvalidOperationException>(() =>
                EstiloVisual.CarregarFonte("Lumora/fonte-inexistente")
            );
        }

        [Test]
        public void Texto_RespeitaMinimoMaximoEValorIntermediario()
        {
            Assert.AreEqual(ConfigVisual.Tipografia.Leitura, EstiloVisual.LimitarTexto(-1));
            Assert.AreEqual(20, EstiloVisual.LimitarTexto(20));
            Assert.AreEqual(ConfigVisual.Tipografia.LeituraMaxima, EstiloVisual.LimitarTexto(100));
        }

        [Test]
        public void LeituraAumentada_NaoDeixaSubtituloMenorQueCorpo()
        {
            var raiz = new VisualElement();
            var texto = new Label("Corpo");
            var subtitulo = new Label("Seção");
            subtitulo.AddToClassList("ui-subtitulo");
            raiz.Add(texto);
            raiz.Add(subtitulo);
            EstiloVisual.DefinirTamanhoTexto(raiz, ConfigVisual.Tipografia.LeituraMaxima);
            Assert.AreEqual(
                ConfigVisual.Tipografia.LeituraMaxima,
                texto.style.fontSize.value.value
            );
            Assert.GreaterOrEqual(
                subtitulo.style.fontSize.value.value,
                texto.style.fontSize.value.value
            );
        }

        [Test]
        public void Aplicar_CarregaFontesEEstilosESoRegistraUmaVez()
        {
            var raiz = new VisualElement();
            EstiloVisual.Aplicar(raiz);
            var total = raiz.styleSheets.count;
            EstiloVisual.Aplicar(raiz);
            Assert.AreEqual(total, raiz.styleSheets.count);
            Assert.IsTrue(raiz.ClassListContains("lumora-ui"));
            var botao = ComponentesVisuais.Botao("Convidar", () => { }, principal: true);
            Assert.IsTrue(botao.ClassListContains("ui-primario"));
            Assert.IsTrue(
                ComponentesVisuais.Texto("Título", titulo: true).ClassListContains("ui-titulo")
            );
        }
    }
}
