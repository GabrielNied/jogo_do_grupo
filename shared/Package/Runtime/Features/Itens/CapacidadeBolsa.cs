namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>Onde um item pode estar guardado. Só a bolsa funciona por enquanto.</summary>
    public enum ConteinerItem
    {
        Bolsa,

        /// <summary>Reservado (banco do personagem).</summary>
        Banco,

        /// <summary>Reservado (baú do grupo; a funcionalidade é de Economia).</summary>
        BauDoGrupo,
    }

    /// <summary>
    /// Tamanho da bolsa: quantas abas e quantas casas 1x1 em cada. Vem do servidor; o client
    /// desenha o que receber e nunca assume 40.
    /// </summary>
    [System.Serializable]
    public class CapacidadeBolsa
    {
        public int Abas = RegrasItens.AbasBolsaInicial;
        public int CasasPorAba = RegrasItens.CasasPorAbaBolsa;

        public int Total => Abas * CasasPorAba;

        public bool Contem(int aba, int casa) =>
            aba >= 0 && aba < Abas && casa >= 0 && casa < CasasPorAba;
    }
}
