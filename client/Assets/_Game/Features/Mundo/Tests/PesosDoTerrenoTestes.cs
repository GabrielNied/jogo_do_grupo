using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class PesosDoTerrenoTestes
    {
        // Pontos de referência de cada bioma (centro de cada um, longe das fronteiras).
        private static readonly (Bioma bioma, float x, float z)[] Referencias =
        {
            (Bioma.Campo, 0f, 300f),
            (Bioma.Mata, 300f, 0f),
            (Bioma.Colinas, -300f, 0f),
            (Bioma.Deserto, -1100f, 0f),
            (Bioma.Neve, 0f, 1100f),
            (Bioma.Lava, 0f, -1100f),
            (Bioma.Pantano, 1100f, 0f),
        };

        [Test]
        public void Pesos_somam_um_em_toda_a_grade_amostrada()
        {
            for (var j = 0; j < MapaDoMundo.PontosPorLado; j += 7)
            {
                for (var i = 0; i < MapaDoMundo.PontosPorLado; i += 7)
                {
                    var altura = MapaDoMundo.AmostrarNaGrade(i, j, out var biomas);
                    var pesos = PesosDoTerreno.Calcular(
                        biomas,
                        altura,
                        MapaDoMundo.CoordenadaDaGrade(i),
                        MapaDoMundo.CoordenadaDaGrade(j)
                    );
                    Assert.AreEqual(1f, pesos.Soma, 0.0001f, $"({i}, {j})");
                    Assert.GreaterOrEqual(pesos.Grama, 0f);
                    Assert.GreaterOrEqual(pesos.Terra, 0f);
                    Assert.GreaterOrEqual(pesos.Areia, 0f);
                    Assert.GreaterOrEqual(pesos.Neve, 0f);
                    Assert.GreaterOrEqual(pesos.Lama, 0f);
                }
            }
        }

        [Test]
        public void Material_dominante_bate_com_o_bioma_nos_pontos_de_referencia()
        {
            foreach (var (bioma, x, z) in Referencias)
            {
                Assert.AreEqual(bioma, MapaDoMundo.BiomaEm(x, z), $"Referência de {bioma} errada.");
                var altura = MapaDoMundo.AlturaEm(x, z);
                var pesos = PesosDoTerreno.Calcular(MapaDoMundo.PesosEm(x, z), altura, x, z);

                switch (bioma)
                {
                    case Bioma.Campo:
                    case Bioma.Mata:
                        Assert.Greater(pesos.Grama, 0.5f, bioma.ToString());
                        break;
                    case Bioma.Colinas:
                        Assert.Greater(pesos.Terra + pesos.Grama, 0.8f, bioma.ToString());
                        break;
                    case Bioma.Deserto:
                        Assert.Greater(pesos.Areia, 0.5f, bioma.ToString());
                        break;
                    case Bioma.Neve:
                        Assert.Greater(pesos.Neve, 0.5f, bioma.ToString());
                        break;
                    case Bioma.Lava:
                        Assert.Greater(pesos.Terra, 0.5f, bioma.ToString());
                        break;
                    case Bioma.Pantano:
                        Assert.Greater(pesos.Lama + pesos.Areia, 0.5f, bioma.ToString());
                        break;
                }
            }
        }

        [Test]
        public void Fundo_do_alagado_e_lama_e_a_margem_e_areia()
        {
            var pantano = new PesosDosBiomas(0f, 0f, 0f, 0f, 0f, 0f, 1f);

            var fundo = PesosDoTerreno.Calcular(
                pantano,
                MapaDoMundo.NivelDaAgua - PesosDoTerreno.ProfundidadeDoLodo,
                1100f,
                0f
            );
            Assert.Greater(fundo.Lama, 0.9f);

            var margem = PesosDoTerreno.Calcular(
                pantano,
                MapaDoMundo.NivelDaAgua + 0.2f,
                1100f,
                0f
            );
            Assert.Greater(margem.Areia, margem.Lama);
        }

        [Test]
        public void Textura_guarda_quatro_pesos_e_a_lama_e_o_que_falta()
        {
            var pesos = new PesosDoTerreno(0.4f, 0.3f, 0.1f, 0.1f, 0.1f);
            var pixel = pesos.ParaTextura();
            Assert.AreEqual(102, pixel.r, 1);
            Assert.AreEqual(77, pixel.g, 1);
            Assert.AreEqual(26, pixel.b, 1);
            Assert.AreEqual(26, pixel.a, 1);
            var lama = 1f - (pixel.r + pixel.g + pixel.b + pixel.a) / 255f;
            Assert.AreEqual(0.1f, lama, 0.02f);
        }
    }
}
