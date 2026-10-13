using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Ajustes do baú lidos da configuração (<c>Itens:Bau</c>). Os padrões são os de produção; os
/// testes que não tratam de posição ou recarga desligam as duas conferências.
/// </summary>
public sealed class OpcoesBau
{
    /// <summary>Confere a distância pela posição aceita pelo Mundo.</summary>
    public bool ExigirPosicao { get; set; } = true;

    /// <summary>Segundos entre duas aberturas do mesmo personagem (0 desliga).</summary>
    public double RecargaSegundos { get; set; } = ConfigBau.RecargaSegundos;
}

/// <summary>
/// Recarga do baú por personagem, em memória (reiniciar o servidor zera: é só um freio contra
/// abrir em rajada). A reserva é atômica: dois pedidos simultâneos do mesmo personagem nunca
/// passam os dois.
/// </summary>
public sealed class RecargaDoBau(OpcoesBau opcoes, TimeProvider relogio)
{
    private readonly object _trava = new();
    private readonly Dictionary<long, DateTimeOffset> _liberaEm = new();

    /// <summary>Reserva a abertura; falso se ainda está em recarga.</summary>
    public bool TentarReservar(long personagemId)
    {
        if (opcoes.RecargaSegundos <= 0)
        {
            return true;
        }

        var agora = relogio.GetUtcNow();
        lock (_trava)
        {
            if (_liberaEm.TryGetValue(personagemId, out var liberaEm) && agora < liberaEm)
            {
                return false;
            }

            _liberaEm[personagemId] = agora.AddSeconds(opcoes.RecargaSegundos);
            return true;
        }
    }

    /// <summary>Devolve a reserva quando a abertura não aconteceu (bolsa cheia, erro).</summary>
    public void Liberar(long personagemId)
    {
        lock (_trava)
        {
            _liberaEm.Remove(personagemId);
        }
    }
}
