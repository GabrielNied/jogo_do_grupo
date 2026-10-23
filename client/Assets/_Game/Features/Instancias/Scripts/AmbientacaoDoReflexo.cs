using UnityEngine;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>
    /// Peças de ambientação da Mina do Reflexo do livro, geradas por código: cristais (malha
    /// hexagonal de ponta) e orbes de luz flutuantes. Sem modelos externos nem colisor: só
    /// leitura visual (luz e cristais), o chão e as paredes continuam sendo do construtor.
    /// </summary>
    internal static class AmbientacaoDoReflexo
    {
        public static readonly Color CorDoGelo = new Color(0.55f, 0.82f, 1f);
        public static readonly Color CorDoOrbe = new Color(0.75f, 0.92f, 1f);
        public static readonly Color CorDoChefe = new Color(0.62f, 0.5f, 1f);
        public static readonly Color CorDoCristalApagado = new Color(0.14f, 0.17f, 0.24f);

        private static Mesh _malhaDoCristal;

        /// <summary>Um cristal em pé no chão; apagado ou aceso conforme o material.</summary>
        public static GameObject Cristal(
            Transform pai,
            Material material,
            Vector3 posicao,
            float altura,
            float largura,
            float inclinacaoGraus,
            float giroGraus
        )
        {
            var cristal = new GameObject("Cristal");
            cristal.transform.SetParent(pai, false);
            cristal.transform.position = posicao;
            cristal.transform.rotation =
                Quaternion.Euler(0f, giroGraus, 0f) * Quaternion.Euler(inclinacaoGraus, 0f, 0f);
            cristal.transform.localScale = new Vector3(largura, altura, largura);
            cristal.AddComponent<MeshFilter>().sharedMesh = MalhaDoCristal();
            cristal.AddComponent<MeshRenderer>().sharedMaterial = material;
            return cristal;
        }

        /// <summary>Um orbe de luz parado no ar, com leve flutuação e uma luz pontual opcional.</summary>
        public static GameObject Orbe(
            Transform pai,
            Material material,
            Vector3 posicao,
            float raio,
            Color corDaLuz,
            float intensidade,
            float alcance
        )
        {
            var orbe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbe.name = "Orbe";
            orbe.transform.SetParent(pai, false);
            orbe.transform.position = posicao;
            orbe.transform.localScale = Vector3.one * (raio * 2f);
            orbe.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(orbe.GetComponent<Collider>());
            if (intensidade > 0f)
            {
                var luz = new GameObject("LuzDoOrbe");
                luz.transform.SetParent(orbe.transform, false);
                var componente = luz.AddComponent<Light>();
                componente.type = LightType.Point;
                componente.color = corDaLuz;
                componente.intensity = intensidade;
                componente.range = alcance;
                componente.shadows = LightShadows.None;
            }

            orbe.AddComponent<OrbeFlutuante>();
            return orbe;
        }

        /// <summary>
        /// Material de cristal: cor base e, se <paramref name="brilho"/> &gt; 0, emissão da mesma cor.
        /// </summary>
        public static Material MaterialDeCristal(Color cor, float brilho)
        {
            var material = MaterialUrp(cor);
            if (brilho > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", cor * brilho);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.85f);
            }

            return material;
        }

        // Shader do URP pelo nome (#1199).
        public static Material MaterialUrp(Color cor)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = cor };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", cor);
            }

            return material;
        }

        // Prisma hexagonal de raio 0,5 e altura 1 (base em y=0) com ponta em pirâmide.
        private static Mesh MalhaDoCristal()
        {
            if (_malhaDoCristal != null)
            {
                return _malhaDoCristal;
            }

            const int lados = 6;
            const float raio = 0.5f;
            const float alturaDoCorpo = 0.7f;
            var vertices = new Vector3[(lados * 4) + 1];
            var triangulos = new int[lados * 12];
            // Para cada face lateral: 4 vértices próprios (sombreamento facetado).
            var t = 0;
            for (var i = 0; i < lados; i++)
            {
                var a0 = i * Mathf.PI * 2f / lados;
                var a1 = (i + 1) * Mathf.PI * 2f / lados;
                var p0 = new Vector3(Mathf.Cos(a0) * raio, 0f, Mathf.Sin(a0) * raio);
                var p1 = new Vector3(Mathf.Cos(a1) * raio, 0f, Mathf.Sin(a1) * raio);
                var b = i * 4;
                vertices[b] = p0;
                vertices[b + 1] = p1;
                vertices[b + 2] = new Vector3(p1.x, alturaDoCorpo, p1.z);
                vertices[b + 3] = new Vector3(p0.x, alturaDoCorpo, p0.z);
                triangulos[t++] = b;
                triangulos[t++] = b + 3;
                triangulos[t++] = b + 1;
                triangulos[t++] = b + 1;
                triangulos[t++] = b + 3;
                triangulos[t++] = b + 2;
            }

            var ponta = lados * 4;
            vertices[ponta] = new Vector3(0f, 1f, 0f);
            // Pirâmide da ponta: reaproveita os vértices do topo de cada face (b+2, b+3).
            for (var i = 0; i < lados; i++)
            {
                var b = i * 4;
                triangulos[t++] = b + 3;
                triangulos[t++] = ponta;
                triangulos[t++] = b + 2;
            }

            var malha = new Mesh { name = "CristalDoReflexo" };
            malha.vertices = vertices;
            malha.triangles = triangulos;
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            _malhaDoCristal = malha;
            return malha;
        }
    }

    /// <summary>Faz o orbe de luz subir e descer devagar, como se respirasse.</summary>
    internal sealed class OrbeFlutuante : MonoBehaviour
    {
        private const float Amplitude = 0.25f;
        private const float Velocidade = 0.8f;

        private Vector3 _base;
        private float _fase;

        private void Start()
        {
            _base = transform.position;
            _fase = (_base.x * 0.37f) + (_base.z * 0.21f);
        }

        private void Update()
        {
            var y = Mathf.Sin((Time.time * Velocidade) + _fase) * Amplitude;
            transform.position = _base + new Vector3(0f, y, 0f);
        }
    }
}
