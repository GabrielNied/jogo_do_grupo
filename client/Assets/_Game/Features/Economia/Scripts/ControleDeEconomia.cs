using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;

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
                ConexaoTempoReal.Assinar<EconomiaRecusada>(
                    EconomiaMessageIds.EconomiaRecusada,
                    AoRecusado
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

        public static void Comprar(int itemId, int quantidade = 1)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ComprarItemDaLoja,
                new ComprarItemDaLoja(itemId, quantidade)
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
