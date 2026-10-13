using UnityEngine;

namespace JogoDoGrupo.Client.Features.Desenvolvimento
{
    /// <summary>
    /// Comandos de teste no client: editor sempre; build de jogador só com
    /// <see cref="Debug.isDebugBuild"/> (Unity 6.6+ deprecou <c>DEVELOPMENT_BUILD</c> em <c>#if</c>).
    /// </summary>
    internal static class ComandosDeTesteNoClient
    {
        internal static bool Permite =>
#if UNITY_EDITOR
            true;
#else
            Debug.isDebugBuild;
#endif
    }
}
