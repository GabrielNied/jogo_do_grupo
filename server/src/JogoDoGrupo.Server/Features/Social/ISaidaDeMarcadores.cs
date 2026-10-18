using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

public interface ISaidaDeMarcadores
{
    void Marcadores(long personagemId, MarcadoresDeAlvoMudaram mensagem);
    void Recusada(long personagemId, DefinirMarcadorRecusado recusa);
}
