using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// Estado e capacidade do banco pessoal da conta (issue #177).
/// </summary>
public sealed class EstadoBanco
{
    /// <summary>Conta dona do banco.</summary>
    public long ContaId { get; set; }

    /// <summary>Quantidade de abas desbloqueadas no banco (1 a 4).</summary>
    public int Abas { get; set; } = RegrasBanco.AbasBancoInicial;
}
