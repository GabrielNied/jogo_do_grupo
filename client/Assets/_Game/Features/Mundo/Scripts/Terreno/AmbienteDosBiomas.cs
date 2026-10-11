using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.Rendering;

namespace JogoDoGrupo.Client.Features.Mundo.Terreno
{
    /// <summary>Efeito visual leve de um bioma. Não muda regra nenhuma.</summary>
    public enum EfeitoDoClima
    {
        Nenhum = 0,
        Poeira = 1,
        NeveCaindo = 2,
        Brasa = 3,
        NevoaRasteira = 4,
        Vagalume = 5,
    }

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

        /// <summary>Efeito visual leve deste lugar. Nenhum nos biomas sem clima visual.</summary>
        public EfeitoDoClima Efeito;

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
                Efeito = t < 0.5f ? a.Efeito : b.Efeito,
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
        private Light _preenchimento;
        private Ambiente _atual;
        private bool _aplicado;
        private ParticleSystem _particulas;
        private EfeitoDoClima _efeitoAtual = EfeitoDoClima.Nenhum;

        /// <summary>Ambiente em volta da cidade (campo, mata e colinas): o que o jogo já tinha.</summary>
        public static Ambiente Padrao =>
            new Ambiente
            {
                Ceu = new Color(0.76f, 0.83f, 0.94f),
                Horizonte = new Color(0.72f, 0.7f, 0.66f),
                Solo = new Color(0.56f, 0.5f, 0.42f),
                Nevoa = new Color(0.66f, 0.76f, 0.86f),
                Densidade = 0.0016f,
                Sol = new Color(1f, 0.96f, 0.86f),
                ForcaDoSol = 1.2f,
                Efeito = EfeitoDoClima.Nenhum,
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
                        Densidade = 0.0024f,
                        Sol = new Color(1f, 0.9f, 0.7f),
                        ForcaDoSol = 1.2f,
                        Efeito = EfeitoDoClima.Poeira,
                    };
                case Bioma.Neve:
                    return new Ambiente
                    {
                        Ceu = new Color(0.74f, 0.82f, 0.92f),
                        Horizonte = new Color(0.62f, 0.68f, 0.76f),
                        Solo = new Color(0.5f, 0.54f, 0.6f),
                        Nevoa = new Color(0.86f, 0.9f, 0.95f),
                        Densidade = 0.007f,
                        Sol = new Color(0.9f, 0.95f, 1f),
                        ForcaDoSol = 0.95f,
                        Efeito = EfeitoDoClima.NeveCaindo,
                    };
                case Bioma.Lava:
                    return new Ambiente
                    {
                        Ceu = new Color(0.36f, 0.22f, 0.2f),
                        Horizonte = new Color(0.48f, 0.26f, 0.18f),
                        Solo = new Color(0.55f, 0.24f, 0.12f),
                        Nevoa = new Color(0.3f, 0.15f, 0.12f),
                        Densidade = 0.0045f,
                        Sol = new Color(1f, 0.62f, 0.42f),
                        ForcaDoSol = 0.7f,
                        Efeito = EfeitoDoClima.Brasa,
                    };
                case Bioma.Pantano:
                    return new Ambiente
                    {
                        Ceu = new Color(0.42f, 0.5f, 0.42f),
                        Horizonte = new Color(0.42f, 0.48f, 0.4f),
                        Solo = new Color(0.32f, 0.36f, 0.28f),
                        Nevoa = new Color(0.44f, 0.52f, 0.42f),
                        Densidade = 0.011f,
                        Sol = new Color(0.82f, 0.9f, 0.74f),
                        ForcaDoSol = 0.75f,
                        Efeito = EfeitoDoClima.NevoaRasteira,
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

        /// <summary>
        /// Hora do dia, de 0 a 24. O ciclo (#163) ainda não existe: fica em meio-dia até ele
        /// escrever aqui. Noite é das 20h às 5h.
        /// </summary>
        public static float Hora = 12f;

        /// <summary>Se <paramref name="hora"/> cai na noite.</summary>
        public static bool EhNoite(float hora) => hora >= 20f || hora < 5f;

        /// <summary>
        /// Escurece o céu e o sol quando é noite. A névoa mantém a cor do bioma.
        /// </summary>
        public static Ambiente ComHora(Ambiente ambiente, float hora)
        {
            if (!EhNoite(hora))
            {
                return ambiente;
            }

            ambiente.Ceu = Color.Lerp(ambiente.Ceu, Color.black, 0.72f);
            ambiente.Horizonte = Color.Lerp(ambiente.Horizonte, Color.black, 0.55f);
            ambiente.Sol = Color.Lerp(ambiente.Sol, new Color(0.55f, 0.62f, 0.8f), 0.7f);
            ambiente.ForcaDoSol *= 0.15f;
            return ambiente;
        }

        /// <summary>Ambiente em (<paramref name="x"/>, <paramref name="z"/>), na <see cref="Hora"/> atual.</summary>
        public static Ambiente Em(float x, float z)
        {
            var ambiente = ComHora(Misturar(MapaDoMundo.PesosEm(x, z)), Hora);
            if (MapaDoMundo.BiomaEm(x, z) == Bioma.Mata && EhNoite(Hora))
            {
                ambiente.Efeito = EfeitoDoClima.Vagalume;
            }

            return ambiente;
        }

        /// <summary>Cor da luz de preenchimento: a do céu, com um pouco da cor do sol.</summary>
        public static Color CorDoPreenchimento(Ambiente ambiente)
        {
            return Color.Lerp(
                ambiente.Ceu,
                ambiente.Sol,
                ConfigIluminacao.MisturaDoSolNoPreenchimento
            );
        }

        /// <summary>Força da luz de preenchimento: uma fração da do sol, para nunca passar dela.</summary>
        public static float ForcaDoPreenchimento(Ambiente ambiente)
        {
            return ambiente.ForcaDoSol * ConfigIluminacao.FracaoDoPreenchimento;
        }

        /// <summary>Aplica <paramref name="ambiente"/> à cena (e ao <paramref name="sol"/>, se houver).</summary>
        public static void Aplicar(Ambiente ambiente, Light sol)
        {
            Aplicar(ambiente, sol, null);
        }

        /// <summary>
        /// Aplica <paramref name="ambiente"/> à cena, ao <paramref name="sol"/> e à luz de
        /// <paramref name="preenchimento"/> (que ilumina o lado do personagem fora do sol); os
        /// dois são opcionais.
        /// </summary>
        public static void Aplicar(Ambiente ambiente, Light sol, Light preenchimento)
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

            if (preenchimento != null)
            {
                preenchimento.color = CorDoPreenchimento(ambiente);
                preenchimento.intensity = ForcaDoPreenchimento(ambiente);
            }
        }

