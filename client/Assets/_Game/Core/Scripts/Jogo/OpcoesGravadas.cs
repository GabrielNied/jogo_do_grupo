using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    internal enum GrupoDeOpcoes
    {
        Audio,
        Qualidade,
        Tela,
        Jogo,
    }

    [System.Flags]
    internal enum AplicacaoDeOpcoes
    {
        Nenhuma = 0,
        Audio = 1,
        Qualidade = 2,
        Tela = 4,
        Todas = Audio | Qualidade | Tela,
    }

    /// <summary>Grava as opções em <see cref="PlayerPrefs"/> e aplica o que dá para aplicar na hora.</summary>
    internal static class OpcoesGravadas
    {
        private const string Chave = "jogo.opcoes";

        public static void Carregar()
        {
            OpcoesLocais.Substituir(OpcoesLocais.Ler(PlayerPrefs.GetString(Chave, "")));
            Aplicar(AplicacaoDeOpcoes.Todas);
        }

        public static void Salvar(GrupoDeOpcoes grupo)
        {
            PlayerPrefs.SetString(Chave, OpcoesLocais.Atual.Gravar());
            PlayerPrefs.Save();
            Aplicar(PlanejarAplicacao(grupo));
        }

        internal static AplicacaoDeOpcoes PlanejarAplicacao(GrupoDeOpcoes grupo) =>
            grupo switch
            {
                GrupoDeOpcoes.Audio => AplicacaoDeOpcoes.Audio,
                GrupoDeOpcoes.Qualidade => AplicacaoDeOpcoes.Qualidade,
                GrupoDeOpcoes.Tela => AplicacaoDeOpcoes.Tela,
                _ => AplicacaoDeOpcoes.Nenhuma,
            };

        private static void Aplicar(AplicacaoDeOpcoes aplicacoes)
        {
            if ((aplicacoes & AplicacaoDeOpcoes.Audio) != 0)
            {
                AudioListener.volume = OpcoesLocais.Atual.VolumeGeral / 100f;
            }

            if ((aplicacoes & AplicacaoDeOpcoes.Qualidade) != 0)
            {
                var nivel = (int)OpcoesLocais.Atual.NivelQualidade;
                if (QualitySettings.names != null && nivel < QualitySettings.names.Length)
                {
                    QualitySettings.SetQualityLevel(nivel, true);
                }
            }

            if ((aplicacoes & AplicacaoDeOpcoes.Tela) == 0)
            {
                return;
            }

            if (OpcoesLocais.Atual.Largura > 0 && OpcoesLocais.Atual.Altura > 0)
            {
                Screen.SetResolution(
                    OpcoesLocais.Atual.Largura,
                    OpcoesLocais.Atual.Altura,
                    OpcoesLocais.Atual.TelaCheia
                );
            }
            else
            {
                Screen.fullScreen = OpcoesLocais.Atual.TelaCheia;
            }
        }
    }
}
