namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>Motivos de recusa de <see cref="SkillRecusada"/>. Nada é gasto numa recusa.</summary>
    public static class CodigosErroSkills
    {
        /// <summary>Não há arma equipada no conjunto ativo (ou o espaço pedido não existe).</summary>
        public const string SemSkill = "SemSkill";

        /// <summary>A skill ainda está em recarga.</summary>
        public const string EmRecarga = "EmRecarga";

        /// <summary>Falta stamina ou mana para o custo.</summary>
        public const string RecursoInsuficiente = "RecursoInsuficiente";

        /// <summary>O personagem está caído.</summary>
        public const string VoceEstaCaido = "VoceEstaCaido";

        /// <summary>O personagem está trocando de arma (os 0,4 s da troca).</summary>
        public const string AcaoEmAndamento = "AcaoEmAndamento";
    }
}
