using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Registra no Core o visual do jogador (um prefab por raça) antes de qualquer cena
    /// carregar. O Core instancia o prefab ao entrar no mundo.
    /// </summary>
    internal static class RegistroVisualJogador
    {
        /// <summary>Raça usada quando o personagem não está na última lista recebida.</summary>
        internal const Raca RacaPadrao = Raca.Valdren;

        private static readonly Dictionary<
            long,
            (Raca Raca, AparenciaPersonagem Aparencia)
        > _dadosPorPersonagem = new Dictionary<long, (Raca, AparenciaPersonagem)>();

        /// <summary>Caminho do prefab da raça em <c>Resources</c>.</summary>
        internal static string CaminhoDoPrefab(Raca raca)
        {
            return "Personagem/Jogador_" + raca;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            _dadosPorPersonagem.Clear();
            EntradaNoJogo.DefinirVisualDoJogador(CarregarVisual);
        }

        /// <summary>
        /// Guarda a raça e a aparência de cada personagem da lista do servidor (a seleção chama ao carregar),
        /// para o <see cref="EntradaNoJogo"/> pedir o visual só pelo id.
        /// </summary>
        internal static void LembrarRacas(IEnumerable<PersonagemResumo> personagens)
        {
            _dadosPorPersonagem.Clear();
            if (personagens == null)
            {
                return;
            }

            foreach (var personagem in personagens)
            {
                if (personagem != null)
                {
                    _dadosPorPersonagem[personagem.Id] = (personagem.Raca, personagem.Aparencia);
                }
            }
        }

        internal static GameObject CarregarVisual(long personagemId)
        {
            var dados = _dadosPorPersonagem.TryGetValue(personagemId, out var lembrados)
                ? lembrados
                : (Raca: RacaPadrao, Aparencia: new AparenciaPersonagem());
            var prefab = Resources.Load<GameObject>(CaminhoDoPrefab(dados.Raca));
            if (prefab == null)
            {
                return null;
            }

            var instancia = Object.Instantiate(prefab);
            AplicadorAparencia.Aplicar(instancia, dados.Aparencia);
            return instancia;
        }
    }
}
