using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Componente do client para apresentação da barra de quebra (stagger) de chefes (#171, combate.md).
    /// </summary>
    internal sealed class ControleBarraDeQuebra : MonoBehaviour
    {
        private ItemBarraDeQuebra _quebra;
        private long _chefeId;

        public long ChefeId => _chefeId;
        public ItemBarraDeQuebra Quebra => _quebra;
        public float Fracao => _quebra.Fracao;
        public bool Quebrado => _quebra.Quebrado;
        public string Texto => _quebra.Texto;

        public void Configurar(long chefeId)
        {
            _chefeId = chefeId;
            _quebra = new ItemBarraDeQuebra(
                0,
                ConfigEfeitosDeStatus.CapacidadeBarraDeQuebraBoss,
                false
            );
        }

        public void AplicarDoServidor(BarraDeQuebraMudou msg)
        {
            if (msg == null || msg.EntidadeId != _chefeId)
            {
                return;
            }

            _quebra = ApresentacaoEfeitosDeStatus.MontarQuebra(msg);
        }
    }
}
