using JogoDoGrupo.Shared.Features.Personagem;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Personagem.Tests
{
    public class ProgressoTestes
    {
        [Test]
        public void AplicarProgresso_CopiaOEstadoEZeraORascunho()
        {
            var estado = new EstadoDeProgresso();
            estado.Aplicar(Progresso(nivel: 2, xp: 10, custo: 40, livres: 3));
            estado.AumentarRascunho("Forca");

            estado.Aplicar(
                new ProgressoResponse
                {
                    Nivel = 3,
                    XpAtual = 1,
                    XpDoNivel = 80,
                    PontosLivres = 5,
                    Distribuidos = new PontosDeAtributos { Forca = 2, Vitalidade = 1 },
                }
            );

            Assert.AreEqual(3, estado.Nivel);
            Assert.AreEqual(1, estado.XpAtual);
            Assert.AreEqual(5, estado.PontosLivres);
            Assert.AreEqual(2, estado.Distribuidos.Forca);
            Assert.AreEqual(0, estado.Rascunho.Forca);
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
            Assert.AreEqual(0, estado.Distribuidos.Forca);
        }

        [Test]
        public void Rascunho_NaoGastaPontosAteOServidorResponder()
        {
            var estado = new EstadoDeProgresso();
            estado.Aplicar(Progresso(nivel: 1, xp: 0, custo: 40, livres: 2));

            Assert.IsTrue(estado.AumentarRascunho("Destreza"));
            Assert.IsTrue(estado.AumentarRascunho("Destreza"));
            Assert.IsFalse(estado.AumentarRascunho("Forca"));

            var pedido = estado.MontarPedido();
            Assert.AreEqual(2, estado.PontosLivres);
            Assert.AreEqual(2, pedido.Pontos.Destreza);
            Assert.AreEqual(0, pedido.Pontos.Forca);

            estado.Aplicar(Progresso(nivel: 1, xp: 0, custo: 40, livres: 2));
            Assert.AreEqual(2, estado.PontosLivres);
            Assert.IsNull(estado.MontarPedido());
        }

        [Test]
        public void PedidoInvalido_NaoGeraIntencao()
        {
            var estado = new EstadoDeProgresso();
            estado.Aplicar(Progresso(nivel: 1, xp: 0, custo: 40, livres: 1));

            Assert.IsNull(estado.MontarPedido());
            Assert.AreEqual(1, estado.PontosLivres);
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
                Distribuidos = new PontosDeAtributos(),
            };
    }
}
