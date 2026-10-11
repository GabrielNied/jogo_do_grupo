using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>Saída de rede das listas de amigos e ignorados (sem referência ao UDP).</summary>
public interface ISaidaAmigos
{
    void ListaDeAmigos(long personagemId, ListaDeAmigos lista);

    void ListaDeIgnorados(long personagemId, ListaDeIgnorados lista);

    void AmigoMudouDeEstado(long personagemId, AmigoMudouDeEstado mudanca);

    void RecusarAmizade(long personagemId, string codigo);
}
