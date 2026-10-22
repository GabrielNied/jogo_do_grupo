using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Audio
{
    /// <summary>
    /// Volumes de Música e Efeitos do menu de opções, prontos para o <c>AudioSource</c>. O Geral
    /// vale no <c>AudioListener</c>. Sem mixer: cada fonte multiplica pelo volume do seu grupo.
    /// </summary>
    public static class VolumesDoJogo
    {
        /// <summary>Volume geral (0 a 1), já na escala de amplitude (para o AudioListener).</summary>
        public static float Geral => Amplitude(OpcoesLocais.Atual.VolumeGeral);

        /// <summary>Volume da música (0 a 1), já na escala de amplitude.</summary>
        public static float Musica => Amplitude(OpcoesLocais.Atual.VolumeMusica);

        /// <summary>Volume dos efeitos (0 a 1), já na escala de amplitude.</summary>
        public static float Efeitos => Amplitude(OpcoesLocais.Atual.VolumeEfeitos);

        /// <summary>
        /// Converte o controle de 0 a 100 em amplitude. O quadrado faz o meio do controle soar
        /// como "metade" para quem ouve (amplitude linear soaria alta demais até perto do fim).
        /// Fora da faixa é limitado; 0 silencia.
        /// </summary>
        public static float Amplitude(int volume)
        {
            var fracao = Mathf.Clamp01(volume / 100f);
            return fracao * fracao;
        }
    }
}
