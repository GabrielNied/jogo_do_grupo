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

        [Test]
        public void Pular_NoChao_Sobe()
        {
            var movimento = new MovimentoTerceiraPessoa();

            var velocidade = movimento.Pular(
                MovimentoTerceiraPessoa.VelocidadeGrudarNoChao,
                true,
                true
            );

            Assert.Greater(velocidade, 0f);
            Assert.AreEqual(movimento.VelocidadeDoPulo, velocidade, Tolerancia);
        }

        [Test]
        public void Pular_SemPedido_NaoMuda()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(-2f, movimento.Pular(-2f, true, false), Tolerancia);
        }

        [Test]
        public void Pular_NoAr_NaoSomaOutraVelocidade()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(3f, movimento.Pular(3f, false, true), Tolerancia);
            Assert.AreEqual(-4f, movimento.Pular(-4f, false, true), Tolerancia);
        }

        [Test]
        public void Pular_JaSubindo_NaoSomaOutraVelocidade()
        {
            var movimento = new MovimentoTerceiraPessoa();
            var subindo = movimento.Pular(-2f, true, true);

            Assert.AreEqual(subindo, movimento.Pular(subindo, true, true), Tolerancia);
        }

        [Test]
        public void VelocidadeDoPulo_VemDaGravidadeEDaAltura()
        {
            var movimento = new MovimentoTerceiraPessoa { Gravidade = 20f, AlturaPulo = 1.25f };

            Assert.AreEqual(Mathf.Sqrt(2f * 20f * 1.25f), movimento.VelocidadeDoPulo, Tolerancia);
        }

        [Test]
        public void AlturaEstimadaDoPulo_FicaPertoDe125Metros()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(1.25f, movimento.AlturaEstimadaDoPulo, 0.01f);
        }

        [Test]
        public void Pulo_SimuladoFrameAFrame_AlcancaCercaDe125Metros()
        {
            var movimento = new MovimentoTerceiraPessoa();
            const float dt = 1f / 60f;
            var velocidade = movimento.Pular(
                MovimentoTerceiraPessoa.VelocidadeGrudarNoChao,
                true,
                true
            );
            var altura = 0f;
            var maxima = 0f;

            // Mesma ordem do ControleTerceiraPessoa: sobe com a velocidade, depois a gravidade age.
            for (var i = 0; i < 600 && (altura > 0f || i == 0); i++)
            {
                altura += velocidade * dt;
                maxima = Mathf.Max(maxima, altura);
                velocidade = movimento.VelocidadeVertical(velocidade, false, dt);
            }

            Assert.AreEqual(1.25f, maxima, 0.1f);
            Assert.LessOrEqual(altura, 0f);
        }

        [Test]
        public void Gravidade_ZeroOuAlturaZero_NaoGeraPulo()
        {
            Assert.AreEqual(0f, new MovimentoTerceiraPessoa { Gravidade = 0f }.VelocidadeDoPulo);
            Assert.AreEqual(0f, new MovimentoTerceiraPessoa { AlturaPulo = 0f }.VelocidadeDoPulo);
        }

        [Test]
        public void Padrao_Andar2Correr55SemVoo()
        {
            var movimento = new MovimentoTerceiraPessoa();

            Assert.AreEqual(2f, movimento.VelocidadeAndar);
            Assert.AreEqual(5.5f, movimento.VelocidadeCorrer);
            Assert.AreEqual(20f, movimento.Aceleracao);
            Assert.AreEqual(20f, movimento.Gravidade);
            Assert.AreEqual(1.25f, movimento.AlturaPulo);
            Assert.IsFalse(movimento.Voando);
        }

        [Test]
        public void CorrerAlterado_EntraNoCalculo()
        {
            var movimento = new MovimentoTerceiraPessoa();

            movimento.VelocidadeCorrer = 12f;

            Assert.AreEqual(12f, movimento.VelocidadeAlvo(Vector2.up, true), Tolerancia);
            Assert.AreEqual(2f, movimento.VelocidadeAlvo(Vector2.up, false), Tolerancia);
        }

        [Test]
        public void Voando_NoAr_NaoCai()
        {
            var movimento = new MovimentoTerceiraPessoa { Voando = true };

            Assert.AreEqual(0f, movimento.VelocidadeVertical(0f, false, 0.1f), Tolerancia);
            Assert.AreEqual(0f, movimento.VelocidadeVertical(-10f, false, 1f), Tolerancia);
        }

        [Test]
        public void Voando_SobeEDesce()
        {
            var movimento = new MovimentoTerceiraPessoa
            {
                Voando = true,
                VelocidadeVerticalVoo = 6f,
            };

            Assert.AreEqual(6f, movimento.VelocidadeVertical(0f, false, 0.1f, 1f), Tolerancia);
            Assert.AreEqual(-6f, movimento.VelocidadeVertical(0f, true, 0.1f, -1f), Tolerancia);
            Assert.AreEqual(6f, movimento.VelocidadeVertical(0f, false, 0.1f, 5f), Tolerancia);
        }

        [Test]
        public void Voando_NaoPula()
        {
            var movimento = new MovimentoTerceiraPessoa { Voando = true };

            Assert.AreEqual(0f, movimento.Pular(0f, true, true), Tolerancia);
        }

        [Test]
        public void VooDesligado_PuloEQuedaIguais()
        {
            var movimento = new MovimentoTerceiraPessoa { Voando = true };
            movimento.Voando = false;

            Assert.AreEqual(
                movimento.VelocidadeDoPulo,
                movimento.Pular(MovimentoTerceiraPessoa.VelocidadeGrudarNoChao, true, true),
                Tolerancia
            );
            Assert.AreEqual(-2f, movimento.VelocidadeVertical(0f, false, 0.1f, 1f), Tolerancia);
            Assert.AreEqual(
                MovimentoTerceiraPessoa.VelocidadeGrudarNoChao,
                movimento.VelocidadeVertical(-5f, true, 0.1f, 1f),
                Tolerancia
            );
        }

        private static void AssertVetor(Vector3 esperado, Vector3 obtido)
        {
            Assert.AreEqual(esperado.x, obtido.x, Tolerancia, "x");
            Assert.AreEqual(esperado.y, obtido.y, Tolerancia, "y");
            Assert.AreEqual(esperado.z, obtido.z, Tolerancia, "z");
        }
    }
}
