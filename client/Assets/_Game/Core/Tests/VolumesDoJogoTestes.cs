using JogoDoGrupo.Client.Core.Audio;
using JogoDoGrupo.Client.Core.Jogo;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class VolumesDoJogoTestes
    {
        [Test]
        public void Volume_zero_silencia_o_grupo()
        {
            Assert.AreEqual(0f, VolumesDoJogo.Amplitude(0));
            Assert.AreEqual(0f, VolumesDoJogo.Amplitude(-1));
            Assert.AreEqual(0f, VolumesDoJogo.Amplitude(-50));
        }

        [Test]
        public void Volume_limitado_entre_zero_e_cem()
        {
            Assert.AreEqual(0f, VolumesDoJogo.Amplitude(-10));
            Assert.AreEqual(1f, VolumesDoJogo.Amplitude(100));
            Assert.AreEqual(1f, VolumesDoJogo.Amplitude(120));
        }

        [Test]
        public void Escala_quadratica_converte_linear_para_percepcao_humana()
        {
            // 50 no controle (metade) equivale a 0.25 (25%) de amplitude física
            Assert.AreEqual(0.25f, VolumesDoJogo.Amplitude(50), 0.001f);
            // 80 no controle equivale a 0.64 (64%)
            Assert.AreEqual(0.64f, VolumesDoJogo.Amplitude(80), 0.001f);
        }

        [Test]
        public void Propriedades_refletem_opcoes_locais()
        {
            var opcoes = OpcoesLocais.Atual;
            var geralOriginal = opcoes.VolumeGeral;
            var musicaOriginal = opcoes.VolumeMusica;
            var efeitosOriginal = opcoes.VolumeEfeitos;

            try
            {
                opcoes.DefinirVolumeGeral(0);
                Assert.AreEqual(0f, VolumesDoJogo.Geral);

                opcoes.DefinirVolumeMusica(100);
                Assert.AreEqual(1f, VolumesDoJogo.Musica);

                opcoes.DefinirVolumeEfeitos(0);
                Assert.AreEqual(0f, VolumesDoJogo.Efeitos);
            }
            finally
            {
                opcoes.DefinirVolumeGeral(geralOriginal);
                opcoes.DefinirVolumeMusica(musicaOriginal);
                opcoes.DefinirVolumeEfeitos(efeitosOriginal);
            }
        }
    }
}
