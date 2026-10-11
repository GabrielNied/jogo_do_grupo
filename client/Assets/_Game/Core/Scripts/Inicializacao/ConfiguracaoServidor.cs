using System;
using System.IO;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.TempoReal;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Inicializacao
{
    /// <summary>Endereço do servidor: API HTTP e tempo real (UDP).</summary>
    internal readonly struct EnderecoServidor
    {
        public EnderecoServidor(string urlApi, string hostTempoReal, int portaTempoReal)
        {
            UrlApi = urlApi;
            HostTempoReal = hostTempoReal;
            PortaTempoReal = portaTempoReal;
        }

        public string UrlApi { get; }
        public string HostTempoReal { get; }
        public int PortaTempoReal { get; }
    }

    /// <summary>
    /// Escolhe em qual servidor o client conecta (#198). Cada endereço segue esta ordem:
    /// argumento de linha de comando (<c>-servidor https://jogo.exemplo.org</c> e
    /// <c>-tempo-real host:porta</c>), depois o arquivo <c>servidor.json</c> ao lado do executável
    /// (no Editor, na pasta <c>client/</c>), depois o padrão (localhost).
    /// </summary>
    /// <remarks>
    /// Endereço inválido não trava a abertura: gera um aviso no log e a fonte seguinte vale.
    /// O <c>servidor.json</c> tem os campos <c>servidor</c> e <c>tempoReal</c>, os dois opcionais:
    /// <code>{ "servidor": "https://jogo.exemplo.org", "tempoReal": "jogo.exemplo.org:5081" }</code>
    /// </remarks>
    internal static class ConfiguracaoServidor
    {
        public const string ArgumentoApi = "-servidor";
        public const string ArgumentoTempoReal = "-tempo-real";
        public const string NomeArquivo = "servidor.json";

        // Preenchido pelo JsonUtility, que o compilador não enxerga (CS0649).
#pragma warning disable CS0649
        [Serializable]
        private class ArquivoServidor
        {
            public string servidor;
            public string tempoReal;
        }
#pragma warning restore CS0649

        /// <summary>Lê argumentos e arquivo e aplica em <see cref="ClienteApi"/> e <see cref="ConexaoTempoReal"/>.</summary>
        public static void Aplicar()
        {
            var endereco = Resolver(
                Environment.GetCommandLineArgs(),
                LerArquivo(),
                new EnderecoServidor(
                    ClienteApi.UrlBasePadrao,
                    ConexaoTempoReal.HostPadrao,
                    ConexaoTempoReal.PortaPadrao
                ),
                aviso => Debug.LogWarning($"[ConfiguracaoServidor] {aviso}")
            );

            ClienteApi.UrlBase = endereco.UrlApi;
            ConexaoTempoReal.Host = endereco.HostTempoReal;
            ConexaoTempoReal.Porta = endereco.PortaTempoReal;
        }

        /// <summary>
        /// Regra pura: argumentos &gt; conteúdo do <c>servidor.json</c> &gt; padrão.
        /// <paramref name="conteudoArquivo"/> nulo ou vazio quer dizer que não há arquivo.
        /// </summary>
        public static EnderecoServidor Resolver(
            string[] argumentos,
            string conteudoArquivo,
            EnderecoServidor padrao,
            Action<string> avisar
        )
        {
            var arquivo = LerJson(conteudoArquivo, avisar);

            var url = padrao.UrlApi;
            foreach (
                var (candidato, fonte) in new[]
                {
                    (ValorDoArgumento(argumentos, ArgumentoApi), $"argumento {ArgumentoApi}"),
                    (arquivo?.servidor, $"{NomeArquivo} (servidor)"),
                }
            )
            {
                if (string.IsNullOrWhiteSpace(candidato))
                    continue;
                if (UrlValida(candidato))
                {
                    url = candidato.Trim().TrimEnd('/');
                    break;
                }

                avisar($"Endereço inválido em {fonte}: \"{candidato}\". Use http:// ou https://.");
            }

            var host = padrao.HostTempoReal;
            var porta = padrao.PortaTempoReal;
            foreach (
                var (candidato, fonte) in new[]
                {
                    (
                        ValorDoArgumento(argumentos, ArgumentoTempoReal),
                        $"argumento {ArgumentoTempoReal}"
                    ),
                    (arquivo?.tempoReal, $"{NomeArquivo} (tempoReal)"),
                }
            )
            {
                if (string.IsNullOrWhiteSpace(candidato))
                    continue;
                if (TentarHostPorta(candidato, padrao.PortaTempoReal, out var h, out var p))
                {
                    host = h;
                    porta = p;
                    break;
                }

                avisar(
                    $"Endereço de tempo real inválido em {fonte}: \"{candidato}\". Use host:porta."
                );
            }

            return new EnderecoServidor(url, host, porta);
        }

        internal static string ValorDoArgumento(string[] argumentos, string nome)
        {
            if (argumentos == null)
                return null;
            for (var i = 0; i < argumentos.Length - 1; i++)
            {
                if (string.Equals(argumentos[i], nome, StringComparison.OrdinalIgnoreCase))
                    return argumentos[i + 1];
            }

            return null;
        }

        internal static bool UrlValida(string valor) =>
            Uri.TryCreate(valor.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host);

        /// <summary>Aceita <c>host:porta</c> ou só <c>host</c> (usa a porta padrão).</summary>
        internal static bool TentarHostPorta(
            string valor,
            int portaPadrao,
            out string host,
            out int porta
        )
        {
            host = null;
            porta = portaPadrao;
            var texto = valor.Trim();
            var separador = texto.LastIndexOf(':');
            if (separador >= 0)
            {
                if (
                    !int.TryParse(texto.Substring(separador + 1), out porta)
                    || porta < 1
                    || porta > 65535
                )
                    return false;
                texto = texto.Substring(0, separador);
            }

            if (texto.Length == 0 || texto.Contains("/") || texto.Contains(" "))
                return false;
            host = texto;
            return true;
        }

        private static ArquivoServidor LerJson(string conteudo, Action<string> avisar)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
                return null;
            try
            {
                return JsonUtility.FromJson<ArquivoServidor>(conteudo);
            }
            catch (ArgumentException)
            {
                avisar($"{NomeArquivo} não é um JSON válido; ignorado.");
                return null;
            }
        }

        private static string LerArquivo()
        {
            // Application.dataPath é <pasta do jogo>/<Nome>_Data no build e client/Assets no Editor.
            var pasta = Path.GetDirectoryName(Application.dataPath);
            var caminho = pasta == null ? null : Path.Combine(pasta, NomeArquivo);
            try
            {
                return caminho != null && File.Exists(caminho) ? File.ReadAllText(caminho) : null;
            }
            catch (IOException erro)
            {
                Debug.LogWarning(
                    $"[ConfiguracaoServidor] Não consegui ler {caminho}: {erro.Message}"
                );
                return null;
            }
        }
    }
}
