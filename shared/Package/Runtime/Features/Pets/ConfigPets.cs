namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>Números ajustáveis dos pets (docs/features/pets.md).</summary>
    public static class ConfigPets
    {
        /// <summary>O pet para a esta distância do dono, sem ficar em cima dele.</summary>
        public const float DistanciaDeSeguidaMetros = 2f;

        /// <summary>Quanto o dono pode estar acima ou abaixo do chão do mapa e ainda contar como "no chão".</summary>
        public const float ToleranciaDoChaoMetros = 1.5f;

        /// <summary>
        /// Folga para o pet voltar a andar: parado, só recomeça se o dono passar de
        /// seguida + folga. Evita o pet tremer na borda.
        /// </summary>
        public const float FolgaDeSeguidaMetros = 0.5f;

        /// <summary>Além disto o servidor põe o pet de volta na distância de seguida.</summary>
        public const float DistanciaParaAlcancarMetros = 20f;

        /// <summary>Velocidade do pet, acima da corrida do jogador (5,5) para ele alcançar.</summary>
        public const float VelocidadeMetrosPorSegundo = 6.5f;

        /// <summary>O pet procura loot a até esta distância dele (e não do dono).</summary>
        public const float RaioDeBuscaDeLootMetros = 8f;

        /// <summary>Chegou ao item: a esta distância o pet pede a coleta.</summary>
        public const float DistanciaParaPegarMetros = 0.8f;

        /// <summary>De quanto em quanto tempo o pet procura loot enquanto segue.</summary>
        public const float IntervaloDeBuscaDeLootSegundos = 0.5f;

        /// <summary>Sem espaço no inventário, o pet para de buscar loot por este tempo.</summary>
        public const float PausaSemEspacoSegundos = 30f;
    }
}
