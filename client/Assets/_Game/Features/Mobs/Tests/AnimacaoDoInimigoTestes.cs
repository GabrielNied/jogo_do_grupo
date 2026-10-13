using JogoDoGrupo.Shared.Features.Mobs;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Mobs.Tests
{
    /// <summary>Quando o modelo do inimigo anda e quando dispara o golpe.</summary>
    public class AnimacaoDoInimigoTestes
    {
        private const float Quadro = 0.02f;

        private static void Rodar(
            AnimacaoDoInimigo animacao,
            EstadoDeInimigo estado,
            float velocidade,
            int quadros,
            int golpes = 0
        )
        {
            for (var i = 0; i < quadros; i++)
            {
                animacao.Atualizar(estado, velocidade * Quadro, Quadro, golpes);
            }
        }

        [TestCase(EstadoDeInimigo.Perseguicao)]
        [TestCase(EstadoDeInimigo.Retorno)]
        public void Andando_EmPerseguicaoOuRetornoComVelocidade_Anda(EstadoDeInimigo estado)
        {
            var animacao = new AnimacaoDoInimigo();

            Rodar(animacao, estado, 3f, 30);

            Assert.IsTrue(animacao.Andando);
        }

        [TestCase(EstadoDeInimigo.Alerta)]
        [TestCase(EstadoDeInimigo.Ataque)]
        [TestCase(EstadoDeInimigo.Morto)]
        public void Andando_EmEstadoParado_NaoAndaMesmoComSobraDeMovimento(EstadoDeInimigo estado)
        {
            var animacao = new AnimacaoDoInimigo();
            Rodar(animacao, EstadoDeInimigo.Perseguicao, 3f, 30);

            Rodar(animacao, estado, 3f, 1);

            Assert.IsFalse(animacao.Andando);
        }

        [Test]
        public void Andando_ManadaEmRepousoSeguindoARota_Anda()
        {
            var animacao = new AnimacaoDoInimigo();

            Rodar(animacao, EstadoDeInimigo.Repouso, 1.5f, 30);

            Assert.IsTrue(animacao.Andando);
        }

        [Test]
        public void Andando_EmRepousoParado_NaoAnda()
        {
            var animacao = new AnimacaoDoInimigo();

            Rodar(animacao, EstadoDeInimigo.Repouso, 0f, 30);

            Assert.IsFalse(animacao.Andando);
        }

        [Test]
        public void Andando_PerseguindoMasParado_NaoAndaNoLugar()
        {
            var animacao = new AnimacaoDoInimigo();

            Rodar(animacao, EstadoDeInimigo.Perseguicao, 0f, 30);

            Assert.IsFalse(animacao.Andando);
        }

        [Test]
        public void Andando_VelocidadeEntreOsLimites_MantemOEstadoAnterior()
        {
            var animacao = new AnimacaoDoInimigo();
            var meio =
                (AnimacaoDoInimigo.VelocidadeParaAndar + AnimacaoDoInimigo.VelocidadeParaParar)
                / 2f;

            Rodar(animacao, EstadoDeInimigo.Perseguicao, meio, 60);
            Assert.IsFalse(animacao.Andando);

            Rodar(animacao, EstadoDeInimigo.Perseguicao, 3f, 30);
            Rodar(animacao, EstadoDeInimigo.Perseguicao, meio, 60);
            Assert.IsTrue(animacao.Andando);
        }

        [Test]
        public void DisparouAtaque_DisparaUmaVezPorGolpe()
        {
            var animacao = new AnimacaoDoInimigo();

            Rodar(animacao, EstadoDeInimigo.Ataque, 0f, 1, golpes: 0);
            Assert.IsFalse(animacao.DisparouAtaque);

            Rodar(animacao, EstadoDeInimigo.Ataque, 0f, 1, golpes: 1);
            Assert.IsTrue(animacao.DisparouAtaque);

            Rodar(animacao, EstadoDeInimigo.Ataque, 0f, 1, golpes: 1);
            Assert.IsFalse(animacao.DisparouAtaque);

            Rodar(animacao, EstadoDeInimigo.Ataque, 0f, 1, golpes: 2);
            Assert.IsTrue(animacao.DisparouAtaque);
        }

        [Test]
        public void PrepararAtaque_NoRegistro_ContaOGolpeDoInimigo()
        {
            var registro = new RegistroDeInimigos();
            registro.SubstituirArea(
                new InimigosDaArea
                {
                    Inimigos = new[]
                    {
                        new InimigoNoMundo
                        {
                            InimigoId = 7,
                            CriaturaId = CatalogoCriaturas.JavaliDoCampo,
                            Vida = 10,
                            VidaMaxima = 10,
                        },
                    },
                }
            );

            registro.PrepararAtaque(new InimigoPreparaAtaque { InimigoId = 7, PreparoMs = 500 });
            registro.Atualizar(Quadro);

            Assert.AreEqual(1, registro.Obter(7).GolpesPreparados);
            Assert.IsTrue(registro.Obter(7).Animacao.DisparouAtaque);
        }

        [Test]
        public void VelocidadeDoClipe_FazOContatoCairNoFimDoPreparo()
        {
            // Clipe de 1 s, contato no meio, preparo de 1 s: o contato leva 0,5 s, então 0,5x.
            Assert.AreEqual(0.5f, AnimacaoDoInimigo.VelocidadeDoClipe(1f, 1f), 0.001f);
            // Preparo mais curto acelera o clipe.
            Assert.AreEqual(0.5f / 0.7f, AnimacaoDoInimigo.VelocidadeDoClipe(1f, 0.7f), 0.001f);
            // Sem clipe ou sem preparo: velocidade normal.
            Assert.AreEqual(1f, AnimacaoDoInimigo.VelocidadeDoClipe(0f, 1f));
            Assert.AreEqual(1f, AnimacaoDoInimigo.VelocidadeDoClipe(1f, 0f));
        }
    }
}
