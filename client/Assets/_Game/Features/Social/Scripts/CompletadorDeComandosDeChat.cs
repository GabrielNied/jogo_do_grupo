using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Chat;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>Autocomplete de verbos com barra enquanto a pessoa digita (Tab).</summary>
    internal static class CompletadorDeComandosDeChat
    {
        private static readonly string[] _comandosDoChat =
        {
            "c",
            "cidade",
            "g",
            "grupo",
            "s",
            "w",
            "sussurrar",
            "r",
            "responder",
            "ignorar",
            "designorar",
            "comandos",
        };

        /// <summary>Completa o primeiro verbo após <c>/</c>. Devolve null se não houver sugestão.</summary>
        public static string Completar(string entrada)
        {
            var texto = entrada ?? "";
            if (texto.Length == 0 || texto[0] != '/')
            {
                return "";
            }

            var corpo = texto.Substring(1);
            if (corpo.Contains(" "))
            {
                return "";
            }

            var prefixo = corpo.ToLowerInvariant();
            var candidatos = new List<string>();
            foreach (var nome in TodosOsNomes())
            {
                if (nome.StartsWith(prefixo, StringComparison.Ordinal))
                {
                    candidatos.Add(nome);
                }
            }

            if (candidatos.Count == 0)
            {
                return "";
            }

            string escolhido;
            if (candidatos.Count == 1)
            {
                escolhido = candidatos[0];
            }
            else
            {
                escolhido = PrefixoComum(candidatos);
                if (escolhido.Length <= prefixo.Length)
                {
                    return "";
                }
            }

            var completo = "/" + escolhido;
            if (string.Equals(completo, texto, StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            return completo + " ";
        }

        private static string PrefixoComum(IReadOnlyList<string> nomes)
        {
            if (nomes.Count == 0)
            {
                return "";
            }

            var comum = nomes[0];
            for (var i = 1; i < nomes.Count; i++)
            {
                var nome = nomes[i];
                var tamanho = Math.Min(comum.Length, nome.Length);
                var j = 0;
                while (j < tamanho && comum[j] == nome[j])
                {
                    j++;
                }

                comum = comum.Substring(0, j);
                if (comum.Length == 0)
                {
                    break;
                }
            }

            return comum;
        }

        private static IEnumerable<string> TodosOsNomes()
        {
            foreach (var nome in _comandosDoChat)
            {
                yield return nome;
            }

            foreach (var nome in ComandosDeBarraNoChat.NomesParaAutocomplete())
            {
                yield return nome;
            }
        }
    }
}
