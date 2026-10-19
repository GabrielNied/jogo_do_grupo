namespace JogoDoGrupo.Server.Contratos.Economia;

/// <summary>
/// Publicado depois do commit por quem mexe em coroas fora de Economia (a troca direta de Social).
/// Economia assina e reenvia o saldo atual ao client do personagem.
/// </summary>
public sealed record CoroasMovimentadas(long PersonagemId);
