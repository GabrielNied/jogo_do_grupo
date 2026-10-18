using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

public interface ISaidaDeInspecao
{
    void Retrato(long paraPersonagemId, RetratoInspecaoResponse retrato);
    void Recusada(long paraPersonagemId, InspecaoRecusada recusa);
}
