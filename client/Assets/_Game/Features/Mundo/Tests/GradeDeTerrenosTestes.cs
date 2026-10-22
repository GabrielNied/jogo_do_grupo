using System;
using JogoDoGrupo.Client.Features.Mundo.Editor;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class GradeDeTerrenosTestes
    {
        [TestCase(-1500f, -1500f, 0)]
        [TestCase(-1000f, -1500f, 1)]
        [TestCase(0f, 0f, 21)]
        [TestCase(1500f, 1500f, 35)]
        [TestCase(1500f, -1500f, 5)]
        public void Consulta_escolhe_o_bloco_incluindo_divisas_e_limite_externo(
            float x,
            float z,
            int indice
        )
        {
            Assert.AreEqual(indice, GradeDeTerrenosDoMundo.Indice(x, z));
        }

        [Test]
        public void Consulta_recusa_fora_do_mundo()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GradeDeTerrenosDoMundo.Indice(1501f, 0f)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                GradeDeTerrenosDoMundo.Indice(0f, float.NaN)
            );
        }

        [Test]
        public void Reamostragem_preserva_extremos_e_interpola_nas_duas_direcoes()
        {
            var alturas = new float[,]
            {
                { 0f, 0.2f },
                { 0.6f, 1f },
            };
            Assert.AreEqual(0f, DivisorDoTerrenoEmBlocos.Amostrar(alturas, 0f, 0f));
            Assert.AreEqual(1f, DivisorDoTerrenoEmBlocos.Amostrar(alturas, 1f, 1f));
            Assert.AreEqual(
                0.45f,
                DivisorDoTerrenoEmBlocos.Amostrar(alturas, 0.5f, 0.5f),
                0.00001f
            );
        }

        [Test]
        public void Ferramentas_nativas_atravessam_o_encontro_de_quatro_blocos()
        {
            ValidacaoDosBlocosDeTerreno.Executar();
        }
    }
}
