using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Features.Economia.UI;
using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Economia
{
    /// <summary>
    /// Gerencia o estado de economia no client: saldo de coroas do personagem,
    /// eventos de compra/venda/reparo e sincronização com o servidor via tempo real.
    /// </summary>
    internal sealed class ControleDeEconomia : MonoBehaviour
    {
        private static ControleDeEconomia _instancia;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public static long SaldoCoroas { get; private set; }

        public static event Action SaldoMudou;
        public static event Action<ItemCompradoDaLoja> ItemComprado;
        public static event Action<ItemVendidoParaNpc> ItemVendido;
        public static event Action<ItemReparado> ItemReparado;
        public static event Action<string> OperacaoRecusada;

        /// <summary>O último estado do mercado (#176) que o servidor mandou; nulo antes do primeiro.</summary>
        public static EstadoDoMercado Mercado { get; private set; }

        public static event Action MercadoMudou;

        /// <summary>O último estado do correio (#212) enviado pelo servidor.</summary>
        public static EstadoDoCorreio Correio { get; private set; }

        /// <summary>Detalhes da carta atualmente aberta e inspecionada.</summary>
        public static DetalhesDaCarta CartaAberta { get; private set; }

        public static event Action CorreioMudou;
        public static event Action CartaAbertaMudou;
        public static event Action<CartaEnviada> CartaEnviada;
        public static event Action<AnexosColetados> AnexosColetados;
        public static event Action<CartaExcluida> CartaExcluida;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
            SaldoCoroas = 0;
            SaldoMudou = null;
            ItemComprado = null;
            ItemVendido = null;
            ItemReparado = null;
            OperacaoRecusada = null;
            Mercado = null;
            MercadoMudou = null;
            Correio = null;
            CartaAberta = null;
            CorreioMudou = null;
            CartaAbertaMudou = null;
            CartaEnviada = null;
            AnexosColetados = null;
            CartaExcluida = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
                return;

            var objeto = new GameObject("Economia.Controle");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ControleDeEconomia>();
        }

        // O registro compartilhado aceita apenas letras; F8 pertence ao controle de Economia.
        private void Update()
        {
            if (
                !Application.isFocused
                || !EntradaNoJogo.NoJogo
                || EntradaNoJogo.EntradaPendente
                || JanelasDoJogo.MenuAberto
            )
                return;

            if (Keyboard.current?.f8Key.wasPressedThisFrame != true)
                return;

            if (JanelasDoJogo.EstaAberta(JanelaBarraca.Chave))
                JanelasDoJogo.Fechar(JanelaBarraca.Chave);
            else
                JanelasDoJogo.Abrir(JanelaBarraca.Chave);
        }

        private void Awake()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<SaldoCoroasAlterado>(
                    EconomiaMessageIds.SaldoCoroasAlterado,
                    AoSaldoAlterado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ItemCompradoDaLoja>(
                    EconomiaMessageIds.ItemCompradoDaLoja,
                    AoItemComprado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ItemVendidoParaNpc>(
                    EconomiaMessageIds.ItemVendidoParaNpc,
                    AoItemVendido
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ItemReparado>(
                    EconomiaMessageIds.ItemReparado,
                    AoItemReparado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDoMercado>(
                    EconomiaMessageIds.EstadoDoMercado,
                    AoEstadoDoMercado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AvisoDoMercado>(
                    EconomiaMessageIds.AvisoDoMercado,
                    AoAvisoDoMercado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EconomiaRecusada>(
                    EconomiaMessageIds.EconomiaRecusada,
                    AoRecusado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDoCorreio>(
                    EconomiaMessageIds.EstadoDoCorreio,
                    AoEstadoDoCorreio
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<DetalhesDaCarta>(
                    EconomiaMessageIds.DetalhesDaCarta,
                    AoDetalhesDaCarta
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<CartaEnviada>(
                    EconomiaMessageIds.CartaEnviada,
                    AoCartaEnviada
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AnexosColetados>(
                    EconomiaMessageIds.AnexosColetados,
                    AoAnexosColetados
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<CartaExcluida>(
                    EconomiaMessageIds.CartaExcluida,
                    AoCartaExcluida
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AvisoNovoCorreio>(
                    EconomiaMessageIds.AvisoNovoCorreio,
                    AoAvisoNovoCorreio
                )
            );
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
                assinatura?.Dispose();
            _assinaturas.Clear();
        }

        public static void ConsultarSaldo()
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ConsultarSaldoCoroas,
                new ConsultarSaldoCoroas()
            );
        }

        public static void ConsultarCatalogo(
            CategoriaLoja categoria = CategoriaLoja.Nenhuma,
            int npcId = 0
        )
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ConsultarCatalogoLoja,
                new ConsultarCatalogoLoja(categoria, npcId)
            );
        }

        public static void Comprar(
            int itemId,
            int quantidade = 1,
            CategoriaLoja categoria = CategoriaLoja.Nenhuma,
            int npcId = 0
        )
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ComprarItemDaLoja,
                new ComprarItemDaLoja(itemId, quantidade, categoria, npcId)
            );
        }

        public static void Vender(long instanciaId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.VenderItemParaNpc,
                new VenderItemParaNpc(instanciaId)
            );
        }

        public static void Reparar(long instanciaId)
        {
            ConexaoTempoReal.Enviar(EconomiaMessageIds.RepararItem, new RepararItem(instanciaId));
        }

        public static void ConsultarMercado(int itemId = 0, string filtro = "")
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ConsultarMercado,
                new ConsultarMercado { ItemId = itemId, Filtro = filtro ?? "" }
            );
        }

        public static void OferecerVenda(long instanciaId, long precoCoroas)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.OferecerVenda,
                new OferecerVenda { InstanciaId = instanciaId, PrecoCoroas = precoCoroas }
            );
        }

        public static void OferecerCompra(int itemId, int quantidade, long precoUnitario)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.OferecerCompra,
                new OferecerCompra
                {
                    ItemId = itemId,
                    Quantidade = quantidade,
                    PrecoUnitario = precoUnitario,
                }
            );
        }

        public static void ComprarOrdem(long ordemId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ComprarOrdem,
                new ComprarOrdem { OrdemId = ordemId }
            );
        }

        public static void CancelarOrdem(long ordemId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.CancelarOrdem,
                new CancelarOrdem { OrdemId = ordemId }
            );
        }

        public static void ReceberDevolucoes()
        {
            ConexaoTempoReal.Enviar(EconomiaMessageIds.ReceberDevolucoes, new ReceberDevolucoes());
        }

        private void AoEstadoDoMercado(EstadoDoMercado msg)
        {
            Mercado = msg;
            SaldoCoroas = msg.Saldo;
            SaldoMudou?.Invoke();
            MercadoMudou?.Invoke();
        }

        private void AoAvisoDoMercado(AvisoDoMercado msg)
        {
            AvisosDoJogo.Mostrar(UI.TextosMercado.Aviso(msg), TipoDeAviso.Informacao);
        }

        private void AoSaldoAlterado(SaldoCoroasAlterado msg)
        {
            SaldoCoroas = msg.Saldo;
            SaldoMudou?.Invoke();
        }

        private void AoItemComprado(ItemCompradoDaLoja msg)
        {
            SaldoCoroas = msg.SaldoAtualizado;
            SaldoMudou?.Invoke();
            ItemComprado?.Invoke(msg);
        }

        private void AoItemVendido(ItemVendidoParaNpc msg)
        {
            SaldoCoroas = msg.SaldoAtualizado;
            SaldoMudou?.Invoke();
            ItemVendido?.Invoke(msg);
        }

        private void AoItemReparado(ItemReparado msg)
        {
            SaldoCoroas = msg.SaldoAtualizado;
            SaldoMudou?.Invoke();
            ItemReparado?.Invoke(msg);
        }

        private void AoRecusado(EconomiaRecusada msg)
        {
            OperacaoRecusada?.Invoke(msg.Motivo);
        }

        public static void ConsultarCorreio()
        {
            ConexaoTempoReal.Enviar(EconomiaMessageIds.ConsultarCorreio, new ConsultarCorreio());
        }

        public static void LerCarta(long cartaId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.LerCarta,
                new LerCarta { CartaId = cartaId }
            );
        }

        public static void EnviarCarta(
            string destinatario,
            string assunto,
            string mensagem,
            long coroas,
            long[] itensInstancias
        )
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.EnviarCarta,
                new EnviarCarta
                {
                    Destinatario = destinatario,
                    Assunto = assunto,
                    Mensagem = mensagem,
                    Coroas = coroas,
                    InstanciasItens = itensInstancias ?? Array.Empty<long>(),
                }
            );
        }

        public static void ColetarAnexos(long cartaId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ColetarAnexos,
                new ColetarAnexos { CartaId = cartaId }
            );
        }

        public static void ExcluirCarta(long cartaId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ExcluirCarta,
                new ExcluirCarta { CartaId = cartaId }
            );
        }

        private void AoEstadoDoCorreio(EstadoDoCorreio msg)
        {
            Correio = msg;
            CorreioMudou?.Invoke();
        }

        private void AoDetalhesDaCarta(DetalhesDaCarta msg)
        {
            CartaAberta = msg;
            CartaAbertaMudou?.Invoke();
        }

        private void AoCartaEnviada(CartaEnviada msg)
        {
            CartaEnviada?.Invoke(msg);
            ConsultarSaldo();
            ConsultarCorreio();
            AvisosDoJogo.Mostrar("Carta enviada com sucesso!", TipoDeAviso.Informacao);
        }

        private void AoAnexosColetados(AnexosColetados msg)
        {
            AnexosColetados?.Invoke(msg);
            ConsultarSaldo();
            ConsultarCorreio();
            if (CartaAberta != null && CartaAberta.Id == msg.CartaId)
            {
                CartaAberta.AnexosColetados = true;
                CartaAberta.Coroas = 0;
                CartaAberta.Itens = Array.Empty<AnexoItemCorreio>();
                CartaAbertaMudou?.Invoke();
            }
            AvisosDoJogo.Mostrar("Anexos recolhidos para a sua bolsa!", TipoDeAviso.Informacao);
        }

        private void AoCartaExcluida(CartaExcluida msg)
        {
            CartaExcluida?.Invoke(msg);
            if (CartaAberta != null && CartaAberta.Id == msg.CartaId)
            {
                CartaAberta = null;
                CartaAbertaMudou?.Invoke();
            }
            ConsultarCorreio();
        }

        private void AoAvisoNovoCorreio(AvisoNovoCorreio msg)
        {
            AvisosDoJogo.Mostrar(
                $"Novo correio de {msg.Remetente}: {msg.Assunto}",
                TipoDeAviso.Informacao
            );
            ConsultarCorreio();
        }
    }
}
