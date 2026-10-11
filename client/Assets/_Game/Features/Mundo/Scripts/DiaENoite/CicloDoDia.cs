using System;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.DiaENoite
{
    /// <summary>
    /// Ouve a <see cref="HoraDoMundo"/> do servidor e escreve a hora em
    /// <see cref="AmbienteDosBiomas.Hora"/>, que escurece o céu, a névoa e o sol, gira a luz e
    /// acende as lanternas. Vive na cena do mundo, criado pelo <c>ConstrutorHub</c>.
    /// </summary>
    public sealed class CicloDoDia : MonoBehaviour
    {
        private readonly RelogioDoMundoNoClient _relogio = new RelogioDoMundoNoClient();
        private IDisposable _assinatura;

        /// <summary>O relógio desta cena (para os testes).</summary>
        internal RelogioDoMundoNoClient Relogio => _relogio;

        private void OnEnable()
        {
            _assinatura = ConexaoTempoReal.Assinar<HoraDoMundo>(
                MundoMessageIds.HoraDoMundo,
                _relogio.Receber
            );
        }

        private void OnDisable()
        {
            _assinatura?.Dispose();
            _assinatura = null;
        }

        private void OnDestroy()
        {
            // Fora do mundo não há hora: a próxima entrada espera a do servidor.
            AmbienteDosBiomas.Hora = AmbienteDosBiomas.HoraSemCiclo;
        }

        private void Update()
        {
            _relogio.Avancar(Time.deltaTime);
            if (_relogio.TemHora)
            {
                AmbienteDosBiomas.Hora = _relogio.Hora;
            }
        }
    }
}
