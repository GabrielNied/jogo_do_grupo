namespace JogoDoGrupo.Shared.Features.Instancias
{
    /// <summary>Solicita iniciar a dungeon ou entrar numa corrida ativa do grupo.</summary>
    [System.Serializable]
    public class EntrarNaDungeon
    {
        /// <summary>Nível de dificuldade escolhido pelo líder (mínimo 1).</summary>
        public int Nivel = 1;
    }
}
