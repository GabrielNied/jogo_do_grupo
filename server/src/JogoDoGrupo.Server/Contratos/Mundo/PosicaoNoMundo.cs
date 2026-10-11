namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>Onde um personagem está: a área e a posição autoritativa dentro dela.</summary>
public readonly record struct PosicaoNoMundo(
    string Area,
    float X,
    float Y,
    float Z,
    float RotacaoY
);
