namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>Diz quando já passou tempo suficiente para o próximo envio (ex.: 10 Hz).</summary>
    public sealed class LimitadorDeTaxa
    {
        private readonly float _intervalo;
        private float _acumulado;

        public LimitadorDeTaxa(float porSegundo)
        {
            _intervalo = porSegundo > 0f ? 1f / porSegundo : 0f;
            _acumulado = _intervalo;
        }

        /// <summary>
        /// Soma o tempo do quadro e devolve <c>true</c> quando é hora de enviar. Quadros longos
        /// valem um envio só, sem rajada para recuperar.
        /// </summary>
        public bool Deve(float delta)
        {
            _acumulado += delta;
            if (_acumulado < _intervalo)
            {
                return false;
            }

            _acumulado = _intervalo > 0f ? _acumulado % _intervalo : 0f;
            return true;
        }
    }
}
