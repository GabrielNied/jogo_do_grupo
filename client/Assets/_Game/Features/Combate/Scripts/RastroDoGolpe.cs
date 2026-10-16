using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// O rastro de um golpe que não pegou ninguém: um leque no chão, nos ângulos e no alcance do
    /// arco da lâmina (<see cref="ArcoDoGolpe"/>, os mesmos dados que o servidor testa), que aparece e some em
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
        public static void Criar(Vector3 pes, float direcaoYGraus, ArcoDoGolpe arco)
        {
            var objeto = new GameObject("Rastro do golpe");
            objeto.transform.SetPositionAndRotation(
                pes + Vector3.up * AlturaDoChao,
                Quaternion.Euler(0f, direcaoYGraus, 0f)
            );
            var filtro = objeto.AddComponent<MeshFilter>();
            filtro.sharedMesh = MontarLeque(arco);
            var render = objeto.AddComponent<MeshRenderer>();
            var rastro = objeto.AddComponent<RastroDoGolpe>();
            rastro._material = new Material(Shader.Find("Sprites/Default"));
            render.sharedMaterial = rastro._material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            rastro.Pintar(OpacidadeInicial);
        }

        /// <summary>
        /// Gizmo de depuração do arco da lâmina (Sintonia <c>Combate.ArcoGizmo</c>): desenha o leque
        /// (bordas, arco da ponta e as alturas da lâmina) por <c>ArcoGizmoSegundos</c>. Só no editor
        /// e em build de desenvolvimento, como a própria Sintonia.
        /// </summary>
        public static void DesenharGizmo(Vector3 pes, float direcaoYGraus, ArcoDoGolpe arco)
        {
            if (!Sintonia.Ativa || ConfigSensacaoCombate.ArcoGizmo < 0.5f)
            {
                return;
            }

            var duracao = ConfigSensacaoCombate.ArcoGizmoSegundos;
            var alcance = (float)arco.AlcanceMetros;
            var cor = new Color(1f, 0.55f, 0.1f, 1f);
            Vector3 Ponto(float grau, float altura, float raio) =>
                pes
                + Quaternion.Euler(0f, direcaoYGraus + grau, 0f) * Vector3.forward * raio
                + Vector3.up * altura;
            var minimo = (float)arco.AnguloMinimoGraus;
            var maximo = (float)arco.AnguloMaximoGraus;
            foreach (
                var altura in new[]
                {
                    (float)arco.AlturaMinimaMetros,
                    (float)arco.AlturaMaximaMetros,
                }
            )
            {
                Debug.DrawLine(
                    Ponto(minimo, altura, 0f),
                    Ponto(minimo, altura, alcance),
                    cor,
                    duracao
                );
                Debug.DrawLine(
                    Ponto(maximo, altura, 0f),
                    Ponto(maximo, altura, alcance),
                    cor,
                    duracao
                );
                for (var i = 0; i < Segmentos; i++)
                {
                    var a = minimo + (maximo - minimo) * i / Segmentos;
                    var b = minimo + (maximo - minimo) * (i + 1) / Segmentos;
                    Debug.DrawLine(
                        Ponto(a, altura, alcance),
                        Ponto(b, altura, alcance),
                        cor,
                        duracao
                    );
                }
            }
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
        private static Mesh MontarLeque(ArcoDoGolpe arco)
        {
            var alcance = (float)arco.AlcanceMetros;
            var minimo = (float)arco.AnguloMinimoGraus;
            var largura = (float)(arco.AnguloMaximoGraus - arco.AnguloMinimoGraus);
            var vertices = new Vector3[Segmentos + 2];
            var triangulos = new int[Segmentos * 3];
            vertices[0] = Vector3.zero;
            for (var i = 0; i <= Segmentos; i++)
            {
                var angulo = (minimo + largura * i / Segmentos) * Mathf.Deg2Rad;
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
