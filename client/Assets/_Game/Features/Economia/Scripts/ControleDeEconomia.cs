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

        /// <summary>O último estado do banco da guilda (#338) recebido do servidor.</summary>
        public static EstadoDoBancoGuilda BancoGuilda { get; private set; }

        public static event Action BancoGuildaMudou;

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
            BancoGuilda = null;
            BancoGuildaMudou = null;
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
                ConexaoTempoReal.Assinar<EstadoDoBancoGuilda>(
                    EconomiaMessageIds.EstadoDoBancoGuilda,
                    AoEstadoDoBancoGuilda
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AvisoBancoGuilda>(
                    EconomiaMessageIds.AvisoBancoGuilda,
                    AoAvisoBancoGuilda
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

        public static void ConsultarBancoGuilda()
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ConsultarBancoGuilda,
                new ConsultarBancoGuilda()
            );
        }

        public static void DepositarItemBancoGuilda(long instanciaId, int aba = 0, int casa = 0)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.DepositarItemBancoGuilda,
                new DepositarItemBancoGuilda
                {
                    InstanciaId = instanciaId,
                    Aba = aba,
                    Casa = casa,
                }
            );
        }

        public static void RetirarItemBancoGuilda(long itemBancoId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.RetirarItemBancoGuilda,
                new RetirarItemBancoGuilda { ItemBancoId = itemBancoId }
            );
        }

        public static void DepositarCoroasBancoGuilda(long quantidade)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.DepositarCoroasBancoGuilda,
                new DepositarCoroasBancoGuilda { Quantidade = quantidade }
            );
        }

        public static void RetirarCoroasBancoGuilda(long quantidade)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.RetirarCoroasBancoGuilda,
                new RetirarCoroasBancoGuilda { Quantidade = quantidade }
            );
        }

        private void AoEstadoDoBancoGuilda(EstadoDoBancoGuilda msg)
        {
            BancoGuilda = msg;
            BancoGuildaMudou?.Invoke();
        }

        private void AoAvisoBancoGuilda(AvisoBancoGuilda msg)
        {
            AvisosDoJogo.Mostrar(msg.Mensagem, TipoDeAviso.Informacao);
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
    }
}
