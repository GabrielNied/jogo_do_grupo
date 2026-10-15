using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Montarias
{
    /// <summary>
    /// Visual 3D procedural da montaria: monta um companheiro de montaria estilizado
    /// acoplado à base do avatar, ajustando a posição do cavaleiro e acompanhando
    /// seu movimento sem colisor físico.
    /// </summary>
    public sealed class VisualDaMontaria : MonoBehaviour
    {
        private static readonly Color CorDoPelo = new Color(0.48f, 0.32f, 0.20f); // Castanho aconchegante
        private static readonly Color CorSecundaria = new Color(0.35f, 0.22f, 0.14f);
        private static readonly Color CorDaSela = new Color(0.18f, 0.12f, 0.08f); // Couro escuro
        private static readonly Color CorDoDetalhe = new Color(0.85f, 0.68f, 0.25f); // Dourado metálico

        private readonly List<Material> _materiais = new List<Material>();
        private Transform _raizVisual;

        /// <summary>
        /// Instancia e conecta o visual da montaria ao avatar especificado.
        /// </summary>
        public static VisualDaMontaria Criar(Transform dono, int tipo = 1)
        {
            var objeto = new GameObject("VisualMontaria");
            objeto.transform.SetParent(dono, false);
            objeto.transform.localPosition = Vector3.zero;
            objeto.transform.localRotation = Quaternion.identity;

            var visual = objeto.AddComponent<VisualDaMontaria>();
            visual.Construir(tipo);
            return visual;
        }

        private void Construir(int tipo)
        {
            _raizVisual = new GameObject("Modelo").transform;
            _raizVisual.SetParent(transform, false);
            _raizVisual.localPosition = new Vector3(0f, 0f, 0f);

            // Tronco / Corpo da montaria
            Parte(
                PrimitiveType.Capsule,
                "Tronco",
                new Vector3(0f, 0.75f, 0f),
                new Vector3(0.7f, 0.7f, 1.4f),
                CorDoPelo
            ).localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Sela
            Parte(
                PrimitiveType.Cube,
                "Sela",
                new Vector3(0f, 0.88f, -0.05f),
                new Vector3(0.65f, 0.15f, 0.65f),
                CorDaSela
            );
            Parte(
                PrimitiveType.Cylinder,
                "Manta",
                new Vector3(0f, 0.82f, -0.05f),
                new Vector3(0.72f, 0.06f, 0.72f),
                CorDoDetalhe
            );

            // Pescoço e Cabeça
            Parte(
                PrimitiveType.Capsule,
                "Pescoco",
                new Vector3(0f, 1.15f, 0.65f),
                new Vector3(0.35f, 0.45f, 0.35f),
                CorDoPelo
            ).localRotation = Quaternion.Euler(30f, 0f, 0f);
            Parte(
                PrimitiveType.Cube,
                "Cabeca",
                new Vector3(0f, 1.45f, 0.85f),
                new Vector3(0.35f, 0.35f, 0.55f),
                CorSecundaria
            );

            // Focinho
            Parte(
                PrimitiveType.Cube,
                "Focinho",
                new Vector3(0f, 1.35f, 1.15f),
                new Vector3(0.25f, 0.22f, 0.35f),
                CorDoPelo
            );

            // Orelhas
            Parte(
                PrimitiveType.Cube,
                "OrelhaEsq",
                new Vector3(-0.16f, 1.68f, 0.75f),
                new Vector3(0.08f, 0.22f, 0.12f),
                CorDoPelo
            );
            Parte(
                PrimitiveType.Cube,
                "OrelhaDir",
                new Vector3(0.16f, 1.68f, 0.75f),
                new Vector3(0.08f, 0.22f, 0.12f),
                CorDoPelo
            );

            // 4 Patas
            CriarPata("PataDianteiraEsq", new Vector3(-0.28f, 0.4f, 0.5f));
            CriarPata("PataDianteiraDir", new Vector3(0.28f, 0.4f, 0.5f));
            CriarPata("PataTraseiraEsq", new Vector3(-0.28f, 0.4f, -0.5f));
            CriarPata("PataTraseiraDir", new Vector3(0.28f, 0.4f, -0.5f));

            // Cauda
            Parte(
                PrimitiveType.Capsule,
                "Cauda",
                new Vector3(0f, 0.7f, -0.85f),
                new Vector3(0.12f, 0.4f, 0.12f),
                CorSecundaria
            ).localRotation = Quaternion.Euler(-35f, 0f, 0f);
        }

        private void CriarPata(string nome, Vector3 posicao)
        {
            Parte(
                PrimitiveType.Cylinder,
                nome,
                posicao,
                new Vector3(0.16f, 0.45f, 0.16f),
                CorSecundaria
            );
            Parte(
                PrimitiveType.Cube,
                nome + "_Casco",
                posicao - new Vector3(0f, 0.4f, 0f),
                new Vector3(0.18f, 0.1f, 0.2f),
                CorDaSela
            );
        }

        private Transform Parte(
            PrimitiveType forma,
            string nome,
            Vector3 local,
            Vector3 escala,
            Color cor
        )
        {
            var parte = GameObject.CreatePrimitive(forma);
            parte.name = nome;
            RemoverColisor(parte);

            parte.transform.SetParent(_raizVisual, false);
            parte.transform.localPosition = local;
            parte.transform.localScale = escala;

            var renderer = parte.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = NovoMaterial(cor);
            }

            return parte.transform;
        }

        private Material NovoMaterial(Color cor)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader) { color = cor };
            _materiais.Add(mat);
            return mat;
        }

        private static void RemoverColisor(GameObject objeto)
        {
            var colisor = objeto.GetComponent<Collider>();
            if (colisor == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(colisor);
            }
            else
            {
                DestroyImmediate(colisor);
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < _materiais.Count; i++)
            {
                if (_materiais[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(_materiais[i]);
                    }
                    else
                    {
                        DestroyImmediate(_materiais[i]);
                    }
                }
            }

            _materiais.Clear();
        }
    }
}
