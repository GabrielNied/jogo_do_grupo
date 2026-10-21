using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
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
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private PainelEfeitosDeStatus _painel;
        private float _imunidadeAtordoadoRestante;
        private long _entidadeId;
        private ItemDeEscudo _escudo;

        public long EntidadeId => _entidadeId;
        public float ImunidadeAtordoadoRestante => _imunidadeAtordoadoRestante;
        public IReadOnlyList<ItemDeStatus> Efeitos => _efeitos;
        public ItemDeEscudo Escudo => _escudo;

        public void Configurar(long entidadeId, PainelEfeitosDeStatus painel = null)
        {
            _entidadeId = entidadeId;
            _painel = painel;

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EfeitosDeStatusMudaram>(
                    CombateMessageIds.EfeitosDeStatusMudaram,
                    AoMudarEfeitos
                )
            );

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EscudoMudou>(CombateMessageIds.EscudoMudou, AoMudarEscudo)
            );

            _painel?.Atualizar(_efeitos, _imunidadeAtordoadoRestante, _escudo);
        }

        private void AoMudarEscudo(EscudoMudou msg)
        {
            if (msg != null && msg.EntidadeId == _entidadeId)
            {
                AplicarEscudoDoServidor(msg);
            }
        }

        /// <summary>Aplica o escudo recebido do servidor (absorção 0 tira o escudo).</summary>
        public void AplicarEscudoDoServidor(EscudoMudou msg)
        {
            if (msg == null || msg.EntidadeId != _entidadeId)
            {
                return;
            }

            _escudo = new ItemDeEscudo(
                msg.Absorcao,
                msg.AbsorcaoMaxima,
                msg.DuracaoRestanteSegundos
            );
            _painel?.Atualizar(_efeitos, _imunidadeAtordoadoRestante, _escudo);
        }

        private void AoMudarEfeitos(EfeitosDeStatusMudaram msg)
        {
            if (msg != null && msg.EntidadeId == _entidadeId)
            {
                AplicarDoServidor(msg);
            }
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
            _painel?.Atualizar(_efeitos, _imunidadeAtordoadoRestante, _escudo);
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
            var tinhaAlgo = _efeitos.Count > 0 || _imunidadeAtordoadoRestante > 0f || _escudo.Ativo;
            if (!tinhaAlgo)
            {
                return;
            }

            var dt = Time.deltaTime;
            if (_imunidadeAtordoadoRestante > 0f)
            {
                _imunidadeAtordoadoRestante = Math.Max(0f, _imunidadeAtordoadoRestante - dt);
            }

            if (_escudo.Ativo)
            {
                _escudo = new ItemDeEscudo(
                    _escudo.Absorcao,
                    _escudo.AbsorcaoMaxima,
                    _escudo.DuracaoRestante - dt
                );
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

            _painel?.Atualizar(_efeitos, _imunidadeAtordoadoRestante, _escudo);
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
