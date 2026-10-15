using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    public class ApresentacaoEfeitosDeStatusTestes
    {
        [Test]
        public void MontarItem_FormataTextoENomeCorretamente()
        {
            var instancia = new InstanciaEfeitoDeStatus
            {
                Tipo = TipoEfeitoDeStatus.Sangramento,
                Acumulos = 3,
                DuracaoRestanteSegundos = 4.5f,
                DuracaoTotalSegundos = 6.0f,
            };

            var item = ApresentacaoEfeitosDeStatus.MontarItem(instancia);

            Assert.AreEqual(TipoEfeitoDeStatus.Sangramento, item.Tipo);
            Assert.AreEqual(3, item.Acumulos);
            Assert.AreEqual("Sangramento", item.Nome);
            Assert.AreEqual("Sangramento x3 (4.5s)", item.TextoDescritivo);
            Assert.AreEqual(0.75f, item.Fracao, 0.001f);
        }

        [Test]
        public void MontarQuebra_QuandoQuebrado_TextoEVulneravel()
        {
            var msg = new BarraDeQuebraMudou
            {
                EntidadeId = 42,
                ValorAtual = 0,
                CapacidadeMax = 100,
                Quebrado = true,
            };

            var quebra = ApresentacaoEfeitosDeStatus.MontarQuebra(msg);

            Assert.IsTrue(quebra.Quebrado);
            Assert.AreEqual("VULNERÁVEL", quebra.Texto);
            Assert.AreEqual(0f, quebra.Fracao);
        }

        [Test]
        public void MontarQuebra_QuandoAtivo_TextoMostraFracao()
        {
            var msg = new BarraDeQuebraMudou
            {
                EntidadeId = 42,
                ValorAtual = 50,
                CapacidadeMax = 100,
                Quebrado = false,
            };

            var quebra = ApresentacaoEfeitosDeStatus.MontarQuebra(msg);

            Assert.IsFalse(quebra.Quebrado);
            Assert.AreEqual("Quebra 50 / 100", quebra.Texto);
            Assert.AreEqual(0.5f, quebra.Fracao, 0.001f);
        }

        [Test]
        public void PainelEfeitosDeStatus_SemEfeitos_FicaOculto()
        {
            var container = new VisualElement();
            var painel = new PainelEfeitosDeStatus(container);

            painel.Atualizar(new List<ItemDeStatus>(), 0f);

            Assert.AreEqual(DisplayStyle.None, painel.Raiz.style.display.value);
            Assert.AreEqual(0, painel.Raiz.childCount);
        }

        [Test]
        public void PainelEfeitosDeStatus_ComEfeitosEImunidade_ExibeElementos()
        {
            var container = new VisualElement();
            var painel = new PainelEfeitosDeStatus(container);

            var efeitos = new List<ItemDeStatus>
            {
                new ItemDeStatus(TipoEfeitoDeStatus.Lentidao, 1, 3.2f, 4f),
                new ItemDeStatus(TipoEfeitoDeStatus.Queimadura, 2, 5.0f, 6f),
            };

            painel.Atualizar(efeitos, 4.5f);

            Assert.AreEqual(DisplayStyle.Flex, painel.Raiz.style.display.value);
            // 2 efeitos + 1 chip de imunidade = 3 elementos filhos
            Assert.AreEqual(3, painel.Raiz.childCount);
        }

        [Test]
        public void PainelBarraDeQuebra_SemQuebra_FicaOculto()
        {
            var container = new VisualElement();
            var painel = new PainelBarraDeQuebra(container);

            painel.Atualizar(new ItemBarraDeQuebra(0, 100, false));

            Assert.AreEqual(DisplayStyle.None, painel.Raiz.style.display.value);
        }

        [Test]
        public void PainelBarraDeQuebra_QuandoQuebrado_ExibeVulneravel()
        {
            var container = new VisualElement();
            var painel = new PainelBarraDeQuebra(container);

            painel.Atualizar(new ItemBarraDeQuebra(0, 100, true));

            Assert.AreEqual(DisplayStyle.Flex, painel.Raiz.style.display.value);
            var preenchimento = painel.Raiz.Q(className: "quebra-boss-preenchimento");
            Assert.IsNotNull(preenchimento);
            Assert.IsTrue(preenchimento.ClassListContains("quebra-boss-vulneravel"));
        }
    }
}
