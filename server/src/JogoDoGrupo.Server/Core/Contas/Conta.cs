namespace JogoDoGrupo.Server.Core.Contas;

/// <summary>Conta de jogador. As funcionalidades referenciam a conta só pelo <see cref="Id"/>.</summary>
public sealed class Conta
{
    public long Id { get; set; }

    /// <summary>Usuário como foi digitado no registro.</summary>
    public string Usuario { get; set; } = "";

    /// <summary>Usuário em maiúsculas, com índice único: "Ana" e "ana" são a mesma conta.</summary>
    public string UsuarioNormalizado { get; set; } = "";

    public string SenhaHash { get; set; } = "";

    public DateTime CriadaEm { get; set; }

    /// <summary>
    /// Carimbo aleatório alterado ao trocar senha ou revogar sessões; tokens emitidos antes perdem a validade (#517).
    /// </summary>
    public string CarimboSeguranca { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Indica se a conta possui permissão administrativa de Game Master (#187).</summary>
    public bool EhGm { get; set; }

    /// <summary>Data UTC até a qual a conta está temporariamente banida, ou nulo se ativa (#187).</summary>
    public DateTime? BanidaAte { get; set; }

    /// <summary>Motivo descritivo do banimento para auditoria e aviso (#187).</summary>
    public string? MotivoBanimento { get; set; }

    public void RenovarCarimboSeguranca() => CarimboSeguranca = Guid.NewGuid().ToString("N");

    public static string Normalizar(string usuario) => usuario.ToUpperInvariant();
}
