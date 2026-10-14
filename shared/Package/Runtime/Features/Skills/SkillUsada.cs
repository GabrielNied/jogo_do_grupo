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
