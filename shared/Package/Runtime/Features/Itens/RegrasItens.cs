namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Limites do recorte de teste de inventário e baú (2026-10-11). Só os números; quem
    /// aplica as regras (sorteio, checagem de espaço) é o servidor.
    /// </summary>
    public static class RegrasItens
    {
        /// <summary>Espaços do inventário. Cada <see cref="EntradaInventario"/> ocupa um.</summary>
        public const int EspacosInventario = 40;

        /// <summary>Menor quantidade de itens que uma abertura de baú pode dar.</summary>
        public const int MinimoItensPorBau = 1;

        /// <summary>Maior quantidade de itens que uma abertura de baú pode dar.</summary>
        public const int MaximoItensPorBau = 3;
    }
}
