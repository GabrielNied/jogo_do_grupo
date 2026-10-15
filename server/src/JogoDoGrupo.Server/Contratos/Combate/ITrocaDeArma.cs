namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// A troca de arma de Combate vista pelas outras funcionalidades (ADR 0016). Skills consulta
/// <see cref="EmTroca"/> para recusar a skill nos 0,4 s da troca; Itens chama
/// <see cref="TentarReservar"/> nos botões Conj. 1/Conj. 2 do inventário, para valerem a mesma
/// regra do R (docs/features/combate.md, "Duas armas").
/// </summary>
public interface ITrocaDeArma
{
    /// <summary>Verdadeiro nos 0,4 s em que o personagem está trocando de arma.</summary>
    bool EmTroca(long personagemId);

    /// <summary>
    /// Reserva o personagem pelos 0,4 s da troca, com as mesmas recusas do R (preparo, guarda,
    /// esquiva, reação, atordoado, caído). Devolve <c>null</c> se reservou (ou se o personagem não
    /// está no mundo, onde não há combate) ou o código de recusa de Combate.
    /// </summary>
    string? TentarReservar(long personagemId);
}
