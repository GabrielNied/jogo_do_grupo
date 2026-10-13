using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Server.Contratos.Talentos;

/// <summary>
/// Atributos principais que os nós comprados da árvore dão a um personagem, oferecidos por
/// Talentos a Combate (ADR 0016). Não inclui base, equipamento nem efeito. Pode ser chamada de
/// qualquer thread; cada chamada faz uma leitura curta no banco.
/// </summary>
public interface IAtributosDaArvore
{
    /// <summary>Zeros se o personagem não existe ou não comprou nada.</summary>
    Task<AtributosDaArvore> ObterAsync(long personagemId, CancellationToken cancelamento);
}
