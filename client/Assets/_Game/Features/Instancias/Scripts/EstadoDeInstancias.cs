using System;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Instancias;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>Ouve o servidor sobre dungeons e avisa a UI e o construtor dos salões.</summary>
    internal sealed class EstadoDeInstancias : IDisposable
    {
        private readonly IDisposable[] _assinaturas;
        private readonly Action _desligar;

        public EstadoDeInstancias(IConexaoDeInstancias conexao)
        {
            // Sem conexão a corrida some do ponto de vista do client: o servidor leva quem
            // reconecta de volta à cidade.
            conexao.EstadoMudou += AoMudarEstado;
            _desligar = () => conexao.EstadoMudou -= AoMudarEstado;
            _assinaturas = new IDisposable[]
            {
                conexao.Assinar<DungeonIniciada>(
                    InstanciasMessageIds.DungeonIniciada,
                    m =>
                    {
                        AreaAtual = m.Area;
                        IdInstancia = m.IdInstancia;
                        DungeonIniciada?.Invoke(m);
                    }
                ),
                conexao.Assinar<EntradaRecusada>(
                    InstanciasMessageIds.EntradaRecusada,
                    m => EntradaRecusada?.Invoke(m)
                ),
                conexao.Assinar<DungeonEncerrada>(
                    InstanciasMessageIds.DungeonEncerrada,
                    m =>
                    {
                        DungeonEncerrada?.Invoke(m);
                        AreaAtual = string.Empty;
                        IdInstancia = string.Empty;
                    }
                ),
            };
        }

        public string AreaAtual { get; private set; } = string.Empty;

        public string IdInstancia { get; private set; } = string.Empty;

        public bool NaInstancia =>
            !string.IsNullOrEmpty(AreaAtual)
            && AreaAtual.StartsWith("instancia:", StringComparison.Ordinal);

        public event Action<DungeonIniciada> DungeonIniciada;

        public event Action<EntradaRecusada> EntradaRecusada;

        public event Action<DungeonEncerrada> DungeonEncerrada;

        private void AoMudarEstado(EstadoTempoReal estado)
        {
            if (estado == EstadoTempoReal.Reconectando || estado == EstadoTempoReal.Desconectado)
            {
                AreaAtual = string.Empty;
                IdInstancia = string.Empty;
            }
        }

        public void Dispose()
        {
            _desligar();
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }
        }
    }

    internal interface IConexaoDeInstancias
    {
        event Action<EstadoTempoReal> EstadoMudou;

        bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega);

        IDisposable Assinar<T>(ushort id, Action<T> aoReceber);
    }

    internal sealed class ConexaoDeInstanciasReal : IConexaoDeInstancias
    {
        public event Action<EstadoTempoReal> EstadoMudou
        {
            add => ConexaoTempoReal.EstadoMudou += value;
            remove => ConexaoTempoReal.EstadoMudou -= value;
        }

        public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega) =>
            ConexaoTempoReal.Enviar(id, mensagem, entrega);

        public IDisposable Assinar<T>(ushort id, Action<T> aoReceber) =>
            ConexaoTempoReal.Assinar(id, aoReceber);
    }
}
