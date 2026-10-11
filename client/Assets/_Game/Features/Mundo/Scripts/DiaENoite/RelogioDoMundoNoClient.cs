using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Client.Features.Mundo.DiaENoite
{
    /// <summary>
    /// A hora do mundo no client (issue #382). O dono do relógio é o servidor: cada
    /// <see cref="HoraDoMundo"/> acerta a hora, e entre duas mensagens ela anda sozinha com
    /// <see cref="RegrasDiaENoite.HoraApos(double, double, double)"/>. Antes da primeira mensagem
    /// o client não tem hora e não inventa uma.
    /// </summary>
    public sealed class RelogioDoMundoNoClient
    {
        private double _horaRecebida;
        private double _duracaoDoDia = RegrasDiaENoite.DuracaoDoDiaSegundos;
        private double _segundosDesde;

        /// <summary>Já chegou alguma hora do servidor.</summary>
        public bool TemHora { get; private set; }

        /// <summary>A hora agora, de 0 até menos de 24. Sem hora do servidor, a hora inicial.</summary>
        public float Hora =>
            TemHora
                ? RegrasDiaENoite.HoraApos(_horaRecebida, _segundosDesde, _duracaoDoDia)
                : RegrasDiaENoite.HoraInicial;

        /// <summary>O servidor mandou a hora: ela substitui a que o client contava.</summary>
        public void Receber(HoraDoMundo mensagem)
        {
            if (mensagem == null)
            {
                return;
            }

            _horaRecebida = RegrasDiaENoite.Normalizar(mensagem.Hora);
            _duracaoDoDia = RegrasDiaENoite.DuracaoValida(mensagem.DuracaoDoDiaSegundos);
            _segundosDesde = 0;
            TemHora = true;
        }

        /// <summary>Passaram <paramref name="segundos"/> reais. Sem hora do servidor, nada anda.</summary>
        public void Avancar(float segundos)
        {
            if (!TemHora || float.IsNaN(segundos) || float.IsInfinity(segundos) || segundos <= 0f)
            {
                return;
            }

            _segundosDesde += segundos;
        }
    }
}
