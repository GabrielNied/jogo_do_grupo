namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Contrato de persistência da posição dos personagens no mundo (issue #1203).
/// </summary>
public interface IPersistenciaDePosicaoDoMundo
{
    PosicaoDoPersonagem? Carregar(long personagemId);
    Task<PosicaoDoPersonagem?> CarregarAsync(long personagemId, CancellationToken ct = default);
    void Salvar(long personagemId, string area, float x, float y, float z, float rotacaoY);
    Task SalvarAsync(
        long personagemId,
        string area,
        float x,
        float y,
        float z,
        float rotacaoY,
        CancellationToken ct = default
    );
}
