using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Gerencia pedidos de inspeção de outros jogadores no client.
    /// Envia <see cref="PedirInspecao"/> e recebe <see cref="RetratoInspecaoResponse"/>
    /// ou <see cref="InspecaoRecusada"/> para exibir na <see cref="JanelaInspecao"/>.
    /// </summary>
    internal sealed class EstadoInspecao : IDisposable
    {
        private static EstadoInspecao _instancia;

        private readonly IConexaoDoGrupo _conexao;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public RetratoInspecaoResponse UltimoRetrato { get; private set; }
        public CodigoErroInspecao? UltimoErro { get; private set; }
        public string UltimaMensagemErro { get; private set; } = "";

        public event Action<RetratoInspecaoResponse> RecebeuRetrato;
        public event Action<InspecaoRecusada> Recusada;

        public EstadoInspecao() { }

        public EstadoInspecao(IConexaoDoGrupo conexao)
        {
            _conexao = conexao;
            if (_conexao == null)
            {
                return;
            }

            _assinaturas.Add(
                _conexao.Assinar<RetratoInspecaoResponse>(
                    SocialMessageIds.RetratoInspecaoResponse,
                    AoReceberRetrato
                )
            );
            _assinaturas.Add(
                _conexao.Assinar<InspecaoRecusada>(
                    SocialMessageIds.InspecaoRecusada,
                    AoReceberRecusa
                )
            );
        }

        public static EstadoInspecao Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    _instancia = new EstadoInspecao(new ConexaoDoGrupoReal());
                }

                return _instancia;
            }
        }

        public bool Pedir(long personagemId)
        {
            if (personagemId <= 0)
            {
                return false;
            }

            UltimoErro = null;
            UltimaMensagemErro = "";
            return _conexao?.Enviar(
                    SocialMessageIds.PedirInspecao,
                    new PedirInspecao { PersonagemId = personagemId },
                    EntregaTempoReal.Confiavel
                ) ?? false;
        }

        public bool PedirPorNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return false;
            }

            UltimoErro = null;
            UltimaMensagemErro = "";
            return _conexao?.Enviar(
                    SocialMessageIds.PedirInspecao,
                    new PedirInspecao { Nome = nome.Trim() },
                    EntregaTempoReal.Confiavel
                )
                ?? false;
        }

        internal void AoReceberRetrato(RetratoInspecaoResponse retrato)
        {
            UltimoRetrato = retrato;
            UltimoErro = null;
            UltimaMensagemErro = "";
            RecebeuRetrato?.Invoke(retrato);
            JanelasDoJogo.Abrir(JanelaInspecao.Chave);
        }

        internal void AoReceberRecusa(InspecaoRecusada recusa)
        {
            UltimoErro = recusa.Motivo;
            UltimaMensagemErro = TextoDeErro(recusa.Motivo);
            Recusada?.Invoke(recusa);
        }

        public static string TextoDeErro(CodigoErroInspecao motivo)
        {
            switch (motivo)
            {
                case CodigoErroInspecao.AlvoNaoEncontrado:
                    return "Jogador não encontrado.";
                case CodigoErroInspecao.AlvoOffline:
                    return "O jogador está offline.";
                case CodigoErroInspecao.AlvoForaDoAlcance:
                    return "O jogador está fora de alcance (longe ou em outra área).";
                default:
                    return "Não foi possível inspecionar o jogador.";
            }
        }

        public void Dispose()
        {
            foreach (var a in _assinaturas)
            {
                a?.Dispose();
            }

            _assinaturas.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia?.Dispose();
            _instancia = null;
        }
    }
}
