namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Publicado por quem aplica a cura (Combate ou Skills, quando a cura existir) a cada cura que
/// recuperou vida de verdade num aliado. Mobs assina: quem cura um aliado que já está em luta com
/// um inimigo passa a participar da morte dele (loot e experiência). Cura em si mesmo não
/// publica. <see cref="Area"/> é onde o curado estava. Pode sair de qualquer thread: o assinante
/// não pode bloquear.
/// </summary>
public sealed record PersonagemCurouAliado(long CuradorId, long CuradoId, string Area);
