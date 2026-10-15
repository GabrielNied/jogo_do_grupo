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

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>
    /// Grava o relato em <c>relatos/AAAA-MM-DD_HHMMSS/</c>: as duas folhas de quadros, todos os
    /// quadros, a linha do tempo, a tabela por quadro e a nota. No editor a pasta fica na raiz
    /// do repositório (fora do git); no build, em <c>persistentDataPath</c>. A skill
    /// <c>/relato</c> lê nessa ordem: nota, linha do tempo, folha de impacto.
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

        /// <summary>Pasta onde os relatos ficam.</summary>
        public static string PastaDosRelatos =>
            Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "relatos"))
                : Path.Combine(Application.persistentDataPath, "relatos");

        /// <returns>A pasta do relato criado.</returns>
        public static string Escrever(Momento momento, string frase)
        {
            var nome = DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
            var pasta = Path.Combine(PastaDosRelatos, nome);
            var pastaDosQuadros = Path.Combine(pasta, "quadros");
            Directory.CreateDirectory(pastaDosQuadros);

            var imagens = new List<Texture2D>(momento.Texturas.Count);
            var ampliadas = new Texture2D[momento.Zooms.Count];
            try
            {
                foreach (var textura in momento.Texturas)
                {
                    imagens.Add(Ler(textura));
                }

                for (var i = 0; i < imagens.Count; i++)
                {
                    File.WriteAllBytes(
                        Path.Combine(
                            pastaDosQuadros,
                            i.ToString("000", CultureInfo.InvariantCulture) + ".png"
                        ),
                        imagens[i].EncodeToPNG()
                    );
                }

                var geral = EscolhaDeQuadros.Espalhados(
                    imagens.Count,
                    ConfigRelato.QuadrosPorFolha
                );
                var centro = EscolhaDeQuadros.CentroDoImpacto(
                    momento.Quadros,
                    momento.Eventos,
                    IdsDeAcerto
                );
                var impacto = EscolhaDeQuadros.EmVolta(
                    imagens.Count,
                    centro,
                    ConfigRelato.QuadrosPorFolha,
                    ConfigRelato.QuadrosAntesDoAcerto
                );
                GravarFolha(Path.Combine(pasta, "folha-geral.png"), imagens, geral);
                foreach (var indice in impacto)
                {
                    ampliadas[indice] = Ler(momento.Zooms[indice]);
                }

                GravarFolha(Path.Combine(pasta, "folha-impacto.png"), ampliadas, impacto);

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
                File.WriteAllText(
                    Path.Combine(pasta, "nota.txt"),
                    Nota(momento, frase, geral, impacto, centro),
                    SemBom
                );
                File.WriteAllText(Path.Combine(PastaDosRelatos, "ultimo.txt"), nome, SemBom);
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
            int centro
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

            texto.Append("folha-geral.png: quadros ").Append(Lista(geral)).Append('\n');
            texto
                .Append("folha-impacto.png: quadros ")
                .Append(Lista(impacto))
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

        private static string Lista(int[] indices)
        {
            return indices.Length == 0
                ? "(nenhum)"
                : $"q{indices[0]:000}–q{indices[indices.Length - 1]:000}";
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

        private static void GravarFolha(
            string caminho,
            IReadOnlyList<Texture2D> imagens,
            int[] indices
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
                Numerar(folha, x + 4, y + altura - 4, indices[i]);
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
