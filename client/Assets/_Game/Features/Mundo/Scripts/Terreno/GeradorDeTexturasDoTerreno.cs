using System;
using System.IO;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Gerador determinístico de texturas de alta resolução (512x512) para os materiais do terreno:
    /// Grama (lâminas nítidas e detalhes orgânicos), Terra (partículas de solo e seixos),
    /// Areia (ondulações e grãos finos), Rocha (estratificação e fendas) e Neve (cristais).
    /// Gera texturas contínuas (seamless / tileable) com empacotamento toroidal para repetição sem emendas.
    /// </summary>
    public static class GeradorDeTexturasDoTerreno
    {
        public const int ResolucaoPadrao = 512;
        public const int ResolucaoDetalhe = 1024;

        private static Texture2D _texturaGrama;
        private static Texture2D _texturaTerra;
        private static Texture2D _texturaAreia;
        private static Texture2D _texturaRocha;
        private static Texture2D _texturaNeve;
        private static Texture2D _texturaDetalhes;

        public static Texture2D ObterGrama() => _texturaGrama ??= CriarTexturaGrama();

        public static Texture2D ObterTerra() => _texturaTerra ??= CriarTexturaTerra();

        public static Texture2D ObterAreia() => _texturaAreia ??= CriarTexturaAreia();

        public static Texture2D ObterRocha() => _texturaRocha ??= CriarTexturaRocha();

        public static Texture2D ObterNeve() => _texturaNeve ??= CriarTexturaNeve();

        public static Texture2D ObterDetalhes() => _texturaDetalhes ??= CriarTexturaDetalhes();

        /// <summary>
        /// Textura contínua de grama de fantasia com lâminas verdes bem definidas,
        /// variações de tom, trevos sutis e sombras profundas entre os tufos.
        /// </summary>
        public static Texture2D CriarTexturaGrama(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Grama_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var corBase = new Color(0.26f, 0.50f, 0.17f);
            var corLuz = new Color(0.44f, 0.72f, 0.24f);
            var corSombra = new Color(0.13f, 0.25f, 0.08f);
            var corTrevo = new Color(0.48f, 0.68f, 0.20f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    // Ruído orgânico de base para manchas de vegetação
                    var macro = RuidoToroidal(u * 4f, v * 4f, 131u);
                    var meso = RuidoToroidal(u * 12f, v * 12f, 173u);

                    // Padrão direcional de lâminas de grama (feixes verticais inclinados)
                    var laminaX = u * 64f + Mathf.Sin(v * 32f) * 0.35f;
                    var laminaY = v * 32f;
                    var lamina = RuidoToroidal(laminaX, laminaY, 239u);

                    // Fibras finas de alta frequência (detalhe milimétrico)
                    var fibra = RuidoToroidal(u * 128f, v * 128f, 311u);

                    // Pequenos trevos e toques florais
                    var trevo = Mathf.Pow(RuidoToroidal(u * 24f, v * 24f, 401u), 4f);

                    var t = lamina * 0.55f + fibra * 0.25f + meso * 0.20f;
                    var cor = Color.Lerp(corSombra, corLuz, t);
                    cor = Color.Lerp(cor, corBase, macro * 0.4f);
                    cor = Color.Lerp(cor, corTrevo, trevo * 0.6f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de terra rica com torrões, grânulos de solo,
        /// seixos dispersos e pequenas fissuras naturais.
        /// </summary>
        public static Texture2D CriarTexturaTerra(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Terra_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var corBase = new Color(0.36f, 0.25f, 0.16f);
            var corEscura = new Color(0.22f, 0.15f, 0.09f);
            var corClara = new Color(0.48f, 0.36f, 0.25f);
            var corSeixo = new Color(0.56f, 0.52f, 0.46f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    var macro = RuidoToroidal(u * 5f, v * 5f, 503u);
                    var terra = RuidoToroidal(u * 28f, v * 28f, 541u);
                    var grao = RuidoToroidal(u * 96f, v * 96f, 587u);

                    // Seixos e cascalho (pontos nítidos com luz em cima e sombra embaixo)
                    var seixoMascara = Mathf.Pow(RuidoToroidal(u * 36f, v * 36f, 619u), 5f);

                    var t = terra * 0.6f + grao * 0.4f;
                    var cor = Color.Lerp(corEscura, corClara, t);
                    cor = Color.Lerp(cor, corBase, macro * 0.35f);
                    cor = Color.Lerp(cor, corSeixo, seixoMascara * 0.75f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de areia dourada com ondulações de vento,
        /// cristas iluminadas e micro-brilho granular.
        /// </summary>
        public static Texture2D CriarTexturaAreia(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Areia_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var corBase = new Color(0.85f, 0.73f, 0.48f);
            var corCrista = new Color(0.92f, 0.82f, 0.58f);
            var corCava = new Color(0.72f, 0.60f, 0.38f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    // Ondulações de vento (dunas menores e ripples na praia)
                    var angulo = (u * 18f + v * 6f) * Mathf.PI * 2f;
                    var onda = Mathf.Sin(angulo + Mathf.Sin(v * 12f) * 0.75f) * 0.5f + 0.5f;

                    var macro = RuidoToroidal(u * 4f, v * 4f, 709u);
                    var grao = RuidoToroidal(u * 120f, v * 120f, 751u);

                    var t = onda * 0.65f + grao * 0.35f;
                    var cor = Color.Lerp(corCava, corCrista, t);
                    cor = Color.Lerp(cor, corBase, macro * 0.3f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de rocha estratificada para paredões e encostas,
        /// com fendas fraturadas e granito áspero.
        /// </summary>
        public static Texture2D CriarTexturaRocha(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Rocha_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var corBase = new Color(0.46f, 0.45f, 0.44f);
            var corFenda = new Color(0.18f, 0.17f, 0.17f);
            var corCrista = new Color(0.66f, 0.64f, 0.62f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    // Camadas horizontais estratificadas de rocha
                    var estrato = Mathf.Sin(v * 48f + RuidoToroidal(u * 8f, v * 8f, 809u) * 3f);
                    estrato = estrato * 0.5f + 0.5f;

                    // Fendas angulares de fratura
                    var fenda1 = Mathf.Abs(RuidoToroidal(u * 20f, v * 20f, 853u) * 2f - 1f);
                    var fenda2 = Mathf.Abs(RuidoToroidal(u * 40f, v * 40f, 887u) * 2f - 1f);
                    var fenda = Mathf.Min(fenda1, fenda2);

                    var grao = RuidoToroidal(u * 90f, v * 90f, 929u);

                    var t = estrato * 0.4f + grao * 0.35f + (1f - fenda) * 0.25f;
                    var cor = Color.Lerp(corBase, corCrista, t);
                    if (fenda < 0.18f)
                    {
                        var pesoFenda = 1f - (fenda / 0.18f);
                        cor = Color.Lerp(cor, corFenda, pesoFenda * 0.85f);
                    }

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de neve cristalina com crosta soprada pelo vento e brilho.
        /// </summary>
        public static Texture2D CriarTexturaNeve(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Neve_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var corBase = new Color(0.92f, 0.94f, 0.98f);
            var corSombra = new Color(0.80f, 0.84f, 0.92f);
            var corCristal = new Color(1.0f, 1.0f, 1.0f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    var macro = RuidoToroidal(u * 6f, v * 6f, 967u);
                    var vento = Mathf.Sin((u * 12f + v * 4f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    var cristal = Mathf.Pow(RuidoToroidal(u * 110f, v * 110f, 997u), 3.5f);

                    var cor = Color.Lerp(corSombra, corBase, vento * 0.6f + macro * 0.4f);
                    cor = Color.Lerp(cor, corCristal, cristal * 0.5f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Mapa de detalhe albedo x2 de altíssima definição (1024x1024) centrado em cinza linear (0.5),
        /// perfeitamente compatível com o shader nativo Lit do Universal Render Pipeline (_DetailAlbedoMap).
        /// Em 0.5 a cor do bioma é mantida intacta; acima de 0.5 realça lâminas e arestas; abaixo aprofunda fendas e sombras.
        /// </summary>
        public static Texture2D CriarTexturaDetalhes(int tamanho = ResolucaoDetalhe)
        {
            var tex = CriarTexturaBase("Terreno_Detalhe_1024", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    // Micro-relevo de alta frequência: lâminas, seixos, grânulos e fraturas
                    var f1 = RuidoToroidal(u * 48f, v * 48f, 1013u);
                    var f2 = RuidoToroidal(u * 128f, v * 128f, 1049u);
                    var f3 = RuidoToroidal(u * 256f, v * 256f, 1091u);

                    // Contraste centrado em 0.5 (cinza linear neutro do URP)
                    var valor = 0.5f + (f1 * 0.5f + f2 * 0.35f + f3 * 0.15f - 0.5f) * 0.72f;
                    valor = Mathf.Clamp01(valor);

                    pixels[y * tamanho + x] = new Color(valor, valor, valor, 1f);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Salva as texturas geradas como arquivos PNG no disco em Assets/Art/Textures.
        /// </summary>
        public static void SalvarNoDisco(string pastaDestino)
        {
            Directory.CreateDirectory(pastaDestino);

            SalvarPNG(ObterGrama(), Path.Combine(pastaDestino, "Grama_512.png"));
            SalvarPNG(ObterTerra(), Path.Combine(pastaDestino, "Terra_512.png"));
            SalvarPNG(ObterAreia(), Path.Combine(pastaDestino, "Areia_512.png"));
            SalvarPNG(ObterRocha(), Path.Combine(pastaDestino, "Rocha_512.png"));
            SalvarPNG(ObterNeve(), Path.Combine(pastaDestino, "Neve_512.png"));
            SalvarPNG(ObterDetalhes(), Path.Combine(pastaDestino, "Detalhe_Terreno.png"));
        }

        private static void SalvarPNG(Texture2D textura, string caminho)
        {
            try
            {
                var bytes = textura.EncodeToPNG();
                File.WriteAllBytes(caminho, bytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[Terreno] Não foi possível salvar PNG em {caminho}: {ex.Message}"
                );
            }
        }

        private static Texture2D CriarTexturaBase(string nome, int tamanho)
        {
            return new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, true)
            {
                name = nome,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
            };
        }

        /// <summary>
        /// Amostragem periódica toroidal de ruído para garantir que a textura se repita
        /// perfeitamente em todas as direções sem emendas visíveis.
        /// </summary>
        private static float RuidoToroidal(float u, float v, uint semente)
        {
            var uMod = u - Mathf.Floor(u);
            var vMod = v - Mathf.Floor(v);

            // Mapeia para um toro 4D via senos e cossenos
            var nx = Mathf.Cos(uMod * Mathf.PI * 2f);
            var ny = Mathf.Sin(uMod * Mathf.PI * 2f);
            var nz = Mathf.Cos(vMod * Mathf.PI * 2f);
            var nw = Mathf.Sin(vMod * Mathf.PI * 2f);

            var r1 = Sorteio((int)((nx + 1f) * 128f), (int)((ny + 1f) * 128f), semente);
            var r2 = Sorteio((int)((nz + 1f) * 128f), (int)((nw + 1f) * 128f), semente + 53u);

            return (r1 + r2) * 0.5f;
        }

        private static float Sorteio(int x, int z, uint semente)
        {
            unchecked
            {
                var h = (uint)x * 374761393u + (uint)z * 668265263u + semente * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFFu) / 16777216f;
            }
        }
    }
}
