using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Core;

/// <summary>
/// Serviços compartilhados que o core oferece às funcionalidades
/// (rede, persistência, eventos). Vai crescer conforme o core for implementado.
/// </summary>
public sealed class ServerContext(ServidorTempoReal tempoReal, EventosDoServidor eventos)
{
    /// <summary>
    /// Canal UDP: sessões autenticadas, envio unicast e broadcast, e os eventos
    /// <c>SessaoAutenticada</c> e <c>SessaoEncerrada</c>.
    /// </summary>
    public ServidorTempoReal TempoReal { get; } = tempoReal;

    /// <summary>
    /// Eventos entre funcionalidades: uma publica, as outras assinam, sem referência direta
    /// (ADR 0016). Assine no <c>Inicializar</c>.
    /// </summary>
    public EventosDoServidor Eventos { get; } = eventos;
}
