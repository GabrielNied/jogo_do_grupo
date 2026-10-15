using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Componente do client para apresentação da barra de quebra (stagger) de chefes (#171, combate.md).
    /// </summary>
    internal sealed class ControleBarraDeQuebra : MonoBehaviour
    {
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private ItemBarraDeQuebra _quebra;
        private PainelBarraDeQuebra _painel;
        private long _chefeId;

        public long ChefeId => _chefeId;
        public ItemBarraDeQuebra Quebra => _quebra;
        public float Fracao => _quebra.Fracao;
        public bool Quebrado => _quebra.Quebrado;
        public string Texto => _quebra.Texto;

        public void Configurar(long chefeId, PainelBarraDeQuebra painel = null)
        {
            _chefeId = chefeId;
            _painel = painel;
            _quebra = new ItemBarraDeQuebra(
                0,
                ConfigEfeitosDeStatus.CapacidadeBarraDeQuebraBoss,
                false
            );

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<BarraDeQuebraMudou>(
                    CombateMessageIds.BarraDeQuebraMudou,
                    AoMudarQuebra
                )
            );

            _painel?.Atualizar(_quebra);
        }

        private void AoMudarQuebra(BarraDeQuebraMudou msg)
        {
            if (msg == null)
            {
                return;
            }

            if (_chefeId == 0 || msg.EntidadeId == _chefeId)
            {
                _chefeId = msg.EntidadeId;
                AplicarDoServidor(msg);
            }
        }

        public void AplicarDoServidor(BarraDeQuebraMudou msg)
        {
            if (msg == null)
            {
                return;
            }

            if (_chefeId != 0 && msg.EntidadeId != _chefeId)
            {
                return;
            }

            _chefeId = msg.EntidadeId;
            _quebra = ApresentacaoEfeitosDeStatus.MontarQuebra(msg);
            _painel?.Atualizar(_quebra);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _assinaturas.Count; i++)
            {
                _assinaturas[i]?.Dispose();
            }
            _assinaturas.Clear();
            _painel?.Descartar();
        }
    }
}
