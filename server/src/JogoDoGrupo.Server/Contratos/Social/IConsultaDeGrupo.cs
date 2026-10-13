namespace JogoDoGrupo.Server.Contratos.Social;

/// <summary>Consulta o estado atual do grupo de um personagem, se houver.</summary>
public interface IConsultaDeGrupo
{
    /// <summary>
    /// Devolve um retrato consistente do grupo, com membros na ordem de entrada, ou
    /// <see langword="null"/> quando o personagem está solo.
    /// </summary>
    GrupoAtual? Consultar(long personagemId);
}

/// <summary>Estado atual do grupo publicado por Social para outras funcionalidades do servidor.</summary>
public sealed record GrupoAtual(long GrupoId, long LiderId, IReadOnlyList<long> Membros);
