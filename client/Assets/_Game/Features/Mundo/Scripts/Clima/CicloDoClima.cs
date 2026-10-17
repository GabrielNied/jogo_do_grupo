using System;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Clima
{
    /// <summary>
    /// Ouve o <see cref="ClimaDoMundo"/> do servidor e aplica a névoa, a visão e os efeitos
    /// em <see cref="AmbienteDosBiomas.ClimaAtual"/> (issue #902). Vive na cena do mundo,
    /// criado pelo <c>ConstrutorHub</c>.
    /// </summary>
    public sealed class CicloDoClima : MonoBehaviour
    {
        private IDisposable _assinatura;

        private void OnEnable()
        {
            _assinatura = ConexaoTempoReal.Assinar<ClimaDoMundo>(
                MundoMessageIds.ClimaDoMundo,
                AoReceberClima
            );
        }

        private void OnDisable()
        {
            _assinatura?.Dispose();
            _assinatura = null;
        }

        private void OnDestroy()
        {
            // Fora do mundo o clima volta a ser o limpo padrão.
            AmbienteDosBiomas.ClimaAtual = TipoDeClima.Limpo;
        }

        public void AoReceberClima(ClimaDoMundo msg)
        {
            if (msg == null)
            {
                return;
            }

            AmbienteDosBiomas.ClimaAtual = msg.Clima;
        }
    }
}
