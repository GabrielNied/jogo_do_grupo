namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Valores ajustáveis da vegetação do mundo (árvores, pedras, cacto): quanto desenhar,
    /// até onde e quanto tempo a montagem pode gastar por quadro.
    /// </summary>
    public static class ConfigVegetacao
    {
        /// <summary>Lado do bloco de desenho e de corte, em metros (o mesmo do bloco do chão).</summary>
        public const float LadoDoBloco =
            ConstrutorDoTerreno.CelulasPorBloco
            * JogoDoGrupo.Shared.Features.Mundo.MapaDoMundo.PassoDaGrade;

        /// <summary>Até esta distância da câmera (m) um bloco desenha as peças grandes (árvores e pedras grandes).</summary>
        public const float DistanciaDasPecasGrandes = 320f;

        /// <summary>Até esta distância (m) um bloco desenha as peças pequenas (arbusto, cacto, pedrinha).</summary>
        public const float DistanciaDasPecasPequenas = 140f;

        /// <summary>Tempo (ms) que a montagem da vegetação gasta por quadro antes de ceder a vez.</summary>
        public const float OrcamentoDeMontagemPorQuadroMs = 6f;

        /// <summary>Instâncias por chamada de desenho (o limite do instancing é 1023).</summary>
        public const int InstanciasPorChamada = 1000;

        /// <summary>Teto de materiais distintos de toda a vegetação (um por tipo de superfície e bioma).</summary>
        public const int LimiteDeMateriais = 80;

        /// <summary>Escala das árvores nas regiões de mata desenhadas pelo criador de mapas.</summary>
        public const float EscalaDaArvoreNaMataAutoral = 2f;

        /// <summary>Peças pequenas somem mais perto: arbusto, cacto e pedras pequenas.</summary>
        public static bool EhPequena(string nome)
        {
            switch (nome)
            {
                case "Arbusto":
                case "Cacto":
                case "Pedra_Pequena":
                case "Rocha_Vulcanica":
                    return true;
                default:
                    return false;
            }
        }
    }
}
