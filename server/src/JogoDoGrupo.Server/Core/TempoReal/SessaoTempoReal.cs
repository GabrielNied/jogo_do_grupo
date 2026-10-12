using LiteNetLib;

namespace JogoDoGrupo.Server.Core.TempoReal;

/// <summary>
/// Uma conexão UDP. As funcionalidades só recebem sessões já autenticadas, ligadas a uma
/// conta e a um personagem dela. Compare sessões pela referência ou por <see cref="Id"/>
/// enquanto estiverem ativas (o Id pode ser reaproveitado depois que a sessão encerra).
/// </summary>
public sealed class SessaoTempoReal
{
    internal SessaoTempoReal(NetPeer peer, int mensagensPorSegundo, int rajadaMensagens)
    {
        Peer = peer;
        ConectadaEm = Environment.TickCount64;
        Limite = new LimiteDeMensagens(mensagensPorSegundo, rajadaMensagens, ConectadaEm);
    }

    internal NetPeer Peer { get; }

    internal long ConectadaEm { get; }

    internal LimiteDeMensagens Limite { get; }

    internal bool AutenticacaoEmAndamento { get; set; }

    /// <summary>Identificador da conexão enquanto ela está ativa.</summary>
    public int Id => Peer.Id;

    public bool Autenticada { get; internal set; }

    /// <summary>Id da conta (claim <c>sub</c> do JWT). Zero antes de autenticar.</summary>
    public long ContaId { get; internal set; }

    /// <summary>Personagem com que a conta entrou. Zero antes de autenticar.</summary>
    public long PersonagemId { get; internal set; }
}
