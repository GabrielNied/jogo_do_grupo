using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Features.Mundo.Presenca;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    /// <summary>Nome, raça e visual de reserva dos outros jogadores (#498).</summary>
    public class VisualDosOutrosJogadoresTestes
    {
        [Test]
        public void Entrou_GuardaNomeERacaDoServidor()
        {
            var registro = new RegistroDeAvatares(1);

            registro.Entrou(
                new JogadorVisivel
                {
                    PersonagemId = 2,
                    Nome = "Bia",
                    Raca = 5,
                }
            );

            Assert.IsTrue(registro.TentarObter(2, out var avatar));
            Assert.AreEqual("Bia", avatar.Nome);
            Assert.AreEqual(5, avatar.Raca);
        }

        [Test]
        public void Aplicar_EstadoDaCidadeTrazNomeERacaDeCadaUm()
        {
            var registro = new RegistroDeAvatares(1);

            registro.Aplicar(
                new EstadoDaCidade
                {
                    Outros = new[]
                    {
                        new JogadorVisivel
                        {
                            PersonagemId = 2,
                            Nome = "Bia",
                            Raca = 2,
                        },
                        new JogadorVisivel
                        {
                            PersonagemId = 3,
                            Nome = "Caio",
                            Raca = 3,
                        },
                    },
                }
            );

            Assert.IsTrue(registro.TentarObter(3, out var caio));
            Assert.AreEqual("Caio", caio.Nome);
            Assert.AreEqual(3, caio.Raca);
        }

        [Test]
        public void IdentidadeTardia_AvisaParaTrocarOModelo()
        {
            var registro = new RegistroDeAvatares(1);
            registro.Mover(new PosicaoJogador { PersonagemId = 2 });
            AvatarRemoto mudou = null;
            registro.IdentidadeMudou += a => mudou = a;

            registro.Entrou(
                new JogadorVisivel
                {
                    PersonagemId = 2,
                    Nome = "Bia",
                    Raca = 4,
                }
            );

            Assert.IsNotNull(mudou);
            Assert.AreEqual(4, mudou.Raca);
        }

        [Test]
        public void DefinirIdentidade_InformacaoVaziaNaoApagaOQueSeSabe()
        {
            var avatar = new AvatarRemoto(2, Vector3.zero, 0f, "Bia", 4);

            Assert.IsFalse(avatar.DefinirIdentidade("", 0));

            Assert.AreEqual("Bia", avatar.Nome);
            Assert.AreEqual(4, avatar.Raca);
        }

        [Test]
        public void SemCriadorRegistrado_OVisualDeReservaEUmaCapsulaNaCena()
        {
            var visual = EntradaNoJogo.CriarVisualDeOutroJogador(99);
            try
            {
                Assert.IsNotNull(visual);
                Assert.IsTrue(visual.scene.IsValid());
            }
            finally
            {
                Object.DestroyImmediate(visual);
            }
        }

        [TestCase("Bia", 10f, true, true)]
        [TestCase("", 10f, true, false)]
        [TestCase("   ", 10f, true, false)]
        [TestCase("Bia", 10f, false, false)]
        [TestCase("Bia", 500f, true, false)]
        public void Etiqueta_SoApareceComNomePertoEAFrente(
            string nome,
            float distancia,
            bool aFrente,
            bool esperado
        )
        {
            Assert.AreEqual(esperado, EtiquetasDeNomes.Visivel(nome, distancia, aFrente));
        }
    }
}
