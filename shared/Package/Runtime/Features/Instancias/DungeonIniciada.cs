namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>Servidor confirma que o personagem entrou numa corrida de dungeon.</summary>
    [System.Serializable]
    public class DungeonIniciada
    {
        /// <summary>Identificador estável da corrida enquanto ela existir.</summary>
        public string IdInstancia = string.Empty;

        /// <summary>Área isolada do Mundo para a corrida.</summary>
        public string Area = string.Empty;

        /// <summary>Nível de dificuldade da corrida (mínimo 1).</summary>
        public int Nivel = 1;

        /// <summary>Tempo limite para vencer no tempo em segundos.</summary>
        public int TempoLimiteSegundos = RegrasDificuldadeSemFim.TempoLimitePadraoSegundos;
    }
}
