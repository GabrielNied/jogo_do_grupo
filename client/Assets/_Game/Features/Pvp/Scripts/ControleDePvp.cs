using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Pvp;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>
    /// Componente Unity que gerencia o fluxo em jogo de PvP:
    /// mostra avisos de duelo, arena e bandeira (a resposta ao convite é do
    /// <see cref="ConviteDeDueloNaTela"/>), atualiza notificações e exibe resultados.
    /// </summary>
    public sealed class ControleDePvp : MonoBehaviour
    {
        private EstadoDePvp _estado;

        public EstadoDePvp Estado => _estado;

        public void Configurar(EstadoDePvp estado)
        {
            _estado = estado ?? throw new ArgumentNullException(nameof(estado));

            _estado.AoReceberConviteDuelo += AoReceberConviteDuelo;
            _estado.AoIniciarDuelo += AoIniciarDuelo;
            _estado.AoEncerrarDuelo += AoEncerrarDuelo;
            _estado.AoRecusarDuelo += AoRecusarDuelo;

            _estado.AoMudarStatusFilaArena += AoMudarStatusFilaArena;
            _estado.AoIniciarPartidaArena += AoIniciarPartidaArena;
            _estado.AoEncerrarPartidaArena += AoEncerrarPartidaArena;

            _estado.AoMudarBandeiraPvp += AoMudarBandeiraPvp;
        }

        private void OnDestroy()
        {
            if (_estado != null)
            {
                _estado.AoReceberConviteDuelo -= AoReceberConviteDuelo;
                _estado.AoIniciarDuelo -= AoIniciarDuelo;
                _estado.AoEncerrarDuelo -= AoEncerrarDuelo;
                _estado.AoRecusarDuelo -= AoRecusarDuelo;

                _estado.AoMudarStatusFilaArena -= AoMudarStatusFilaArena;
                _estado.AoIniciarPartidaArena -= AoIniciarPartidaArena;
                _estado.AoEncerrarPartidaArena -= AoEncerrarPartidaArena;

                _estado.AoMudarBandeiraPvp -= AoMudarBandeiraPvp;
            }
        }

        private void AoReceberConviteDuelo(ConviteDeDueloRecebido msg)
        {
            AvisosDoJogo.Mostrar(
                TextosPvp.MensagemConviteDuelo(msg.NomeDesafiante),
                TipoDeAviso.Aviso
            );
        }

        private void AoIniciarDuelo(DueloIniciado msg)
        {
            AvisosDoJogo.Mostrar(
                TextosPvp.MensagemDueloIniciado(msg.ContagemRegressivaSegundos),
                TipoDeAviso.Aviso
            );
        }

        private void AoEncerrarDuelo(DueloEncerrado msg)
        {
            var meuId = EntradaNoJogo.PersonagemId;
            var vitoria = msg.VencedorId == meuId;
            AvisosDoJogo.Mostrar(
                TextosPvp.MensagemDueloEncerrado(vitoria, msg.Motivo),
                vitoria ? TipoDeAviso.Informacao : TipoDeAviso.Aviso
            );
        }

        private void AoRecusarDuelo(string motivo)
        {
            AvisosDoJogo.Mostrar(TextosPvp.TraduzirRecusa(motivo), TipoDeAviso.Erro);
        }

        private void AoMudarStatusFilaArena(bool naFila, int segundos)
        {
            AvisosDoJogo.Mostrar(
                TextosPvp.MensagemFilaArena(naFila, segundos),
                TipoDeAviso.Informacao
            );
        }

        private void AoIniciarPartidaArena(PartidaDeArenaIniciada msg)
        {
            AvisosDoJogo.Mostrar("⚔️ Partida de Arena 3v3 iniciada!", TipoDeAviso.Aviso);
        }

        private void AoEncerrarPartidaArena(PartidaDeArenaEncerrada msg)
        {
            var meuId = EntradaNoJogo.PersonagemId;
            bool vitoria;
            if (_estado.PartidaArenaAtual != null)
            {
                var minhaEquipe =
                    Array.IndexOf(_estado.PartidaArenaAtual.TimeAzul, meuId) >= 0 ? 0 : 1;
                vitoria = minhaEquipe == msg.EquipeVencedora;
            }
            else
            {
                vitoria = msg.PontosVariacao >= 0;
            }

            AvisosDoJogo.Mostrar(
                TextosPvp.MensagemResultadoArena(vitoria, msg.PontosVariacao, msg.NovoRanking),
                vitoria ? TipoDeAviso.Informacao : TipoDeAviso.Aviso
            );
        }

        private void AoMudarBandeiraPvp(long personagemId, bool ativa)
        {
            if (personagemId == EntradaNoJogo.PersonagemId)
            {
                AvisosDoJogo.Mostrar(TextosPvp.MensagemBandeira(ativa), TipoDeAviso.Informacao);
            }
        }
    }
}
