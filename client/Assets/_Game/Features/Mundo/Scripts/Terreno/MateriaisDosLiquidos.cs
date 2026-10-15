using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>
    /// Materiais da água (transparente, cor por profundidade, normal em movimento) e da lava
    /// (emissiva, com crosta que corre). Os shaders moram em <c>Resources/Mundo/Shaders/</c> para
    /// entrarem no build; se faltarem, volta o Lit do URP com uma cor só.
    /// </summary>
    public static class MateriaisDosLiquidos
    {
        public const string ShaderDaAgua = "Mundo/Shaders/AguaMundo";
        public const string ShaderDaLava = "Mundo/Shaders/LavaMundo";

        public static Material Agua(Material modelo)
        {
            var shader = Resources.Load<Shader>(ShaderDaAgua);
            if (shader != null && shader.isSupported)
            {
                return new Material(shader)
                {
                    name = "Agua",
                    renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent,
                };
            }

            return Reserva(modelo, CoresDosBiomas.Agua, 0.85f, false);
        }

        public static Material Lava(Material modelo)
        {
            var shader = Resources.Load<Shader>(ShaderDaLava);
            if (shader != null && shader.isSupported)
            {
                return new Material(shader) { name = "Lava" };
            }

            return Reserva(modelo, CoresDosBiomas.LavaViva, 0.3f, true);
        }

        // Material de reserva: copia do modelo (ou Lit do URP) com uma cor só.
        private static Material Reserva(Material modelo, Color cor, float brilho, bool emissivo)
        {
            Material material;
            if (modelo != null)
            {
                material = new Material(modelo);
            }
            else
            {
                var padrao = Shader.Find("Universal Render Pipeline/Lit");
                if (padrao == null)
                {
                    return null;
                }

                material = new Material(padrao);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", cor);
            }

            material.color = cor;
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", brilho);
            }

            if (emissivo && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", cor * 1.6f);
            }

            return material;
        }
    }
}
