namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Regras e constantes de balanceamento do banco pessoal na cidade (issue #177).
    /// Compartilhado entre os personagens da mesma conta.
    /// </summary>
    public static class RegrasBanco
    {
        /// <summary>Abas iniciais do banco para uma conta nova.</summary>
        public const int AbasBancoInicial = 1;

        /// <summary>Quantidade de casas 1x1 em cada aba do banco.</summary>
        public const int CasasPorAbaBanco = 40;

        /// <summary>Limite máximo de abas que o banco pessoal pode ter.</summary>
        public const int AbasBancoMaximas = 4;

        /// <summary>Custo em coroas para desbloquear uma nova aba no banco pessoal.</summary>
        public const long CustoExpansaoBancoCoroas = 500;

        /// <summary>Capacidade inicial do banco pessoal (1 aba de 40 casas).</summary>
        public static CapacidadeBolsa CapacidadeBancoInicial() =>
            new CapacidadeBolsa { Abas = AbasBancoInicial, CasasPorAba = CasasPorAbaBanco };

        /// <summary>Capacidade do banco a partir do número de abas (1 a 4).</summary>
        public static CapacidadeBolsa CapacidadeBancoDe(int abas)
        {
            var clamped = abas;
            if (clamped < AbasBancoInicial)
            {
                clamped = AbasBancoInicial;
            }
            else if (clamped > AbasBancoMaximas)
            {
                clamped = AbasBancoMaximas;
            }

            return new CapacidadeBolsa { Abas = clamped, CasasPorAba = CasasPorAbaBanco };
        }
    }
}
