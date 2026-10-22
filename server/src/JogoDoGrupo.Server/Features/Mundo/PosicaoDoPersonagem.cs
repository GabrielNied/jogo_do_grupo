namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Registra a última posição conhecida do personagem no mundo persistente entre sessões (issue #1203).
/// </summary>
public class PosicaoDoPersonagem
{
    public long PersonagemId { get; set; }
    public string Area { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float RotacaoY { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
