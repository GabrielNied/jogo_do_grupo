using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Core.Api;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class FluxoCarregamentoTestes
    {
        [Test]
        public void TelaDeEntrada_MostraEtapaRealEFalhaRecuperavel()
        {
            var tela = new TelaCarregamento();
            var raiz = new VisualElement();
            tela.Abrir(raiz);
            tela.MostrarEtapa(EtapaDoCarregamento.Conexao);

            Assert.AreEqual(
                "Conectando ao mundo...",
                raiz.Q<Label>(className: "entrada-mundo-estado").text
            );
            Assert.IsTrue(
                raiz.Query<Label>(className: "entrada-mundo-etapa-ativa").First().text == "Conexão"
            );

            tela.MostrarFalha("Não foi possível entrar", "A conexão não foi concluída.");
            Assert.AreEqual(
                "Não foi possível entrar",
                raiz.Q<Label>(className: "entrada-mundo-estado").text
            );
            Assert.AreEqual(2, raiz.Query<Button>().ToList().Count);
            tela.Fechar();
        }

        [Test]
        public void EsperaDaPosicao_ExpiraNoPrazo()
        {
            Assert.IsFalse(EntradaNoJogo.PrazoEsgotado(10f, 29.9f));
            Assert.IsTrue(EntradaNoJogo.PrazoEsgotado(10f, 30f));
        }

        [Test]
        public void Abertura_BloqueiaOfflineEversaoIncompativel()
        {
            Assert.IsTrue(TelaAbertura.FalhaDaSaude(false, null).Contains("fora do ar"));
            Assert.IsTrue(
                TelaAbertura
                    .FalhaDaSaude(true, new EstadoDaSaude { Estado = "ok", Versao = 2 })
                    .Contains("incompatível")
            );
            Assert.IsNull(
                TelaAbertura.FalhaDaSaude(
                    true,
                    new EstadoDaSaude { Estado = "ok", Versao = EstadoDaSaude.VersaoAtual }
                )
            );
        }
    }
}
