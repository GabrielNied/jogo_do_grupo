namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>Operações para mudar um personagem ativo entre áreas do Mundo.</summary>
public interface ITransicoesDeAreaDoMundo
{
    /// <summary>
    /// Move o personagem para a área indicada e para a posição inicial informada. Retorna falso
    /// quando o personagem não está presente no Mundo, a área não é válida ou a posição não é
    /// finita. Ao concluir, publica os eventos de saída e entrada da área.
    /// </summary>
    bool TentarMoverParaArea(long personagemId, PosicaoNoMundo destino);
}
