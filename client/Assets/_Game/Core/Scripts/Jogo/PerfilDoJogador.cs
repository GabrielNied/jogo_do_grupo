using System;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Nome, raça e nível do personagem que está jogando, para o HUD (quadro do personagem,
    /// retrato) sem uma funcionalidade conhecer a outra. Quem sabe preenche (a seleção de
    /// personagem e o progresso, em Personagem; o estado da cidade, em Mundo); quem mostra
    /// assina <see cref="Mudou"/>.
    /// </summary>
    public static class PerfilDoJogador
    {
        /// <summary>Nome do personagem, ou vazio antes de entrar.</summary>
        public static string Nome { get; private set; } = string.Empty;

        /// <summary>Raça (valor do enum <c>Raca</c> do shared), ou 0 se não se sabe.</summary>
        public static int Raca { get; private set; }

        /// <summary>Nível, ou 0 se ainda não chegou do servidor.</summary>
        public static int Nivel { get; private set; }

        /// <summary>Disparado quando qualquer campo muda.</summary>
        public static event Action Mudou;

        public static void Definir(string nome, int raca, int nivel)
        {
            nome ??= string.Empty;
            if (nome == Nome && raca == Raca && nivel == Nivel)
                return;
            Nome = nome;
            Raca = raca;
            Nivel = nivel;
            Mudou?.Invoke();
        }

        public static void DefinirNome(string nome, int raca)
        {
            Definir(string.IsNullOrEmpty(nome) ? Nome : nome, raca > 0 ? raca : Raca, Nivel);
        }

        public static void DefinirNivel(int nivel)
        {
            if (nivel <= 0 || nivel == Nivel)
                return;
            Nivel = nivel;
            Mudou?.Invoke();
        }

        /// <summary>Cor de fundo do retrato por raça (até termos arte de rosto).</summary>
        public static Color CorDaRaca(int raca)
        {
            switch (raca)
            {
                case 1:
                    return new Color(0.42f, 0.30f, 0.20f); // Valdren: couro
                case 2:
                    return new Color(0.62f, 0.48f, 0.18f); // Aurídeos: ouro
                case 3:
                    return new Color(0.36f, 0.38f, 0.40f); // Graníticos: pedra
                case 4:
                    return new Color(0.45f, 0.16f, 0.10f); // Cinéreos: brasa
                case 5:
                    return new Color(0.20f, 0.40f, 0.22f); // Silvanos: mata
                default:
                    return new Color(0.25f, 0.20f, 0.15f);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Nome = string.Empty;
            Raca = 0;
            Nivel = 0;
            Mudou = null;
        }
    }
}
