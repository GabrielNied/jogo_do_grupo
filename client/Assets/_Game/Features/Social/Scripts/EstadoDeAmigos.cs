using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Listas de amigos, pedidos e ignorados como o servidor mandou. A lista inteira substitui
    /// a anterior; <see cref="AmigoMudouDeEstado"/> troca só a linha daquele amigo.
    /// </summary>
    internal sealed class EstadoDeAmigos
    {
        private readonly List<AmigoDaLista> _amigos = new List<AmigoDaLista>();
        private readonly List<PedidoDeAmizade> _recebidos = new List<PedidoDeAmizade>();
        private readonly List<PedidoDeAmizade> _enviados = new List<PedidoDeAmizade>();
        private readonly List<JogadorIgnorado> _ignorados = new List<JogadorIgnorado>();

        public IReadOnlyList<AmigoDaLista> Amigos => _amigos;
        public IReadOnlyList<PedidoDeAmizade> PedidosRecebidos => _recebidos;
        public IReadOnlyList<PedidoDeAmizade> PedidosEnviados => _enviados;
        public IReadOnlyList<JogadorIgnorado> Ignorados => _ignorados;

        public event Action Mudou;

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

        private static void Copiar<T>(T[] origem, List<T> destino, Func<T, T> copiar)
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
            };

        private static PedidoDeAmizade CopiarPedido(PedidoDeAmizade pedido) =>
            new PedidoDeAmizade { PedidoId = pedido.PedidoId, Nome = pedido.Nome ?? "" };

        private static JogadorIgnorado CopiarIgnorado(JogadorIgnorado ignorado) =>
            new JogadorIgnorado { IgnoradoId = ignorado.IgnoradoId, Nome = ignorado.Nome ?? "" };

        private static string Normalizar(string nome) => (nome ?? "").Trim().ToLowerInvariant();
    }
}
