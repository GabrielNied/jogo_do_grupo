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
    }
}
