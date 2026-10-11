using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs.Editor
{
    /// <summary>
    /// Lista clipes, malhas e materiais dos modelos das criaturas num arquivo de texto na pasta
    /// temporaria (apoio ao artista: o modo batch nao mostra o console).
    /// </summary>
    public static class InspetorDeModelos
    {
        public static void Listar()
        {
            var sb = new StringBuilder();
            foreach (var caminho in GeradorDeCriaturas.Modelos())
            {
                sb.AppendLine(caminho);
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(caminho))
                {
                    var extra = "";
                    if (o is AnimationClip c)
                        extra = " len=" + c.length + " loop=" + c.isLooping;
                    if (o is Material m)
                        extra = " cor=" + m.color + " shader=" + m.shader.name;
                    if (o is Mesh me)
                        extra = " bounds=" + me.bounds.size;
                    sb.AppendLine("  " + o.GetType().Name + ": " + o.name + extra);
                }
            }
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "inspetor-mobs.txt"), sb.ToString());
        }
    }
}
