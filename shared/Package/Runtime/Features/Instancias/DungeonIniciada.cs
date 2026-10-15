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
    }
}
