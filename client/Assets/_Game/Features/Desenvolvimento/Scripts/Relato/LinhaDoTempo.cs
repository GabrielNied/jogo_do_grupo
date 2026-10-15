using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Transforma os quadros e as mensagens guardados em texto curto para a IA ler: a linha do
    /// tempo só com o que mudou e a tabela completa por quadro.
    /// </summary>
    internal static class LinhaDoTempo
    {
        private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

        /// <summary>
        /// Uma linha por mudança, em ordem: entrada, mensagem do servidor, <c>timeScale</c>,
        /// clipe e <c>speed</c> dos Animators, alvo trocado e começo e fim do tremor. O tempo é em
        /// ms desde o primeiro quadro; <c>qNNN</c> é o quadro das folhas e de <c>quadros/</c>.
        /// </summary>
        public static string Montar(
            IReadOnlyList<AmostraDeQuadro> quadros,
            IReadOnlyList<EventoDeRede> eventos
        )
        {
            var texto = new StringBuilder();
            if (quadros.Count == 0)
            {
                return "(nenhum quadro guardado)\n";
            }

            var inicio = quadros[0].Tempo;
            var frequentes = Frequentes(eventos);
            var linhas = new List<(double Tempo, int Ordem, string Texto)>();

            var primeiro = quadros[0];
            linhas.Add(
                (
                    primeiro.Tempo,
                    0,
                    Linha(
                        inicio,
                        primeiro.Tempo,
                        0,
                        "início",
                        $"timeScale {Numero(primeiro.TimeScale)} · jogador {Descrever(primeiro.Jogador)} · alvo {DescreverAlvo(primeiro.Alvo)}"
                    )
                )
            );

            var tremendo = false;
            var maiorTremor = 0f;
            for (var i = 0; i < quadros.Count; i++)
            {
                var atual = quadros[i];
                if (!string.IsNullOrEmpty(atual.Entrada))
                {
                    linhas.Add(
                        (atual.Tempo, 1, Linha(inicio, atual.Tempo, i, "entrada", atual.Entrada))
                    );
                }

                if (i > 0)
                {
                    var anterior = quadros[i - 1];
                    if (Math.Abs(atual.TimeScale - anterior.TimeScale) > 0.0001f)
                    {
                        linhas.Add(
                            (
                                atual.Tempo,
                                2,
                                Linha(
                                    inicio,
                                    atual.Tempo,
                                    i,
                                    "tempo",
                                    $"timeScale {Numero(anterior.TimeScale)} → {Numero(atual.TimeScale)}"
                                )
                            )
                        );
                    }

                    Comparar(
                        linhas,
                        inicio,
                        i,
                        "jogador",
                        anterior.Jogador,
                        atual.Jogador,
                        atual.Tempo
                    );
                    Comparar(linhas, inicio, i, "alvo", anterior.Alvo, atual.Alvo, atual.Tempo);
                }

                var tremeAgora = atual.Solavanco > ConfigRelato.LimiarDoTremor;
                if (tremeAgora && !tremendo)
                {
                    maiorTremor = atual.Solavanco;
                    linhas.Add(
                        (
                            atual.Tempo,
                            4,
                            Linha(
                                inicio,
                                atual.Tempo,
                                i,
                                "câmera",
                                $"tremor começou (solavanco {Metros(atual.Solavanco)})"
                            )
                        )
                    );
                }
                else if (tremeAgora)
                {
                    maiorTremor = Math.Max(maiorTremor, atual.Solavanco);
                }
                else if (tremendo)
                {
                    linhas.Add(
                        (
                            atual.Tempo,
                            4,
                            Linha(
                                inicio,
                                atual.Tempo,
                                i,
                                "câmera",
                                $"tremor parou (pico {Metros(maiorTremor)})"
                            )
                        )
                    );
                }

                tremendo = tremeAgora;
            }

            foreach (var evento in eventos)
            {
                if (evento.Tempo < inicio || frequentes.ContainsKey(evento.Id))
                {
                    continue;
                }

                var quadro = EscolhaDeQuadros.QuadroNoTempo(quadros, evento.Tempo);
                linhas.Add(
                    (
                        evento.Tempo,
                        1,
                        Linha(
                            inicio,
                            evento.Tempo,
                            quadro,
                            "rede",
                            $"{evento.Nome} {Cortar(evento.Json)}"
                        )
                    )
                );
            }

            linhas.Sort(
                (a, b) =>
                    a.Tempo != b.Tempo ? a.Tempo.CompareTo(b.Tempo) : a.Ordem.CompareTo(b.Ordem)
            );
            foreach (var linha in linhas)
            {
                texto.Append(linha.Texto).Append('\n');
            }

            if (frequentes.Count > 0)
            {
                texto.Append("frequentes (omitidas):");
                foreach (var par in frequentes)
                {
                    texto.Append(' ').Append(par.Value.Nome).Append(" ×").Append(par.Value.Vezes);
                }

                texto.Append('\n');
            }

            return texto.ToString();
        }

        /// <summary>Uma linha por quadro, separada por <c>;</c>, para a análise fina.</summary>
        public static string Tabela(IReadOnlyList<AmostraDeQuadro> quadros)
        {
            var texto = new StringBuilder(
                "q;ms;dt_ms;timeScale;jogador_clipe;jogador_norm;jogador_speed;alvo;alvo_m;alvo_clipe;alvo_norm;alvo_speed;solavanco_cm;entrada\n"
            );
            if (quadros.Count == 0)
            {
                return texto.ToString();
            }

            var inicio = quadros[0].Tempo;
            for (var i = 0; i < quadros.Count; i++)
            {
                var q = quadros[i];
                var dt = i == 0 ? 0 : q.Tempo - quadros[i - 1].Tempo;
                texto
                    .Append(i.ToString("000", Cultura))
                    .Append(';')
                    .Append(Ms(q.Tempo - inicio))
                    .Append(';')
                    .Append(Ms(dt))
                    .Append(';')
                    .Append(Numero(q.TimeScale))
                    .Append(';')
                    .Append(q.Jogador.Existe ? q.Jogador.Clipe : "")
                    .Append(';')
                    .Append(q.Jogador.Existe ? Numero(q.Jogador.Normalizado) : "")
                    .Append(';')
                    .Append(q.Jogador.Existe ? Numero(q.Jogador.Velocidade) : "")
                    .Append(';')
                    .Append(q.Alvo.Existe ? q.Alvo.Objeto : "")
                    .Append(';')
                    .Append(q.Alvo.Existe ? Numero(q.Alvo.Distancia) : "")
                    .Append(';')
                    .Append(q.Alvo.Existe ? q.Alvo.Clipe : "")
                    .Append(';')
                    .Append(q.Alvo.Existe ? Numero(q.Alvo.Normalizado) : "")
                    .Append(';')
                    .Append(q.Alvo.Existe ? Numero(q.Alvo.Velocidade) : "")
                    .Append(';')
                    .Append(Numero(q.Solavanco * 100f))
                    .Append(';')
                    .Append(q.Entrada)
                    .Append('\n');
            }

            return texto.ToString();
        }

        private static void Comparar(
            List<(double, int, string)> linhas,
            double inicio,
            int indice,
            string quem,
            EstadoDoAnimator antes,
            EstadoDoAnimator agora,
            double tempo
        )
        {
            if (antes.Existe != agora.Existe || antes.Objeto != agora.Objeto)
            {
                var descricao = quem == "alvo" ? DescreverAlvo(agora) : Descrever(agora);
                linhas.Add((tempo, 3, Linha(inicio, tempo, indice, quem, "agora " + descricao)));
                return;
            }

            if (!agora.Existe)
            {
                return;
            }

            if (antes.Clipe != agora.Clipe)
            {
                linhas.Add(
                    (
                        tempo,
                        3,
                        Linha(inicio, tempo, indice, quem, $"clipe {antes.Clipe} → {agora.Clipe}")
                    )
                );
            }

            if (Math.Abs(antes.Velocidade - agora.Velocidade) > ConfigRelato.LimiarDaVelocidade)
            {
                linhas.Add(
                    (
                        tempo,
                        3,
                        Linha(
                            inicio,
                            tempo,
                            indice,
                            quem,
                            $"speed {Numero(antes.Velocidade)} → {Numero(agora.Velocidade)}"
                        )
                    )
                );
            }
        }

        private static Dictionary<ushort, (string Nome, int Vezes)> Frequentes(
            IReadOnlyList<EventoDeRede> eventos
        )
        {
            var contagem = new Dictionary<ushort, (string Nome, int Vezes)>();
            foreach (var evento in eventos)
            {
                contagem.TryGetValue(evento.Id, out var atual);
                contagem[evento.Id] = (evento.Nome, atual.Vezes + 1);
            }

            var frequentes = new Dictionary<ushort, (string Nome, int Vezes)>();
            foreach (var par in contagem)
            {
                if (par.Value.Vezes > ConfigRelato.MensagensFrequentes)
                {
                    frequentes[par.Key] = par.Value;
                }
            }

            return frequentes;
        }

        private static string Linha(
            double inicio,
            double tempo,
            int quadro,
            string tipo,
            string texto
        )
        {
            return $"{Ms(tempo - inicio), 6} ms  q{quadro.ToString("000", Cultura)}  {tipo, -8} {texto}";
        }

        private static string Descrever(EstadoDoAnimator estado)
        {
            return estado.Existe
                ? $"{estado.Clipe} (speed {Numero(estado.Velocidade)})"
                : "sem Animator";
        }

        private static string DescreverAlvo(EstadoDoAnimator estado)
        {
            return estado.Existe
                ? $"{estado.Objeto} a {Numero(estado.Distancia)} m, {Descrever(estado)}"
                : "nenhum por perto";
        }

        private static string Cortar(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return "";
            }

            return json.Length <= ConfigRelato.TamanhoDoJson
                ? json
                : json.Substring(0, ConfigRelato.TamanhoDoJson) + "…";
        }

        private static string Ms(double segundos)
        {
            return Math.Round(segundos * 1000).ToString("0", Cultura);
        }

        private static string Numero(float valor)
        {
            return valor.ToString("0.###", Cultura);
        }

        private static string Metros(float valor)
        {
            return (valor * 100f).ToString("0.#", Cultura) + " cm";
        }
    }
}
