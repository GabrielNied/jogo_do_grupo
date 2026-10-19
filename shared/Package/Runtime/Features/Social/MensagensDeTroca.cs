using System;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client → servidor: convida o personagem de nome <see cref="Nome"/> para uma troca direta.
    /// Id: <see cref="SocialMessageIds.PedirTroca"/>.
    /// </summary>
    [Serializable]
    public class PedirTroca
    {
        public string Nome = "";
    }

    /// <summary>
    /// Client → servidor: aceita ou recusa um convite de troca.
    /// Id: <see cref="SocialMessageIds.ResponderConviteDeTroca"/>.
    /// </summary>
    [Serializable]
    public class ResponderConviteDeTroca
    {
        public long TrocaId;
        public bool Aceitar;
    }

    /// <summary>
    /// Client → servidor: troca a oferta do jogador (coroas e itens da bolsa). Substitui a
    /// oferta anterior e desfaz o aceite dos dois lados.
    /// Id: <see cref="SocialMessageIds.AtualizarOfertaDeTroca"/>.
    /// </summary>
    [Serializable]
    public class AtualizarOfertaDeTroca
    {
        public long TrocaId;
        public long Coroas;
        public long[] InstanciaIds = Array.Empty<long>();
    }

    /// <summary>
    /// Client → servidor: aceita a troca como ela está. <see cref="VersaoDaOferta"/> é a versão
    /// que o jogador viu; se a oferta mudou desde então, o aceite é ignorado.
    /// Id: <see cref="SocialMessageIds.ConfirmarTroca"/>.
    /// </summary>
    [Serializable]
    public class ConfirmarTroca
    {
        public long TrocaId;
        public int VersaoDaOferta;
    }

    /// <summary>
    /// Client → servidor: cancela a troca (ou o convite) em andamento.
    /// Id: <see cref="SocialMessageIds.CancelarTroca"/>.
    /// </summary>
    [Serializable]
    public class CancelarTroca
    {
        public long TrocaId;
    }

    /// <summary>
    /// Client → servidor: pede de novo a bolsa e o saldo para montar a oferta.
    /// Id: <see cref="SocialMessageIds.ConsultarBolsaParaTroca"/>.
    /// </summary>
    [Serializable]
    public class ConsultarBolsaParaTroca
    {
        public long TrocaId;
    }

    /// <summary>
    /// Servidor → convidado: chegou um convite de troca. Responda com
    /// <see cref="ResponderConviteDeTroca"/>.
    /// Id: <see cref="SocialMessageIds.ConviteDeTroca"/>.
    /// </summary>
    [Serializable]
    public class ConviteDeTroca
    {
        public long TrocaId;
        public string NomeDe = "";
        public int ValidadeSegundos;
    }

    /// <summary>Um lado da troca: as coroas e os itens oferecidos.</summary>
    [Serializable]
    public class OfertaDeTroca
    {
        public long Coroas;
        public EntradaInventario[] Itens = Array.Empty<EntradaInventario>();
    }

    /// <summary>
    /// Servidor → os dois jogadores: o estado completo da troca, a cada mudança. "Minha" e
    /// "dele" valem para quem recebe. Os dois aceites começam falsos e voltam a falso a cada
    /// mudança de oferta.
    /// Id: <see cref="SocialMessageIds.EstadoDaTroca"/>.
    /// </summary>
    [Serializable]
    public class EstadoDaTroca
    {
        public long TrocaId;
        public string NomeDoOutro = "";

        /// <summary>Sobe a cada mudança de oferta; vai de volta no <see cref="ConfirmarTroca"/>.</summary>
        public int VersaoDaOferta;

        public OfertaDeTroca Minha = new OfertaDeTroca();
        public OfertaDeTroca Dele = new OfertaDeTroca();
        public bool MeuAceite;
        public bool AceiteDele;
    }

    /// <summary>
    /// Servidor → jogador: a bolsa (só o que pode ser negociado) e as coroas, para montar a
    /// oferta. Chega ao abrir a troca e a cada <see cref="ConsultarBolsaParaTroca"/>.
    /// Id: <see cref="SocialMessageIds.BolsaParaTroca"/>.
    /// </summary>
    [Serializable]
    public class BolsaParaTroca
    {
        public long TrocaId;
        public long Coroas;
        public EntradaInventario[] Itens = Array.Empty<EntradaInventario>();
    }

    /// <summary>
    /// Servidor → os dois jogadores: a troca acabou. <see cref="Motivo"/> é um dos motivos de
    /// <see cref="CodigosErroTroca"/>.
    /// Id: <see cref="SocialMessageIds.TrocaEncerrada"/>.
    /// </summary>
    [Serializable]
    public class TrocaEncerrada
    {
        public long TrocaId;
        public string Motivo = "";
        public string NomeDoOutro = "";
    }

    /// <summary>
    /// Servidor → quem pediu: o pedido de troca foi recusado e nada mudou (a troca em andamento,
    /// se houver, continua aberta).
    /// Id: <see cref="SocialMessageIds.TrocaRecusada"/>.
    /// </summary>
    [Serializable]
    public class TrocaRecusada
    {
        public long TrocaId;
        public string Codigo = "";
    }
}
