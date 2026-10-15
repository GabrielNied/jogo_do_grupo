using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Textos do <c>estado.txt</c> (onde o jogo estava na hora do report) e do
    /// <c>console.txt</c> (avisos e erros do Unity). O estado vem só do Core e das mensagens do
    /// servidor (a última de cada tipo), sem conhecer as outras funcionalidades.
    /// </summary>
    internal static class EstadoDoMomento
    {
        private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

        public static string Estado(
            string cena,
            string janelaAberta,
            int latenciaMs,
            string posicao,
            IReadOnlyList<EventoDeRede> ultimas,
            double agora
        )
        {
            var texto = new StringBuilder();
            texto.Append("cena: ").Append(string.IsNullOrEmpty(cena) ? "?" : cena).Append('\n');
            texto
                .Append("janela aberta antes do report: ")
                .Append(string.IsNullOrEmpty(janelaAberta) ? "nenhuma" : janelaAberta)
                .Append('\n');
            texto.Append("latência: ").Append(latenciaMs).Append(" ms\n");
            texto
                .Append("posição: ")
                .Append(string.IsNullOrEmpty(posicao) ? "sem personagem" : posicao)
                .Append('\n');
            texto.Append(
                "\núltima mensagem de cada tipo nesta sessão (há quanto tempo, JSON cortado):\n"
            );
            if (ultimas.Count == 0)
            {
                texto.Append("  (nenhuma)\n");
            }

            foreach (var evento in ultimas)
            {
                texto
                    .Append("  ")
                    .Append(evento.Nome)
                    .Append(" (")
                    .Append(evento.Id)
                    .Append(") há ")
                    .Append(Segundos(agora - evento.Tempo))
                    .Append(": ")
                    .Append(Cortar(evento.Json, ConfigRelato.TamanhoDoJson))
                    .Append('\n');
            }

            return texto.ToString();
        }

        /// <returns>Vazio sem aviso nem erro.</returns>
        public static string Console(IReadOnlyList<LinhaDoConsole> linhas, double agora)
        {
            var texto = new StringBuilder();
            foreach (var linha in linhas)
            {
                texto
                    .Append("há ")
                    .Append(Segundos(agora - linha.Tempo))
                    .Append("  ")
                    .Append(linha.Tipo)
                    .Append("  ")
                    .Append(linha.Mensagem.Trim())
                    .Append('\n');
                if (linha.Tipo == LogType.Warning)
                {
                    continue;
                }

                var pilha = linha.Pilha.Split('\n');
                for (var i = 0; i < pilha.Length && i < ConfigRelato.LinhasDaPilha; i++)
                {
                    if (pilha[i].Trim().Length > 0)
                    {
                        texto.Append("    ").Append(pilha[i].Trim()).Append('\n');
                    }
                }
            }

            return texto.ToString();
        }

        private static string Segundos(double segundos)
        {
            return segundos.ToString("0.0", Cultura) + " s";
        }

        private static string Cortar(string texto, int tamanho)
        {
            texto = (texto ?? "").Replace('\n', ' ');
            return texto.Length > tamanho ? texto.Substring(0, tamanho) + "…" : texto;
        }
    }
}
