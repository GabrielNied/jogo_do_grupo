namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>
    /// Valores ajustáveis de Skills (regra 17). Valores de teste até o balanceamento final.
    /// </summary>
    public static class ConfigSkills
    {
        /// <summary>Skills ativas por arma, na barra (SKILL-002: teclas 1, 2 e 3).</summary>
        public const int SkillsPorArma = 3;

        /// <summary>Menor recarga aceita, em ms: protege contra catálogo mal preenchido.</summary>
        public const int RecargaMinimaMs = 1000;

        /// <summary>Quanto tempo o número de dano da skill fica na tela do client, em segundos.</summary>
        public const float DuracaoDoNumeroSegundos = 1.1f;
    }
}
