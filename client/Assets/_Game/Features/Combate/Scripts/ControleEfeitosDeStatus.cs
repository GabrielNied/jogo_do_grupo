using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Componente do client que gerencia e exibe os efeitos de status ativos recebidos do servidor
    /// por <see cref="EfeitosDeStatusMudaram"/> (#171, combate.md).
    /// </summary>
    internal sealed class ControleEfeitosDeStatus : MonoBehaviour
    {
        private readonly List<ItemDeStatus> _efeitos = new List<ItemDeStatus>();
        private float _imunidadeAtordoadoRestante;
        private long _entidadeId;

        public long EntidadeId => _entidadeId;
        public float ImunidadeAtordoadoRestante => _imunidadeAtordoadoRestante;
        public IReadOnlyList<ItemDeStatus> Efeitos => _efeitos;

        public void Configurar(long entidadeId)
        {
            _entidadeId = entidadeId;
        }

        /// <summary>
        /// Aplica os efeitos recebidos da mensagem de rede do servidor.
        /// </summary>
        public void AplicarDoServidor(EfeitosDeStatusMudaram msg)
        {
            if (msg == null || msg.EntidadeId != _entidadeId)
            {
                return;
            }

            _efeitos.Clear();
            if (msg.Efeitos != null)
            {
                for (int i = 0; i < msg.Efeitos.Length; i++)
                {
                    _efeitos.Add(ApresentacaoEfeitosDeStatus.MontarItem(msg.Efeitos[i]));
                }
            }
            _imunidadeAtordoadoRestante = Math.Max(0f, msg.ImunidadeAtordoadoRestanteSegundos);
        }

        /// <summary>Se o efeito de status está ativo.</summary>
        public bool TemEfeito(TipoEfeitoDeStatus tipo)
        {
            for (int i = 0; i < _efeitos.Count; i++)
            {
                if (_efeitos[i].Tipo == tipo && _efeitos[i].DuracaoRestante > 0f)
                {
                    return true;
                }
            }
            return false;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (_imunidadeAtordoadoRestante > 0f)
            {
                _imunidadeAtordoadoRestante = Math.Max(0f, _imunidadeAtordoadoRestante - dt);
            }

            for (int i = _efeitos.Count - 1; i >= 0; i--)
            {
                var e = _efeitos[i];
                var restante = Math.Max(0f, e.DuracaoRestante - dt);
                if (restante <= 0f)
                {
                    _efeitos.RemoveAt(i);
                }
                else
                {
                    _efeitos[i] = new ItemDeStatus(e.Tipo, e.Acumulos, restante, e.DuracaoTotal);
                }
            }
        }
    }
}
