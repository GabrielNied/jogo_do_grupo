using JogoDoGrupo.Client.Core.Jogo.Chat;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Registra os comandos <c>/inspecionar [nome]</c> e <c>/inspect [nome]</c> no chat.
    /// </summary>
    internal static class ComandoDeInspecaoNoChat
    {
        public const string VerboInspecionar = "inspecionar";
        public const string VerboInspect = "inspect";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            ComandosDeBarraNoChat.Registrar(
                VerboInspecionar,
                Executar,
                "/inspecionar <nome> — visualiza o equipamento e status de outro jogador"
            );
            ComandosDeBarraNoChat.Registrar(
                VerboInspect,
                Executar,
                "/inspect <nome> — alias para /inspecionar"
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
                contexto.MostrarSistema("Uso: /inspecionar <nome do personagem>");
                return new ResultadoDeComandoDeBarra();
            }

            contexto.MostrarSistema($"Solicitando inspeção de {nome}...");
            EstadoInspecao.Instancia.PedirPorNome(nome);
            return new ResultadoDeComandoDeBarra();
        }
    }
}
