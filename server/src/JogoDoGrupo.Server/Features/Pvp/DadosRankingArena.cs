using JogoDoGrupo.Shared.Features.Pvp;

namespace JogoDoGrupo.Server.Features.Pvp;

public sealed class DadosRankingArena
{
    public int Pontos { get; set; } = RegrasPvp.PontosIniciaisArena;
    public int Vitorias { get; set; }
    public int Derrotas { get; set; }
    public int Temporada { get; set; } = 1;
}
