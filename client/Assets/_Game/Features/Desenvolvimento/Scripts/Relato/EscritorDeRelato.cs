using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Grava o report em <c>relatos/pendentes/AAAA-MM-DD_HHMMSS/</c>: o <c>reporte.json</c>, a
    /// nota, a linha do tempo, o estado, o console, a tela inteira, as duas folhas de quadros,
    /// todos os quadros e a tabela por quadro. No editor a pasta fica na raiz do repositório
    /// (fora do git); no build, em <c>persistentDataPath</c>. O <c>scripts/relatos/reporte.mjs</c>
    /// transforma a pasta em issue e a move para <c>relatos/enviados/</c>.
    /// </summary>
    internal static class EscritorDeRelato
    {
        private static readonly Encoding SemBom = new UTF8Encoding(false);

        private static readonly ushort[] IdsDeAcerto =
        {
            CombateMessageIds.AtaqueAcertou,
            MobsMessageIds.InimigoAcertou,
        };

        // Dígitos 3×5 para numerar os quadros nas folhas (1 = aceso), linha a linha.
        private static readonly string[] Digitos =
        {
            "111101101101111",
            "010110010010111",
            "111001111100111",
            "111001111001111",
            "101101111001001",
            "111100111001111",
            "111100111101111",
            "111001001001001",
            "111101111101111",
            "111101111001111",
        };

        /// <summary>Raiz do repositório (no editor; o projeto Unity fica em <c>client/</c>).</summary>
        public static string RaizDoRepositorio =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        /// <summary>Pasta onde os relatos ficam.</summary>
        public static string PastaDosRelatos =>
            Application.isEditor
                ? Path.Combine(RaizDoRepositorio, "relatos")
                : Path.Combine(Application.persistentDataPath, "relatos");

        /// <summary>Reports ainda sem issue.</summary>
        public static string PastaDosPendentes => Path.Combine(PastaDosRelatos, "pendentes");

        /// <returns>A pasta do report criado (o nome é o id).</returns>
        public static string Escrever(Momento momento, DadosDoReporte dados)
        {
            var agora = DateTime.Now;
            var nome = ManifestoDoReporte.NovoId(agora);
            var pasta = Path.Combine(PastaDosPendentes, nome);
            for (var n = 2; Directory.Exists(pasta); n++)
            {
                pasta = Path.Combine(PastaDosPendentes, nome + "-" + n);
            }

            nome = Path.GetFileName(pasta);
            var pastaDosQuadros = Path.Combine(pasta, "quadros");
            Directory.CreateDirectory(pastaDosQuadros);
            var primeira = momento.PrimeiraImagem;

            var imagens = new List<Texture2D>(momento.Texturas.Count);
            var ampliadas = new Texture2D[momento.Zooms.Count];
            try
            {
                foreach (var textura in momento.Texturas)
                {
                    imagens.Add(Ler(textura));
                }

                // O arquivo leva o número do quadro (qNNN da linha do tempo), não o da imagem.
                for (var i = 0; i < imagens.Count; i++)
                {
                    File.WriteAllBytes(
                        Path.Combine(
                            pastaDosQuadros,
                            (primeira + i).ToString("000", CultureInfo.InvariantCulture) + ".png"
                        ),
                        imagens[i].EncodeToPNG()
                    );
                }

                var geral = EscolhaDeQuadros.Espalhados(
                    imagens.Count,
                    ConfigRelato.QuadrosPorFolha
                );
                var centro = CentroComImagem(
                    EscolhaDeQuadros.CentroDoImpacto(momento.Quadros, momento.Eventos, IdsDeAcerto),
                    primeira,
                    imagens.Count
                );
                var impacto = EscolhaDeQuadros.EmVolta(
                    imagens.Count,
                    centro,
                    ConfigRelato.QuadrosPorFolha,
                    ConfigRelato.QuadrosAntesDoAcerto
                );
                GravarFolha(Path.Combine(pasta, "folha-geral.png"), imagens, geral, primeira);
                foreach (var indice in impacto)
                {
                    ampliadas[indice] = Ler(momento.Zooms[indice]);
                }

                GravarFolha(Path.Combine(pasta, "folha-impacto.png"), ampliadas, impacto, primeira);
                if (momento.Tela != null)
                {
                    File.WriteAllBytes(Path.Combine(pasta, "tela.png"), momento.Tela.EncodeToPNG());
                }

                File.WriteAllText(
                    Path.Combine(pasta, "linha-do-tempo.txt"),
                    LinhaDoTempo.Montar(momento.Quadros, momento.Eventos),
                    SemBom
                );
                File.WriteAllText(
                    Path.Combine(pasta, "quadros.csv"),
                    LinhaDoTempo.Tabela(momento.Quadros),
                    SemBom
                );
                var posicao = Posicao();
                File.WriteAllText(
                    Path.Combine(pasta, "estado.txt"),
                    EstadoDoMomento.Estado(
                        SceneManager.GetActiveScene().name,
                        momento.JanelaAberta,
                        ConexaoTempoReal.Latencia,
                        posicao == null
                            ? null
                            : $"{Numero(posicao.Value.x)} {Numero(posicao.Value.y)} {Numero(posicao.Value.z)}",
                        momento.Ultimas,
                        momento.Agora
                    ),
                    SemBom
                );
                var console = EstadoDoMomento.Console(momento.Console, momento.Agora);
                if (console.Length > 0)
                {
                    File.WriteAllText(Path.Combine(pasta, "console.txt"), console, SemBom);
                }

                File.WriteAllText(
                    Path.Combine(pasta, "nota.txt"),
                    Nota(momento, dados.Frase, geral, impacto, centro + primeira, primeira),
                    SemBom
                );
                File.WriteAllText(
                    Path.Combine(pasta, "reporte.json"),
                    JsonUtility.ToJson(Manifesto(nome, momento, dados, posicao), true),
                    SemBom
                );
                File.WriteAllText(
                    Path.Combine(PastaDosRelatos, "ultimo.txt"),
                    "pendentes/" + nome,
                    SemBom
                );
            }
            finally
            {
                foreach (var imagem in imagens)
                {
                    UnityEngine.Object.Destroy(imagem);
                }

                foreach (var imagem in ampliadas)
                {
                    if (imagem != null)
                    {
                        UnityEngine.Object.Destroy(imagem);
                    }
                }
            }

            return pasta;
        }

        // Índice da imagem do centro da folha de impacto: o acerto, se ele tem imagem; senão o fim.
        private static int CentroComImagem(int quadro, int primeira, int imagens)
        {
            return quadro >= primeira ? quadro - primeira : Math.Max(0, imagens - 1);
        }

        private static Vector3? Posicao()
        {
            var jogador = EntradaNoJogo.Jogador;
            return jogador != null ? jogador.transform.position : (Vector3?)null;
        }

        private static ManifestoDoReporte Manifesto(
            string id,
            Momento momento,
            DadosDoReporte dados,
            Vector3? posicao
        )
        {
            var git = Application.isEditor ? InfoDoGit.Ler(RaizDoRepositorio) : ("", "");
            return new ManifestoDoReporte
            {
                Id = id,
                Tipo = dados.Tipo,
                Severidade = dados.Severidade,
                Area = dados.Area,
                Frase = dados.Frase.Trim(),
                Quando = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                QuandoUtc = momento.QuandoUtc.ToString("o", CultureInfo.InvariantCulture),
                Versao = Application.version,
                Editor = Application.isEditor,
                Tela = Screen.width + "x" + Screen.height,
                LatenciaMs = ConexaoTempoReal.Latencia,
                Posicao =
                    posicao == null
                        ? new float[0]
                        : new[] { posicao.Value.x, posicao.Value.y, posicao.Value.z },
                Cena = SceneManager.GetActiveScene().name,
                JanelaAberta = momento.JanelaAberta ?? "",
                AjustesF10 = Sintonia.Alterados(),
                Branch = git.Item1,
                Commit = git.Item2,
                SegundosDaLinhaDoTempo = ConfigRelato.SegundosDaLinhaDoTempo,
                SegundosDeQuadros = ConfigRelato.SegundosGuardados,
            };
        }

        /// <summary>Grava os valores fora do padrão do F10 em <c>relatos/ajustes-&lt;data&gt;.txt</c>.</summary>
        public static string GravarAjustes(string alterados)
        {
            Directory.CreateDirectory(PastaDosRelatos);
            var caminho = Path.Combine(
                PastaDosRelatos,
                "ajustes-"
                    + DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture)
                    + ".txt"
            );
            File.WriteAllText(caminho, alterados, SemBom);
            return caminho;
        }

        private static string Nota(
            Momento momento,
            string frase,
            int[] geral,
            int[] impacto,
            int centro,
            int primeira
        )
        {
            var quadros = momento.Quadros;
            var duracao =
                quadros.Count > 1 ? quadros[quadros.Count - 1].Tempo - quadros[0].Tempo : 0;
            var jogador = EntradaNoJogo.Jogador;
            var texto = new StringBuilder();
            texto
                .Append("frase: ")
                .Append(string.IsNullOrWhiteSpace(frase) ? "(sem frase)" : frase.Trim())
                .Append('\n');
            texto
                .Append("quando: ")
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                .Append('\n');
            texto
                .Append("versão: ")
                .Append(Application.version)
                .Append(Application.isEditor ? " (editor)" : "")
                .Append('\n');
            texto
                .Append("tela: ")
                .Append(Screen.width)
                .Append('×')
                .Append(Screen.height)
                .Append('\n');
            texto
                .Append("janela: ")
                .Append(quadros.Count)
                .Append(" quadros em ")
                .Append(Math.Round(duracao * 1000).ToString("0", CultureInfo.InvariantCulture))
                .Append(" ms (")
                .Append(
                    duracao > 0
                        ? (quadros.Count / duracao).ToString("0", CultureInfo.InvariantCulture)
                        : "?"
                )
                .Append(" quadros/s guardados)\n");
            texto.Append("latência: ").Append(ConexaoTempoReal.Latencia).Append(" ms\n");
            if (jogador != null)
            {
                var p = jogador.transform.position;
                texto
                    .Append("posição: ")
                    .Append(Numero(p.x))
                    .Append(' ')
                    .Append(Numero(p.y))
                    .Append(' ')
                    .Append(Numero(p.z))
                    .Append('\n');
            }

            texto.Append("folha-geral.png: quadros ").Append(Lista(geral, primeira)).Append('\n');
            texto
                .Append("folha-impacto.png: quadros ")
                .Append(Lista(impacto, primeira))
                .Append(" (centro q")
                .Append(centro.ToString("000", CultureInfo.InvariantCulture))
                .Append(", último acerto ou o fim; centro da tela ampliado 2×)\n");
            texto.Append(
                "leitura das folhas: esquerda para a direita, de cima para baixo; o número no canto é o qNNN.\n"
            );

            var alterados = Sintonia.Alterados();
            texto
                .Append("\najustes fora do padrão (F10):\n")
                .Append(alterados.Length > 0 ? alterados : "(nenhum)\n");
            return texto.ToString();
        }

        private static string Lista(int[] indices, int primeira)
        {
            return indices.Length == 0
                ? "(nenhum)"
                : $"q{primeira + indices[0]:000}–q{primeira + indices[indices.Length - 1]:000}";
        }

        private static string Numero(float valor) =>
            valor.ToString("0.##", CultureInfo.InvariantCulture);

        private static Texture2D Ler(RenderTexture textura)
        {
            var anterior = RenderTexture.active;
            RenderTexture.active = textura;
            var imagem = new Texture2D(textura.width, textura.height, TextureFormat.RGB24, false);
            imagem.ReadPixels(new Rect(0, 0, textura.width, textura.height), 0, 0);
            imagem.Apply();
            RenderTexture.active = anterior;
            return imagem;
        }

        // "primeira" soma ao índice da imagem para o número desenhado ser o qNNN do quadro.
        private static void GravarFolha(
            string caminho,
            IReadOnlyList<Texture2D> imagens,
            int[] indices,
            int primeira
        )
        {
            if (indices.Length == 0)
            {
                return;
            }

            var largura = ConfigRelato.LarguraDoQuadro;
            var altura = ConfigRelato.AlturaDoQuadro;
            var folha = new Texture2D(
                largura * ConfigRelato.ColunasDaFolha,
                altura * ConfigRelato.LinhasDaFolha,
                TextureFormat.RGB24,
                false
            );
            var fundo = new Color32[folha.width * folha.height];
            folha.SetPixels32(fundo);

            for (var i = 0; i < indices.Length; i++)
            {
                var coluna = i % ConfigRelato.ColunasDaFolha;
                // A textura começa embaixo: a primeira linha da grade é a de cima.
                var linha = ConfigRelato.LinhasDaFolha - 1 - i / ConfigRelato.ColunasDaFolha;
                var x = coluna * largura;
                var y = linha * altura;
                folha.SetPixels(x, y, largura, altura, imagens[indices[i]].GetPixels());
                Numerar(folha, x + 4, y + altura - 4, primeira + indices[i]);
            }

            folha.Apply();
            File.WriteAllBytes(caminho, folha.EncodeToPNG());
            UnityEngine.Object.Destroy(folha);
        }

        // Escreve o número com fundo preto no canto de cima à esquerda, a partir de (x, topo).
        private static void Numerar(Texture2D folha, int x, int topo, int numero)
        {
            var texto = numero.ToString("000", CultureInfo.InvariantCulture);
            var escala = ConfigRelato.EscalaDoNumero;
            var largura = (texto.Length * 4 + 1) * escala;
            var altura = 7 * escala;
            for (var px = 0; px < largura; px++)
            {
                for (var py = 0; py < altura; py++)
                {
                    folha.SetPixel(x + px, topo - py, Color.black);
                }
            }

            for (var d = 0; d < texto.Length; d++)
            {
                var desenho = Digitos[texto[d] - '0'];
                for (var linha = 0; linha < 5; linha++)
                {
                    for (var coluna = 0; coluna < 3; coluna++)
                    {
                        if (desenho[linha * 3 + coluna] != '1')
                        {
                            continue;
                        }

                        for (var ex = 0; ex < escala; ex++)
                        {
                            for (var ey = 0; ey < escala; ey++)
                            {
                                folha.SetPixel(
                                    x + (1 + d * 4 + coluna) * escala + ex,
                                    topo - (1 + linha) * escala - ey,
                                    Color.yellow
                                );
                            }
                        }
                    }
                }
            }
        }
    }
}
