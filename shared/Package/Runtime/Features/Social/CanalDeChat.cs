namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Canal de uma mensagem de chat. Os valores são estáveis: nunca mude nem reaproveite um
    /// número já publicado. O canal Sistema não está aqui: ele é só do client.
    /// </summary>
    public enum CanalDeChat
    {
        /// <summary>Todos os personagens que estão na cidade.</summary>
        Cidade = 0,

        /// <summary>Os membros online do grupo, em qualquer área.</summary>
        Grupo = 1,

        /// <summary>Só o personagem indicado (e o remetente vê a própria mensagem).</summary>
        Sussurro = 2,
    }
}
