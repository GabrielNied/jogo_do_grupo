namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Limites e custos da expansão da bolsa (#564). Valores configuráveis.
    /// </summary>
    public static class ConfigBolsa
    {
        /// <summary>Máximo de abas que um personagem pode desbloquear.</summary>
        public const int AbasMaximas = 4;

        /// <summary>
        /// Custo em coroas para desbloquear a aba especificada (índice base 1: aba 2, 3 ou 4).
        /// </summary>
        public static long CustoPorAba(int abaDestino)
        {
            switch (abaDestino)
            {
                case 2:
                    return 100;
                case 3:
                    return 250;
                case 4:
                    return 500;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Custo em coroas para desbloquear a próxima aba dada a quantidade atual de abas.
        /// Retorna 0 se já atingiu o máximo ou valor inválido.
        /// </summary>
        public static long CustoProximaAba(int abasAtuais)
        {
            if (abasAtuais < 1 || abasAtuais >= AbasMaximas)
            {
                return 0;
            }
            return CustoPorAba(abasAtuais + 1);
        }
    }
}
