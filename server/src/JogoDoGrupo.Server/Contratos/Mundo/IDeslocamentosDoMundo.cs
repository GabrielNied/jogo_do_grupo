namespace JogoDoGrupo.Server.Contratos.Mundo;

/// <summary>
/// Pedido que outra funcionalidade faz ao Mundo para deixar um personagem se deslocar mais
/// rápido que a corrida por um instante (ex.: a esquiva, de Combate). Quem pede já decidiu que
/// o deslocamento vale (custo pago); o Mundo continua validando cada posição (ADR 0016). Pode
/// ser chamado de qualquer thread.
/// </summary>
public interface IDeslocamentosDoMundo
{
    /// <summary>
    /// Abre uma janela a partir de agora: durante <paramref name="duracaoSegundos"/> o
    /// personagem pode se afastar até <paramref name="distanciaMetros"/> de onde está, na
    /// velocidade que isso dá. Fora da janela volta o limite normal. Uma janela nova substitui a
    /// anterior. Falso se o personagem não está em nenhuma área ou se os números não servem.
    /// </summary>
    bool AbrirJanela(long personagemId, double distanciaMetros, double duracaoSegundos);
}
