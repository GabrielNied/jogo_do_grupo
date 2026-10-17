using JogoDoGrupo.Client.Core.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    [TestFixture]
    public class TextosDoLivroTestes
    {
        [Test]
        public void TextosDoLivro_ContemTodosOsTextosAprovadosDoLivro()
        {
            Assert.AreEqual(
                "Você caiu. Aguarde ajuda ou retorne a um ponto seguro.",
                TextosDoLivro.CaidoAjuda
            );
            Assert.AreEqual(
                "Levantado: recupere a vida antes de voltar à linha de frente.",
                TextosDoLivro.Levantado
            );
            Assert.AreEqual(
                "Mire na direção do perigo. Mova-se para fora da área de ataque.",
                TextosDoLivro.AjudaMira
            );
            Assert.AreEqual(
                "Defesa insuficiente para bloquear completamente este impacto.",
                TextosDoLivro.DefesaInsuficiente
            );
            Assert.AreEqual(
                "Distribua seus pontos. Seus equipamentos dão possibilidades.",
                TextosDoLivro.AtributosOrientacao
            );
            Assert.AreEqual(
                "Entre, encontre seus amigos e continue de onde parou.",
                TextosDoLivro.EntradaNoMundo
            );
            Assert.AreEqual(
                "Cada saída pode render uma peça melhor, uma combinação nova ou uma história para contar.",
                TextosDoLivro.DiarioMissoes
            );
        }

        [Test]
        public void TelaCarregamento_ExibeLemaDoMundo()
        {
            var tela = new TelaCarregamento();
            var raiz = new VisualElement();
            tela.Abrir(raiz);

            var lema = raiz.Q<Label>(className: "entrada-mundo-lema");
            Assert.IsNotNull(lema);
            Assert.AreEqual(TextosDoLivro.EntradaNoMundo, lema.text);

            tela.Fechar();
        }
    }
}
