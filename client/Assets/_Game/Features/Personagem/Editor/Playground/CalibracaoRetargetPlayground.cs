using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JogoDoGrupo.Client.Features.Personagem.Editor.Playground
{
    /// <summary>Reproduz a configuracao candidata, sem alterar o modelo/Avatar original.</summary>
    public static class CalibracaoRetargetPlayground
    {
        // Estimado do apoio da fonte: tornozelo ~0,128 m e segmento Foot-Toes 0,173 m.
        const float InclinacaoPeFonte = -47.5f;
        const string Pasta = CharacterPlayground.Pasta + "/Locomotion";

        public static void Gerar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Gerar calibracao fora do Play Mode.");
            foreach (var nome in new[] { "Idle", "Walking", "Running" })
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(Pasta + "/" + nome + ".fbx");
                var descricao = importer.humanDescription;
                if (
                    descricao.human == null
                    || !descricao.human.Any(h => h.boneName.StartsWith("mixamorig:"))
                )
                    throw new InvalidOperationException(
                        "A fonte mudou; auditar novamente antes de calibrar."
                    );
                var objeto = Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath)
                );
                objeto.name = nome + "Reference";
                try
                {
                    var transforms = objeto
                        .GetComponentsInChildren<Transform>(true)
                        .ToDictionary(t => t.name);
                    var animator = objeto.GetComponent<Animator>();
                    if (animator != null)
                        animator.enabled = false;
                    // Somente a referencia do Avatar de origem. Nenhuma curva ou FBX e reescrito.
                    foreach (var bone in descricao.skeleton)
                        if (transforms.TryGetValue(bone.name, out var t))
                        {
                            t.localPosition = bone.position;
                            t.localRotation = bone.rotation;
                            t.localScale = bone.scale;
                        }
                    Transform Humano(string nomeHumano) =>
                        transforms[descricao.human.Single(h => h.humanName == nomeHumano).boneName];
                    foreach (var lado in new[] { "Left", "Right" })
                    {
                        Humano(lado + "UpperLeg").rotation = Quaternion.LookRotation(
                            Vector3.forward,
                            Vector3.down
                        );
                        Humano(lado + "LowerLeg").rotation = Quaternion.LookRotation(
                            Vector3.forward,
                            Vector3.down
                        );
                        var angulo = InclinacaoPeFonte * Mathf.Deg2Rad;
                        var direcao = new Vector3(0, Mathf.Sin(angulo), Mathf.Cos(angulo));
                        Humano(lado + "Foot").rotation = Quaternion.LookRotation(
                            Vector3.Cross(Vector3.left, direcao),
                            direcao
                        );
                    }
                    descricao.skeleton = objeto
                        .GetComponentsInChildren<Transform>(true)
                        .Select(t => new SkeletonBone
                        {
                            name = t.name,
                            position = t.localPosition,
                            rotation = t.localRotation,
                            scale = t.localScale,
                        })
                        .ToArray();
                    var avatar = Gravar(
                        AvatarBuilder.BuildHumanAvatar(objeto, descricao),
                        nome + "Reference"
                    );
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = avatar;
                    importer.SaveAndReimport();
                }
                finally
                {
                    Object.DestroyImmediate(objeto);
                }
            }

            var modelo = AssetDatabase
                .GetDependencies(CharacterPlayground.PrefabInicial, true)
                .Single(p => p.EndsWith(".fbx"));
            var original = (ModelImporter)AssetImporter.GetAtPath(modelo);
            var humano = original.humanDescription;
            // Compativel com a amplitude da clavicula deste prototipo; nao muda
            // mapeamento, skeleton, pose de referencia, bind poses ou pesos.
            for (var n = 0; n < humano.human.Length; n++)
                if (humano.human[n].humanName.EndsWith("Shoulder"))
                    humano.human[n].limit = new HumanLimit
                    {
                        useDefaultValues = false,
                        min = new Vector3(0, -10, -10),
                        max = new Vector3(0, 10, 20),
                        center = Vector3.zero,
                    };
            var instancia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelo));
            instancia.name = "HumanMaleLocomotion";
            try
            {
                Gravar(
                    AvatarBuilder.BuildHumanAvatar(instancia, humano),
                    "HumanMaleLocomotionAvatar"
                );
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
            AssetDatabase.SaveAssets();
        }

        static Avatar Gravar(Avatar gerado, string nome)
        {
            if (!gerado.isValid || !gerado.isHuman)
            {
                Object.DestroyImmediate(gerado);
                throw new InvalidOperationException("Avatar candidato invalido: " + nome);
            }
            gerado.name = nome;
            var path = Pasta + "/" + nome + ".asset";
            var atual = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            if (atual == null)
            {
                AssetDatabase.CreateAsset(gerado, path);
                return gerado;
            }
            EditorUtility.CopySerialized(gerado, atual);
            EditorUtility.SetDirty(atual);
            Object.DestroyImmediate(gerado);
            return atual;
        }
    }
}
