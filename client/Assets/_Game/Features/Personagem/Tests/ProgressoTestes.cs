using JogoDoGrupo.Shared.Features.Personagem;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Personagem.Tests
{
    public class ProgressoTestes
    {
        [Test]
        public void AplicarProgresso_CopiaOEstado()
        {
            var estado = new EstadoDeProgresso();
            estado.Aplicar(Progresso(nivel: 2, xp: 10, custo: 40, livres: 3));

            estado.Aplicar(
                new ProgressoResponse
                {
                    Nivel = 3,
                    XpAtual = 1,
                    XpDoNivel = 80,
                    PontosLivres = 5,
                }
            );

            Assert.AreEqual(3, estado.Nivel);
            Assert.AreEqual(1, estado.XpAtual);
            Assert.AreEqual(5, estado.PontosLivres);
        }

        [Test]
        public void ExperienciaGanha_SubstituiOEstadoSemSomarAQuantidade()
        {
            var estado = new EstadoDeProgresso();
            estado.Aplicar(Progresso(nivel: 1, xp: 10, custo: 40, livres: 0));
            estado.Aplicar(
                new ExperienciaGanha
                {
                    Quantidade = 50,
                    XpAtual = 20,
                    XpDoNivel = 80,
                    Nivel = 2,
                    PontosLivres = 3,
                }
            );

            Assert.AreEqual(20, estado.XpAtual);
            Assert.AreEqual(2, estado.Nivel);
            Assert.AreEqual(3, estado.PontosLivres);
        }

        [Test]
        public void SubiuDeNivel_NaoMexeNaBarraDoDono()
        {
            var estado = new EstadoDeProgresso();
            estado.Aplicar(Progresso(nivel: 4, xp: 3, custo: 10, livres: 0));

            estado.Aplicar(new SubiuDeNivel { PersonagemId = 9, Nivel = 6 });

            Assert.AreEqual(4, estado.Nivel);
            Assert.AreEqual(3, estado.XpAtual);
            Assert.IsTrue(estado.TentarNivelNaArea(9, out var nivel));
            Assert.AreEqual(6, nivel);
        }

        private static ProgressoResponse Progresso(int nivel, long xp, long custo, int livres) =>
            new ProgressoResponse
            {
                Nivel = nivel,
                XpAtual = xp,
                XpDoNivel = custo,
                PontosLivres = livres,
            };
    }
}
