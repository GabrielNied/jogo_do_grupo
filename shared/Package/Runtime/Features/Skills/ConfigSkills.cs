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

        /// <summary>A skill com multiplicador a partir daqui é forte: tem preparo e pode ser cancelada.</summary>
        public const double MultiplicadorDeSkillForte = 2.5;

        /// <summary>Duração do preparo de uma skill forte, em ms (servidor e client usam o mesmo valor).</summary>
        public const int PreparoDaSkillForteMs = 1200;

        /// <summary>Parte do custo cobrada ao cancelar o preparo (#345: 50% do recurso).</summary>
        public const double FracaoDoCustoAoCancelar = 0.5;
    }
}
