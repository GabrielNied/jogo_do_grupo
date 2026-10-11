using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>Grava as opções em <see cref="PlayerPrefs"/> e aplica o que dá para aplicar na hora.</summary>
    internal static class OpcoesGravadas
    {
        private const string Chave = "jogo.opcoes";

        public static void Carregar()
        {
            OpcoesLocais.Substituir(OpcoesLocais.Ler(PlayerPrefs.GetString(Chave, "")));
            Aplicar();
        }

        public static void Salvar()
        {
            PlayerPrefs.SetString(Chave, OpcoesLocais.Atual.Gravar());
            PlayerPrefs.Save();
            Aplicar();
        }

        public static void Aplicar()
        {
            AudioListener.volume = OpcoesLocais.Atual.VolumeGeral / 100f;
            var nivel =
                OpcoesLocais.Atual.Qualidade == "Baixa" ? 0
                : OpcoesLocais.Atual.Qualidade == "Alta" ? 2
                : 1;
            if (QualitySettings.names != null && nivel < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(nivel, true);
            }

            if (OpcoesLocais.Atual.Largura > 0 && OpcoesLocais.Atual.Altura > 0)
            {
                Screen.SetResolution(
                    OpcoesLocais.Atual.Largura,
                    OpcoesLocais.Atual.Altura,
                    OpcoesLocais.Atual.TelaCheia
                );
                return;
            }

            Screen.fullScreen = OpcoesLocais.Atual.TelaCheia;
        }
    }
}
