using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O que o client sabe da troca direta (#175): o convite que espera resposta, a troca aberta
    /// (oferta dos dois lados e os aceites), a bolsa para montar a oferta e a última mensagem.
    /// Tudo vem do servidor, que decide; aqui só se pede e se guarda o que ele respondeu.
    /// </summary>
    internal sealed class EstadoDaTrocaDoClient : IDisposable
    {
        private static EstadoDaTrocaDoClient _instancia;

        private const float SegundosDaMensagem = 6f;

        private readonly IConexaoDoGrupo _conexao;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public ConviteDeTroca Convite { get; private set; }
        public float ConviteRestante { get; private set; }
        public EstadoDaTroca Troca { get; private set; }
        public BolsaParaTroca Bolsa { get; private set; }
        public string Mensagem { get; private set; } = "";
        public bool MensagemEhErro { get; private set; }
        public float MensagemRestante { get; private set; }

        /// <summary>Convite enviado que ainda não teve resposta (nome de quem foi convidado).</summary>
        public string ConviteEnviadoPara { get; private set; } = "";

        public bool EmTroca => Troca != null;

        public event Action Mudou;

        public EstadoDaTrocaDoClient(IConexaoDoGrupo conexao)
        {
            _conexao = conexao;
            if (_conexao == null)
            {
                return;
            }

            _assinaturas.Add(
                _conexao.Assinar<ConviteDeTroca>(SocialMessageIds.ConviteDeTroca, AoReceberConvite)
            );
            _assinaturas.Add(
                _conexao.Assinar<EstadoDaTroca>(SocialMessageIds.EstadoDaTroca, AoReceberEstado)
            );
            _assinaturas.Add(
                _conexao.Assinar<BolsaParaTroca>(SocialMessageIds.BolsaParaTroca, AoReceberBolsa)
            );
            _assinaturas.Add(
                _conexao.Assinar<TrocaEncerrada>(SocialMessageIds.TrocaEncerrada, AoEncerrar)
            );
            _assinaturas.Add(
                _conexao.Assinar<TrocaRecusada>(SocialMessageIds.TrocaRecusada, AoRecusar)
            );
        }

        public static EstadoDaTrocaDoClient Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    _instancia = new EstadoDaTrocaDoClient(new ConexaoDoGrupoReal());
                }

                return _instancia;
            }
        }

        // ── Pedidos ───────────────────────────────────────────────────────────────

        public bool Pedir(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return false;
            }

            var ok = Enviar(SocialMessageIds.PedirTroca, new PedirTroca { Nome = nome.Trim() });
            if (ok)
            {
                ConviteEnviadoPara = nome.Trim();
                DefinirMensagem(TextosTroca.ConviteEnviado(ConviteEnviadoPara), false);
            }

            return ok;
        }

        public void ResponderConvite(bool aceitar)
        {
            if (Convite == null)
            {
                return;
            }

            Enviar(
                SocialMessageIds.ResponderConviteDeTroca,
                new ResponderConviteDeTroca { TrocaId = Convite.TrocaId, Aceitar = aceitar }
            );
            Convite = null;
            Mudou?.Invoke();
        }

        /// <summary>Põe o item na oferta, ou tira se já estava. Reenvia a oferta inteira.</summary>
        public void AlternarItem(long instanciaId)
        {
            if (Troca == null)
            {
                return;
            }

            var ids = new List<long>();
            var estava = false;
            foreach (var item in Troca.Minha.Itens)
            {
                if (item.InstanciaId == instanciaId)
                {
                    estava = true;
                }
                else
                {
                    ids.Add(item.InstanciaId);
                }
            }

            if (!estava)
            {
                if (ids.Count >= RegrasTroca.ItensMaximosPorLado)
                {
                    DefinirMensagem(TextosTroca.Codigo(CodigosErroTroca.OfertaInvalida), true);
                    return;
                }

                ids.Add(instanciaId);
            }

            EnviarOferta(Troca.Minha.Coroas, ids.ToArray());
        }

        public void DefinirCoroas(long coroas)
        {
            if (Troca == null)
            {
                return;
            }

            var ids = new long[Troca.Minha.Itens.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = Troca.Minha.Itens[i].InstanciaId;
            }

            EnviarOferta(coroas < 0 ? 0 : coroas, ids);
        }

        public void Confirmar()
        {
            if (Troca == null)
            {
                return;
            }

            Enviar(
                SocialMessageIds.ConfirmarTroca,
                new ConfirmarTroca
                {
                    TrocaId = Troca.TrocaId,
                    VersaoDaOferta = Troca.VersaoDaOferta,
                }
            );
        }

        public void Cancelar()
        {
            if (Troca != null)
            {
                Enviar(
                    SocialMessageIds.CancelarTroca,
                    new CancelarTroca { TrocaId = Troca.TrocaId }
                );
            }
            else if (Convite != null)
            {
                ResponderConvite(false);
            }
            else if (!string.IsNullOrEmpty(ConviteEnviadoPara))
            {
                // Id 0 quer dizer "a minha": quem convidou ainda não conhece o id do convite.
                Enviar(SocialMessageIds.CancelarTroca, new CancelarTroca { TrocaId = 0 });
                ConviteEnviadoPara = "";
            }
        }

        /// <summary>Avança o relógio do convite e da mensagem (chamado todo quadro no mundo).</summary>
        public void Atualizar(float segundos)
        {
            var mudou = false;
            if (Convite != null)
            {
                ConviteRestante -= segundos;
                if (ConviteRestante <= 0f)
                {
                    Convite = null;
                    mudou = true;
                }
            }

            if (MensagemRestante > 0f)
            {
                MensagemRestante -= segundos;
                if (MensagemRestante <= 0f)
                {
                    Mensagem = "";
                    mudou = true;
                }
            }

            if (mudou)
            {
                Mudou?.Invoke();
            }
        }

        /// <summary>Esquece tudo (saiu do mundo ou trocou de personagem).</summary>
        public void Limpar()
        {
            if (Convite == null && Troca == null && Bolsa == null && string.IsNullOrEmpty(Mensagem))
            {
                return;
            }

            Convite = null;
            Troca = null;
            Bolsa = null;
            ConviteEnviadoPara = "";
            Mensagem = "";
            MensagemRestante = 0f;
            Mudou?.Invoke();
        }

        // ── Mensagens do servidor ─────────────────────────────────────────────────

        internal void AoReceberConvite(ConviteDeTroca convite)
        {
            Convite = convite;
            ConviteRestante = convite.ValidadeSegundos > 0 ? convite.ValidadeSegundos : 30f;
            Mudou?.Invoke();
        }

        internal void AoReceberEstado(EstadoDaTroca estado)
        {
            var primeira = Troca == null || Troca.TrocaId != estado.TrocaId;
            Troca = estado;
            Convite = null;
            ConviteEnviadoPara = "";
            Mudou?.Invoke();
            if (primeira)
            {
                JanelasDoJogo.Abrir(JanelaTroca.Chave);
            }
        }

        internal void AoReceberBolsa(BolsaParaTroca bolsa)
        {
            Bolsa = bolsa;
            Mudou?.Invoke();
        }

        internal void AoEncerrar(TrocaEncerrada encerrada)
        {
            Troca = null;
            Bolsa = null;
            Convite = null;
            ConviteEnviadoPara = "";
            var concluida = encerrada.Motivo == CodigosErroTroca.Concluida;
            DefinirMensagem(
                TextosTroca.Encerrada(encerrada.Motivo, encerrada.NomeDoOutro),
                !concluida
            );
            JanelasDoJogo.Fechar(JanelaTroca.Chave);
        }

        internal void AoRecusar(TrocaRecusada recusa)
        {
            ConviteEnviadoPara = "";
            DefinirMensagem(TextosTroca.Codigo(recusa.Codigo), true);
        }

        // ── Apoio ─────────────────────────────────────────────────────────────────

        private void EnviarOferta(long coroas, long[] ids) =>
            Enviar(
                SocialMessageIds.AtualizarOfertaDeTroca,
                new AtualizarOfertaDeTroca
                {
                    TrocaId = Troca.TrocaId,
                    Coroas = coroas,
                    InstanciaIds = ids,
                }
            );

        private bool Enviar<T>(ushort id, T mensagem) =>
            _conexao != null && _conexao.Enviar(id, mensagem, EntregaTempoReal.Confiavel);

        private void DefinirMensagem(string texto, bool erro)
        {
            Mensagem = texto;
            MensagemEhErro = erro;
            MensagemRestante = SegundosDaMensagem;
            // Também vai para o HUD e o chat de sistema: a janela pode estar fechada.
            AvisosDoJogo.Mostrar(texto, erro ? TipoDeAviso.Aviso : TipoDeAviso.Informacao);
            Mudou?.Invoke();
        }

        public void Dispose()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura?.Dispose();
            }

            _assinaturas.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia?.Dispose();
            _instancia = null;
        }
    }
}
