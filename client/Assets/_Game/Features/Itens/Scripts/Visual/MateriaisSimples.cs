using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Visual
{
    /// <summary>
    /// Material de cor lisa para os desenhos de reserva feitos em código. Pede o shader do URP pelo
    /// nome em vez de copiar o <c>sharedMaterial</c> de uma primitiva: o material padrão da
    /// primitiva é o da engine, resolvido pelo pipeline só no editor, e no build saía sem a cor
    /// certa (#1199).
    /// </summary>
    internal static class MateriaisSimples
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int Metallic = Shader.PropertyToID("_Metallic");

        public static Material Criar(Color cor)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = cor };
            if (material.HasProperty(BaseColor))
            {
                material.SetColor(BaseColor, cor);
            }

            if (material.HasProperty(Smoothness))
            {
                material.SetFloat(Smoothness, 0.25f);
            }

            if (material.HasProperty(Metallic))
            {
                material.SetFloat(Metallic, 0f);
            }

            return material;
        }
    }
}
