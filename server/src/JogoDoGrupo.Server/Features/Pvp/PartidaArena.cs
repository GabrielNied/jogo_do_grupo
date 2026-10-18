using System.Collections.Generic;
using JogoDoGrupo.Server.Contratos.Mundo;

namespace JogoDoGrupo.Server.Features.Pvp;

public sealed class PartidaArena
{
    public string Id { get; init; } = "";
    public string Area { get; init; } = "";
    public long[] TimeAzul { get; init; } = System.Array.Empty<long>();
    public long[] TimeVermelho { get; init; } = System.Array.Empty<long>();
    public HashSet<long> Eliminados { get; } = [];
    public Dictionary<long, int> VidaPorPersonagem { get; } = [];
    public Dictionary<long, int> VidaMaxPorPersonagem { get; } = [];
    public Dictionary<long, PosicaoNoMundo> PosicoesDeRetorno { get; } = [];
    public long CriadaEmMs { get; init; }
    public long LiberadaEmMs { get; init; }
    public bool Encerrada { get; set; }

    public bool EstaLiberada(long agoraMs) => agoraMs >= LiberadaEmMs;

    public int EquipeDe(long personagemId)
    {
        if (System.Array.IndexOf(TimeAzul, personagemId) >= 0)
        {
            return 0; // Azul
        }
        if (System.Array.IndexOf(TimeVermelho, personagemId) >= 0)
        {
            return 1; // Vermelho
        }
        return -1;
    }

    public bool SaoOponentes(long p1, long p2)
    {
        var e1 = EquipeDe(p1);
        var e2 = EquipeDe(p2);
        return e1 >= 0 && e2 >= 0 && e1 != e2;
    }
}
