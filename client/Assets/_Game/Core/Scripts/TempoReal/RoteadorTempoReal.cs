using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Core;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.TempoReal
{
    /// <summary>
    /// Entrega cada mensagem recebida a quem assinou o Id dela ou a faixa que contém o Id.
    /// Um assinante que lança exceção não impede os outros de receber.
    /// </summary>
    internal sealed class RoteadorTempoReal
    {
        private readonly List<Assinatura> _assinaturas = new List<Assinatura>();

        public int Quantidade => _assinaturas.Count;

        /// <summary>Assina um Id. Descarte o retorno para cancelar.</summary>
        public IDisposable Assinar(ushort id, Action<MensagemTempoReal> aoReceber) =>
            Adicionar(new MessageIdRange(id, id), aoReceber);

        /// <summary>Assina um Id, já convertendo o corpo no DTO <typeparamref name="T"/>.</summary>
        public IDisposable Assinar<T>(ushort id, Action<T> aoReceber)
        {
            if (aoReceber == null)
            {
                throw new ArgumentNullException(nameof(aoReceber));
            }

            return Assinar(id, mensagem => aoReceber(mensagem.Ler<T>()));
        }

        /// <summary>Assina todos os Ids de uma faixa (ex.: <c>MundoMessageIds.Faixa</c>).</summary>
        public IDisposable AssinarFaixa(
            MessageIdRange faixa,
            Action<MensagemTempoReal> aoReceber
        ) => Adicionar(faixa, aoReceber);

        /// <summary>Entrega a mensagem e devolve quantos assinantes a receberam.</summary>
        public int Despachar(MensagemTempoReal mensagem)
        {
            if (_assinaturas.Count == 0)
            {
                return 0;
            }

            // Cópia: um assinante pode assinar ou cancelar durante a entrega.
            var entregues = 0;
            foreach (var assinatura in _assinaturas.ToArray())
            {
                if (assinatura.Cancelada || !assinatura.Faixa.Contem(mensagem.Id))
                {
                    continue;
                }

                entregues++;
                try
                {
                    assinatura.AoReceber(mensagem);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return entregues;
        }

        /// <summary>Cancela todas as assinaturas.</summary>
        public void Limpar()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Cancelada = true;
            }

            _assinaturas.Clear();
        }

        private IDisposable Adicionar(MessageIdRange faixa, Action<MensagemTempoReal> aoReceber)
        {
            if (aoReceber == null)
            {
                throw new ArgumentNullException(nameof(aoReceber));
            }

            var assinatura = new Assinatura(this, faixa, aoReceber);
            _assinaturas.Add(assinatura);
            return assinatura;
        }

        private sealed class Assinatura : IDisposable
        {
            private readonly RoteadorTempoReal _roteador;

            public Assinatura(
                RoteadorTempoReal roteador,
                MessageIdRange faixa,
                Action<MensagemTempoReal> aoReceber
            )
            {
                _roteador = roteador;
                Faixa = faixa;
                AoReceber = aoReceber;
            }

            public MessageIdRange Faixa { get; }
            public Action<MensagemTempoReal> AoReceber { get; }
            public bool Cancelada { get; set; }

            public void Dispose()
            {
                if (Cancelada)
                {
                    return;
                }

                Cancelada = true;
                _roteador._assinaturas.Remove(this);
            }
        }
    }
}
