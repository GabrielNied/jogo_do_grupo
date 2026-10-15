using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class ProjecaoDoMapaTestes
    {
        [Test]
        public void DoMapa_EInversoDoNoMapa_ECoordenadasArredondam()
        {
            ProjecaoDoMapa.NoMapa(-220f, -80f, out var u, out var v);
            ProjecaoDoMapa.DoMapa(u, v, out var x, out var z);
            Assert.AreEqual(-220f, x, 0.01f);
            Assert.AreEqual(-80f, z, 0.01f);

            ProjecaoDoMapa.DoMapa(0.5f, 0.5f, out x, out z);
            Assert.AreEqual(0f, x, 0.01f);
            Assert.AreEqual(0f, z, 0.01f);

            Assert.AreEqual("X -220  Z -80", ProjecaoDoMapa.Coordenadas(-219.6f, -80.2f));
        }

        [Test]
        public void Minimapa_CentroBordaEForaDoRaio()
        {
            Assert.IsTrue(ProjecaoDoMapa.NoMinimapa(0f, 0f, out var x, out var y));
            Assert.AreEqual(0f, x, 0.001f);
            Assert.AreEqual(0f, y, 0.001f);

            Assert.IsTrue(
                ProjecaoDoMapa.NoMinimapa(0f, ProjecaoDoMapa.RaioDoMinimapa, out x, out y)
            );
            Assert.AreEqual(0f, x, 0.001f);
            Assert.AreEqual(1f, y, 0.001f);

            Assert.IsFalse(
                ProjecaoDoMapa.NoMinimapa(
                    ProjecaoDoMapa.RaioDoMinimapa,
                    ProjecaoDoMapa.RaioDoMinimapa,
                    out _,
                    out _
                )
            );
            Assert.IsFalse(
                ProjecaoDoMapa.NoMinimapa(0f, ProjecaoDoMapa.RaioDoMinimapa + 1f, out _, out _)
            );
        }

        [Test]
        public void Seta_GiraComARotacaoY()
        {
            Assert.AreEqual(0f, ProjecaoDoMapa.AnguloDaSeta(0f), 0.001f);
            Assert.AreEqual(90f, ProjecaoDoMapa.AnguloDaSeta(90f), 0.001f);
            Assert.AreEqual(-35f, ProjecaoDoMapa.AnguloDaSeta(-35f), 0.001f);
        }

        [Test]
        public void Mapa_CobreOMundoEDestacaSoAAreaAtual()
        {
            ProjecaoDoMapa.NoMapa(0f, 0f, out var u, out var v);
            Assert.AreEqual(0.5f, u, 0.001f);
            Assert.AreEqual(0.5f, v, 0.001f);

            ProjecaoDoMapa.NoMapa(-MapaDoMundo.Metade, -MapaDoMundo.Metade, out u, out v);
            Assert.AreEqual(0f, u, 0.001f);
            Assert.AreEqual(0f, v, 0.001f);

            Assert.IsTrue(
                ProjecaoDoMapa.NaAreaAtual(ProjecaoDoMapa.AreaDaCidade, ProjecaoDoMapa.AreaDaCidade)
            );
            Assert.IsFalse(ProjecaoDoMapa.NaAreaAtual("dungeon", ProjecaoDoMapa.AreaDaCidade));
            Assert.IsTrue(ProjecaoDoMapa.DentroDaCidade(0f, 0f));
            Assert.IsFalse(ProjecaoDoMapa.DentroDaCidade(100f, 0f));
        }

        [Test]
        public void AmostraDoCentro_CaiNaCidade()
        {
            var meio = ProjecaoDoMapa.LadoDasAmostras / 2;
            ProjecaoDoMapa.MundoDaAmostra(meio, meio, out var x, out var z);
            Assert.AreEqual(Bioma.Cidade, MapaDoMundo.BiomaEm(x, z));
        }

        [Test]
        public void LugaresFicamSobreAsEstruturasDoMundo()
        {
            var esperado = new[] { (107, 137), (158, 149), (103, 145), (129, 122) };
            var lado = ProjecaoDoMapa.LadoDasAmostras;
            Assert.AreEqual(esperado.Length, EstruturasDoMapa.Lugares.Length);
            for (var i = 0; i < esperado.Length; i++)
            {
                ProjecaoDoMapa.PixelDoLugar(
                    EstruturasDoMapa.Lugares[i],
                    lado,
                    lado,
                    out var x,
                    out var y
                );
                Assert.AreEqual(esperado[i].Item1, x);
                Assert.AreEqual(esperado[i].Item2, y);
            }
        }

        [Test]
        public void TexturaDoMapa_EGeradaUmaVez()
        {
            var geracoes = TexturaDoMapa.Geracoes;
            Assert.IsTrue(TexturaDoMapa.Avancar(ProjecaoDoMapa.LadoDasAmostras));
            Assert.IsNotNull(TexturaDoMapa.Pronta);
            Assert.AreEqual(ProjecaoDoMapa.LadoDasAmostras, TexturaDoMapa.Pronta.width);
            Assert.AreEqual(ProjecaoDoMapa.LadoDasAmostras, TexturaDoMapa.Pronta.height);
            Assert.IsTrue(TexturaDoMapa.Avancar(1));
            Assert.AreEqual(geracoes + (geracoes == 0 ? 1 : 0), TexturaDoMapa.Geracoes);
        }

        [Test]
        public void SobOChao_so_vale_dentro_do_tunel_da_caverna()
        {
            PrimeiraCaverna.PontoDoPercurso(40f, out var x, out var z);
            var piso = PrimeiraCaverna.PisoNoPlano(x, z).Value;

            Assert.IsTrue(ProjecaoDoMapa.SobOChao(x, piso, z));
            Assert.IsFalse(
                ProjecaoDoMapa.SobOChao(x, MapaDoMundo.AlturaEm(x, z), z),
                "em cima do morro"
            );
            Assert.IsFalse(ProjecaoDoMapa.SobOChao(0f, 0f, 0f));
            Assert.IsFalse(ProjecaoDoMapa.SobOChao(0f, 400f, 0f));
        }

        [Test]
        public void TeclaM_AbreOMapa()
        {
            var definicao = JanelaMapa.CriarDefinicao();

            Assert.AreEqual("M", definicao.Tecla);
            Assert.AreEqual(JanelaMapa.Chave, definicao.Chave);
            Assert.IsInstanceOf<JanelaDoMapa>(definicao.Criar());
        }
    }
}