        /// <summary>
        /// Diz qual luz é o sol (e qual é o preenchimento, se houver), para a cor e a força
        /// delas acompanharem o bioma.
        /// </summary>
        public void Configurar(Light sol, Light preenchimento = null)
        {
            _sol = sol;
            _preenchimento = preenchimento;
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
            Aplicar(_atual, _sol, _preenchimento);
            AplicarEfeito(_atual.Efeito, posicao);

            // O fundo da câmera é o céu do bioma. A névoa, mais à frente, esconde o fim do alcance.
            var camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = _atual.Ceu;
            }
        }

        private void AplicarEfeito(EfeitoDoClima efeito, Vector3 posicao)
        {
            if (_particulas == null)
            {
                var objeto = new GameObject("EfeitoDoClima");
                objeto.transform.SetParent(transform, false);
                _particulas = objeto.AddComponent<ParticleSystem>();
                var principal = _particulas.main;
                principal.loop = true;
                principal.playOnAwake = false;
                principal.maxParticles = 40;
                principal.simulationSpace = ParticleSystemSimulationSpace.World;
                principal.startLifetime = 3f;
                var emissao = _particulas.emission;
                emissao.rateOverTime = 8f;
                var forma = _particulas.shape;
                forma.shapeType = ParticleSystemShapeType.Sphere;
                forma.radius = 8f;
            }

            _particulas.transform.position = posicao + Vector3.up * 2f;
            if (efeito == _efeitoAtual)
            {
                return;
            }

            _efeitoAtual = efeito;
            var ligar = _particulas.emission;
            ligar.enabled = efeito != EfeitoDoClima.Nenhum;
            if (efeito == EfeitoDoClima.Nenhum)
            {
                _particulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }

            var modulo = _particulas.main;
            modulo.startColor = CorDoEfeito(efeito);
            modulo.startSpeed = efeito == EfeitoDoClima.NeveCaindo ? 1.5f : 0.4f;
            modulo.gravityModifier = efeito == EfeitoDoClima.Brasa ? -0.3f : 0.2f;
            _particulas.Play();
        }

        private static Color CorDoEfeito(EfeitoDoClima efeito)
        {
            switch (efeito)
            {
                case EfeitoDoClima.Poeira:
                    return new Color(0.85f, 0.72f, 0.45f);
                case EfeitoDoClima.NeveCaindo:
                    return Color.white;
                case EfeitoDoClima.Brasa:
                    return new Color(1f, 0.4f, 0.1f);
                case EfeitoDoClima.NevoaRasteira:
                    return new Color(0.55f, 0.65f, 0.5f);
                default:
                    return new Color(0.8f, 1f, 0.4f);
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
