namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Limites do recorte de teste de inventário e baú (2026-10-11). Só os números; quem
    /// aplica as regras (sorteio, checagem de espaço) é o servidor.
    /// </summary>
    public static class RegrasItens
    {
        /// <summary>Abas da bolsa de um personagem novo (a expansão é a #564).</summary>
        public const int AbasBolsaInicial = 1;

        /// <summary>Casas 1x1 por aba da bolsa.</summary>
        public const int CasasPorAbaBolsa = 40;

        /// <summary>
        /// Espaços da bolsa inicial (abas x casas). Cada <see cref="EntradaInventario"/> ocupa
        /// um. A capacidade real vem do servidor em <see cref="CapacidadeBolsa"/>.
        /// </summary>
        public const int EspacosInventario = AbasBolsaInicial * CasasPorAbaBolsa;

        /// <summary>Valor de <c>Aba</c> e <c>Casa</c> de um item equipado (fora da bolsa).</summary>
        public const int SemCasa = -1;

        /// <summary>Capacidade da bolsa inicial.</summary>
        public static CapacidadeBolsa CapacidadeInicial() =>
            new CapacidadeBolsa { Abas = AbasBolsaInicial, CasasPorAba = CasasPorAbaBolsa };

        /// <summary>Menor quantidade de itens que uma abertura de baú pode dar.</summary>
        public const int MinimoItensPorBau = 1;

        /// <summary>Maior quantidade de itens que uma abertura de baú pode dar.</summary>
        public const int MaximoItensPorBau = 3;
    }
}
