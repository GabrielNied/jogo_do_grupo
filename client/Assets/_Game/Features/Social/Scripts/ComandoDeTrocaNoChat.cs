using JogoDoGrupo.Client.Core.Jogo.Chat;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>Registra <c>/trocar [nome]</c> e <c>/trade [nome]</c> no chat (troca direta, #175).</summary>
    internal static class ComandoDeTrocaNoChat
    {
        public const string VerboTrocar = "trocar";
        public const string VerboTrade = "trade";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            ComandosDeBarraNoChat.Registrar(
                VerboTrocar,
                Executar,
                "/trocar <nome> — convida um jogador a até 10 m para trocar itens e coroas"
            );
            ComandosDeBarraNoChat.Registrar(
                VerboTrade,
                Executar,
                "/trade <nome> — alias para /trocar"
            );
        }

        private static ResultadoDeComandoDeBarra Executar(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            var nome = (resto ?? "").Trim();
            if (string.IsNullOrEmpty(nome))
            {
                contexto.MostrarSistema("Uso: /trocar <nome do personagem>");
                return new ResultadoDeComandoDeBarra();
            }

            if (EstadoDaTrocaDoClient.Instancia.Pedir(nome))
            {
                contexto.MostrarSistema(TextosTroca.ConviteEnviado(nome));
            }
            else
            {
                contexto.MostrarSistema("Sem conexão com o servidor.");
            }

            return new ResultadoDeComandoDeBarra();
        }
    }
}
