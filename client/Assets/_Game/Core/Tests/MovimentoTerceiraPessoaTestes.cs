using JogoDoGrupo.Client.Core.Jogo.Controle;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class MovimentoTerceiraPessoaTestes
    {
        private const float Tolerancia = 0.0001f;

        [Test]
        public void SemInput_FicaParado()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(Vector3.zero, MovimentoTerceiraPessoa.DirecaoNoMundo(Vector2.zero, 0f));
            Assert.AreEqual(0f, movimento.VelocidadeAlvo(Vector2.zero, false));
        }

        [Test]
        public void InputNaZonaMorta_FicaParado()
        {
            var movimento = new MovimentoTerceiraPessoa();
            var quaseNada = new Vector2(0.05f, 0.05f);

            Assert.AreEqual(Vector3.zero, MovimentoTerceiraPessoa.DirecaoNoMundo(quaseNada, 0f));
            Assert.AreEqual(0f, movimento.VelocidadeAlvo(quaseNada, true));
        }

        [Test]
        public void Frente_ComCameraParaZ_AndaParaZ()
        {
            var direcao = MovimentoTerceiraPessoa.DirecaoNoMundo(Vector2.up, 0f);

            AssertVetor(Vector3.forward, direcao);
        }

        [Test]
        public void Frente_ComCameraVirada90_AndaParaX()
        {
            var direcao = MovimentoTerceiraPessoa.DirecaoNoMundo(Vector2.up, 90f);

            AssertVetor(Vector3.right, direcao);
        }

        [Test]
        public void Direita_ComCameraParaZ_AndaParaX()
        {
            var direcao = MovimentoTerceiraPessoa.DirecaoNoMundo(Vector2.right, 0f);

            AssertVetor(Vector3.right, direcao);
        }

        [Test]
        public void Diagonal_DirecaoUnitariaNoPlano()
        {
            var direcao = MovimentoTerceiraPessoa.DirecaoNoMundo(new Vector2(1f, 1f), 37f);

            Assert.AreEqual(1f, direcao.magnitude, Tolerancia);
            Assert.AreEqual(0f, direcao.y, Tolerancia);
        }

        [Test]
        public void Andar_UsaVelocidadeDeAndar()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(
                movimento.VelocidadeAndar,
                movimento.VelocidadeAlvo(Vector2.up, false),
                Tolerancia
            );
        }

        [Test]
        public void Correr_UsaVelocidadeDeCorrer()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(
                movimento.VelocidadeCorrer,
                movimento.VelocidadeAlvo(Vector2.up, true),
                Tolerancia
            );
            Assert.Greater(movimento.VelocidadeCorrer, movimento.VelocidadeAndar);
        }

        [Test]
        public void DiagonalDoTeclado_NaoAndaMaisRapido()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(
                movimento.VelocidadeAndar,
                movimento.VelocidadeAlvo(new Vector2(1f, 1f), false),
                Tolerancia
            );
        }

        [Test]
        public void AnalogicoPelaMetade_AndaPelaMetade()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(
                movimento.VelocidadeCorrer * 0.5f,
                movimento.VelocidadeAlvo(new Vector2(0f, 0.5f), true),
                Tolerancia
            );
        }

        [Test]
        public void Acelerar_LimitadoPelaAceleracao()
        {
            var movimento = new MovimentoTerceiraPessoa { Aceleracao = 10f };

            Assert.AreEqual(1f, movimento.Acelerar(0f, 5f, 0.1f), Tolerancia);
            Assert.AreEqual(5f, movimento.Acelerar(4.5f, 5f, 0.1f), Tolerancia);
            Assert.AreEqual(4f, movimento.Acelerar(5f, 0f, 0.1f), Tolerancia);
        }

        [Test]
        public void Acelerar_TempoNegativo_NaoMuda()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(2f, movimento.Acelerar(2f, 5f, -1f), Tolerancia);
        }

        [Test]
        public void NoChao_GrudaNoChao()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(
                MovimentoTerceiraPessoa.VelocidadeGrudarNoChao,
                movimento.VelocidadeVertical(-10f, true, 0.1f),
                Tolerancia
            );
        }

        [Test]
        public void NoAr_SomaGravidade()
        {
            var movimento = new MovimentoTerceiraPessoa { Gravidade = 20f };

            Assert.AreEqual(-2f, movimento.VelocidadeVertical(0f, false, 0.1f), Tolerancia);
        }

        [Test]
        public void Subindo_NoChao_NaoGruda()
        {
            var movimento = new MovimentoTerceiraPessoa { Gravidade = 20f };

            Assert.AreEqual(3f, movimento.VelocidadeVertical(5f, true, 0.1f), Tolerancia);
        }

        [Test]
        public void Queda_LimitadaPelaMaxima()
        {
            var movimento = new MovimentoTerceiraPessoa { VelocidadeMaximaQueda = 50f };

            Assert.AreEqual(-50f, movimento.VelocidadeVertical(-49.9f, false, 1f), Tolerancia);
        }

        private static void AssertVetor(Vector3 esperado, Vector3 obtido)
        {
            Assert.AreEqual(esperado.x, obtido.x, Tolerancia, "x");
            Assert.AreEqual(esperado.y, obtido.y, Tolerancia, "y");
            Assert.AreEqual(esperado.z, obtido.z, Tolerancia, "z");
        }
    }
}
