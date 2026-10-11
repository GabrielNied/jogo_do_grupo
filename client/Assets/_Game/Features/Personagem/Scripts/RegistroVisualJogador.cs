using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Registra no Core o visual do jogador (o humano provisório) antes de qualquer cena
    /// carregar. O Core instancia o prefab ao entrar no mundo.
    /// </summary>
    internal static class RegistroVisualJogador
    {
        /// <summary>Caminho do prefab em <c>Resources</c>.</summary>
        internal const string CaminhoPrefabHumano = "Personagem/Jogador_Humano";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            EntradaNoJogo.DefinirVisualDoJogador(CarregarVisual);
        }

        // Só existe a raça Humano, então o id do personagem não muda o visual por enquanto.
        internal static GameObject CarregarVisual(long personagemId)
        {
            return Resources.Load<GameObject>(CaminhoPrefabHumano);
        }
    }
}
