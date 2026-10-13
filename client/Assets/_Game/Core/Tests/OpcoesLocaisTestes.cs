using JogoDoGrupo.Client.Core.Jogo;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class OpcoesLocaisTestes
    {
        [Test]
        public void Gravar_e_ler_devolve_os_mesmos_valores()
        {
            var opcoes = OpcoesLocais.Padrao();
            opcoes.DefinirVolumeGeral(40);
            opcoes.DefinirVolumeMusica(15);
            opcoes.DefinirVolumeEfeitos(100);
            opcoes.DefinirQualidade("Baixa");
            opcoes.DefinirTelaCheia(false);
            opcoes.DefinirSensibilidade(1.5f);
            opcoes.DefinirResolucao(1280, 720);

            var lido = OpcoesLocais.Ler(opcoes.Gravar());

            Assert.AreEqual(40, lido.VolumeGeral);
            Assert.AreEqual(15, lido.VolumeMusica);
            Assert.AreEqual(100, lido.VolumeEfeitos);
            Assert.AreEqual("Baixa", lido.Qualidade);
            Assert.IsFalse(lido.TelaCheia);
            Assert.AreEqual(1.5f, lido.Sensibilidade, 0.001f);
            Assert.AreEqual(1280, lido.Largura);
            Assert.AreEqual(720, lido.Altura);
            Assert.AreEqual(OpcoesLocais.IdiomaPadrao, lido.Idioma);
        }

        [Test]
        public void Resolucao_padrao_ou_invalida_vale_zero()
        {
            var padrao = OpcoesLocais.Ler(OpcoesLocais.Padrao().Gravar());
            Assert.AreEqual(0, padrao.Largura);
            Assert.AreEqual(0, padrao.Altura);

            var invalida = OpcoesLocais.Padrao();
            invalida.DefinirResolucao(1280, 0);
            var lido = OpcoesLocais.Ler(invalida.Gravar());
            Assert.AreEqual(0, lido.Largura);
            Assert.AreEqual(0, lido.Altura);

            var negativa = OpcoesLocais.Ler("geral=80\nlargura=-5\naltura=720");
            Assert.AreEqual(0, negativa.Largura);
            Assert.AreEqual(0, negativa.Altura);
        }

        [Test]
        public void Valor_fora_da_faixa_e_limitado()
        {
            Assert.AreEqual(0, OpcoesLocais.LimitarVolume(-10));
            Assert.AreEqual(100, OpcoesLocais.LimitarVolume(140));
            Assert.AreEqual(80, OpcoesLocais.LimitarVolume(82));
            Assert.AreEqual(0.5f, OpcoesLocais.LimitarSensibilidade(0.1f), 0.001f);
            Assert.AreEqual(2f, OpcoesLocais.LimitarSensibilidade(3f), 0.001f);
        }

        [Test]
        public void Arquivo_corrompido_volta_ao_padrao()
        {
            var lido = OpcoesLocais.Ler("isto nao e um arquivo");

            Assert.AreEqual(OpcoesLocais.VolumeGeralPadrao, lido.VolumeGeral);
            Assert.AreEqual(OpcoesLocais.VolumeMusicaPadrao, lido.VolumeMusica);
            Assert.AreEqual(OpcoesLocais.VolumeEfeitosPadrao, lido.VolumeEfeitos);
            Assert.AreEqual("Média", lido.Qualidade);
            Assert.IsTrue(lido.TelaCheia);
            Assert.AreEqual(OpcoesLocais.SensibilidadePadrao, lido.Sensibilidade, 0.001f);
        }

        [Test]
        public void Qualidade_gravada_em_texto_continua_compativel_com_enum()
        {
            var opcoes = OpcoesLocais.Ler("geral=80\nqualidade=Alta");
            Assert.AreEqual(QualidadeGrafica.Alta, opcoes.NivelQualidade);
            opcoes.DefinirQualidade(QualidadeGrafica.Baixa);
            Assert.That(opcoes.Gravar(), Does.Contain("qualidade=Baixa"));
        }

        [Test]
        public void Alterar_audio_ou_jogo_nao_reaplica_tela_e_qualidade()
        {
            Assert.AreEqual(
                AplicacaoDeOpcoes.Audio,
                OpcoesGravadas.PlanejarAplicacao(GrupoDeOpcoes.Audio)
            );
            Assert.AreEqual(
                AplicacaoDeOpcoes.Nenhuma,
                OpcoesGravadas.PlanejarAplicacao(GrupoDeOpcoes.Jogo)
            );
            Assert.AreEqual(
                AplicacaoDeOpcoes.Tela,
                OpcoesGravadas.PlanejarAplicacao(GrupoDeOpcoes.Tela)
            );
        }

        [Test]
        public void Resolucao_aparece_uma_vez_por_tamanho_mesmo_com_taxas_repetidas()
        {
            var resolucoes = MenuJogo.ResolucoesSemDuplicata(
                new[]
                {
                    new Vector2Int(1920, 1080),
                    new Vector2Int(1920, 1080),
                    new Vector2Int(1280, 720),
                    new Vector2Int(0, 0),
                }
            );

            CollectionAssert.AreEqual(
                new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) },
                resolucoes
            );
        }
    }
}
