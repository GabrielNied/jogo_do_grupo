namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor informa o clima atual da área (issue #902): enviado ao entrar na área, na mudança
    /// de clima e periodicamente. O clima é decidido pelo servidor e todos na área recebem o mesmo.
    /// Id: <see cref="MundoMessageIds.ClimaDoMundo"/>.
    /// </summary>
    [System.Serializable]
    public class ClimaDoMundo
    {
        /// <summary>Tipo do clima atual sorteado pelo servidor.</summary>
        public TipoDeClima Clima = TipoDeClima.Limpo;

        /// <summary>Fator de velocidade aplicado na nevasca (ex.: 0.70).</summary>
        public float FatorVelocidade = ConfigClima.FatorVelocidadePadrao;

        /// <summary>Alcance de visão máximo sob o clima (m).</summary>
        public float DistanciaVisao = ConfigClima.DistanciaVisaoLimpo;

        /// <summary>Densidade da névoa exponencial.</summary>
        public float DensidadeNevoa = ConfigClima.DensidadeNevoaLimpo;

        /// <summary>Duração em segundos reais deste período de clima.</summary>
        public int DuracaoSegundos = ConfigClima.DuracaoPadraoClimaSegundos;
    }
}
