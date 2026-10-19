namespace JogoDoGrupo.Shared.Features.Profissoes
{
    /// <summary>Motivos de recusa de Profissões (<see cref="ProfissoesRecusada.Codigo"/>).</summary>
    public static class CodigosErroProfissoes
    {
        /// <summary>A profissão não existe ou ainda não está disponível para aprender.</summary>
        public const string ProfissaoInvalida = "ProfissaoInvalida";

        /// <summary>O personagem já aprendeu essa profissão.</summary>
        public const string JaAprendida = "JaAprendida";

        /// <summary>O personagem não aprendeu a profissão que a ação exige.</summary>
        public const string ProfissaoNaoAprendida = "ProfissaoNaoAprendida";

        /// <summary>O ponto de coleta não existe.</summary>
        public const string NoInexistente = "NoInexistente";

        /// <summary>Outro jogador levou o ponto; ele volta depois do respawn.</summary>
        public const string NoEsgotado = "NoEsgotado";

        /// <summary>O personagem está longe do ponto de coleta ou da forja.</summary>
        public const string Longe = "Longe";

        /// <summary>A receita não existe.</summary>
        public const string ReceitaInexistente = "ReceitaInexistente";

        /// <summary>Falta material para a receita; nada é gasto.</summary>
        public const string MateriaisInsuficientes = "MateriaisInsuficientes";

        /// <summary>A bolsa está cheia; nada é gasto.</summary>
        public const string InventarioCheio = "InventarioCheio";
    }
}
