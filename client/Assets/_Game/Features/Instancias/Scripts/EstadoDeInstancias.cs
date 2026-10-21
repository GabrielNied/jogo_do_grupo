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
        private readonly IConexaoDeInstancias _conexao;

        public EstadoDeInstancias(IConexaoDeInstancias conexao)
        {
            _conexao = conexao;
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
                        NivelAtual = m.Nivel;
                        TempoLimiteSegundos = m.TempoLimiteSegundos;
                        InicioLocal = UnityEngine.Time.unscaledTime;
                        VitoriaAlcancada = false;
                        EhRaid = m.Raid;
                        EtapaAtual = m.EtapaAtual;
                        TotalEtapas = m.TotalEtapas;
                        DungeonIniciada?.Invoke(m);
                    }
                ),
                conexao.Assinar<EntradaRecusada>(
                    InstanciasMessageIds.EntradaRecusada,
                    m => EntradaRecusada?.Invoke(m)
                ),
                conexao.Assinar<EtapaRaidConcluida>(
                    InstanciasMessageIds.EtapaRaidConcluida,
                    m =>
                    {
                        EtapaAtual = m.ProximaEtapa;
                        TotalEtapas = m.TotalEtapas;
                        EtapaConcluida?.Invoke(m);
                    }
                ),
                conexao.Assinar<ChefeDerrotadoNaDungeon>(
                    InstanciasMessageIds.ChefeDerrotadoNaDungeon,
                    m =>
                    {
                        VitoriaAlcancada = true;
                        NivelAtual = m.Nivel;
                        TempoTotalSegundos = m.TempoTotalSegundos;
                        NoTempo = m.NoTempo;
                        ChefeDerrotado?.Invoke(m);
                    }
                ),
                conexao.Assinar<DungeonEncerrada>(
                    InstanciasMessageIds.DungeonEncerrada,
                    m =>
                    {
                        NivelAtual = m.Nivel;
                        TempoTotalSegundos = m.TempoTotalSegundos;
                        NoTempo = m.NoTempo;
                        ProximoNivelLiberado = m.ProximoNivelLiberado;
                        DungeonEncerrada?.Invoke(m);
                        AreaAtual = string.Empty;
                        IdInstancia = string.Empty;
                        VitoriaAlcancada = false;
                        EhRaid = false;
                        EtapaAtual = 1;
                        TotalEtapas = 1;
                    }
                ),
            };
        }

        public string AreaAtual { get; private set; } = string.Empty;

        public string IdInstancia { get; private set; } = string.Empty;

        public bool VitoriaAlcancada { get; private set; }

        public bool EhRaid { get; private set; }

        public int EtapaAtual { get; private set; } = 1;

        public int TotalEtapas { get; private set; } = 1;

        public int NivelAtual { get; private set; } = 1;

        public int TempoLimiteSegundos { get; private set; } =
            RegrasDificuldadeSemFim.TempoLimitePadraoSegundos;

        public int TempoTotalSegundos { get; private set; }

        public bool NoTempo { get; private set; }

        public int ProximoNivelLiberado { get; private set; } = 1;

        public float InicioLocal { get; private set; }

        public float SegundosRestantesNoCronometro =>
            Math.Max(0f, TempoLimiteSegundos - (UnityEngine.Time.unscaledTime - InicioLocal));

        public bool NaInstancia =>
            !string.IsNullOrEmpty(AreaAtual)
            && AreaAtual.StartsWith("instancia:", StringComparison.Ordinal);

        public event Action<DungeonIniciada> DungeonIniciada;

        public event Action<EntradaRecusada> EntradaRecusada;

        public event Action<EtapaRaidConcluida> EtapaConcluida;

        public event Action<ChefeDerrotadoNaDungeon> ChefeDerrotado;

        public event Action<DungeonEncerrada> DungeonEncerrada;

        public bool PedirEntrar(int nivel = 1) =>
            _conexao.Enviar(
                InstanciasMessageIds.EntrarNaDungeon,
                new EntrarNaDungeon { Nivel = Math.Max(1, nivel) },
                EntregaTempoReal.Confiavel
            );

        public bool PedirSair() =>
            _conexao.Enviar(
                InstanciasMessageIds.SairDaDungeon,
                new SairDaDungeon(),
                EntregaTempoReal.Confiavel
            );

        private void AoMudarEstado(EstadoTempoReal estado)
        {
            if (estado == EstadoTempoReal.Reconectando || estado == EstadoTempoReal.Desconectado)
            {
                AreaAtual = string.Empty;
                IdInstancia = string.Empty;
                VitoriaAlcancada = false;
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
