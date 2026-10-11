using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    /// <summary>Como a resposta de recursos vira barras (sem cálculo de regra).</summary>
    public class ApresentacaoCombateTestes
    {
        [Test]
        public void TentarMontar_Sucesso_CopiaAtualEMaximoDeCadaRecurso()
        {
            var corpo = new RecursosResponse
            {
                VidaAtual = 80,
                VidaMaxima = 100,
                VigorAtual = 30,
                VigorMaximo = 60,
                EnergiaAtual = 5,
                EnergiaMaxima = 20,
            };

            var ok = ApresentacaoCombate.TentarMontar(
                true,
                corpo,
                out var vida,
                out var vigor,
                out var energia
            );

            Assert.IsTrue(ok);
            Assert.AreEqual("Vida 80 / 100", vida.Texto);
            Assert.AreEqual("Vigor 30 / 60", vigor.Texto);
            Assert.AreEqual("Energia 5 / 20", energia.Texto);
            Assert.AreEqual(0.8f, vida.Fracao, 0.001f);
            Assert.AreEqual(0.5f, vigor.Fracao, 0.001f);
            Assert.AreEqual(0.25f, energia.Fracao, 0.001f);
        }

        [Test]
        public void TentarMontar_FalhaOuSemCorpo_NaoMonta()
        {
            Assert.IsFalse(
                ApresentacaoCombate.TentarMontar(false, new RecursosResponse(), out _, out _, out _)
            );
            Assert.IsFalse(ApresentacaoCombate.TentarMontar(true, null, out _, out _, out _));
        }

        [Test]
        public void Fracao_MaximoZero_EZero()
        {
            Assert.AreEqual(0f, new BarraRecurso("Vida", 10, 0).Fracao);
        }

        [Test]
        public void Fracao_AtualAcimaDoMaximo_LimitaEmUm()
        {
            Assert.AreEqual(1f, new BarraRecurso("Vida", 150, 100).Fracao);
        }

        [Test]
        public void Barra_ValorNegativo_ViraZero()
        {
            var barra = new BarraRecurso("Vigor", -5, -1);

            Assert.AreEqual(0, barra.Atual);
            Assert.AreEqual(0, barra.Maximo);
            Assert.AreEqual(0f, barra.Fracao);
        }
    }
}
