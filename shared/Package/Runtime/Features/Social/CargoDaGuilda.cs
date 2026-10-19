namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Cargos hierárquicos dentro de uma guilda (docs/features/social.md).
    /// </summary>
    public enum CargoDaGuilda
    {
        /// <summary>Fundador ou líder atual; permissão total (convidar, expulsar, promover, banco).</summary>
        Lider = 0,

        /// <summary>Oficial; convida, expulsa membros/recrutas e retira do banco.</summary>
        Oficial = 1,

        /// <summary>Membro pleno; deposita no banco e participa do chat.</summary>
        Membro = 2,

        /// <summary>Recruta recém-chegado em período de experiência.</summary>
        Recruta = 3,
    }
}
