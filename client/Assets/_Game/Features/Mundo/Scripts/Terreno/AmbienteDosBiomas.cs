using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>Luz e névoa de um lugar do mundo.</summary>
    public struct Ambiente
    {
        /// <summary>Luz ambiente que vem de cima (e cor do céu ao fundo).</summary>
        public Color Ceu;

        /// <summary>Luz ambiente que vem dos lados.</summary>
        public Color Horizonte;

        /// <summary>Luz ambiente que vem de baixo.</summary>
        public Color Solo;

        /// <summary>Cor da névoa.</summary>
        public Color Nevoa;

        /// <summary>Quanto a névoa fecha (0 = sem névoa).</summary>
        public float Densidade;

        /// <summary>Cor do sol.</summary>
        public Color Sol;

        /// <summary>Força do sol.</summary>
        public float ForcaDoSol;

        /// <summary>Mistura dois ambientes: 0 dá <paramref name="a"/>, 1 dá <paramref name="b"/>.</summary>
        public static Ambiente Entre(Ambiente a, Ambiente b, float t)
        {
            return new Ambiente
            {
                Ceu = Color.Lerp(a.Ceu, b.Ceu, t),
                Horizonte = Color.Lerp(a.Horizonte, b.Horizonte, t),
                Solo = Color.Lerp(a.Solo, b.Solo, t),
                Nevoa = Color.Lerp(a.Nevoa, b.Nevoa, t),
                Densidade = Mathf.Lerp(a.Densidade, b.Densidade, t),
                Sol = Color.Lerp(a.Sol, b.Sol, t),
                ForcaDoSol = Mathf.Lerp(a.ForcaDoSol, b.ForcaDoSol, t),
            };
        }
    }

    /// <summary>
    /// Muda a luz e a névoa conforme o bioma em que o jogador está: dia claro em volta da
    /// cidade, ar quente no deserto, branco frio na neve, vermelho escuro na lava e névoa verde
    /// no pântano. Perto das fronteiras os ambientes se misturam pelos pesos do
    /// <see cref="MapaDoMundo"/>, e a troca é gradual. É só aparência: nada aqui muda regra.
    /// </summary>
    public sealed class AmbienteDosBiomas : MonoBehaviour
    {
        /// <summary>Quanto do caminho até o ambiente do lugar é percorrido por segundo.</summary>
        public const float Rapidez = 1.2f;

        private Light _sol;
        private Ambiente _atual;
        private bool _aplicado;

        /// <summary>Ambiente em volta da cidade (campo, mata e colinas): o que o jogo já tinha.</summary>
        public static Ambiente Padrao =>
            new Ambiente
            {
                Ceu = new Color(0.62f, 0.72f, 0.85f),
                Horizonte = new Color(0.5f, 0.52f, 0.5f),
                Solo = new Color(0.32f, 0.3f, 0.26f),
                Nevoa = new Color(0.66f, 0.76f, 0.86f),
                Densidade = 0.003f,
                Sol = new Color(1f, 0.96f, 0.86f),
                ForcaDoSol = 1.1f,
            };

        /// <summary>Ambiente de <paramref name="bioma"/> quando só ele existe no lugar.</summary>
        public static Ambiente Do(Bioma bioma)
        {
            switch (bioma)
            {
                case Bioma.Deserto:
                    return new Ambiente
                    {
                        Ceu = new Color(0.8f, 0.78f, 0.66f),
                        Horizonte = new Color(0.66f, 0.58f, 0.44f),
                        Solo = new Color(0.46f, 0.38f, 0.26f),
                        Nevoa = new Color(0.88f, 0.78f, 0.6f),
                        Densidade = 0.0032f,
                        Sol = new Color(1f, 0.9f, 0.7f),
                        ForcaDoSol = 1.35f,
                    };
                case Bioma.Neve:
                    return new Ambiente
                    {
                        Ceu = new Color(0.74f, 0.82f, 0.92f),
                        Horizonte = new Color(0.62f, 0.68f, 0.76f),
                        Solo = new Color(0.5f, 0.54f, 0.6f),
                        Nevoa = new Color(0.86f, 0.9f, 0.95f),
                        Densidade = 0.0038f,
                        Sol = new Color(0.9f, 0.95f, 1f),
                        ForcaDoSol = 0.95f,
                    };
                case Bioma.Lava:
                    return new Ambiente
                    {
                        Ceu = new Color(0.36f, 0.22f, 0.2f),
                        Horizonte = new Color(0.42f, 0.22f, 0.14f),
                        Solo = new Color(0.5f, 0.2f, 0.08f),
                        Nevoa = new Color(0.3f, 0.15f, 0.12f),
                        Densidade = 0.0045f,
                        Sol = new Color(1f, 0.62f, 0.42f),
                        ForcaDoSol = 0.7f,
                    };
                case Bioma.Pantano:
                    return new Ambiente
                    {
                        Ceu = new Color(0.42f, 0.5f, 0.42f),
                        Horizonte = new Color(0.36f, 0.42f, 0.34f),
                        Solo = new Color(0.2f, 0.24f, 0.18f),
                        Nevoa = new Color(0.44f, 0.52f, 0.42f),
                        Densidade = 0.0065f,
                        Sol = new Color(0.82f, 0.9f, 0.74f),
                        ForcaDoSol = 0.75f,
                    };
                default:
                    return Padrao;
            }
        }

        /// <summary>Ambiente de um lugar: a mistura dos ambientes dos biomas pelos pesos.</summary>
        public static Ambiente Misturar(PesosDosBiomas pesos)
        {
            var fora = pesos.Deserto + pesos.Neve + pesos.Lava + pesos.Pantano;
            var resultado = Padrao;
            if (fora <= 0f)
            {
                return resultado;
            }

            // Primeiro a média dos biomas de fora, depois a mistura dela com o ambiente padrão.
            var deFora = Do(Bioma.Deserto);
            var somado = pesos.Deserto;
            deFora = Acrescentar(deFora, ref somado, Do(Bioma.Neve), pesos.Neve);
            deFora = Acrescentar(deFora, ref somado, Do(Bioma.Lava), pesos.Lava);
            deFora = Acrescentar(deFora, ref somado, Do(Bioma.Pantano), pesos.Pantano);
            return Ambiente.Entre(resultado, deFora, Mathf.Clamp01(fora));
        }

        /// <summary>Ambiente em (<paramref name="x"/>, <paramref name="z"/>).</summary>
        public static Ambiente Em(float x, float z)
        {
            return Misturar(MapaDoMundo.PesosEm(x, z));
        }

        /// <summary>Aplica <paramref name="ambiente"/> à cena (e ao <paramref name="sol"/>, se houver).</summary>
        public static void Aplicar(Ambiente ambiente, Light sol)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambiente.Ceu;
            RenderSettings.ambientEquatorColor = ambiente.Horizonte;
            RenderSettings.ambientGroundColor = ambiente.Solo;
            RenderSettings.fog = ambiente.Densidade > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = ambiente.Nevoa;
            RenderSettings.fogDensity = ambiente.Densidade;

            if (sol != null)
            {
                sol.color = ambiente.Sol;
                sol.intensity = ambiente.ForcaDoSol;
            }
        }

        /// <summary>Diz qual luz é o sol, para a cor e a força dela acompanharem o bioma.</summary>
        public void Configurar(Light sol)
        {
            _sol = sol;
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null)
            {
                return;
            }

            var posicao = jogador.transform.position;
            var destino = Em(posicao.x, posicao.z);

            // Na primeira vez o ambiente entra de uma vez; depois muda aos poucos.
            _atual = _aplicado
                ? Ambiente.Entre(_atual, destino, Mathf.Clamp01(Rapidez * Time.deltaTime))
                : destino;
            _aplicado = true;
            Aplicar(_atual, _sol);

            // A cena do mundo não tem céu próprio: o fundo da câmera acompanha a névoa, para o
            // horizonte não ficar de outra cor.
            var camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = _atual.Nevoa;
            }
        }

        // Média ponderada acumulada: junta "outro" (com "peso") ao que já foi somado.
        private static Ambiente Acrescentar(
            Ambiente acumulado,
            ref float somado,
            Ambiente outro,
            float peso
        )
        {
            if (peso <= 0f)
            {
                return acumulado;
            }

            somado += peso;
            return Ambiente.Entre(acumulado, outro, peso / somado);
        }
    }
}
