using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class MedidasInventarioTestes
    {
        [Test]
        public void EspacoDeSobraUsaACasaMaxima()
        {
            Assert.AreEqual(
                MedidasInventario.CasaMaxima,
                MedidasInventario.TamanhoDaCasa(1200, 900, 100)
            );
        }

        [Test]
        public void PoucaAlturaEncolheACasa()
        {
            // (420 - 100) / 7 - 4 = 41
            Assert.AreEqual(41, MedidasInventario.TamanhoDaCasa(1200, 420, 100));
        }

        [Test]
        public void PoucaLarguraEncolheACasa()
        {
            // (560 - 160 - 8) / 10 = 39,2 -> abaixo do mínimo
            Assert.AreEqual(
                MedidasInventario.CasaMinima,
                MedidasInventario.TamanhoDaCasa(560, 900, 100)
            );
            // (700 - 160 - 8) / 10 = 53,2
            Assert.AreEqual(53, MedidasInventario.TamanhoDaCasa(700, 900, 100));
        }

        [Test]
        public void EspacoInsuficienteFicaNoMinimoSemQuebrar()
        {
            Assert.AreEqual(
                MedidasInventario.CasaMinima,
                MedidasInventario.TamanhoDaCasa(100, 50, 100)
            );
        }

        [Test]
        public void GradeAcompanhaACasaERolaSoComMuitasLinhas()
        {
            Assert.AreEqual(8 * 52f, MedidasInventario.LarguraDaGrade(48, 8, false));
            Assert.AreEqual(8 * 52f + 16f, MedidasInventario.LarguraDaGrade(48, 8, true));
            Assert.AreEqual(5 * 52f, MedidasInventario.AlturaDaGrade(48, 5));
            Assert.AreEqual(5 * 52f, MedidasInventario.AlturaDaGrade(48, 9));
            Assert.AreEqual(3 * 52f, MedidasInventario.AlturaDaGrade(48, 3));
        }
    }
}
