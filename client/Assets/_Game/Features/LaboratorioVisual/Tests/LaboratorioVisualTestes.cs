using System;
using JogoDoGrupo.Client.Features.LaboratorioVisual.Editor;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.LaboratorioVisual.Tests
{
    public class LaboratorioVisualTestes
    {
        [Test]
        public void Navegar_ReconstroiExemploEFechaModalAnterior()
        {
            var tela = new TelaLaboratorio();
            tela.AbrirModal("Convite", "Demonstração");
            tela.Navegar(2);
            Assert.AreEqual(2, tela.SecaoAtual);
            Assert.IsNotNull(tela.Raiz.Q<TextField>("buscar-item"));
            Assert.AreEqual(DisplayStyle.None, tela.Raiz.Q("modal").style.display.value);
            tela.Navegar(1);
            Assert.IsNull(tela.Raiz.Q<TextField>("buscar-item"));
            Assert.IsNotNull(tela.Raiz.Q<TextField>("texto-chat"));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void Navegar_RecusaIndiceInvalidoSemAlterarTela(int indice)
        {
            var tela = new TelaLaboratorio();
            Assert.Throws<ArgumentOutOfRangeException>(() => tela.Navegar(indice));
            Assert.AreEqual(0, tela.SecaoAtual);
        }

        [TestCase(8, 16)]
        [TestCase(20, 20)]
        [TestCase(32, 24)]
        public void TamanhoTexto_RespeitaLimitesEPersisteAoNavegar(int entrada, int esperado)
        {
            var tela = new TelaLaboratorio();
            tela.DefinirTamanhoTexto(entrada);
            tela.Navegar(1);
            Assert.AreEqual(esperado, tela.TamanhoTexto);
            tela.Raiz.Query<Label>(className: "lab-leitura")
                .ForEach(l => Assert.AreEqual(esperado, l.style.fontSize.value.value));
        }

        [Test]
        public void Busca_FiltraERestauraEExibeEstadoVazio()
        {
            var tela = new TelaLaboratorio();
            tela.Navegar(2);
            var busca = tela.Raiz.Q<TextField>("buscar-item");
            busca.value = "ESSÊNCIA";
            Assert.AreEqual(
                DisplayStyle.Flex,
                tela.Raiz.Q<Button>("Essência azul").style.display.value
            );
            Assert.AreEqual(
                DisplayStyle.None,
                tela.Raiz.Q<Button>("Mapa do vale").style.display.value
            );
            busca.value = "inexistente";
            Assert.AreEqual(DisplayStyle.Flex, tela.Raiz.Q("busca-vazia").style.display.value);
            busca.value = "";
            Assert.AreEqual(DisplayStyle.None, tela.Raiz.Q("busca-vazia").style.display.value);
            Assert.AreEqual(
                DisplayStyle.Flex,
                tela.Raiz.Q<Button>("Mapa do vale").style.display.value
            );
        }

        [Test]
        public void Comparacao_PodeAlternarSemPerderSecao()
        {
            var tela = new TelaLaboratorio();
            tela.Navegar(4);
            tela.Comparar(true);
            Assert.IsTrue(tela.Comparando);
            Assert.AreEqual(
                DisplayStyle.Flex,
                tela.Raiz.Q("prancha-referencia").style.display.value
            );
            tela.Comparar(false);
            Assert.AreEqual(4, tela.SecaoAtual);
            Assert.AreEqual(
                DisplayStyle.None,
                tela.Raiz.Q("prancha-referencia").style.display.value
            );
        }

        [Test]
        public void Validacao_SemPainelNaoAfirmaQueRenderizou()
        {
            Assert.That(ValidacaoLaboratorio.Relatar(null), Does.StartWith("FALHA"));
            Assert.That(
                ValidacaoLaboratorio.Relatar(new VisualElement()),
                Does.StartWith("PENDENTE")
            );
        }

        [Test]
        public void Instancias_NaoCompartilhamEstadoEExemplosAbrem()
        {
            var primeira = new TelaLaboratorio();
            var segunda = new TelaLaboratorio();
            for (var i = 0; i < TelaLaboratorio.Secoes.Length; i++)
                Assert.DoesNotThrow(() => primeira.Navegar(i));
            Assert.AreEqual(0, segunda.SecaoAtual);
            Assert.IsFalse(segunda.Raiz.Q<Button>("botao-desabilitado").enabledSelf);
        }
    }
}
