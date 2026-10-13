namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Client envia em <c>POST</c> <see cref="RotasTalentos.Comprar"/> para comprar um rank.
    /// O pedido leva o rank que o jogador quer ter (<see cref="RankAlvo"/>, o que viu mais um):
    /// se o rank já é esse ou maior, o servidor responde sucesso sem cobrar de novo, então um
    /// pedido repetido nunca cobra duas vezes. Servidor responde 200 <see cref="ArvoreResponse"/>;
    /// 400 <see cref="CodigosErroTalentos.NoInvalido"/> ou <see cref="CodigosErroTalentos.RankAlvoInvalido"/>;
    /// 409 nos demais códigos de <see cref="CodigosErroTalentos"/>; 404
    /// <c>PersonagemNaoEncontrado</c>; 401 do Core.
    /// </summary>
    [System.Serializable]
    public class ComprarNoRequest
    {
        public string NoId = "";

        /// <summary>Rank que o nó terá depois da compra (rank visto mais um).</summary>
        public int RankAlvo = 1;

        /// <summary>Versão da configuração que a tela está mostrando.</summary>
        public int VersaoConfiguracao = 0;
    }
}
