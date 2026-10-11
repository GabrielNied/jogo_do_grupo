using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.DiaENoite
{
    /// <summary>
    /// Acende a luz da lanterna à noite e apaga de dia, pela <see cref="AmbienteDosBiomas.Hora"/>.
    /// O intervalo é o de <see cref="RegrasDiaENoite.EhNoite"/> (das 20h às 5h).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class LuzDaLanterna : MonoBehaviour
    {
        private Light _luz;

        /// <summary>A lanterna fica acesa nesta hora?</summary>
        public static bool Acesa(float hora) => RegrasDiaENoite.EhNoite(hora);

        private void Awake()
        {
            _luz = GetComponent<Light>();
        }

        private void Update()
        {
            var acesa = Acesa(AmbienteDosBiomas.Hora);
            if (_luz.enabled != acesa)
            {
                _luz.enabled = acesa;
            }
        }
    }
}
