namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Onde o baú de teste fica e com que frequência abre. Vale para o servidor (que confere
    /// distância e recarga) e para o client (que põe o baú na cena e mostra a dica). Valores
    /// ajustáveis de balanceamento; a regra de conferir é do servidor (#492).
    /// </summary>
    public static class ConfigBau
    {
        /// <summary>Área onde o baú existe (a cidade).</summary>
        public const string Area = "cidade";

        /// <summary>
        /// Posição do baú na cidade (eixo X, em metros): o nascimento (0, -24) mais 3,5 m à
        /// frente e 2,5 m à direita, onde o client sempre o pôs.
        /// </summary>
        public const float X = 2.5f;

        /// <summary>Posição do baú na cidade (eixo Z, em metros).</summary>
        public const float Z = -20.5f;

        /// <summary>
        /// Distância horizontal máxima, em metros, que o servidor aceita entre o personagem e o
        /// baú. Maior que a do client (3 m) para a posição atrasada da rede não recusar quem o
        /// client já via perto.
        /// </summary>
        public const float RaioParaAbrirMetros = 4f;

        /// <summary>Segundos entre duas aberturas do mesmo personagem.</summary>
        public const float RecargaSegundos = 5f;

        /// <summary>Se o ponto (<paramref name="x"/>, <paramref name="z"/>) está ao alcance do baú.</summary>
        public static bool DentroDoRaio(float x, float z)
        {
            var dx = x - X;
            var dz = z - Z;
            return dx * dx + dz * dz <= RaioParaAbrirMetros * RaioParaAbrirMetros;
        }
    }
}
