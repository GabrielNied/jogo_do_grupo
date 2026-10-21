namespace JogoDoGrupo.Server.Contratos.Profissoes;

/// <summary>
/// Publicado pelas Profissões quando a coleta num ponto do mundo foi aceita e o material já
/// está creditado: <see cref="MaterialId"/> é o de <c>ConfigProfissoes.Materiais</c> e
/// <see cref="Quantidade"/> o que entrou nesta coleta (não o saldo). As missões com objetivo
/// <c>Coletar</c> contam por aqui.
/// <para>
/// Sai de uma <c>Task</c> do pedido de coleta: o assinante não pode bloquear.
/// </para>
/// </summary>
public sealed record MaterialColetado(long PersonagemId, int MaterialId, int Quantidade);
