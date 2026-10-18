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
        // Paleta Tipo 1 - Corcel Castanho
        private static readonly Color PeloCastanho = new Color(0.48f, 0.32f, 0.20f);
        private static readonly Color SecundariaCastanho = new Color(0.35f, 0.22f, 0.14f);
        private static readonly Color SelaCastanho = new Color(0.18f, 0.12f, 0.08f);
        private static readonly Color DetalheCastanho = new Color(0.85f, 0.68f, 0.25f);

        // Paleta Tipo 2 - Corcel Noturno
        private static readonly Color PeloNoturno = new Color(0.11f, 0.12f, 0.16f);
        private static readonly Color SecundariaNoturno = new Color(0.18f, 0.22f, 0.34f);
        private static readonly Color SelaNoturno = new Color(0.38f, 0.42f, 0.50f);
        private static readonly Color DetalheNoturno = new Color(0.25f, 0.85f, 1.00f);

        private readonly List<Material> _materiais = new List<Material>();
        private Transform _raizVisual;

        public int Tipo { get; private set; }

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
            Tipo = tipo;
            ObterPaleta(
                tipo,
                out var corPelo,
                out var corSecundaria,
                out var corSela,
                out var corDetalhe
            );

            _raizVisual = new GameObject("Modelo").transform;
            _raizVisual.SetParent(transform, false);
            _raizVisual.localPosition = new Vector3(0f, 0f, 0f);

            // Tronco / Corpo da montaria
            Parte(
                PrimitiveType.Capsule,
                "Tronco",
                new Vector3(0f, 0.75f, 0f),
                new Vector3(0.7f, 0.7f, 1.4f),
                corPelo
            ).localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Sela
            Parte(
                PrimitiveType.Cube,
                "Sela",
                new Vector3(0f, 0.88f, -0.05f),
                new Vector3(0.65f, 0.15f, 0.65f),
                corSela
            );
            Parte(
                PrimitiveType.Cylinder,
                "Manta",
                new Vector3(0f, 0.82f, -0.05f),
                new Vector3(0.72f, 0.06f, 0.72f),
                corDetalhe
            );

            // Pescoço e Cabeça
            Parte(
                PrimitiveType.Capsule,
                "Pescoco",
                new Vector3(0f, 1.15f, 0.65f),
                new Vector3(0.35f, 0.45f, 0.35f),
                corPelo
            ).localRotation = Quaternion.Euler(30f, 0f, 0f);
            Parte(
                PrimitiveType.Cube,
                "Cabeca",
                new Vector3(0f, 1.45f, 0.85f),
                new Vector3(0.35f, 0.35f, 0.55f),
                corSecundaria
            );

            // Focinho
            Parte(
                PrimitiveType.Cube,
                "Focinho",
                new Vector3(0f, 1.35f, 1.15f),
                new Vector3(0.25f, 0.22f, 0.35f),
                corPelo
            );

            // Orelhas
            Parte(
                PrimitiveType.Cube,
                "OrelhaEsq",
                new Vector3(-0.16f, 1.68f, 0.75f),
                new Vector3(0.08f, 0.22f, 0.12f),
                corPelo
            );
            Parte(
                PrimitiveType.Cube,
                "OrelhaDir",
                new Vector3(0.16f, 1.68f, 0.75f),
                new Vector3(0.08f, 0.22f, 0.12f),
                corPelo
            );

            if (tipo == 2 || tipo == 7)
            {
                Parte(
                    PrimitiveType.Cylinder,
                    "ChifreMistico",
                    new Vector3(0f, 1.72f, 0.95f),
                    new Vector3(0.07f, 0.22f, 0.07f),
                    corDetalhe
                ).localRotation = Quaternion.Euler(45f, 0f, 0f);
            }

            if (tipo == 4)
            {
                Parte(
                    PrimitiveType.Cube,
                    "Cristal",
                    new Vector3(0f, 1.05f, 0.2f),
                    new Vector3(0.15f, 0.35f, 0.15f),
                    corDetalhe
                );
            }

            // 4 Patas
            CriarPata("PataDianteiraEsq", new Vector3(-0.28f, 0.4f, 0.5f), corSecundaria, corSela);
            CriarPata("PataDianteiraDir", new Vector3(0.28f, 0.4f, 0.5f), corSecundaria, corSela);
            CriarPata("PataTraseiraEsq", new Vector3(-0.28f, 0.4f, -0.5f), corSecundaria, corSela);
            CriarPata("PataTraseiraDir", new Vector3(0.28f, 0.4f, -0.5f), corSecundaria, corSela);

            // Cauda
            Parte(
                PrimitiveType.Capsule,
                "Cauda",
                new Vector3(0f, 0.7f, -0.85f),
                new Vector3(0.12f, 0.4f, 0.12f),
                corSecundaria
            ).localRotation = Quaternion.Euler(-35f, 0f, 0f);
        }

        private static void ObterPaleta(
            int tipo,
            out Color corPelo,
            out Color corSecundaria,
            out Color corSela,
            out Color corDetalhe
        )
        {
            switch (tipo)
            {
                case 2:
                    corPelo = PeloNoturno;
                    corSecundaria = SecundariaNoturno;
                    corSela = SelaNoturno;
                    corDetalhe = DetalheNoturno;
                    return;
                case 3:
                    corPelo = new Color(0.45f, 0.44f, 0.42f);
                    corSecundaria = new Color(0.32f, 0.30f, 0.28f);
                    corSela = new Color(0.22f, 0.18f, 0.14f);
                    corDetalhe = new Color(0.75f, 0.72f, 0.65f);
                    return;
                case 4:
                    corPelo = new Color(0.20f, 0.22f, 0.28f);
                    corSecundaria = new Color(0.35f, 0.38f, 0.45f);
                    corSela = SelaNoturno;
                    corDetalhe = new Color(0.55f, 0.85f, 0.95f);
                    return;
                case 5:
                    corPelo = new Color(0.75f, 0.82f, 0.92f);
                    corSecundaria = new Color(0.55f, 0.65f, 0.78f);
                    corSela = new Color(0.28f, 0.32f, 0.38f);
                    corDetalhe = new Color(0.90f, 0.95f, 1f);
                    return;
                case 6:
                    corPelo = new Color(0.35f, 0.33f, 0.32f);
                    corSecundaria = new Color(0.48f, 0.42f, 0.38f);
                    corSela = new Color(0.25f, 0.22f, 0.20f);
                    corDetalhe = new Color(0.95f, 0.45f, 0.20f);
                    return;
                case 7:
                    corPelo = new Color(0.14f, 0.15f, 0.22f);
                    corSecundaria = new Color(0.22f, 0.24f, 0.35f);
                    corSela = SelaNoturno;
                    corDetalhe = new Color(0.95f, 0.85f, 0.35f);
                    return;
                case 8:
                    corPelo = new Color(0.52f, 0.48f, 0.40f);
                    corSecundaria = new Color(0.40f, 0.36f, 0.30f);
                    corSela = SelaCastanho;
                    corDetalhe = DetalheCastanho;
                    return;
                case 9:
                    corPelo = new Color(0.60f, 0.42f, 0.25f);
                    corSecundaria = new Color(0.25f, 0.18f, 0.12f);
                    corSela = SelaCastanho;
                    corDetalhe = new Color(0.80f, 0.65f, 0.22f);
                    return;
                default:
                    corPelo = PeloCastanho;
                    corSecundaria = SecundariaCastanho;
                    corSela = SelaCastanho;
                    corDetalhe = DetalheCastanho;
                    return;
            }
        }

        private void CriarPata(string nome, Vector3 posicao, Color corPata, Color corCasco)
        {
            Parte(PrimitiveType.Cylinder, nome, posicao, new Vector3(0.16f, 0.45f, 0.16f), corPata);
            Parte(
                PrimitiveType.Cube,
                nome + "_Casco",
                posicao - new Vector3(0f, 0.4f, 0f),
                new Vector3(0.18f, 0.1f, 0.2f),
                corCasco
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
