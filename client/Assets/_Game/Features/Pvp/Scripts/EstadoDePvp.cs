using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Pvp;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>
    /// Mantém o estado de PvP no client: duelos 1v1, fila e partidas de arena 3v3 e bandeira no mundo aberto.
    /// Registra handlers de mensagens de rede do servidor via ConexaoTempoReal.
    /// </summary>
    public sealed class EstadoDePvp : IDisposable
    {
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public bool EmDuelo { get; private set; }
        public DueloIniciado DueloAtual { get; private set; }
        public ConviteDeDueloRecebido ConvitePendente { get; private set; }

        /// <summary>Segundos que faltam para o convite pendente expirar no servidor.</summary>
        public float SegundosRestantesConvite { get; private set; }

        /// <summary>O estado do jogador que está no mundo, ou null fora dele.</summary>
        public static EstadoDePvp Atual { get; private set; }

        public bool NaFilaArena { get; private set; }
        public int SegundosNaFilaArena { get; private set; }
        public bool EmArena { get; private set; }
        public PartidaDeArenaIniciada PartidaArenaAtual { get; private set; }

        public bool BandeiraPvpAtiva { get; private set; }
        public RespostaRankingArena RankingAtual { get; private set; }

        public event Action<ConviteDeDueloRecebido> AoReceberConviteDuelo;
        public event Action<DueloIniciado> AoIniciarDuelo;
        public event Action<DueloEncerrado> AoEncerrarDuelo;
        public event Action<string> AoRecusarDuelo;

        public event Action<bool, int> AoMudarStatusFilaArena;
        public event Action<PartidaDeArenaIniciada> AoIniciarPartidaArena;
        public event Action<CombatenteDeArenaEliminado> AoEliminarCombatenteArena;
        public event Action<PartidaDeArenaEncerrada> AoEncerrarPartidaArena;
        public event Action<RespostaRankingArena> AoReceberRankingArena;

        public event Action<long, bool> AoMudarBandeiraPvp;

        public EstadoDePvp()
        {
            RegistrarMensagens();
            Atual = this;
        }

        /// <summary>Passa o tempo: o convite pendente some quando o prazo acaba.</summary>
        public void Atualizar(float segundos)
        {
            if (ConvitePendente == null)
            {
                return;
            }

            SegundosRestantesConvite -= segundos;
            if (SegundosRestantesConvite <= 0f)
            {
                ConvitePendente = null;
                SegundosRestantesConvite = 0f;
            }
        }

        private void RegistrarMensagens()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ConviteDeDueloRecebido>(
                    PvpMessageIds.ConviteDeDueloRecebido,
                    AoReceberConviteDueloInterno
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<DueloIniciado>(
                    PvpMessageIds.DueloIniciado,
                    AoIniciarDueloInterno
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<DueloRecusado>(
                    PvpMessageIds.DueloRecusado,
                    AoRecusarDueloInterno
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<DueloEncerrado>(
                    PvpMessageIds.DueloEncerrado,
                    AoEncerrarDueloInterno
                )
            );

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<StatusFilaDeArena>(
                    PvpMessageIds.StatusFilaDeArena,
                    AoMudarStatusFilaArenaInterno
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<PartidaDeArenaIniciada>(
                    PvpMessageIds.PartidaDeArenaIniciada,
                    AoIniciarPartidaArenaInterno
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<CombatenteDeArenaEliminado>(
                    PvpMessageIds.CombatenteDeArenaEliminado,
                    AoEliminarCombatenteArenaInterno
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<PartidaDeArenaEncerrada>(
                    PvpMessageIds.PartidaDeArenaEncerrada,
                    AoEncerrarPartidaArenaInterno
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<RespostaRankingArena>(
                    PvpMessageIds.RespostaRankingArena,
                    AoReceberRankingArenaInterno
                )
            );

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<BandeiraPvpAlterada>(
                    PvpMessageIds.BandeiraPvpAlterada,
                    AoMudarBandeiraPvpInterno
                )
            );
        }

        public void Dispose()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura?.Dispose();
            }
            _assinaturas.Clear();
            if (Atual == this)
            {
                Atual = null;
            }

            EmDuelo = false;
            DueloAtual = null;
            ConvitePendente = null;
            SegundosRestantesConvite = 0f;
            NaFilaArena = false;
            EmArena = false;
            PartidaArenaAtual = null;
        }

        // ── Ações Client → Servidor ──────────────────────────────────────────

        public void SolicitarDuelo(long alvoPersonagemId)
        {
            ConexaoTempoReal.Enviar(
                PvpMessageIds.SolicitarDuelo,
                new SolicitarDuelo { AlvoPersonagemId = alvoPersonagemId }
            );
        }

        public void AceitarDuelo(long desafianteId)
        {
            ConvitePendente = null;
            ConexaoTempoReal.Enviar(
                PvpMessageIds.ResponderConviteDeDuelo,
                new ResponderConviteDeDuelo { DesafianteId = desafianteId, Aceito = true }
            );
        }

        public void RecusarDuelo(long desafianteId)
        {
            ConvitePendente = null;
            ConexaoTempoReal.Enviar(
                PvpMessageIds.ResponderConviteDeDuelo,
                new ResponderConviteDeDuelo { DesafianteId = desafianteId, Aceito = false }
            );
        }

        public void CancelarDuelo()
        {
            ConexaoTempoReal.Enviar(PvpMessageIds.CancelarDuelo, new CancelarDuelo());
        }

        public void EntrarNaFilaArena()
        {
            ConexaoTempoReal.Enviar(PvpMessageIds.EntrarNaFilaDeArena, new EntrarNaFilaDeArena());
        }

        public void SairDaFilaArena()
        {
            ConexaoTempoReal.Enviar(PvpMessageIds.SairDaFilaDeArena, new SairDaFilaDeArena());
        }

        public void ConsultarRankingArena()
        {
            ConexaoTempoReal.Enviar(
                PvpMessageIds.ConsultarRankingArena,
                new ConsultarRankingArena()
            );
        }

        public void AlternarBandeiraPvp(bool ativar)
        {
            ConexaoTempoReal.Enviar(
                PvpMessageIds.AlternarBandeiraPvp,
                new AlternarBandeiraPvp { Ativar = ativar }
            );
        }

        // ── Handlers Internos ────────────────────────────────────────────────

        private void AoReceberConviteDueloInterno(ConviteDeDueloRecebido msg)
        {
            ConvitePendente = msg;
            SegundosRestantesConvite = msg.SegundosParaExpirar;
            AoReceberConviteDuelo?.Invoke(msg);
        }

        private void AoIniciarDueloInterno(DueloIniciado msg)
        {
            EmDuelo = true;
            DueloAtual = msg;
            ConvitePendente = null;
            AoIniciarDuelo?.Invoke(msg);
        }

        private void AoRecusarDueloInterno(DueloRecusado msg)
        {
            AoRecusarDuelo?.Invoke(msg.Motivo);
        }

        private void AoEncerrarDueloInterno(DueloEncerrado msg)
        {
            EmDuelo = false;
            DueloAtual = null;
            AoEncerrarDuelo?.Invoke(msg);
        }

        private void AoMudarStatusFilaArenaInterno(StatusFilaDeArena msg)
        {
            NaFilaArena = msg.NaFila;
            SegundosNaFilaArena = msg.SegundosNaFila;
            AoMudarStatusFilaArena?.Invoke(msg.NaFila, msg.SegundosNaFila);
        }

        private void AoIniciarPartidaArenaInterno(PartidaDeArenaIniciada msg)
        {
            EmArena = true;
            NaFilaArena = false;
            PartidaArenaAtual = msg;
            AoIniciarPartidaArena?.Invoke(msg);
        }

        private void AoEliminarCombatenteArenaInterno(CombatenteDeArenaEliminado msg)
        {
            AoEliminarCombatenteArena?.Invoke(msg);
        }

        private void AoEncerrarPartidaArenaInterno(PartidaDeArenaEncerrada msg)
        {
            EmArena = false;
            PartidaArenaAtual = null;
            AoEncerrarPartidaArena?.Invoke(msg);
        }

        private void AoReceberRankingArenaInterno(RespostaRankingArena msg)
        {
            RankingAtual = msg;
            AoReceberRankingArena?.Invoke(msg);
        }

        private void AoMudarBandeiraPvpInterno(BandeiraPvpAlterada msg)
        {
            if (msg.PersonagemId == EntradaNoJogo.PersonagemId)
            {
                BandeiraPvpAtiva = msg.Ativa;
            }
            AoMudarBandeiraPvp?.Invoke(msg.PersonagemId, msg.Ativa);
        }
    }
}
