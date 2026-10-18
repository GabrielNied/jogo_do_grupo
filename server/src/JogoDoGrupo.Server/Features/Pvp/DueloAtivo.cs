namespace JogoDoGrupo.Server.Features.Pvp;

public sealed class DueloAtivo
{
    public long DesafianteId { get; init; }
    public long DesafiadoId { get; init; }
    public string Area { get; init; } = "";
    public double CentroX { get; init; }
    public double CentroZ { get; init; }
    public long InicioMs { get; init; }
    public long LiberadoEmMs { get; init; }
    public int VidaDesafiante { get; set; }
    public int VidaMaxDesafiante { get; set; }
    public int VidaDesafiado { get; set; }
    public int VidaMaxDesafiado { get; set; }

    public bool EstaLiberado(long agoraMs) => agoraMs >= LiberadoEmMs;

    public bool Participa(long personagemId) =>
        personagemId == DesafianteId || personagemId == DesafiadoId;

    public long OponenteDe(long personagemId) =>
        personagemId == DesafianteId ? DesafiadoId : DesafianteId;
}
