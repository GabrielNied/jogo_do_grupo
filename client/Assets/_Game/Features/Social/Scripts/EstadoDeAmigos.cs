using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Listas de amigos, pedidos e ignorados conforme o servidor manda. A lista inteira substitui
    /// a anterior; <see cref="AmigoMudouDeEstado"/> troca só a linha daquele amigo.
    /// </summary>
    internal sealed class EstadoDeAmigos
    {
        private static EstadoDeAmigos _estadoDoClient;

        private readonly List<AmigoDaLista> _amigos = new List<AmigoDaLista>();
        private readonly List<PedidoDeAmizade> _recebidos = new List<PedidoDeAmizade>();
        private readonly List<PedidoDeAmizade> _enviados = new List<PedidoDeAmizade>();
        private readonly List<JogadorIgnorado> _ignorados = new List<JogadorIgnorado>();
        private readonly IConexaoDoGrupo _conexao;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public EstadoDeAmigos() { }

        public EstadoDeAmigos(IConexaoDoGrupo conexao)
        {
            _conexao = conexao;
            if (_conexao == null)
            {
                return;
            }

            _assinaturas.Add(
                _conexao.Assinar<ListaDeAmigos>(SocialMessageIds.ListaDeAmigos, Aplicar)
            );
            _assinaturas.Add(
                _conexao.Assinar<AmigoMudouDeEstado>(SocialMessageIds.AmigoMudouDeEstado, Aplicar)
            );
            _assinaturas.Add(
                _conexao.Assinar<JogoDoGrupo.Shared.Features.Social.ListaDeIgnorados>(
                    SocialMessageIds.ListaDeIgnorados,
                    Aplicar
                )
            );
            _assinaturas.Add(
                _conexao.Assinar<OperacaoDeAmizadeRecusada>(
                    SocialMessageIds.OperacaoDeAmizadeRecusada,
                    Aplicar
                )
            );
        }

        public static EstadoDeAmigos DoClient
        {
            get
            {
                if (_estadoDoClient == null)
                {
                    _estadoDoClient = new EstadoDeAmigos(new ConexaoDoGrupoReal());
                }

                return _estadoDoClient;
            }
        }

        public IReadOnlyList<AmigoDaLista> Amigos => _amigos;
        public IReadOnlyList<PedidoDeAmizade> PedidosRecebidos => _recebidos;
        public IReadOnlyList<PedidoDeAmizade> PedidosEnviados => _enviados;
        public IReadOnlyList<JogadorIgnorado> Ignorados => _ignorados;

        public event Action Mudou;
        public event Action<string> OperacaoRecusada;

        public bool PedirAmizade(string nome) =>
            Enviar(SocialMessageIds.PedirAmizade, new PedirAmizade { Nome = nome ?? "" });

        public bool ResponderPedido(long pedidoId, bool aceitar) =>
            Enviar(
                SocialMessageIds.ResponderPedidoDeAmizade,
                new ResponderPedidoDeAmizade { PedidoId = pedidoId, Aceitar = aceitar }
            );

        public bool CancelarPedido(long pedidoId) =>
            Enviar(
                SocialMessageIds.CancelarPedidoDeAmizade,
                new CancelarPedidoDeAmizade { PedidoId = pedidoId }
            );

        public bool RemoverAmigo(long amigoId) =>
            Enviar(SocialMessageIds.RemoverAmigo, new RemoverAmigo { AmigoId = amigoId });

        public bool Ignorar(string nome) =>
            Enviar(SocialMessageIds.IgnorarJogador, new IgnorarJogador { Nome = nome ?? "" });

        public bool DeixarDeIgnorar(long ignoradoId) =>
            Enviar(
                SocialMessageIds.DeixarDeIgnorar,
                new DeixarDeIgnorar { IgnoradoId = ignoradoId }
            );

        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad
        )]
        private static void Iniciar()
        {
            _ = DoClient;
        }

        public void Aplicar(ListaDeAmigos lista)
        {
            _amigos.Clear();
            _recebidos.Clear();
            _enviados.Clear();
            Copiar(lista?.Amigos, _amigos, CopiarAmigo);
            Copiar(lista?.PedidosRecebidos, _recebidos, CopiarPedido);
            Copiar(lista?.PedidosEnviados, _enviados, CopiarPedido);
            Mudou?.Invoke();
        }

        public void Aplicar(AmigoMudouDeEstado mudanca)
        {
            if (mudanca == null)
            {
                return;
            }

            for (var i = 0; i < _amigos.Count; i++)
            {
                if (_amigos[i].AmigoId != mudanca.AmigoId)
                {
                    continue;
                }

                _amigos[i].Online = mudanca.Online;
                _amigos[i].PersonagemOnline = mudanca.PersonagemOnline ?? "";
                _amigos[i].Area = mudanca.Area ?? "";
                Mudou?.Invoke();
                return;
            }
        }

        public void Aplicar(JogoDoGrupo.Shared.Features.Social.ListaDeIgnorados lista)
        {
            _ignorados.Clear();
            Copiar(lista?.Ignorados, _ignorados, CopiarIgnorado);
            Mudou?.Invoke();
        }

        public void Aplicar(OperacaoDeAmizadeRecusada recusa)
        {
            if (recusa != null)
            {
                OperacaoRecusada?.Invoke(recusa.Codigo ?? "");
            }
        }

        /// <summary>O nome está na lista do servidor, sem diferenciar maiúsculas.</summary>
        public bool Contem(string nome)
        {
            return TentarIgnorado(nome, out _);
        }

        public bool TentarIgnorado(string nome, out JogadorIgnorado ignorado)
        {
            var alvo = Normalizar(nome);
            if (alvo.Length == 0)
            {
                ignorado = null;
                return false;
            }

            for (var i = 0; i < _ignorados.Count; i++)
            {
                if (Normalizar(_ignorados[i].Nome) == alvo)
                {
                    ignorado = _ignorados[i];
                    return true;
                }
            }

            ignorado = null;
            return false;
        }

        public void Dispose()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
        }

        private bool Enviar<T>(ushort id, T mensagem) =>
            _conexao != null && _conexao.Enviar(id, mensagem, EntregaTempoReal.Confiavel);

        private static void Copiar<T>(T[] origem, List<T> destino, Func<T, T> copiar)
            where T : class
        {
            if (origem == null)
            {
                return;
            }

            for (var i = 0; i < origem.Length; i++)
            {
                if (origem[i] != null)
                {
                    destino.Add(copiar(origem[i]));
                }
            }
        }

        private static AmigoDaLista CopiarAmigo(AmigoDaLista amigo) =>
            new AmigoDaLista
            {
                AmigoId = amigo.AmigoId,
                Nome = amigo.Nome ?? "",
                Online = amigo.Online,
                PersonagemOnline = amigo.PersonagemOnline ?? "",
                Area = amigo.Area ?? "",
            };

        private static PedidoDeAmizade CopiarPedido(PedidoDeAmizade pedido) =>
            new PedidoDeAmizade { PedidoId = pedido.PedidoId, Nome = pedido.Nome ?? "" };

        private static JogadorIgnorado CopiarIgnorado(JogadorIgnorado ignorado) =>
            new JogadorIgnorado { IgnoradoId = ignorado.IgnoradoId, Nome = ignorado.Nome ?? "" };

        private static string Normalizar(string nome) => (nome ?? "").Trim().ToLowerInvariant();

        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration
        )]
        private static void Reiniciar()
        {
            _estadoDoClient?.Dispose();
            _estadoDoClient = null;
        }
    }
}
