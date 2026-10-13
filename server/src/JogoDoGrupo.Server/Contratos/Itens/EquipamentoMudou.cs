namespace JogoDoGrupo.Server.Contratos.Itens;

/// <summary>
/// Publicado por Itens, <b>depois de gravar</b>, sempre que o que o personagem veste muda:
/// equipar, desequipar (inclusive a troca de peça numa casa ocupada), trocar o conjunto de
/// armas, fundir uma peça que está vestida e limpar o inventário de teste. Quem guarda os
/// atributos finais do personagem (Combate) invalida a entrada na hora e relê por
/// <see cref="IAtributosDosEquipamentos"/>; o evento não carrega valores. Sai da thread da
/// requisição HTTP, então o assinante precisa ser seguro entre threads e não pode bloquear
/// (ADR 0016). Mover ou descartar item da bolsa não vestido não publica.
/// </summary>
public sealed record EquipamentoMudou(long PersonagemId);
