using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// O rastro de um golpe que não pegou ninguém: um leque no chão, na abertura e no alcance do
    /// volume do golpe (<see cref="RegrasAtaque"/>), que aparece e some em
    /// <see cref="ConfigSensacaoCombate.RastroSegundos"/>. É só retorno visual: quem acertou o quê é o servidor.
    /// </summary>
    internal sealed class RastroDoGolpe : MonoBehaviour
    {
        public const float AlturaDoChao = 0.05f;
        public const int Segmentos = 12;
        public const float OpacidadeInicial = 0.45f;

        private static readonly Color CorDoRastro = new Color(0.95f, 0.92f, 0.82f, 1f);

        private Material _material;
        private float _idade;

        /// <summary>Cria o rastro a partir dos pés do atacante, olhando para <paramref name="direcaoYGraus"/>.</summary>
        public static void Criar(Vector3 pes, float direcaoYGraus)
        {
            var objeto = new GameObject("Rastro do golpe");
            objeto.transform.SetPositionAndRotation(
                pes + Vector3.up * AlturaDoChao,
                Quaternion.Euler(0f, direcaoYGraus, 0f)
            );
            var filtro = objeto.AddComponent<MeshFilter>();
            filtro.sharedMesh = MontarLeque();
            var render = objeto.AddComponent<MeshRenderer>();
            var rastro = objeto.AddComponent<RastroDoGolpe>();
            rastro._material = new Material(Shader.Find("Sprites/Default"));
            render.sharedMaterial = rastro._material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            rastro.Pintar(OpacidadeInicial);
        }

        /// <summary>
        /// A opacidade do rastro depois de <paramref name="idade"/> segundos: cheia ao nascer e
        /// zero ao fim, sem voltar a subir.
        /// </summary>
        public static float Opacidade(float idade) =>
            idade >= ConfigSensacaoCombate.RastroSegundos
                ? 0f
                : OpacidadeInicial * (1f - idade / ConfigSensacaoCombate.RastroSegundos);

        private void Update()
        {
            _idade += Time.unscaledDeltaTime;
            if (_idade >= ConfigSensacaoCombate.RastroSegundos)
            {
                Destroy(gameObject);
                return;
            }

            Pintar(Opacidade(_idade));
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }

            var filtro = GetComponent<MeshFilter>();
            if (filtro != null && filtro.sharedMesh != null)
            {
                Destroy(filtro.sharedMesh);
            }
        }

        private void Pintar(float opacidade)
        {
            var cor = CorDoRastro;
            cor.a = opacidade;
            _material.color = cor;
        }

        // O leque no plano do chão: 0 grau aponta para +Z e cresce para +X, como no servidor.
        private static Mesh MontarLeque()
        {
            var alcance = (float)RegrasAtaque.AlcanceMetros;
            var metade = (float)RegrasAtaque.AberturaDoConeGraus * 0.5f;
            var vertices = new Vector3[Segmentos + 2];
            var triangulos = new int[Segmentos * 3];
            vertices[0] = Vector3.zero;
            for (var i = 0; i <= Segmentos; i++)
            {
                var angulo = (-metade + 2f * metade * i / Segmentos) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(
                    Mathf.Sin(angulo) * alcance,
                    0f,
                    Mathf.Cos(angulo) * alcance
                );
            }

            for (var i = 0; i < Segmentos; i++)
            {
                // Sentido horário visto de cima: a face fica virada para cima.
                triangulos[i * 3] = 0;
                triangulos[i * 3 + 1] = i + 2;
                triangulos[i * 3 + 2] = i + 1;
            }

            var malha = new Mesh { name = "Leque do golpe" };
            malha.vertices = vertices;
            malha.triangles = triangulos;
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            return malha;
        }
    }
}
