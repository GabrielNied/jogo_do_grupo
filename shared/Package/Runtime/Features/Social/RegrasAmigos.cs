using System;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Números e decisões puras da lista de amigos e de ignorados. Regras em
    /// docs/features/social.md; o servidor é quem decide, o client usa só para mostrar (vagas) e
    /// para não enviar pedido que seria recusado.
    /// </summary>
    public static class RegrasAmigos
    {
        // ── Balanceamento ─────────────────────────────────────────────────────────

        /// <summary>Amigos que uma conta pode ter.</summary>
        public const int MaximoDeAmigos = 100;

        /// <summary>Pedidos sem resposta que uma conta pode ter recebido.</summary>
        public const int MaximoDePedidosRecebidos = 50;

        /// <summary>Jogadores que uma conta pode ignorar.</summary>
        public const int MaximoDeIgnorados = 50;

        /// <summary>Segundos mínimos entre dois pedidos de amizade da mesma conta.</summary>
        public const double IntervaloEntrePedidosSegundos = 1.0;

        /// <summary>Resposta de <c>RecusaDe...</c> quando a operação pode seguir.</summary>
        public const string SemRecusa = "";

        // ── Vagas ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Vagas que ainda dá para prometer: o máximo menos os amigos e os pedidos enviados sem
        /// resposta (cada pedido guarda a vaga de quem pediu). Nunca é negativo.
        /// </summary>
        public static int VagasLivres(int amigos, int pedidosEnviados)
        {
            long ocupadas = (long)Math.Max(0, amigos) + Math.Max(0, pedidosEnviados);
            return (int)Math.Max(0L, MaximoDeAmigos - ocupadas);
        }

        /// <summary>Se cabe mais um amigo ou pedido (senão é <c>ListaDeAmigosCheia</c>).</summary>
        public static bool TemVaga(int amigos, int pedidosEnviados) =>
            VagasLivres(amigos, pedidosEnviados) > 0;

        /// <summary>Se o alvo já tem pedidos sem resposta demais para receber outro.</summary>
        public static bool AlvoComPedidosDemais(int pedidosRecebidosDoAlvo) =>
            pedidosRecebidosDoAlvo >= MaximoDePedidosRecebidos;

        /// <summary>Se cabe mais um nome na lista de ignorados.</summary>
        public static bool CabeMaisUmIgnorado(int ignorados) => ignorados < MaximoDeIgnorados;

        /// <summary>Se a conta está pedindo amizade rápido demais.</summary>
        public static bool PedidoRapidoDemais(double segundosDesdeOUltimoPedido) =>
            segundosDesdeOUltimoPedido < IntervaloEntrePedidosSegundos;

        // ── Decisões ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Pedir amizade a quem já tinha pedido a você vale como aceitar o pedido dele.
        /// </summary>
        public static bool PedidoViraAceite(RelacaoDeAmizade relacao) =>
            relacao == RelacaoDeAmizade.PedidoRecebido;

        /// <summary>
        /// O código de <see cref="CodigosErroSocial"/> que recusa um pedido de amizade, ou
        /// <see cref="SemRecusa"/> se ele pode seguir. A ordem das conferências é a da tabela de
        /// recusas de docs/features/social.md. Quem é ignorado pelo alvo recebe
        /// <c>AlvoNaoEncontrado</c>, a mesma resposta de um nome que não existe, para não saber
        /// que foi ignorado.
        /// </summary>
        /// <param name="paraSiMesmo">O alvo é da mesma conta de quem pede.</param>
        /// <param name="relacao">O que já existe entre os dois.</param>
        /// <param name="alvoIgnoradoPorQuemPede">Quem pede ignora o alvo.</param>
        /// <param name="quemPedeIgnoradoPeloAlvo">O alvo ignora quem pede.</param>
        /// <param name="amigos">Amigos de quem pede.</param>
        /// <param name="pedidosEnviados">Pedidos sem resposta de quem pede.</param>
        /// <param name="pedidosRecebidosDoAlvo">Pedidos sem resposta que o alvo já recebeu.</param>
        /// <param name="segundosDesdeOUltimoPedido">Tempo desde o último pedido dele.</param>
        public static string RecusaDoPedido(
            bool paraSiMesmo,
            RelacaoDeAmizade relacao,
            bool alvoIgnoradoPorQuemPede,
            bool quemPedeIgnoradoPeloAlvo,
            int amigos,
            int pedidosEnviados,
            int pedidosRecebidosDoAlvo,
            double segundosDesdeOUltimoPedido
        )
        {
            if (paraSiMesmo)
            {
                return CodigosErroSocial.AmizadeConsigoMesmo;
            }
            if (alvoIgnoradoPorQuemPede)
            {
                return CodigosErroSocial.AlvoIgnorado;
            }
            if (relacao == RelacaoDeAmizade.Amigos)
            {
                return CodigosErroSocial.JaSaoAmigos;
            }
            if (relacao == RelacaoDeAmizade.PedidoEnviado)
            {
                return CodigosErroSocial.PedidoJaPendente;
            }
            if (PedidoRapidoDemais(segundosDesdeOUltimoPedido))
            {
                return CodigosErroSocial.PedidosRapidosDemais;
            }
            if (!TemVaga(amigos, pedidosEnviados))
            {
                return CodigosErroSocial.ListaDeAmigosCheia;
            }
            if (PedidoViraAceite(relacao))
            {
                return SemRecusa;
            }
            if (quemPedeIgnoradoPeloAlvo)
            {
                return CodigosErroSocial.AlvoNaoEncontrado;
            }
            if (AlvoComPedidosDemais(pedidosRecebidosDoAlvo))
            {
                return CodigosErroSocial.AlvoComPedidosDemais;
            }
            return SemRecusa;
        }

        /// <summary>
        /// O código que recusa aceitar um pedido recebido, ou <see cref="SemRecusa"/>. Quem pediu
        /// já tinha a vaga guardada; só a lista de quem aceita (amigos e pedidos enviados dele) pode
        /// estar cheia.
        /// </summary>
        public static string RecusaDoAceite(int amigos, int pedidosEnviados)
        {
            if (!TemVaga(amigos, pedidosEnviados))
            {
                return CodigosErroSocial.ListaDeAmigosCheia;
            }
            return SemRecusa;
        }

        /// <summary>
        /// O código que recusa ignorar um jogador, ou <see cref="SemRecusa"/>.
        /// </summary>
        public static string RecusaDeIgnorar(bool paraSiMesmo, bool jaIgnorado, int ignorados)
        {
            if (paraSiMesmo)
            {
                return CodigosErroSocial.IgnorarASiMesmo;
            }
            if (jaIgnorado)
            {
                return CodigosErroSocial.JaIgnorado;
            }
            if (!CabeMaisUmIgnorado(ignorados))
            {
                return CodigosErroSocial.ListaDeIgnoradosCheia;
            }
            return SemRecusa;
        }

        /// <summary>
        /// Se um sussurro, convite de grupo ou pedido de amizade de um jogador para outro deve ser
        /// barrado por estar ignorado. Vale nos dois sentidos: quem ignora também não fala com o
        /// ignorado sem antes tirá-lo da lista.
        /// </summary>
        public static bool ContatoBarrado(bool origemIgnoraOAlvo, bool alvoIgnoraAOrigem) =>
            origemIgnoraOAlvo || alvoIgnoraAOrigem;
    }
}
