using JogoDoGrupo.Shared.Features.Npcs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Npcs
{
    public class PrologoDoSolTestes
    {
        [Fact]
        public void Falas_SaoContiguasCurtasEComFalantesValidos()
        {
            var anterior = 0f;
            foreach (var fala in PrologoDoSol.Falas)
            {
                Assert.Equal(anterior, fala.Inicio, 3);
                Assert.True(fala.Duracao > 0f);
                Assert.InRange(fala.Texto.Length, 1, 120);
                Assert.True(
                    fala.Falante == FalaDoPrologo.Narrador
                        || fala.Falante == FalaDoPrologo.Viajante
                        || (fala.Falante >= 0 && fala.Falante < PrologoDoSol.QuantidadeDeMagos)
                );
                anterior = fala.Fim;
            }

            Assert.Equal(anterior, PrologoDoSol.DuracaoTotal, 3);
            Assert.Equal(PrologoDoSol.QuantidadeDeMagos, PrologoDoSol.NomesDosMagos.Length);
        }

        [Fact]
        public void FalaEm_AcertaOInstanteENaoHaFalaDepoisDoFim()
        {
            Assert.Same(PrologoDoSol.Falas[0], PrologoDoSol.FalaEm(0f));
            Assert.Same(PrologoDoSol.Falas[2], PrologoDoSol.FalaEm(14f));
            Assert.Null(PrologoDoSol.FalaEm(PrologoDoSol.DuracaoTotal));
            Assert.Null(PrologoDoSol.FalaEm(-1f));
        }

        [Fact]
        public void Clarao_VemDepoisDaEsferaCrescerEDissolveAManadaSoDepois()
        {
            Assert.Equal(0f, PrologoDoSol.ClaraoEm(PrologoDoSol.TempoDoClarao - 1f), 3);
            Assert.Equal(
                1f,
                PrologoDoSol.ClaraoEm(
                    PrologoDoSol.TempoDoClarao + PrologoDoSol.DuracaoDoClarao / 2f
                ),
                3
            );
            Assert.Equal(
                0f,
                PrologoDoSol.ClaraoEm(
                    PrologoDoSol.TempoDoClarao + PrologoDoSol.DuracaoDoClarao + 1f
                ),
                3
            );

            Assert.Equal(0f, PrologoDoSol.DissolucaoEm(PrologoDoSol.TempoDoClarao), 3);
            Assert.Equal(1f, PrologoDoSol.DissolucaoEm(PrologoDoSol.DuracaoTotal), 3);

            var anterior = -1f;
            for (var t = 0f; t <= PrologoDoSol.DuracaoTotal; t += 0.5f)
            {
                var brilho = PrologoDoSol.BrilhoEm(t);
                Assert.True(brilho >= anterior);
                anterior = brilho;
            }

            Assert.Equal(1f, PrologoDoSol.BrilhoEm(PrologoDoSol.TempoDoClarao), 3);
            Assert.Equal(1f, PrologoDoSol.AproximacaoEm(PrologoDoSol.TempoDoClarao), 3);
        }

        [Fact]
        public void ViajanteFechaACenaApontandoAParaACidade_SemEntregarMissao()
        {
            var ultima = PrologoDoSol.Falas[PrologoDoSol.Falas.Length - 1];
            Assert.Equal(FalaDoPrologo.Viajante, ultima.Falante);
            Assert.Contains("Brasa Mansa", ultima.Texto);
        }
    }
}
