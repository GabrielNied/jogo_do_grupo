namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>
/// Balde de fichas de uma conexão (issue #211): cada pacote recebido gasta uma ficha, e as
/// fichas voltam a <c>mensagensPorSegundo</c> até o máximo de <c>rajada</c>. Sem ficha, o pacote
/// passou do limite. Conta em milésimos de ficha (inteiros, sem erro de arredondamento). Usado
/// só pelo laço de rede (sem trava).
/// </summary>
public sealed class LimiteDeMensagens
{
    private const long MilesimosPorFicha = 1000;

    private readonly long _porMs;
    private readonly long _maximo;
    private long _milesimos;
    private long _ultimoMs;

    public LimiteDeMensagens(int mensagensPorSegundo, int rajada, long agoraMs)
    {
        // mensagensPorSegundo fichas por 1000 ms = mensagensPorSegundo milésimos por ms.
        _porMs = mensagensPorSegundo;
        _maximo = rajada * MilesimosPorFicha;
        _milesimos = _maximo;
        _ultimoMs = agoraMs;
    }

    /// <summary>Gasta uma ficha. Devolve <c>false</c> se o pacote passou do limite.</summary>
    public bool Consumir(long agoraMs)
    {
        if (agoraMs > _ultimoMs)
        {
            var ganho = (agoraMs - _ultimoMs) * _porMs;
            _milesimos = ganho >= _maximo - _milesimos ? _maximo : _milesimos + ganho;
            _ultimoMs = agoraMs;
        }

        if (_milesimos < MilesimosPorFicha)
        {
            return false;
        }

        _milesimos -= MilesimosPorFicha;
        return true;
    }
}
