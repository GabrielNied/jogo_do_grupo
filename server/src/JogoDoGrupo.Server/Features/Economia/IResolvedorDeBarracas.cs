namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Permite ao mercado saber se uma ordem pertence a alguém com barraca montada na praça (#905).
/// </summary>
public interface IResolvedorDeBarracas
{
    bool TentarObterNomeBarraca(long personagemId, out string nomeOuTitulo);
}
