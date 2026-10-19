using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>IDs de mensagem de Skills (magias). Use somente valores entre 4000 e 4999.</summary>
    public static class SkillsMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(4000, 4999);

        /// <summary>Client → servidor: usar a skill de um espaço da barra, com a direção da mira.</summary>
        public const ushort UsarSkill = 4000;

        /// <summary>Client → servidor: pede a barra atual (ao entrar no mundo).</summary>
        public const ushort PedirBarraDeSkills = 4001;

        /// <summary>Servidor → dono: a barra de skills (da arma equipada) e as recargas em curso.</summary>
        public const ushort BarraDeSkillsMudou = 4002;

        /// <summary>Servidor → dono: uma skill foi usada; custo gasto e recarga começou.</summary>
        public const ushort SkillUsada = 4003;

        /// <summary>Servidor → quem está na área: o efeito da skill acertou um ou mais alvos.</summary>
        public const ushort SkillAcertou = 4004;

        /// <summary>Servidor → dono: o pedido de uso foi recusado. Nada foi gasto.</summary>
        public const ushort SkillRecusada = 4005;

        /// <summary>Servidor → dono: um poder lendário ou bônus de set disparou na skill.</summary>
        public const ushort PoderDisparou = 4006;

        /// <summary>Servidor → dono: o preparo de uma skill forte começou (nada foi cobrado ainda).</summary>
        public const ushort SkillPreparoIniciado = 4007;

        /// <summary>Client → servidor: cancela o preparo em curso (custa parte do recurso).</summary>
        public const ushort CancelarSkill = 4008;

        /// <summary>Servidor → dono: o preparo foi cancelado e o custo do cancelamento cobrado.</summary>
        public const ushort SkillCancelada = 4009;
    }
}
