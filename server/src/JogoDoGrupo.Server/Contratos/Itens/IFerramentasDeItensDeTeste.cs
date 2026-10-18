using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// Ferramentas de itens da tela de teste de Desenvolvimento. O servidor segue sendo quem gera,
/// grava e valida: a tela só escolhe o modelo, o grau e a origem. Só essa tela chama.
/// </summary>
public interface IFerramentasDeItensDeTeste
{
    /// <summary>
    /// Gera até <paramref name="quantidade"/> equipamentos na primeira casa livre da bolsa
    /// (os que não couberem ficam de fora e o texto diz quantos).
    /// </summary>
    Task<(bool Aceito, string Texto)> GerarAsync(
        long personagemId,
        int modeloId,
        GrauEquipamento grau,
        OrigemDoLoot origem,
        int quantidade,
        CancellationToken c
    );

    /// <summary>Veste o último item que a tela gerou, pela regra normal de equipar.</summary>
    Task<(bool Aceito, string Texto)> EquiparUltimoGeradoAsync(
        long personagemId,
        CancellationToken c
    );

    /// <summary>Apaga os itens da bolsa, menos os favoritos.</summary>
    Task<(bool Aceito, string Texto)> LimparBolsaAsync(long personagemId, CancellationToken c);

    /// <summary>Zera (<paramref name="quebrar"/>) ou devolve a durabilidade das peças vestidas.</summary>
    Task<(bool Aceito, string Texto)> AjustarDurabilidadeDosEquipadosAsync(
        long personagemId,
        bool quebrar,
        CancellationToken c
    );

    /// <summary>Soma essências de dungeon e de raid.</summary>
    Task<(bool Aceito, string Texto)> CreditarEssenciasAsync(
        long personagemId,
        int quantidade,
        CancellationToken c
    );
}
