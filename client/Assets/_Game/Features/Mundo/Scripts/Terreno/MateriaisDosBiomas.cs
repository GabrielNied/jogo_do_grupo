using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Materiais da vegetação desenhada por instancing: uma cópia por material de origem e bioma
    /// (multiplicada pela cor do bioma), com o instancing ligado. Todas as peças do mesmo tipo e
    /// bioma dividem o mesmo material.
    /// </summary>
    public static class MateriaisDosBiomas
    {
        private static readonly Dictionary<(Material, Bioma), Material> _cache =
            new Dictionary<(Material, Bioma), Material>();

        /// <summary>Quantas cópias existem agora.</summary>
        public static int Quantidade => _cache.Count;

        public static void Limpar()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Cópia de <paramref name="original"/> para <paramref name="bioma"/>, com a cor das peças
        /// do bioma multiplicada e o instancing ligado.
        /// </summary>
        public static Material Para(Material original, Bioma bioma)
        {
            if (original == null)
            {
                return null;
            }

            var chave = (original, bioma);
            if (_cache.TryGetValue(chave, out var pronto) && pronto != null)
            {
                return pronto;
            }

            var cor = CoresDosBiomas.DasPecas(bioma);
            var copia = new Material(original)
            {
                name = $"{original.name}_{bioma}",
                enableInstancing = true,
            };
            ConfigurarRecorteDaFolhagem(copia);
            if (copia.HasProperty("_BaseColor"))
            {
                copia.SetColor("_BaseColor", copia.GetColor("_BaseColor") * cor);
            }
            else if (copia.HasProperty("_Color"))
            {
                copia.SetColor("_Color", copia.GetColor("_Color") * cor);
            }

            _cache[chave] = copia;
            return copia;
        }

        // O MegaKit traz folhas e flores em cartões com canal alpha. O material FBX padrão vem
        // opaco no URP e desenha o fundo preto desses cartões; aqui convertemos só esses mapas
        // para Lit com alpha clip, que também continua compatível com instancing.
        private static void ConfigurarRecorteDaFolhagem(Material material)
        {
            var textura = material.HasProperty("_BaseMap")
                ? material.GetTexture("_BaseMap")
                : material.mainTexture;
            if (textura == null || !UsaTransparencia(textura.name))
            {
                return;
            }

            var cor = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.color;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                return;
            }

            material.shader = lit;
            material.SetTexture("_BaseMap", textura);
            material.SetColor("_BaseColor", cor);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.45f);
            material.EnableKeyword("_ALPHATEST_ON");
        }

        private static bool UsaTransparencia(string nomeDaTextura)
        {
            return nomeDaTextura.StartsWith("Leaves")
                || nomeDaTextura == "Flowers"
                || nomeDaTextura == "Grass"
                || nomeDaTextura == "Mushrooms";
        }
    }
}
