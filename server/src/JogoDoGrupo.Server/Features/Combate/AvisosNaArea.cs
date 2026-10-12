using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Envia uma mensagem a quem está na área de um personagem, incluindo o próprio personagem
/// mesmo se acabou de sair dela. Seguro para qualquer thread.
/// </summary>
public sealed class AvisosNaArea(ServidorTempoReal tempoReal, IPosicoesDoMundo posicoes)
{
    public void Enviar<T>(string area, long personagemId, ushort id, T mensagem)
    {
        var presentes = new HashSet<long>(posicoes.PersonagensNaArea(area)) { personagemId };
        foreach (var sessao in tempoReal.Sessoes)
        {
            if (presentes.Contains(sessao.PersonagemId))
            {
                tempoReal.Enviar(sessao, id, mensagem);
            }
        }
    }
}
