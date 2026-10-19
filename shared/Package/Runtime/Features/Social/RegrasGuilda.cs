namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Regras canônicas e limites de guilda (docs/features/social.md, decidido em 2026-10-16).
    /// </summary>
    public static class RegrasGuilda
    {
        /// <summary>Tamanho máximo da guilda na v1.</summary>
        public const int MaximoDeMembros = 100;

        /// <summary>Tamanho mínimo do nome da guilda.</summary>
        public const int NomeMinimoCaracteres = 3;

        /// <summary>Tamanho máximo do nome da guilda.</summary>
        public const int NomeMaximoCaracteres = 24;

        /// <summary>Custo em coroas para fundar uma nova guilda.</summary>
        public const long CustoCriacaoCoroas = 1000;

        /// <summary>
        /// Valida o nome da guilda: entre 3 e 24 caracteres, sem símbolos estranhos e sem espaços nas pontas.
        /// </summary>
        public static bool ValidarNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return false;
            }

            var limpo = nome.Trim();
            if (limpo.Length < NomeMinimoCaracteres || limpo.Length > NomeMaximoCaracteres)
            {
                return false;
            }

            if (nome != limpo)
            {
                return false;
            }

            bool espacoAnterior = false;
            for (int i = 0; i < limpo.Length; i++)
            {
                char c = limpo[i];
                if (char.IsLetterOrDigit(c))
                {
                    espacoAnterior = false;
                    continue;
                }

                if (c == ' ')
                {
                    if (espacoAnterior)
                    {
                        return false;
                    }

                    espacoAnterior = true;
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>Líder e oficial podem convidar novos membros.</summary>
        public static bool PodeConvidar(CargoDaGuilda cargo) =>
            cargo == CargoDaGuilda.Lider || cargo == CargoDaGuilda.Oficial;

        /// <summary>
        /// Líder pode expulsar qualquer um (exceto outro líder); oficial pode expulsar membros e recrutas.
        /// </summary>
        public static bool PodeExpulsar(CargoDaGuilda executor, CargoDaGuilda alvo)
        {
            if (executor == CargoDaGuilda.Lider)
            {
                return alvo != CargoDaGuilda.Lider;
            }

            if (executor == CargoDaGuilda.Oficial)
            {
                return alvo == CargoDaGuilda.Membro || alvo == CargoDaGuilda.Recruta;
            }

            return false;
        }

        /// <summary>
        /// Líder pode alterar cargos entre Oficial, Membro e Recruta.
        /// Oficial pode alternar apenas entre Membro e Recruta.
        /// </summary>
        public static bool PodeAlterarCargo(
            CargoDaGuilda executor,
            CargoDaGuilda cargoAtualAlvo,
            CargoDaGuilda novoCargo
        )
        {
            if (cargoAtualAlvo == CargoDaGuilda.Lider || novoCargo == CargoDaGuilda.Lider)
            {
                return false;
            }

            if (executor == CargoDaGuilda.Lider)
            {
                return true;
            }

            if (executor == CargoDaGuilda.Oficial)
            {
                bool alvoValido =
                    cargoAtualAlvo == CargoDaGuilda.Membro
                    || cargoAtualAlvo == CargoDaGuilda.Recruta;
                bool novoValido =
                    novoCargo == CargoDaGuilda.Membro || novoCargo == CargoDaGuilda.Recruta;
                return alvoValido && novoValido;
            }

            return false;
        }

        /// <summary>Todos os membros podem depositar no banco da guilda (decisão 2026-10-16).</summary>
        public static bool PodeDepositarNoBanco(CargoDaGuilda cargo) => true;

        /// <summary>Apenas Líder e Oficial podem retirar itens ou valores do banco da guilda.</summary>
        public static bool PodeRetirarDoBanco(CargoDaGuilda cargo) =>
            cargo == CargoDaGuilda.Lider || cargo == CargoDaGuilda.Oficial;
    }
}
