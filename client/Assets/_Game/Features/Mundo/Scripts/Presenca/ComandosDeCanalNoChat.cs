using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Registra os comandos <c>/canal [N]</c> e <c>/canais</c> no chat (ADR 0045).
    /// </summary>
    internal static class ComandosDeCanalNoChat
    {
        public const string VerboCanal = "canal";
        public const string VerboCanais = "canais";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            ComandosDeBarraNoChat.Registrar(
                VerboCanal,
                ExecutarCanal,
                "/canal [numero] — consulta o canal atual ou solicita troca de canal"
            );
            ComandosDeBarraNoChat.Registrar(
                VerboCanais,
                ExecutarCanais,
                "/canais — solicita a lista de canais e sua lotacao"
            );
        }

        private static ResultadoDeComandoDeBarra ExecutarCanal(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            var argumento = (resto ?? "").Trim();
            if (string.IsNullOrEmpty(argumento))
            {
                var canalAtual = PresencaNoMundo.Atual?.CanalAtual ?? 1;
                var total =
                    PresencaNoMundo.Atual?.TotalCanais ?? ConfigCanais.QuantidadeCanaisPadrao;
                contexto.MostrarSistema(
                    $"Você está no Canal {canalAtual} (Total: {total}). Para trocar, use /canal <número>."
                );
                return new ResultadoDeComandoDeBarra();
            }

            if (int.TryParse(argumento, out var canalDestino) && canalDestino >= 1)
            {
                if (PresencaNoMundo.Atual != null)
                {
                    PresencaNoMundo.Atual.PedirTrocaDeCanal(canalDestino);
                }
                else
                {
                    ConexaoTempoReal.Enviar(
                        MundoMessageIds.PedirTrocaDeCanal,
                        new PedirTrocaDeCanal { CanalDestino = canalDestino }
                    );
                }
            }
            else
            {
                contexto.MostrarSistema("Canal inválido. Use /canal <número> (ex.: /canal 2).");
            }

            return new ResultadoDeComandoDeBarra();
        }

        private static ResultadoDeComandoDeBarra ExecutarCanais(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            if (PresencaNoMundo.Atual != null)
            {
                PresencaNoMundo.Atual.ConsultarCanais();
            }
            else
            {
                ConexaoTempoReal.Enviar(MundoMessageIds.ConsultarCanais, new ConsultarCanais());
            }

            return new ResultadoDeComandoDeBarra();
        }
    }
}
