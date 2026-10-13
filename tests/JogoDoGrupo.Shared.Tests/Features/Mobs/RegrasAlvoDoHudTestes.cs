using JogoDoGrupo.Shared.Features.Mobs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs
{
    public class RegrasAlvoDoHudTestes
    {
        private static RegrasAlvoDoHud.Candidato Comum(
            long id,
            float distancia,
            int vida = 50,
            float mira = 1f
        ) =>
            new RegrasAlvoDoHud.Candidato(
                id,
                CatalogoCriaturas.JavaliDoCampo,
                vida,
                100,
                distancia,
                mira,
                0
            );

        private static RegrasAlvoDoHud.Candidato Chefe(long id, float distancia, int vida = 5000) =>
            new RegrasAlvoDoHud.Candidato(
                id,
                CatalogoCriaturas.GuardiaoDoReflexo,
                vida,
                6000,
                distancia,
                1f,
                0
            );

        [Fact]
        public void EscolherAlvoComum_SobMira_GanhaDoUltimoAcertado()
        {
            var lista = new[] { Comum(1, 10f), Comum(2, 8f) };
            var alvo = RegrasAlvoDoHud.EscolherAlvoComum(lista, 2, 1, 1.0);
            Assert.Equal(2, alvo);
        }

        [Fact]
        public void EscolherAlvoComum_SemMira_UsaUltimoContatoDentroDaJanela()
        {
            var lista = new[] { Comum(1, 10f) };
            var alvo = RegrasAlvoDoHud.EscolherAlvoComum(lista, null, 1, 4.9);
            Assert.Equal(1, alvo);
        }

        [Fact]
        public void EscolherAlvoComum_ForaDe30m_NaoConta()
        {
            var lista = new[] { Comum(1, 31f) };
            Assert.Null(RegrasAlvoDoHud.EscolherAlvoComum(lista, 1, 1, 0.0));
        }

        [Fact]
        public void EscolherAlvoComum_Morto_Some()
        {
            var lista = new[] { Comum(1, 5f, vida: 0) };
            Assert.Null(RegrasAlvoDoHud.EscolherAlvoComum(lista, 1, 1, 0.0));
        }

        [Fact]
        public void EscolherBoss_PertoMostraMesmoSemContato()
        {
            var lista = new[] { Chefe(10, 15f) };
            var boss = RegrasAlvoDoHud.EscolherBoss(lista, 99, null, -1);
            Assert.Equal(10, boss);
        }

        [Fact]
        public void EscolherBoss_LongeSemContato_NaoMostra()
        {
            var lista = new[] { Chefe(10, 40f) };
            Assert.Null(RegrasAlvoDoHud.EscolherBoss(lista, 99, null, -1));
        }

        [Fact]
        public void EscolherAlvoSecundario_NaoRepeteOBoss()
        {
            var lista = new[] { Chefe(10, 5f), Comum(2, 8f) };
            var secundario = RegrasAlvoDoHud.EscolherAlvoSecundario(lista, 10, 2, 10, 0.5);
            Assert.Equal(2, secundario);
        }
    }
}
