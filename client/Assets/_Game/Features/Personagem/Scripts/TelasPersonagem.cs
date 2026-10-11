using JogoDoGrupo.Client.Core.UI;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Chaves das telas de Personagem na <see cref="Navegacao"/>, o registro delas e o pouco
    /// estado que passa de uma tela para a outra (a <see cref="Navegacao"/> não leva parâmetros).
    /// </summary>
    public static class TelasPersonagem
    {
        /// <summary>Seleção de personagem: aberta logo depois do login.</summary>
        public const string Selecao = "personagem.selecao";

        /// <summary>Criação de personagem: aberta pelo botão Criar da seleção.</summary>
        public const string Criacao = "personagem.criacao";

        /// <summary>
        /// Id do personagem que a seleção deve deixar selecionado na próxima abertura
        /// (o recém-criado). Consumido pela seleção ao carregar a lista.
        /// </summary>
        internal static long? IdParaSelecionar;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegistrarTelas()
        {
            Navegacao.Registrar(Selecao, () => new TelaSelecaoPersonagem());
            Navegacao.Registrar(Criacao, () => new TelaCriacaoPersonagem());
            Navegacao.DefinirTelaAposLogin(Selecao);
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, os campos estáticos
        // sobrevivem entre execuções; limpa antes de qualquer tela abrir.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            IdParaSelecionar = null;
        }
    }
}
