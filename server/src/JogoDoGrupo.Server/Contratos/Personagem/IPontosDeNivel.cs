namespace JogoDoGrupo.Server.Contratos.Personagem;

/// <summary>
/// Pontos da árvore de atributos que o personagem ganhou ao subir de nível e ainda não gastou,
/// oferecidos por Personagem a Talentos (ADR 0016). Serviço <b>scoped</b>: usa o mesmo
/// contexto do chamador e, como <see cref="Economia.ICarteiraCoroas"/>, participa da transação
/// dele; nunca abre nem confirma transação.
/// </summary>
public interface IPontosDeNivel
{
    /// <summary>Pontos livres. Zero se o personagem não existe.</summary>
    Task<int> ObterAsync(long personagemId, CancellationToken cancelamento);

    /// <summary>Nível do personagem. 1 se ele não existe.</summary>
    Task<int> ObterNivelAsync(long personagemId, CancellationToken cancelamento);

    /// <summary>
    /// Tira<paramref name="quantidade"/> dos pontos livres de uma vez só: falso, sem mudar
    /// nada, se o personagem não existe ou tem menos que isso. A baixa vale já na transação
    /// aberta do chamador (não precisa de <c>SaveChanges</c>) e some junto se ele desfizer.
    /// </summary>
    Task<bool> TentarGastarAsync(long personagemId, int quantidade, CancellationToken cancelamento);

    /// <summary>
    /// Devolve <paramref name="quantidade"/> para os pontos livres de uma vez só (ex.: ao
    /// redistribuir a árvore de talentos). A devolução vale na transação aberta do chamador.
    /// </summary>
    Task DevolverAsync(long personagemId, int quantidade, CancellationToken cancelamento);
}
