namespace JogoDoGrupo.Shared.Features.Skills
{
    /// <summary>
    /// Client pede ao servidor para usar a skill de um espaço da barra. É só a intenção: custo,
    /// recarga, acerto e dano são decididos pelo servidor.
    /// Id: <see cref="SkillsMessageIds.UsarSkill"/>.
    /// </summary>
    [System.Serializable]
    public class UsarSkill
    {
        /// <summary>Espaço da barra, de 0 a <see cref="ConfigSkills.SkillsPorArma"/> - 1.</summary>
        public int Espaco = 0;

        /// <summary>Direção da mira no plano do chão, na convenção de <c>IntencaoDeAtaque.DirecaoY</c>.</summary>
        public float DirecaoY = 0f;

        /// <summary>Número crescente do client; volta em <see cref="SkillRecusada.Sequencia"/>.</summary>
        public int Sequencia = 0;
    }

    /// <summary>
    /// Client cancela o preparo da skill forte em curso. O servidor cobra
    /// <see cref="ConfigSkills.FracaoDoCustoAoCancelar"/> do custo, uma vez só.
    /// Id: <see cref="SkillsMessageIds.CancelarSkill"/>.
    /// </summary>
    [System.Serializable]
    public class CancelarSkill { }

    /// <summary>
    /// Client pede a barra atual (ao entrar no mundo). O servidor responde com
    /// <see cref="BarraDeSkillsMudou"/>. Id: <see cref="SkillsMessageIds.PedirBarraDeSkills"/>.
    /// </summary>
    [System.Serializable]
    public class PedirBarraDeSkills { }
}
