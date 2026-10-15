using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Envia uma mensagem a quem está na área de um personagem, incluindo o próprio personagem
/// mesmo se acabou de sair dela. Seguro para qualquer thread.
/// </summary>
public sealed class AvisosNaArea(
    ServidorTempoReal tempoReal,
    IPosicoesDoMundo posicoes,
    IVizinhanca? vizinhanca = null
)
{
    /// <param name="incluirDono">Falso quando o dono já sabe (ex.: soltou a guarda ele mesmo).</param>
    public void Enviar<T>(
        string area,
        long personagemId,
        ushort id,
        T mensagem,
        bool incluirDono = true
    )
    {
        var quemVe =
            (area == AreasDoMundo.Cidade && vizinhanca != null)
                ? vizinhanca.QuemVe(personagemId)
                : posicoes.PersonagensNaArea(area);
        var presentes = new HashSet<long>(quemVe);
        if (incluirDono)
        {
            presentes.Add(personagemId);
        }
        else
        {
            presentes.Remove(personagemId);
        }

        foreach (var sessao in tempoReal.Sessoes)
        {
            if (presentes.Contains(sessao.PersonagemId))
            {
                tempoReal.Enviar(sessao, id, mensagem);
            }
        }
    }
}
