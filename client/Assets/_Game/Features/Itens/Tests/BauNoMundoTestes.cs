using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    /// <summary>Onde o baú fica, quando está perto e o marcador criado em código.</summary>
    public class BauNoMundoTestes
    {
        private static readonly Vector3 Nascimento = new Vector3(0f, 0f, -12f);

        [Test]
        public void PosicaoPerto_FicaAFrenteEADireitaDoNascimento_AMesmaAltura()
        {
            var posicao = BauNoMundo.PosicaoPerto(Nascimento, Vector3.forward, Vector3.right);

            Assert.AreEqual(BauNoMundo.DistanciaLado, posicao.x, 0.001f);
            Assert.AreEqual(Nascimento.z + BauNoMundo.DistanciaFrente, posicao.z, 0.001f);
            Assert.AreEqual(Nascimento.y, posicao.y, 0.001f);
        }

        [Test]
        public void PosicaoPerto_IgnoraAInclinacaoDosEixos()
        {
            var posicao = BauNoMundo.PosicaoPerto(
                Nascimento,
                new Vector3(0f, 0.5f, 1f),
                new Vector3(1f, -0.5f, 0f)
            );

            Assert.AreEqual(Nascimento.y, posicao.y, 0.001f);
        }

        [Test]
        public void PosicaoPerto_NaoNasceEmCimaDoJogador_MasDentroDoAlcanceDeAndar()
        {
            var posicao = BauNoMundo.PosicaoPerto(Nascimento, Vector3.forward, Vector3.right);
            var distancia = Vector3.Distance(Nascimento, posicao);

            // Fora do alcance de abrir (precisa andar até lá) e à vista perto do nascimento.
            Assert.Greater(distancia, BauNoMundo.DistanciaParaAbrir);
            Assert.Less(distancia, 8f);
        }

        [Test]
        public void EstaPerto_DentroDoAlcance_EVerdadeiro()
        {
            var bau = new Vector3(2.5f, 0f, -8.5f);

            Assert.IsTrue(BauNoMundo.EstaPerto(bau + new Vector3(1.5f, 0f, 0f), bau));
            Assert.IsTrue(BauNoMundo.EstaPerto(bau, bau));
        }

        [Test]
        public void EstaPerto_ForaDoAlcance_EFalso()
        {
            var bau = new Vector3(2.5f, 0f, -8.5f);

            Assert.IsFalse(
                BauNoMundo.EstaPerto(
                    bau + new Vector3(0f, 0f, BauNoMundo.DistanciaParaAbrir + 0.1f),
                    bau
                )
            );
        }

        [Test]
        public void EstaPerto_ContaSoODesenhoHorizontal()
        {
            var bau = new Vector3(0f, 0f, 0f);

            Assert.IsTrue(BauNoMundo.EstaPerto(new Vector3(1f, 50f, 0f), bau));
        }

        [Test]
        public void Criar_TemUmColisorNaRaizEOsBlocosSemColisor()
        {
            var bau = BauNoMundo.Criar(new Vector3(1f, 2f, 3f));
            try
            {
                Assert.AreEqual(new Vector3(1f, 2f, 3f), bau.transform.position);
                Assert.IsNotNull(bau.GetComponent<BoxCollider>());
                Assert.AreEqual(2, bau.transform.childCount);
                Assert.AreEqual(1, bau.GetComponentsInChildren<Collider>().Length);
            }
            finally
            {
                Object.DestroyImmediate(bau);
            }
        }
    }
}
