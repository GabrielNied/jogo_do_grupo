using System.Runtime.CompilerServices;
using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Desenvolvimento;
using UnityEngine;

[assembly: InternalsVisibleTo("JogoDoGrupo.Client.Desenvolvimento.Tests")]

namespace JogoDoGrupo.Client.Features.Desenvolvimento
{
    /// <summary>
    /// Liga os comandos de teste (<c>xp</c>, <c>tp</c>, <c>spawn</c>) ao chat com barra e mostra
    /// a resposta do servidor como linha de Sistema.
    /// </summary>
    internal static class RegistroDeComandosDeTesteNoChat
    {
        private static bool _registrado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Iniciar()
        {
            if (!ComandosDeTesteNoClient.Permite || _registrado)
            {
                return;
            }

            _registrado = true;
            RegistrarVerbo("xp", "/xp <quantidade> — ajusta experiência (só em desenvolvimento)");
            RegistrarVerbo("tp", "/tp <x> <y> <z> — teleporte (só em desenvolvimento)");
            RegistrarVerbo("spawn", "/spawn — cria inimigo de teste perto de você");
            RegistrarVerbo("ajuda", "/ajuda — atalho para a lista de comandos de teste");

            ConexaoTempoReal.Assinar<ResultadoDeComandoDeTeste>(
                DesenvolvimentoMessageIds.ResultadoDeComandoDeTeste,
                AoResultado
            );
        }

        private static void RegistrarVerbo(string verbo, string linhaNaLista)
        {
            ComandosDeBarraNoChat.Registrar(
                verbo,
                (_, resto, ctx) => Executar(verbo, resto, ctx),
                linhaNaLista
            );
        }

        internal static ResultadoDeComandoDeBarra Executar(
            string verbo,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            var texto = MontarTexto(verbo, resto);
            var interpretado = ComandosDeTeste.Interpretar(texto);
            if (interpretado.Tipo == TipoDeComandoDeTeste.Ajuda)
            {
                contexto.MostrarSistema(TextoAjuda());
                return new ResultadoDeComandoDeBarra();
            }

            if (interpretado.Tipo == TipoDeComandoDeTeste.Invalido)
            {
                contexto.MostrarSistema(interpretado.Aviso);
                return new ResultadoDeComandoDeBarra();
            }

            ConexaoTempoReal.Enviar(
                DesenvolvimentoMessageIds.PedirComandoDeTeste,
                new PedirComandoDeTeste { Texto = texto }
            );
            return new ResultadoDeComandoDeBarra();
        }

        internal static string MontarTexto(string verbo, string resto)
        {
            resto = (resto ?? "").Trim();
            return resto.Length == 0 ? verbo : $"{verbo} {resto}";
        }

        internal static string TextoAjuda() =>
            "Comandos de teste: " + ComandosDeTeste.AjudaComBarra;

        private static void AoResultado(ResultadoDeComandoDeTeste resultado)
        {
            if (resultado == null || string.IsNullOrEmpty(resultado.Texto))
            {
                return;
            }

            ChatDoJogo.AdicionarLinhaSistema(resultado.Texto);
        }
    }
}
