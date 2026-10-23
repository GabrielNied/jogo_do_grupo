using System.Linq.Expressions;

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

    /// <summary>E-mail de recuperação como foi digitado, ou nulo (#1167). Sem e-mail, sem recuperação.</summary>
    public string? Email { get; set; }

    /// <summary>E-mail sem espaços nas pontas e em minúsculas, com índice único.</summary>
    public string? EmailNormalizado { get; set; }

    /// <summary>Hash do código de recuperação ativo (o código nunca é guardado), ou nulo.</summary>
    public string? CodigoRecuperacaoHash { get; set; }

    /// <summary>Quando o código ativo vence (UTC).</summary>
    public DateTime? CodigoRecuperacaoExpiraEm { get; set; }

    /// <summary>Tentativas erradas com o código ativo.</summary>
    public int CodigoRecuperacaoTentativas { get; set; }

    public void RenovarCarimboSeguranca() => CarimboSeguranca = Guid.NewGuid().ToString("N");

    /// <summary>Define ou tira (<c>null</c> ou vazio) o e-mail de recuperação.</summary>
    public void DefinirEmail(string? email)
    {
        var limpo = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Email = limpo;
        EmailNormalizado = limpo == null ? null : NormalizarEmail(limpo);
    }

    /// <summary>Troca o código ativo pelo novo: o pedido anterior deixa de valer.</summary>
    public void IniciarRecuperacao(string codigoHash, DateTime expiraEm)
    {
        CodigoRecuperacaoHash = codigoHash;
        CodigoRecuperacaoExpiraEm = expiraEm;
        CodigoRecuperacaoTentativas = 0;
    }

    /// <summary>Descarta o código ativo, se houver.</summary>
    public void EncerrarRecuperacao()
    {
        CodigoRecuperacaoHash = null;
        CodigoRecuperacaoExpiraEm = null;
        CodigoRecuperacaoTentativas = 0;
    }

    /// <summary>
    /// Se a conta tem código que ainda pode ser usado em <paramref name="agora"/> (UTC). É expressão
    /// para valer dentro do <c>ExecuteUpdate</c> que gasta a tentativa: a checagem e o incremento
    /// ficam num só comando no banco, e uma rajada não passa do limite (#1251).
    /// </summary>
    public static Expression<Func<Conta, bool>> RecuperacaoAtiva(DateTime agora) =>
        c =>
            c.CodigoRecuperacaoHash != null
            && c.CodigoRecuperacaoExpiraEm > agora
            && c.CodigoRecuperacaoTentativas < RecuperacaoDeConta.TentativasPorCodigo;

    public static string Normalizar(string usuario) => usuario.ToUpperInvariant();

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
