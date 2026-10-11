namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>
    /// Servidor informa a hora do mundo: ao entrar na área e, de tempos em tempos, de novo, para
    /// corrigir o relógio do client. O relógio é um só, do servidor; entre uma mensagem e outra o
    /// client avança a hora sozinho com <see cref="RegrasDiaENoite.HoraApos"/> e nunca inventa
    /// a própria. Todos na área recebem a mesma hora.
    /// Id: <see cref="MundoMessageIds.HoraDoMundo"/>.
    /// </summary>
    [System.Serializable]
    public class HoraDoMundo
    {
        /// <summary>Hora do dia no momento do envio, de 0 (meia-noite) até menos de 24.</summary>
        public float Hora = RegrasDiaENoite.HoraInicial;

        /// <summary>
        /// Quantos segundos reais dura um dia inteiro do mundo. Vai na mensagem para o client não
        /// depender de ter a mesma constante do servidor.
        /// </summary>
        public float DuracaoDoDiaSegundos = RegrasDiaENoite.DuracaoDoDiaSegundos;
    }
}
