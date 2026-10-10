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

    public static string Normalizar(string usuario) => usuario.ToUpperInvariant();
}
