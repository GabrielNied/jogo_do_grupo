using System;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// O que aparece na tela: a moldura igual para todas as janelas (título, fechar, conteúdo
    /// da funcionalidade e dica de como fechar) e a barra de atalhos.
    /// </summary>
    public class MolduraDeJanelasTestes
    {
        private sealed class JanelaDeTeste : JanelaDoJogo
        {
            public int Montagens;
            public int Aberturas;
            public int Fechamentos;
            public bool FalharAoMontar;

            protected override void Montar(VisualElement conteudo)
            {
                Montagens++;
                if (FalharAoMontar)
                {
                    throw new InvalidOperationException("defeito de teste");
                }

                conteudo.Add(new Label("conteúdo de teste") { name = "teste.rotulo" });
            }

            protected override void AoAbrir()
            {
                Aberturas++;
            }

            protected override void AoFechar()
            {
                Fechamentos++;
            }

            public void TrocarTitulo(string titulo)
            {
                DefinirTitulo(titulo);
            }
        }

        private static DefinicaoDeJanela Definicao(Func<JanelaDoJogo> criar)
        {
            return new DefinicaoDeJanela("itens.inventario", "Inventário", "I", 20, criar);
        }

        [Test]
        public void Mostrar_MontaAMolduraComTituloConteudoEDicaDeFechar()
        {
            var container = new VisualElement();
            var janela = new JanelaDeTeste();
            var apresentador = new ApresentadorDeJanelas(container, null);

            apresentador.Mostrar(Definicao(() => janela));

            Assert.AreEqual(1, container.childCount);
            Assert.AreSame(apresentador.Raiz, container[0]);
            Assert.IsTrue(apresentador.Raiz.ClassListContains(EstiloCore.TelaSobreJogo));
            Assert.AreEqual("Inventário", apresentador.Raiz.Q<Label>(null, EstiloCore.Titulo).text);
            Assert.AreEqual(
                "I ou Esc: fechar",
                apresentador.Raiz.Q<Label>(null, EstiloCore.JanelaRodape).text
            );
            Assert.IsNotNull(apresentador.Raiz.Q<Label>("teste.rotulo"));
            Assert.AreSame(
                apresentador.Raiz.Q<VisualElement>(null, EstiloCore.JanelaConteudo),
                janela.Conteudo
            );
            Assert.IsTrue(janela.Aberta);
            Assert.AreEqual(1, janela.Montagens);
            Assert.AreEqual(1, janela.Aberturas);
        }

        [Test]
        public void Esconder_FechaAJanela_ETiraDaTela()
        {
            var container = new VisualElement();
            var janela = new JanelaDeTeste();
            var apresentador = new ApresentadorDeJanelas(container, null);
            apresentador.Mostrar(Definicao(() => janela));

            apresentador.Esconder();
            apresentador.Esconder();

            Assert.AreEqual(0, container.childCount);
            Assert.IsNull(apresentador.Raiz);
            Assert.IsFalse(janela.Aberta);
            Assert.AreEqual(1, janela.Fechamentos, "Fechar duas vezes avisa a janela uma vez só.");
        }

        [Test]
        public void Mostrar_OutraJanela_FechaAAnterior_SoUmaNaTela()
        {
            var container = new VisualElement();
            var primeira = new JanelaDeTeste();
            var segunda = new JanelaDeTeste();
            var apresentador = new ApresentadorDeJanelas(container, null);

            apresentador.Mostrar(Definicao(() => primeira));
            apresentador.Mostrar(Definicao(() => segunda));

            Assert.AreEqual(1, container.childCount);
            Assert.IsFalse(primeira.Aberta);
            Assert.IsTrue(segunda.Aberta);
        }

        [Test]
        public void DefinirTitulo_TrocaOTituloDaMoldura_SoComAJanelaAberta()
        {
            var janela = new JanelaDeTeste();
            var apresentador = new ApresentadorDeJanelas(new VisualElement(), null);
            apresentador.Mostrar(Definicao(() => janela));
            var titulo = apresentador.Raiz.Q<Label>(null, EstiloCore.Titulo);

            janela.TrocarTitulo("Inventário (3/20)");
            Assert.AreEqual("Inventário (3/20)", titulo.text);

            apresentador.Esconder();
            janela.TrocarTitulo("Depois de fechar");
            Assert.AreEqual("Inventário (3/20)", titulo.text);
        }

        [Test]
        public void JanelaComDefeito_MostraOAviso_ENaoTravaOJogo()
        {
            var container = new VisualElement();
            var apresentador = new ApresentadorDeJanelas(container, null);

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: defeito de teste");
            apresentador.Mostrar(Definicao(() => new JanelaDeTeste { FalharAoMontar = true }));

            var aviso = apresentador.Raiz.Q<Label>(null, EstiloCore.MensagemErro);
            Assert.IsNotNull(aviso);
            Assert.AreEqual(ApresentadorDeJanelas.TextoFalha, aviso.text);
            Assert.DoesNotThrow(() => apresentador.Esconder());
            Assert.AreEqual(0, container.childCount);
        }

        [Test]
        public void FabricaQueDevolveNulo_MostraOAviso()
        {
            var apresentador = new ApresentadorDeJanelas(new VisualElement(), null);

            LogAssert.Expect(
                LogType.Error,
                "[JanelasDoJogo] A janela \"itens.inventario\" não foi criada."
            );
            apresentador.Mostrar(Definicao(() => null));

            Assert.IsNotNull(apresentador.Raiz.Q<Label>(null, EstiloCore.MensagemErro));
        }

        [Test]
        public void SemContainer_NaoFalha()
        {
            var apresentador = new ApresentadorDeJanelas(null, null);

            Assert.DoesNotThrow(() => apresentador.Mostrar(Definicao(() => new JanelaDeTeste())));
            Assert.IsNull(apresentador.Raiz);
        }

        [Test]
        public void JanelaEmConstrucao_MostraOAvisoEOTexto()
        {
            var apresentador = new ApresentadorDeJanelas(new VisualElement(), null);

            apresentador.Mostrar(Definicao(() => new JanelaEmConstrucao("O mapa vai aqui.")));

            Assert.AreEqual(
                JanelaEmConstrucao.Aviso,
                apresentador.Raiz.Q<Label>(null, EstiloCore.MensagemAviso).text
            );
            Assert.AreEqual(
                "O mapa vai aqui.",
                apresentador.Raiz.Q<Label>(null, EstiloCore.Texto).text
            );
        }

        [Test]
        public void BarraDeAtalhos_MostraATeclaDeCadaJanela_EOMenuPorUltimo()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(
                new DefinicaoDeJanela("mundo.mapa", "Mapa", "M", 40, () => new JanelaDeTeste())
            );
            registro.Registrar(
                new DefinicaoDeJanela(
                    "personagem.ficha",
                    "Personagem",
                    "C",
                    10,
                    () => new JanelaDeTeste()
                )
            );
            var container = new VisualElement();

            var barra = new BarraDeAtalhos(container, registro.Definicoes);

            Assert.AreSame(barra.Raiz, container[0]);
            var teclas = barra.Raiz.Query<Label>(null, EstiloCore.AtalhoTecla).ToList();
            var nomes = barra.Raiz.Query<Label>(null, EstiloCore.AtalhoNome).ToList();
            CollectionAssert.AreEqual(new[] { "C", "M", "Esc" }, teclas.ConvertAll(t => t.text));
            CollectionAssert.AreEqual(
                new[] { "Personagem", "Mapa", "Menu" },
                nomes.ConvertAll(n => n.text)
            );
        }

        [Test]
        public void BarraDeAtalhos_NaoRecebeClique()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(
                new DefinicaoDeJanela("mundo.mapa", "Mapa", "M", 40, () => new JanelaDeTeste())
            );

            var barra = new BarraDeAtalhos(new VisualElement(), registro.Definicoes);

            Assert.AreEqual(PickingMode.Ignore, barra.Raiz.pickingMode);
            foreach (var elemento in barra.Raiz.Query<VisualElement>().ToList())
            {
                Assert.AreEqual(PickingMode.Ignore, elemento.pickingMode);
            }
        }

        [Test]
        public void BarraDeAtalhos_DestacaSoAJanelaAberta()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(
                new DefinicaoDeJanela("mundo.mapa", "Mapa", "M", 40, () => new JanelaDeTeste())
            );
            registro.Registrar(
                new DefinicaoDeJanela(
                    "personagem.ficha",
                    "Personagem",
                    "C",
                    10,
                    () => new JanelaDeTeste()
                )
            );
            var barra = new BarraDeAtalhos(new VisualElement(), registro.Definicoes);

            barra.Destacar("mundo.mapa");
            var ativos = barra.Raiz.Query<VisualElement>(null, EstiloCore.AtalhoAtivo).ToList();
            Assert.AreEqual(1, ativos.Count);
            Assert.AreEqual("M", ativos[0].Q<Label>(null, EstiloCore.AtalhoTecla).text);

            barra.Destacar(null);
            Assert.AreEqual(
                0,
                barra.Raiz.Query<VisualElement>(null, EstiloCore.AtalhoAtivo).ToList().Count
            );
        }

        [Test]
        public void CaminhoDaTecla_ViraOCaminhoDoInputSystem()
        {
            Assert.AreEqual("<Keyboard>/i", AtalhosDeJanelas.CaminhoDaTecla("I"));
        }
    }
}
