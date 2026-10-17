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
        private static Texture2D _texturaNormalRocha;
        private static Texture2D _texturaNeve;
        private static Texture2D _texturaLama;
        private static Texture2D _texturaDetalhes;

        public static Texture2D ObterGrama() => _texturaGrama ??= CriarTexturaGrama();

        public static Texture2D ObterTerra() => _texturaTerra ??= CriarTexturaTerra();

        public static Texture2D ObterAreia() => _texturaAreia ??= CriarTexturaAreia();

        public static Texture2D ObterRocha() => _texturaRocha ??= CriarTexturaRocha();

        public static Texture2D ObterNormalRocha() => _texturaNormalRocha ??= CriarNormalMapRocha();

        public static Texture2D ObterNeve() => _texturaNeve ??= CriarTexturaNeve();

        public static Texture2D ObterLama() => _texturaLama ??= CriarTexturaLama();

        public static Texture2D ObterDetalhes() => _texturaDetalhes ??= CriarTexturaDetalhes();

        public static void LimparCache()
        {
            _texturaGrama = null;
            _texturaTerra = null;
            _texturaAreia = null;
            _texturaRocha = null;
            _texturaNormalRocha = null;
            _texturaNeve = null;
            _texturaLama = null;
            _texturaDetalhes = null;
        }

        /// Textura contínua de grama de prado rica e viva (estilo RPG de ação):
        /// tufos orgânicos, lâminas finas, trevos e variação natural sem listras diagonais.
        /// </summary>
        public static Texture2D CriarTexturaGrama(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Grama_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var verdeRaiz = new Color(0.18f, 0.34f, 0.11f);
            var verdeBase = new Color(0.30f, 0.56f, 0.18f);
            var verdePonta = new Color(0.39f, 0.63f, 0.21f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    // 1. Manchas isotrópicas amplas de fertilidade do prado
                    var clareira = FbmPeriodico(u, v, 4, 3, 101u);

                    // Tufos largos e pouco contrastados: o chão lê como grama, não como bolinhas.
                    var tufo1 = FbmPeriodico(u, v, 12, 4, 211u);
                    var tufo2 = FbmPeriodico(u, v, 20, 3, 317u);
                    var tufo = tufo1 * 0.7f + tufo2 * 0.3f;

                    // 3. Fibras finas de lâminas de alta frequência para nitidez de perto
                    var fibras = FbmPeriodico(u, v, 48, 2, 431u);

                    var t = tufo * 0.8f + fibras * 0.2f;
                    var cor = Color.Lerp(verdeRaiz, verdeBase, t);
                    cor = Color.Lerp(cor, verdePonta, Mathf.Pow(tufo, 2.2f) * 0.28f);
                    cor = Color.Lerp(cor, verdeBase, clareira * 0.12f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de terra rica com torrões, partículas de solo,
        /// seixos dispersos e micro-cascalho.
        /// </summary>
        public static Texture2D CriarTexturaTerra(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Terra_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var terraEscura = new Color(0.28f, 0.20f, 0.13f);
            var terraMedia = new Color(0.42f, 0.31f, 0.20f);
            var terraClara = new Color(0.55f, 0.42f, 0.28f);
            var corSeixo = new Color(0.60f, 0.57f, 0.52f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    var baseSolo = FbmPeriodico(u, v, 6, 4, 501u);
                    var microGrao = FbmPeriodico(u, v, 28, 3, 509u);

                    // Seixos e pequenas pedras naturais no solo
                    VoronoiPeriodico(u, v, 18, 601u, out var dSeixo, out _);
                    var seixo = Mathf.Clamp01(1f - dSeixo * 2.8f);

                    var cor = Color.Lerp(terraEscura, terraMedia, baseSolo);
                    cor = Color.Lerp(cor, terraClara, microGrao * 0.35f);
                    cor = Color.Lerp(cor, corSeixo, seixo * 0.85f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de areia dourada de praia e dunas com ondulações suaves
        /// e brilho granular sem marcas artificiais.
        /// </summary>
        public static Texture2D CriarTexturaAreia(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Areia_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var areiaCava = new Color(0.78f, 0.66f, 0.45f);
            var areiaMedia = new Color(0.88f, 0.77f, 0.54f);
            var areiaCrista = new Color(0.96f, 0.87f, 0.65f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    // Ondulações de vento suaves e orgânicas
                    var perturbacao = FbmPeriodico(u, v, 4, 3, 701u) * 0.12f;
                    var ripple =
                        Mathf.Sin((u * 12f + v * 3f + perturbacao * 6f) * Mathf.PI * 2f) * 0.5f
                        + 0.5f;
                    var grao = FbmPeriodico(u, v, 32, 3, 719u);

                    var cor = Color.Lerp(areiaCava, areiaMedia, ripple);
                    cor = Color.Lerp(cor, areiaCrista, Mathf.Pow(ripple, 2.0f) * 0.45f);
                    cor = Color.Lerp(cor, areiaMedia, grao * 0.2f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de rocha com superfície mineral lisa e limpa (estilo RPG de ação),
        /// deixando o relevo 3D modelado se destacar através do normal map com ondas serrilhadas.
        /// </summary>
        public static Texture2D CriarTexturaRocha(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Rocha_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var rochaSombra = new Color(0.40f, 0.42f, 0.44f);
            var rochaMedia = new Color(0.54f, 0.56f, 0.58f);
            var rochaClara = new Color(0.66f, 0.68f, 0.70f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    // Altura da superfície serrilhada
                    var h = AlturaRochaSuperficie(u, v);

                    // Superfície lisa mineral com sombreamento tonal suave acompanhando os degraus
                    var cor = Color.Lerp(rochaSombra, rochaMedia, h);
                    cor = Color.Lerp(cor, rochaClara, Mathf.Pow(h, 2.2f) * 0.45f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Onda serrilhada periódica contínua em [0, 1):
        /// sobe suavemente pela prateleira de pedra e cai em degrau íngreme/abrupto.
        /// </summary>
        private static float OndaSerrilhadaPeriodica(float t)
        {
            var ciclo = t - Mathf.Floor(t);
            if (ciclo < 0.78f)
            {
                // Rampa da prateleira de pedra
                return (ciclo / 0.78f) * 0.95f;
            }
            else
            {
                // Queda abrupta e serrilhada (face vertical do degrau)
                var q = (ciclo - 0.78f) / 0.22f;
                return 0.95f * (1f - q * q * (3f - 2f * q));
            }
        }

        /// <summary>
        /// Perfil de relevo 3D modelado para o normal map:
        /// ondas serrilhadas com degraus geológicos e facetas angulares de pedra.
        /// </summary>
        private static float AlturaRochaSuperficie(float u, float v)
        {
            // 1. Dobras geológicas orgânicas e quebra angular nos estratos
            var dobra = FbmPeriodico(u, v, 3, 2, 809u) * 0.22f;
            var serrilhaHorizontal = Mathf.Abs((u * 6f + dobra * 2f) % 1f - 0.5f) * 0.16f;

            // 2. Ondas serrilhadas primárias (5 degraus/prateleiras de rocha)
            var t1 = v * 5f + dobra * 1.6f + serrilhaHorizontal;
            var degrauPrimario = OndaSerrilhadaPeriodica(t1);

            // 3. Estratificação serrilhada secundária mais fina (10 degraus)
            var t2 = v * 10f - dobra * 0.8f + serrilhaHorizontal * 0.5f;
            var degrauSecundario = OndaSerrilhadaPeriodica(t2);

            // 4. Facetas angulares de quebra de bloco de pedra
            var blocos = FbmPeriodico(u * 2f + v * 0.5f, v * 2f - u * 0.5f, 4, 2, 853u);

            var relevo = degrauPrimario * 0.65f + degrauSecundario * 0.25f + blocos * 0.10f;
            return Mathf.Clamp01(relevo);
        }

        /// <summary>
        /// Normal map contínuo (tangent space) para rugosidade modelada 3D da rocha:
        /// ressalta fisicamente as camadas e facetas sob a luz direcional.
        /// </summary>
        public static Texture2D CriarNormalMapRocha(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Rocha_Normal_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var escalaForca = 5.2f;

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                var vMenos = (float)((y - 1 + tamanho) % tamanho) / tamanho;
                var vMais = (float)((y + 1) % tamanho) / tamanho;

                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;
                    var uMenos = (float)((x - 1 + tamanho) % tamanho) / tamanho;
                    var uMais = (float)((x + 1) % tamanho) / tamanho;

                    var hE = AlturaRochaSuperficie(uMenos, v);
                    var hD = AlturaRochaSuperficie(uMais, v);
                    var hB = AlturaRochaSuperficie(u, vMenos);
                    var hC = AlturaRochaSuperficie(u, vMais);

                    var du = (hD - hE) * escalaForca;
                    var dv = (hC - hB) * escalaForca;

                    var normal = new Vector3(-du, -dv, 1f).normalized;

                    pixels[y * tamanho + x] = new Color32(
                        (byte)
                            Mathf.Clamp(Mathf.RoundToInt((normal.x * 0.5f + 0.5f) * 255f), 0, 255),
                        (byte)
                            Mathf.Clamp(Mathf.RoundToInt((normal.y * 0.5f + 0.5f) * 255f), 0, 255),
                        (byte)
                            Mathf.Clamp(Mathf.RoundToInt((normal.z * 0.5f + 0.5f) * 255f), 0, 255),
                        255
                    );
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de neve limpa e cristalina sem marcas axiais ou pontilhados em grelha.
        /// </summary>
        public static Texture2D CriarTexturaNeve(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Neve_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var neveSombra = new Color(0.85f, 0.88f, 0.94f);
            var neveMedia = new Color(0.95f, 0.96f, 0.99f);
            var neveLuz = new Color(1.0f, 1.0f, 1.0f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    // Manto de neve suave e uniforme
                    var manto = FbmPeriodico(u, v, 4, 3, 967u);
                    var vento =
                        Mathf.Sin((u * 6f + v * 2f + manto * 2.5f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    var microCristal = FbmPeriodico(u, v, 32, 2, 991u);

                    var cor = Color.Lerp(neveSombra, neveMedia, vento * 0.65f + manto * 0.35f);
                    cor = Color.Lerp(cor, neveLuz, Mathf.Pow(microCristal, 2.5f) * 0.35f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Textura contínua de lama escura e úmida do pântano: poças lisas, raízes e torrões.
        /// </summary>
        public static Texture2D CriarTexturaLama(int tamanho = ResolucaoPadrao)
        {
            var tex = CriarTexturaBase("Terreno_Lama_512", tamanho);
            var pixels = new Color32[tamanho * tamanho];

            var lamaFundo = new Color(0.14f, 0.12f, 0.08f);
            var lamaMedia = new Color(0.24f, 0.20f, 0.12f);
            var lamaSeca = new Color(0.36f, 0.30f, 0.19f);
            var limo = new Color(0.20f, 0.27f, 0.12f);

            for (var y = 0; y < tamanho; y++)
            {
                var v = (float)y / tamanho;
                for (var x = 0; x < tamanho; x++)
                {
                    var u = (float)x / tamanho;

                    var base0 = FbmPeriodico(u, v, 5, 4, 1103u);
                    var torrao = FbmPeriodico(u, v, 20, 3, 1109u);
                    VoronoiPeriodico(u, v, 9, 1117u, out var dRachadura, out _);
                    var rachadura = Mathf.Clamp01(1f - dRachadura * 3.2f);
                    var mancha = Mathf.Clamp01(FbmPeriodico(u, v, 7, 2, 1123u) - 0.45f) * 1.8f;

                    var cor = Color.Lerp(lamaFundo, lamaMedia, base0);
                    cor = Color.Lerp(cor, lamaSeca, torrao * 0.35f * rachadura);
                    cor = Color.Lerp(cor, limo, mancha * 0.5f);

                    pixels[y * tamanho + x] = cor;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true, false);
            return tex;
        }

        /// <summary>
        /// Mapa de micro-detalhe albedo centrado em cinza linear (0.5), suave e orgânico.
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

                    var n1 = FbmPeriodico(u, v, 16, 4, 1013u);
                    var n2 = FbmPeriodico(u, v, 48, 3, 1049u);
                    var valor = 0.5f + (n1 * 0.6f + n2 * 0.4f - 0.5f) * 0.32f;
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
            SalvarPNG(ObterNormalRocha(), Path.Combine(pastaDestino, "Rocha_Normal_512.png"));
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
        /// Ruído 2D periódico contínuo C2 (interpolação quíntica), 100% sem emendas (seamless)
        /// no espaço toroidal [0, 1) x [0, 1). Sem dependência de senos 1D ou artefatos axiais.
        /// </summary>
        private static float Ruido2DPeriodico(float x, float y, int periodo, uint semente)
        {
            var p = periodo > 1 ? periodo : 1;
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);

            var x0 = ix % p;
            if (x0 < 0)
                x0 += p;
            var y0 = iy % p;
            if (y0 < 0)
                y0 += p;

            var x1 = (x0 + 1) % p;
            var y1 = (y0 + 1) % p;

            var fx = x - Mathf.Floor(x);
            var fy = y - Mathf.Floor(y);

            // Curva quíntica C2 suave: 6t^5 - 15t^4 + 10t^3
            var sx = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);
            var sy = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);

            var h00 = Hash2D(x0, y0, semente);
            var h10 = Hash2D(x1, y0, semente);
            var h01 = Hash2D(x0, y1, semente);
            var h11 = Hash2D(x1, y1, semente);

            var v0 = Mathf.Lerp(h00, h10, sx);
            var v1 = Mathf.Lerp(h01, h11, sx);
            return Mathf.Lerp(v0, v1, sy);
        }

        private static float FbmPeriodico(
            float u,
            float v,
            int periodoBase,
            int oitavas,
            uint semente
        )
        {
            var total = 0f;
            var amplitude = 1f;
            var maximo = 0f;
            var freq = 1f;
            var periodo = periodoBase;

            for (var o = 0; o < oitavas; o++)
            {
                total +=
                    Ruido2DPeriodico(
                        u * freq * periodoBase,
                        v * freq * periodoBase,
                        periodo,
                        semente + (uint)(o * 101)
                    ) * amplitude;
                maximo += amplitude;
                amplitude *= 0.5f;
                freq *= 2f;
                periodo *= 2;
            }

            return total / maximo;
        }

        /// <summary>
        /// Ruído celular periódico (Voronoi/Worley) no espaço toroidal [0, 1) x [0, 1).
        /// Devolve a distância do ponto ao centro celular mais próximo (F1) e ao segundo mais próximo (F2).
        /// F1 modela pedras e seixos; F2 - F1 modela fendas e fraturas minerais.
        /// </summary>
        private static void VoronoiPeriodico(
            float u,
            float v,
            int celulas,
            uint semente,
            out float f1,
            out float f2
        )
        {
            f1 = 10f;
            f2 = 10f;

            var px = u * celulas;
            var py = v * celulas;

            var cx = Mathf.FloorToInt(px);
            var cy = Mathf.FloorToInt(py);

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var nx = (cx + dx) % celulas;
                    if (nx < 0)
                        nx += celulas;
                    var ny = (cy + dy) % celulas;
                    if (ny < 0)
                        ny += celulas;

                    var rx = Hash2D(nx, ny, semente);
                    var ry = Hash2D(nx, ny, semente + 7919u);

                    var pontoX = (cx + dx) + rx * 0.75f + 0.125f;
                    var pontoY = (cy + dy) + ry * 0.75f + 0.125f;

                    var distX = pontoX - px;
                    var distY = pontoY - py;
                    var d = Mathf.Sqrt(distX * distX + distY * distY);

                    if (d < f1)
                    {
                        f2 = f1;
                        f1 = d;
                    }
                    else if (d < f2)
                    {
                        f2 = d;
                    }
                }
            }
        }

        private static float Hash2D(int x, int y, uint semente)
        {
            unchecked
            {
                var h = (uint)x * 374761393u + (uint)y * 668265263u + semente * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFFu) / 16777216f;
            }
        }
    }
}
