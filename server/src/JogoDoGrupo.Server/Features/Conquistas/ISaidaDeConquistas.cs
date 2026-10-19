using JogoDoGrupo.Shared.Features.Conquistas;

namespace JogoDoGrupo.Server.Features.Conquistas;

public interface ISaidaDeConquistas
{
    void Dados(long personagemId, DadosDeConquistas dados);
    void Desbloqueada(long personagemId, ConquistaDesbloqueada desbloqueada);
    void TituloEquipado(long personagemId, string titulo);
    void Recusada(long personagemId, string codigo);
}
