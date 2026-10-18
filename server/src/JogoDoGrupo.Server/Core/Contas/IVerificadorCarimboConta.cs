namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>
/// Confere se o carimbo de segurança do token confere com o carimbo atual da conta (#517).
/// </summary>
public interface IVerificadorCarimboConta
{
    ValueTask<bool> CarimboValidoAsync(
        long contaId,
        string? carimboDoToken,
        CancellationToken ct = default
    );

    void Invalidar(long contaId);
}
