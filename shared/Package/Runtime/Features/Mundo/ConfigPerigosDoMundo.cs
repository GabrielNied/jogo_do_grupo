namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Valores de calibração para perigos de terreno no mundo (issue #901):
    /// dano contínuo da lava e lentidão na água do pântano.
    /// </summary>
    public static class ConfigPerigosDoMundo
    {
        /// <summary>Dano por segundo ao pisar na lava.</summary>
        public const int DanoPorSegundoLava = 25;

        /// <summary>Intervalo entre aplicações de dano da lava, em milissegundos.</summary>
        public const long IntervaloDanoLavaMs = 1000;

        /// <summary>Fração da velocidade do personagem ao andar na água do pântano (0 a 1).</summary>
        public const double FatorVelocidadePantano = 0.60;

        /// <summary>Duração do teto de velocidade renovado enquanto no pântano, em segundos.</summary>
        public const double DuracaoTetoPantanoSegundos = 2.0;

        /// <summary>Verifica se uma posição no mapa do mundo está na lava.</summary>
        public static bool EstaNaLava(float x, float z) =>
            (MapaDoMundo.AreasEspeciaisEm(x, z) & AreaEspecialDoMapa.Lava) != 0;

        /// <summary>Verifica se uma posição no mapa do mundo está na água do pântano.</summary>
        public static bool EstaNoPantano(float x, float z) =>
            (MapaDoMundo.AreasEspeciaisEm(x, z) & AreaEspecialDoMapa.Pantano) != 0;
    }
}
