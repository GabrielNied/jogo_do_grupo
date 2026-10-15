using System;
using System.IO;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Branch e commit do checkout, lidos direto da pasta <c>.git</c> (sem rodar o git), para o
    /// report dizer em que código o jogo estava. Entende worktree (<c>.git</c> como arquivo) e
    /// <c>packed-refs</c>.
    /// </summary>
    internal static class InfoDoGit
    {
        private const int TamanhoDoCommit = 7;

        /// <returns>Branch (vazio se HEAD solto) e commit curto; os dois vazios sem git.</returns>
        public static (string Branch, string Commit) Ler(string raiz)
        {
            try
            {
                var git = Path.Combine(raiz, ".git");
                var pasta = git;
                if (File.Exists(git))
                {
                    var linha = File.ReadAllText(git).Trim();
                    if (!linha.StartsWith("gitdir:", StringComparison.Ordinal))
                    {
                        return ("", "");
                    }

                    pasta = Path.GetFullPath(Path.Combine(raiz, linha.Substring(7).Trim()));
                }

                var head = Path.Combine(pasta, "HEAD");
                if (!File.Exists(head))
                {
                    return ("", "");
                }

                var comum = pasta;
                var arquivoComum = Path.Combine(pasta, "commondir");
                if (File.Exists(arquivoComum))
                {
                    comum = Path.GetFullPath(
                        Path.Combine(pasta, File.ReadAllText(arquivoComum).Trim())
                    );
                }

                var conteudo = File.ReadAllText(head).Trim();
                if (!conteudo.StartsWith("ref: ", StringComparison.Ordinal))
                {
                    return ("", Curto(conteudo));
                }

                var referencia = conteudo.Substring(5).Trim();
                var branch = referencia.StartsWith("refs/heads/", StringComparison.Ordinal)
                    ? referencia.Substring(11)
                    : referencia;
                return (branch, Curto(Resolver(pasta, comum, referencia)));
            }
            catch (IOException)
            {
                return ("", "");
            }
            catch (UnauthorizedAccessException)
            {
                return ("", "");
            }
        }

        private static string Resolver(string pasta, string comum, string referencia)
        {
            foreach (var onde in new[] { pasta, comum })
            {
                var arquivo = Path.Combine(onde, referencia);
                if (File.Exists(arquivo))
                {
                    return File.ReadAllText(arquivo).Trim();
                }
            }

            var empacotadas = Path.Combine(comum, "packed-refs");
            if (!File.Exists(empacotadas))
            {
                return "";
            }

            foreach (var linha in File.ReadAllLines(empacotadas))
            {
                var partes = linha.Split(' ');
                if (partes.Length == 2 && partes[1] == referencia)
                {
                    return partes[0];
                }
            }

            return "";
        }

        private static string Curto(string commit)
        {
            return commit.Length > TamanhoDoCommit ? commit.Substring(0, TamanhoDoCommit) : commit;
        }
    }
}
