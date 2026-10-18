using System;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Core.Moderacao;
using JogoDoGrupo.Shared.Core.TempoReal;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo.Chat
{
    /// <summary>
    /// Registra comandos de moderação/GM e comando de denúncia no chat (#187).
    /// </summary>
    public static class ComandosDeModeracaoNoChat
    {
        private static bool _registrado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Registrar()
        {
            if (_registrado)
            {
                return;
            }

            _registrado = true;

            ConexaoTempoReal.Assinar<ResultadoComandoGm>(
                TempoRealIds.ResultadoComandoGm,
                AoReceberResultadoGm
            );
            ConexaoTempoReal.Assinar<ResultadoDenuncia>(
                TempoRealIds.ResultadoDenuncia,
                AoReceberResultadoDenuncia
            );

            ComandosDeBarraNoChat.Registrar(
                "denunciar",
                ExecutarDenunciar,
                "/denunciar <jogador> <motivo> [detalhes] — envia denúncia de jogador à moderação"
            );

            ComandosDeBarraNoChat.Registrar(
                "tp",
                ExecutarGm,
                "/tp <x y z> ou /tp <jogador> — [GM] teleporte"
            );
            ComandosDeBarraNoChat.Registrar(
                "puxar",
                ExecutarGm,
                "/puxar <jogador> — [GM] puxa jogador até sua posição"
            );
            ComandosDeBarraNoChat.Registrar(
                "silenciar",
                ExecutarGm,
                "/silenciar <jogador> [minutos] — [GM] silencia jogador no chat"
            );
            ComandosDeBarraNoChat.Registrar(
                "expulsar",
                ExecutarGm,
                "/expulsar <jogador> [motivo] — [GM] desconecta jogador do servidor"
            );
            ComandosDeBarraNoChat.Registrar(
                "banir",
                ExecutarGm,
                "/banir <jogador> [dias] [motivo] — [GM] bane temporariamente conta do jogador"
            );
        }

        private static ResultadoDeComandoDeBarra ExecutarDenunciar(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            var partes = (resto ?? "").Split(
                new[] { ' ' },
                3,
                StringSplitOptions.RemoveEmptyEntries
            );
            if (partes.Length < 2)
            {
                contexto.MostrarSistema(
                    "Uso: /denunciar <jogador> <motivo> [detalhes]. Motivos válidos: ofensa, trapaca, spam, comportamento, outro"
                );
                return new ResultadoDeComandoDeBarra();
            }

            var alvo = partes[0].Trim();
            var motivo = partes[1].Trim();
            var detalhes = partes.Length > 2 ? partes[2].Trim() : "";

            if (!RegrasModeracao.EhMotivoDenunciaValido(motivo))
            {
                contexto.MostrarSistema(
                    $"Motivo inválido '{motivo}'. Motivos válidos: {string.Join(", ", RegrasModeracao.MotivosDenunciaValidos)}"
                );
                return new ResultadoDeComandoDeBarra();
            }

            if (!ConexaoTempoReal.Conectado)
            {
                contexto.MostrarSistema("Você não está conectado ao servidor.");
                return new ResultadoDeComandoDeBarra();
            }

            ConexaoTempoReal.Enviar(
                TempoRealIds.DenunciarJogador,
                new DenunciarJogador
                {
                    AlvoNome = alvo,
                    Motivo = motivo,
                    Detalhes = detalhes,
                }
            );

            contexto.MostrarSistema($"Enviando denúncia contra '{alvo}'...");
            return new ResultadoDeComandoDeBarra();
        }

        private static ResultadoDeComandoDeBarra ExecutarGm(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            if (!ConexaoTempoReal.Conectado)
            {
                contexto.MostrarSistema("Você não está conectado ao servidor.");
                return new ResultadoDeComandoDeBarra();
            }

            ConexaoTempoReal.Enviar(
                TempoRealIds.PedirComandoGm,
                new PedirComandoGm { Comando = comando, Argumentos = resto ?? "" }
            );

            return new ResultadoDeComandoDeBarra();
        }

        private static void AoReceberResultadoGm(ResultadoComandoGm resultado)
        {
            if (resultado == null)
            {
                return;
            }

            var texto = resultado.Mensagem;
            if (!string.IsNullOrEmpty(texto))
            {
                ChatDoJogo.AdicionarLinhaSistema?.Invoke(texto);
            }
        }

        private static void AoReceberResultadoDenuncia(ResultadoDenuncia resultado)
        {
            if (resultado == null)
            {
                return;
            }

            var texto = resultado.Mensagem;
            if (!string.IsNullOrEmpty(texto))
            {
                ChatDoJogo.AdicionarLinhaSistema?.Invoke(texto);
            }
        }
    }
}
