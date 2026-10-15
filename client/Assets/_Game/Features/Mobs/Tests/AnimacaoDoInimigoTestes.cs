using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using NUnit.Framework;
using UnityEngine;

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
            registro.Atualizar(ConfigMovimentoDoInimigo.AtrasoDeRenderSegundos + Quadro);

            Assert.AreEqual(1, registro.Obter(7).GolpesPreparados);
            Assert.IsTrue(registro.Obter(7).Animacao.DisparouAtaque);
        }

        [Test]
        public void VelocidadeDoClipe_FazOContatoCairNoFimDoPreparo()
        {
            // Clipe de 1 s, contato no meio, preparo de 1 s: o contato leva 0,5 s, então 0,5x.
            Assert.AreEqual(0.5f, AnimacaoDoInimigo.VelocidadeDoClipe(1f, 1f, 0.5f), 0.001f);
            // Preparo mais curto acelera o clipe.
            Assert.AreEqual(
                0.5f / 0.7f,
                AnimacaoDoInimigo.VelocidadeDoClipe(1f, 0.7f, 0.5f),
                0.001f
            );
            // Contato mais tarde no clipe pede clipe mais lento para o mesmo preparo.
            Assert.AreEqual(0.85f, AnimacaoDoInimigo.VelocidadeDoClipe(1f, 1f, 0.85f), 0.001f);
            // Sem clipe ou sem preparo: velocidade normal.
            Assert.AreEqual(1f, AnimacaoDoInimigo.VelocidadeDoClipe(0f, 1f, 0.5f));
            Assert.AreEqual(1f, AnimacaoDoInimigo.VelocidadeDoClipe(1f, 0f, 0.5f));
        }

        [TestCase(TipoDeAtaqueDeInimigo.Golpe, "Atacar", 0.71f)]
        [TestCase(TipoDeAtaqueDeInimigo.Investida, "AtacarInvestida", 0.85f)]
        [TestCase(TipoDeAtaqueDeInimigo.Raios, "AtacarRaios", 0.85f)]
        [TestCase(TipoDeAtaqueDeInimigo.Pisada, "AtacarPisada", 0.85f)]
        public void ContatosDosClipes_LigaCadaAtaqueAoSeuClipeEAoSeuContato(
            TipoDeAtaqueDeInimigo tipo,
            string clipe,
            float contato
        )
        {
            Assert.AreEqual(clipe, ContatosDosClipes.ClipeDoAtaque(tipo));
            Assert.AreEqual(contato, ContatosDosClipes.Contato(clipe), 0.0001f);
        }

        [Test]
        public void ContatosDosClipes_ClipeSemMarca_UsaOMeio()
        {
            Assert.AreEqual(0.5f, ContatosDosClipes.Contato("Idle"), 0.0001f);
        }

        [Test]
        public void PrepararAtaque_GuardaOTipoParaEscolherOClipe()
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
                            CriaturaId = CatalogoCriaturas.GuardiaoDoReflexo,
                            Vida = 10,
                            VidaMaxima = 10,
                        },
                    },
                }
            );

            registro.PrepararAtaque(
                new InimigoPreparaAtaque
                {
                    InimigoId = 7,
                    PreparoMs = 3000,
                    Tipo = TipoDeAtaqueDeInimigo.Pisada,
                }
            );

            Assert.AreEqual(TipoDeAtaqueDeInimigo.Pisada, registro.Obter(7).TipoDoGolpe);
        }

        [Test]
        public void VelocidadeDoClipe_NoAvisoDoChefe_AlongaOClipeParaOContatoCairNosTresSegundos()
        {
            const float aviso = 3f;
            const float clipe = 1f;

            var contato = ContatosDosClipes.ContatoInvestida;

            var velocidade = AnimacaoDoInimigo.VelocidadeDoClipe(clipe, aviso, contato);

            // O tempo até a pose de contato, na velocidade escolhida, é o aviso inteiro.
            var ate = clipe * contato / velocidade;
            Assert.AreEqual(aviso, ate, 0.001f);
        }

        // ── Pausa de pose no acerto (#812) ───────────────────────────────────────

        [Test]
        public void AoAcertarAtaque_SeguraPoseDoInimigoAtingido()
        {
            var go = new GameObject("Apresentador");
            var inimigoGo = new GameObject("Inimigo 10");
            try
            {
                var apresentador = go.AddComponent<ApresentadorDeInimigos>();
                var animador = inimigoGo.AddComponent<Animator>();
                apresentador.RegistrarAnimadorParaTeste(10, animador);

                var mensagem = new AtaqueAcertou
                {
                    AtacanteId = 1,
                    Acertos = new[]
                    {
                        new AcertoDeAtaque
                        {
                            AlvoId = 10,
                            Dano = 25,
                            VidaRestante = 75,
                            VidaMaxima = 100,
                        },
                    },
                };

                apresentador.AoAcertarAtaque(mensagem);

                Assert.IsTrue(apresentador.TemPoseSegurada(10));
                Assert.AreEqual(
                    RegrasReacao.PausaVisualNoAcertoMs / 1000f,
                    apresentador.RestanteDaPose(10),
                    0.001f
                );
            }
            finally
            {
                Object.DestroyImmediate(inimigoGo);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AoAcertarAtaque_IgnoraAlvoSemAnimador()
        {
            var go = new GameObject("Apresentador");
            try
            {
                var apresentador = go.AddComponent<ApresentadorDeInimigos>();

                var mensagem = new AtaqueAcertou
                {
                    AtacanteId = 1,
                    Acertos = new[]
                    {
                        new AcertoDeAtaque { AlvoId = 999, Dano = 25 },
                    },
                };

                apresentador.AoAcertarAtaque(mensagem);

                Assert.IsFalse(apresentador.TemPoseSegurada(999));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
