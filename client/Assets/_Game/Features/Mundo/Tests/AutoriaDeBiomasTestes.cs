using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class AutoriaDeBiomasTestes
    {
        [TearDown]
        public void LimparPrevia()
        {
            MapaDoMundo.DefinirRegioesDePrevia(null);
            PecasDosBiomas.LimparCache();
        }

        [Test]
        public void Mata_pintada_fica_mais_cheia_quando_a_densidade_aumenta()
        {
            var pontos = new[]
            {
                new RegiaoDeBioma.Ponto(700f, -500f),
                new RegiaoDeBioma.Ponto(900f, -500f),
                new RegiaoDeBioma.Ponto(900f, -300f),
                new RegiaoDeBioma.Ponto(700f, -300f),
            };
            int Contar(float densidade)
            {
                MapaDoMundo.DefinirRegioesDePrevia(
                    new[] { new RegiaoDeBioma("Mata", Bioma.Mata, pontos, 20f, 1f, densidade) }
                );
                var total = 0;
                foreach (var peca in PecasDosBiomas.Gerar())
                {
                    if (
                        peca.X >= 700f
                        && peca.X <= 900f
                        && peca.Z >= -500f
                        && peca.Z <= -300f
                        && peca.Prefab.StartsWith("Arvore")
                    )
                    {
                        total++;
                    }
                }

                return total;
            }

            Assert.Greater(Contar(2f), Contar(1f));
        }

        [Test]
        public void Regiao_muda_pesos_altura_e_vegetacao_sem_mudar_cidades()
        {
            const float x = 700f;
            const float z = -400f;
            var alturaAnterior = MapaDoMundo.AlturaEm(x, z);
            var cidadeAnterior = MapaDoMundo.AlturaEm(0f, 0f);
            var pesosDaCidade = MapaDoMundo.PesosEm(0f, 0f);
            MapaDoMundo.DefinirRegioesDePrevia(
                new[]
                {
                    new RegiaoDeBioma(
                        "Mata de teste",
                        Bioma.Colinas,
                        new[]
                        {
                            new RegiaoDeBioma.Ponto(650f, -450f),
                            new RegiaoDeBioma.Ponto(750f, -450f),
                            new RegiaoDeBioma.Ponto(750f, -350f),
                            new RegiaoDeBioma.Ponto(650f, -350f),
                        },
                        20f,
                        1.5f,
                        1.7f
                    ),
                }
            );

            Assert.AreEqual(Bioma.Colinas, MapaDoMundo.BiomaEm(x, z));
            Assert.AreNotEqual(alturaAnterior, MapaDoMundo.AlturaEm(x, z));
            Assert.AreEqual(1.7f, MapaDoMundo.DensidadeDaVegetacaoEm(x, z), 0.001f);
            Assert.AreEqual(cidadeAnterior, MapaDoMundo.AlturaEm(0f, 0f));
            Assert.AreEqual(pesosDaCidade.Mata, MapaDoMundo.PesosEm(0f, 0f).Mata);
        }

        [Test]
        public void Altura_da_previa_e_a_mesma_na_grade_e_no_chao_interpolado()
        {
            MapaDoMundo.DefinirRegioesDePrevia(
                new[]
                {
                    new RegiaoDeBioma(
                        "Colina",
                        Bioma.Colinas,
                        new[]
                        {
                            new RegiaoDeBioma.Ponto(550f, -250f),
                            new RegiaoDeBioma.Ponto(650f, -250f),
                            new RegiaoDeBioma.Ponto(650f, -150f),
                            new RegiaoDeBioma.Ponto(550f, -150f),
                        },
                        20f,
                        1f,
                        1f
                    ),
                }
            );

            const int i = 210;
            const int j = 130;
            var x = MapaDoMundo.CoordenadaDaGrade(i);
            var z = MapaDoMundo.CoordenadaDaGrade(j);
            var grade = MapaDoMundo.AmostrarNaGrade(i, j, out var pesos);

            Assert.AreEqual(grade, MapaDoMundo.AlturaEm(x, z), 0.001f);
            Assert.AreEqual(pesos.Dominante, MapaDoMundo.BiomaEm(x, z));
        }
    }
}
