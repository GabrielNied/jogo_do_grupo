using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>
    /// Servidor avisa o dono que o uso valeu: o custo foi gasto e a recarga da skill começou.
    /// Id: <see cref="SkillsMessageIds.SkillUsada"/>.
    /// </summary>
    [System.Serializable]
    public class SkillUsada
    {
        public int Espaco = 0;
        public string Id = "";
        public int Sequencia = 0;
        public int RecargaMs = 0;
    }

    /// <summary>
    /// Servidor avisa o dono que o preparo de uma skill forte começou: nada foi cobrado. O fim vem
    /// como <see cref="SkillUsada"/>, <see cref="SkillRecusada"/> ou <see cref="SkillCancelada"/>.
    /// Id: <see cref="SkillsMessageIds.SkillPreparoIniciado"/>.
    /// </summary>
    [System.Serializable]
    public class SkillPreparoIniciado
    {
        public int Espaco = 0;
        public string Id = "";
        public int Sequencia = 0;
        public int PreparoMs = 0;
    }

    /// <summary>
    /// Servidor avisa o dono que o preparo acabou sem efeito (cancelado por ele, ou por queda ou
    /// troca de arma). <see cref="CustoCobrado"/> é o que o cancelamento custou (0 se nada).
    /// Id: <see cref="SkillsMessageIds.SkillCancelada"/>.
    /// </summary>
    [System.Serializable]
    public class SkillCancelada
    {
        public int Espaco = 0;
        public string Id = "";
        public int Sequencia = 0;
        public int CustoCobrado = 0;
    }

    /// <summary>
    /// Servidor avisa quem está na área que o efeito da skill acertou. Cada alvo aparece uma vez.
    /// Id: <see cref="SkillsMessageIds.SkillAcertou"/>.
    /// </summary>
    [System.Serializable]
    public class SkillAcertou
    {
        public long AtacanteId = 0;
        public string Id = "";
        public string Nome = "";
        public int Sequencia = 0;
        public AcertoDeAtaque[] Acertos = System.Array.Empty<AcertoDeAtaque>();
    }

    /// <summary>
    /// Servidor avisa o dono que o pedido de uso não valeu; nada foi gasto.
    /// Id: <see cref="SkillsMessageIds.SkillRecusada"/>.
    /// </summary>
    [System.Serializable]
    public class SkillRecusada
    {
        public int Sequencia = 0;
        public int Espaco = 0;

        /// <summary>Um dos <see cref="CodigosErroSkills"/>.</summary>
        public string Codigo = "";
    }
}
