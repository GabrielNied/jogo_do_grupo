using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// A janela arrastada reabre onde a pessoa deixou, sempre dentro da vista, e volta ao lugar
    /// padrão se a tela mudou de tamanho.
    /// </summary>
    public class PosicoesDeJanelasTestes
    {
        private static readonly Vector2 Tela = new Vector2(1920f, 1080f);

        private static PosicoesDeJanelas Posicoes(Dictionary<string, string> guardado)
        {
            return new PosicoesDeJanelas(
                chave => guardado.TryGetValue(chave, out var valor) ? valor : "",
                (chave, valor) => guardado[chave] = valor
            );
        }

        [Test]
        public void GravarELer_NaMesmaTela_DevolveAPosicao()
        {
            var posicoes = Posicoes(new Dictionary<string, string>());

            posicoes.Gravar("itens.inventario", new Vector2(120.5f, 64f), Tela);

            Assert.IsTrue(posicoes.TentarLer("itens.inventario", Tela, out var posicao));
            Assert.AreEqual(new Vector2(120.5f, 64f), posicao);
            Assert.IsFalse(
                posicoes.TentarLer("personagem.ficha", Tela, out _),
                "Cada janela tem a sua."
            );
        }

        [Test]
        public void Ler_EmOutraTela_VoltaAoPadrao()
        {
            var posicoes = Posicoes(new Dictionary<string, string>());
            posicoes.Gravar("itens.inventario", new Vector2(300f, 200f), Tela);

            Assert.IsFalse(posicoes.TentarLer("itens.inventario", new Vector2(1280f, 720f), out _));
            Assert.IsTrue(
                posicoes.TentarLer("itens.inventario", Tela + new Vector2(0.5f, -0.5f), out _),
                "Arredondamento do layout não conta como outra tela."
            );
        }

        [Test]
        public void Ler_ValorEstragado_VoltaAoPadrao()
        {
            var guardado = new Dictionary<string, string>
            {
                [PosicoesDeJanelas.PrefixoChave + "a"] = "abc",
                [PosicoesDeJanelas.PrefixoChave + "b"] = "1;2;3",
                [PosicoesDeJanelas.PrefixoChave + "c"] = "1;2;0;0",
                [PosicoesDeJanelas.PrefixoChave + "d"] = "NaN;2;1920;1080",
            };
            var posicoes = Posicoes(guardado);

            foreach (var chave in new[] { "a", "b", "c", "d", "" })
            {
                Assert.IsFalse(posicoes.TentarLer(chave, Tela, out _), chave);
            }
        }

        [Test]
        public void Limitar_PrendeAJanelaDentroDaArea()
        {
            var tamanho = new Vector2(400f, 300f);

            Assert.AreEqual(
                new Vector2(100f, 50f),
                PosicoesDeJanelas.Limitar(new Vector2(100f, 50f), tamanho, Tela)
            );
            Assert.AreEqual(
                Vector2.zero,
                PosicoesDeJanelas.Limitar(new Vector2(-80f, -10f), tamanho, Tela)
            );
            Assert.AreEqual(
                new Vector2(1520f, 780f),
                PosicoesDeJanelas.Limitar(new Vector2(5000f, 5000f), tamanho, Tela)
            );
        }

        [Test]
        public void Limitar_JanelaMaiorQueAArea_FicaEmCimaAEsquerda()
        {
            Assert.AreEqual(
                Vector2.zero,
                PosicoesDeJanelas.Limitar(new Vector2(50f, 50f), new Vector2(3000f, 2000f), Tela)
            );
        }

        [Test]
        public void Apresentador_ReabreNaPosicaoGravada_ComAJanelaSolta()
        {
            var guardado = new Dictionary<string, string>();
            Posicoes(guardado).Gravar("teste.janela", new Vector2(40f, 30f), Tela);
            var apresentador = new ApresentadorDeJanelas(
                new VisualElement(),
                null,
                null,
                Posicoes(guardado)
            );

            apresentador.Mostrar(Definicao("teste.janela"));

            Assert.IsTrue(apresentador.TentarPosicao("teste.janela", out var posicao));
            Assert.AreEqual(new Vector2(40f, 30f), posicao);
            Assert.IsTrue(apresentador.Raiz.ClassListContains(EstiloCore.JanelaSolta));
            Assert.AreEqual(40f, apresentador.Raiz.style.left.value.value);
            Assert.AreEqual(30f, apresentador.Raiz.style.top.value.value);
            apresentador.Esconder();
        }

        [Test]
        public void Apresentador_SemPosicaoGravada_AbreNoLugarPadrao()
        {
            var apresentador = new ApresentadorDeJanelas(
                new VisualElement(),
                null,
                null,
                Posicoes(new Dictionary<string, string>())
            );

            apresentador.Mostrar(Definicao("teste.janela"));

            Assert.IsFalse(apresentador.TentarPosicao("teste.janela", out _));
            Assert.IsFalse(apresentador.Raiz.ClassListContains(EstiloCore.JanelaSolta));
            apresentador.Esconder();
        }

        private static DefinicaoDeJanela Definicao(string chave)
        {
            return new DefinicaoDeJanela(chave, "Teste", "", 50, () => new JanelaEmConstrucao("."));
        }
    }
}
