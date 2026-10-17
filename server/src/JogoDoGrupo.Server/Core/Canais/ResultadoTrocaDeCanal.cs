namespace JogoDoGrupo.Server.Core.Canais;

/// <summary>Resultado da tentativa de troca de canal pelo personagem.</summary>
public sealed record ResultadoTrocaDeCanal(
    bool Sucesso,
    int CanalAtual,
    int CanalDestino,
    string? CodigoErro = null
);
