using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O grupo como o servidor informou: a última <see cref="ListaDoGrupo"/>, os convites que
    /// esperam resposta e a última mensagem para a pessoa. Envia as intenções (convidar,
    /// responder, sair, expulsar, passar liderança) e só muda quando o servidor responde: quem
    /// entra, quem é líder e a ordem da lista são dele. Sem Unity: a rede vem de
    /// <see cref="IConexaoDoGrupo"/> e o tempo avança em <see cref="Atualizar"/>.
    /// </summary>
    internal sealed class EstadoDoGrupo : IDisposable
    {
        /// <summary>Por quanto tempo uma mensagem fica na tela, em segundos.</summary>
        public const float DuracaoDaMensagemSegundos = 6f;

        private static readonly ConviteDoGrupo[] SemConvites = new ConviteDoGrupo[0];

        private readonly IConexaoDoGrupo _conexao;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private readonly List<ConviteDoGrupo> _convites = new List<ConviteDoGrupo>();
        private ListaDoGrupo _lista = new ListaDoGrupo();
        private int _segundosMostrados;

        public EstadoDoGrupo(IConexaoDoGrupo conexao)
        {
            _conexao = conexao ?? throw new ArgumentNullException(nameof(conexao));
            _assinaturas.Add(
                _conexao.Assinar<ConviteRecebido>(
                    SocialMessageIds.ConviteRecebido,
                    AoReceberConvite
                )
            );
            _assinaturas.Add(
                _conexao.Assinar<ListaDoGrupo>(SocialMessageIds.ListaDoGrupo, AoReceberLista)
            );
            _assinaturas.Add(
                _conexao.Assinar<AvisoDoGrupo>(SocialMessageIds.AvisoDoGrupo, AoReceberAviso)
            );
            _assinaturas.Add(
                _conexao.Assinar<OperacaoDoGrupoRecusada>(
                    SocialMessageIds.OperacaoDoGrupoRecusada,
                    AoReceberRecusa
                )
            );
        }

        /// <summary>Disparado quando a lista, os convites ou a mensagem mudam.</summary>
        public event Action Mudou;

        /// <summary>O personagem que está no mundo (para saber quem é "você" na lista).</summary>
        public long PersonagemLocalId { get; private set; }

        /// <summary>Identificador do grupo (0 sem grupo).</summary>
        public long GrupoId => _lista.GrupoId;

        public long LiderId => _lista.LiderId;

        /// <summary>Os membros na ordem que o servidor mandou. Vazio sem grupo.</summary>
        public IReadOnlyList<MembroDoGrupo> Membros =>
            _lista.Membros ?? Array.Empty<MembroDoGrupo>();

        /// <summary>Convites que esperam resposta, do mais antigo ao mais novo.</summary>
        public IReadOnlyList<ConviteDoGrupo> Convites =>
            _convites.Count == 0 ? SemConvites : _convites;

        public bool EmGrupo => Membros.Count > 0;

        public bool SouLider => EmGrupo && PersonagemLocalId != 0 && LiderId == PersonagemLocalId;

        /// <summary>Quem não tem grupo ou lidera um convida; membro comum não.</summary>
        public bool PodeConvidar => !EmGrupo || SouLider;

        /// <summary>Última mensagem para a pessoa; vazia depois de <see cref="DuracaoDaMensagemSegundos"/>.</summary>
        public string Mensagem { get; private set; } = "";

        public bool MensagemEhErro { get; private set; }

        public float MensagemRestante { get; private set; }

        /// <summary>Troca o personagem do mundo. Mudando de personagem, esquece o grupo do anterior.</summary>
        public void DefinirPersonagem(long personagemId)
        {
            if (PersonagemLocalId == personagemId)
            {
                return;
            }

            PersonagemLocalId = personagemId;
            Limpar();
        }

        /// <summary>Esquece grupo, convites e mensagem (saiu do mundo ou trocou de personagem).</summary>
        public void Limpar()
        {
            _lista = new ListaDoGrupo();
            _convites.Clear();
            Mensagem = "";
            MensagemEhErro = false;
            MensagemRestante = 0f;
            _segundosMostrados = 0;
            Mudou?.Invoke();
        }

        /// <summary>Convida pelo nome. Nome vazio ou sem conexão só mostra a mensagem.</summary>
        public bool Convidar(string nome)
        {
            var limpo = nome == null ? "" : nome.Trim();
            if (limpo.Length == 0)
            {
                Informar(TextosGrupo.NomeVazio, true);
                return false;
            }

            return Enviar(
                SocialMessageIds.ConvidarParaGrupo,
                new ConvidarParaGrupo { Nome = limpo }
            );
        }

        /// <summary>Aceita ou recusa um convite recebido. O convite sai da lista na hora; o servidor decide.</summary>
        public bool Responder(long conviteId, bool aceitar)
        {
            var enviou = Enviar(
                SocialMessageIds.ResponderConvite,
                new ResponderConvite { ConviteId = conviteId, Aceitar = aceitar }
            );
            if (enviou)
            {
                _convites.RemoveAll(c => c.ConviteId == conviteId);
                Mudou?.Invoke();
            }

            return enviou;
        }

        public bool Sair() => Enviar(SocialMessageIds.SairDoGrupo, new SairDoGrupo());

        public bool Expulsar(long personagemId) =>
            Enviar(
                SocialMessageIds.ExpulsarDoGrupo,
                new ExpulsarDoGrupo { PersonagemId = personagemId }
            );

        public bool PassarLideranca(long personagemId) =>
            Enviar(
                SocialMessageIds.PassarLideranca,
                new PassarLideranca { PersonagemId = personagemId }
            );

        /// <summary>Avança o tempo: expira convites na tela e apaga a mensagem antiga.</summary>
        public void Atualizar(float segundos)
        {
            if (segundos <= 0f)
            {
                return;
            }

            var mudou = false;
            if (MensagemRestante > 0f)
            {
                MensagemRestante = Math.Max(0f, MensagemRestante - segundos);
                mudou = MensagemRestante <= 0f;
            }

            for (var i = _convites.Count - 1; i >= 0; i--)
            {
                _convites[i].Restante -= segundos;
                if (_convites[i].Restante <= 0f)
                {
                    _convites.RemoveAt(i);
                    mudou = true;
                }
            }

            // Com convite na tela, avisa a cada segundo inteiro para a contagem andar.
            var segundosAgora = SegundosDoConvitePertoDoFim();
            if (segundosAgora != _segundosMostrados)
            {
                _segundosMostrados = segundosAgora;
                mudou = true;
            }

            if (mudou)
            {
                Mudou?.Invoke();
            }
        }

        public void Dispose()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
        }

        private int SegundosDoConvitePertoDoFim()
        {
            if (_convites.Count == 0)
            {
                return 0;
            }

            var menor = float.MaxValue;
            foreach (var convite in _convites)
            {
                menor = Math.Min(menor, convite.Restante);
            }

            return (int)Math.Ceiling(menor);
        }

        private bool Enviar<T>(ushort id, T mensagem)
        {
            if (_conexao.Enviar(id, mensagem, EntregaTempoReal.Confiavel))
            {
                return true;
            }

            Informar(TextosGrupo.SemConexao, true);
            return false;
        }

        private void Informar(string texto, bool erro)
        {
            Mensagem = texto ?? "";
            MensagemEhErro = erro;
            MensagemRestante = string.IsNullOrEmpty(texto) ? 0f : DuracaoDaMensagemSegundos;
            Mudou?.Invoke();
        }

        private void AoReceberConvite(ConviteRecebido convite)
        {
            if (convite == null)
            {
                return;
            }

            _convites.RemoveAll(c => c.ConviteId == convite.ConviteId);

            // Convite que já chega vencido não aparece.
            if (convite.ValidadeSegundos > 0)
            {
                _convites.Add(
                    new ConviteDoGrupo
                    {
                        ConviteId = convite.ConviteId,
                        NomeDoConvidante = convite.NomeDoConvidante ?? "",
                        Restante = convite.ValidadeSegundos,
                    }
                );
            }

            Mudou?.Invoke();
        }

        private void AoReceberLista(ListaDoGrupo lista)
        {
            _lista = lista ?? new ListaDoGrupo();
            if (_lista.Membros == null)
            {
                _lista.Membros = Array.Empty<MembroDoGrupo>();
            }

            Mudou?.Invoke();
        }

        private void AoReceberAviso(AvisoDoGrupo aviso)
        {
            if (aviso == null)
            {
                return;
            }

            // O convite de quem expirou some da tela; o servidor já o descartou.
            if (
                aviso.Tipo == TipoDeAvisoDoGrupo.ConviteExpirou
                && !string.IsNullOrEmpty(aviso.Nome)
            )
            {
                _convites.RemoveAll(c =>
                    string.Equals(
                        c.NomeDoConvidante,
                        aviso.Nome,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            }

            var euMesmo = PersonagemLocalId != 0 && aviso.PersonagemId == PersonagemLocalId;
            var texto = TextosGrupo.Aviso(aviso, euMesmo);
            if (texto != null)
            {
                Informar(texto, false);
            }
            else
            {
                Mudou?.Invoke();
            }
        }

        private void AoReceberRecusa(OperacaoDoGrupoRecusada recusa)
        {
            Informar(TextosGrupo.Recusa(recusa?.Codigo), true);
        }
    }

    /// <summary>Um convite para entrar em um grupo, esperando resposta.</summary>
    internal sealed class ConviteDoGrupo
    {
        public long ConviteId;
        public string NomeDoConvidante = "";

        /// <summary>Segundos que faltam para vencer, só para mostrar. Quem vence é o servidor.</summary>
        public float Restante;
    }
}
