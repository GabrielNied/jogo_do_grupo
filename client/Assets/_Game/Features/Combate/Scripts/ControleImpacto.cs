using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Chaves de conforto do impacto. O Core ainda não tem uma tela de opções para isto: quando
    /// tiver, ela liga estas chaves (ex.: "reduzir movimento" desliga o tremor e o hit-stop).
    /// </summary>
    internal static class ConfiguracaoDeImpacto
    {
        public static bool TremorLigado = true;
        public static bool HitStopLigado = true;
        public static bool SomLigado = true;
    }

    /// <summary>
    /// A sensação do golpe: flash e recuo no alvo, hit-stop, tremor de câmera, sons e o aviso de
    /// esquiva bem-sucedida. Só reage ao que o servidor confirmou (acerto, vida perdida, esquiva
    /// iniciada, golpe de inimigo avisado); nada aparece por previsão. Quem decide a intensidade
    /// é <see cref="EscolhaDeEfeito"/> e quem limita a bagunça é <see cref="LimiteDeEfeitos"/>.
    /// </summary>
    [DefaultExecutionOrder(500)]
    internal sealed class ControleImpacto : MonoBehaviour
    {
        /// <summary>Velocidade do jogo durante o hit-stop (quase parado, sem zerar).</summary>
        public const float EscalaDeTempoNoHitStop = 0.05f;

        public const float VolumeBase = 0.6f;
        public const float VinhetaNormal = 0.5f;
        public const float VinhetaForte = 0.9f;
        public const float VinhetaQuedaPorSegundo = 3f;
        public const float IntervaloDoAlertaSegundos = 1.2f;
        public const string TextoEsquivou = "Esquivou!";
        public const string TextoSaiaDaArea = "Saia da área!";

        private static readonly int CorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int CorSimples = Shader.PropertyToID("_Color");
        private static readonly Color CorDoFlash = new Color(2f, 2f, 2f, 1f);

        private sealed class PausaDePose
        {
            public Animator Animator;
            public float Restante;
            public float Velocidade;
        }

        private sealed class Reacao
        {
            public GameObject Alvo;
            public Renderer[] Renderers;
            public float Restante;
            public float Duracao;
            public Vector3 Recuo;
        }

        private readonly LimiteDeEfeitos _limite = new LimiteDeEfeitos();
        private readonly DetectorDeEsquiva _detector = new DetectorDeEsquiva();
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private readonly List<Reacao> _reacoes = new List<Reacao>();
        private readonly List<PausaDePose> _pausas = new List<PausaDePose>();
        private readonly Dictionary<SomDeCombate, AudioClip> _clipes =
            new Dictionary<SomDeCombate, AudioClip>();
        private MaterialPropertyBlock _bloco;

        private long _personagemId;
        private PainelImpacto _painel;
        private AudioSource _fonte;
        private int? _vidaAnterior;
        private float _vinheta;
        private float _proximoAlerta;
        private bool _congelado;

        private void Awake()
        {
            // O Unity recusa MaterialPropertyBlock no construtor e no inicializador de campo.
            _bloco = new MaterialPropertyBlock();
        }

        public void Configurar(long personagemId, PainelImpacto painel)
        {
            _personagemId = personagemId;
            _painel = painel;
            _fonte = gameObject.AddComponent<AudioSource>();
            _fonte.playOnAwake = false;
            _fonte.spatialBlend = 0f;

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AtaqueIniciado>(
                    CombateMessageIds.AtaqueIniciado,
                    AoIniciarAtaque
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AtaqueAcertou>(
                    CombateMessageIds.AtaqueAcertou,
                    AoAcertarAtaque
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<RecursosMudaram>(
                    CombateMessageIds.RecursosMudaram,
                    AoMudarRecursos
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EsquivaIniciada>(
                    CombateMessageIds.EsquivaIniciada,
                    AoIniciarEsquiva
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoPreparaAtaque>(
                    MobsMessageIds.InimigoPreparaAtaque,
                    AoPrepararAtaqueDeInimigo
                )
            );
        }

        private void OnDisable()
        {
            Descongelar();
            SoltarPoses(true);
        }

        private void OnDestroy()
        {
            Descongelar();
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            foreach (var reacao in _reacoes)
            {
                Restaurar(reacao);
            }

            _reacoes.Clear();
            foreach (var clipe in _clipes.Values)
            {
                if (clipe != null)
                {
                    Destroy(clipe);
                }
            }
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _limite.Atualizar(dt);
            SoltarPoses(false);

            var congelar = ConfiguracaoDeImpacto.HitStopLigado && _limite.HitStopRestante > 0f;
            if (congelar && !_congelado)
            {
                Time.timeScale = EscalaDeTempoNoHitStop;
                _congelado = true;
            }
            else if (!congelar)
            {
                Descongelar();
            }

            for (var i = _reacoes.Count - 1; i >= 0; i--)
            {
                var reacao = _reacoes[i];
                reacao.Restante -= dt;
                if (reacao.Restante <= 0f || reacao.Alvo == null)
                {
                    Restaurar(reacao);
                    _reacoes.RemoveAt(i);
                }
            }

            _vinheta = Mathf.Max(0f, _vinheta - VinhetaQuedaPorSegundo * dt);
            _painel?.Mostrar(_vinheta);

            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                var posicao = jogador.transform.position;
                var esquivas = _detector.Atualizar(Time.unscaledTime, posicao.x, posicao.z);
                if (esquivas > 0)
                {
                    AvisosDoJogo.Mostrar(TextoEsquivou, TipoDeAviso.Informacao);
                    Tocar(SomDeCombate.EsquivaBemSucedida, 0.8f);
                }
            }
        }

        private void LateUpdate()
        {
            // Depois da câmera e do Mobs (que reescrevem a posição todo quadro): o recuo e o
            // tremor são só um deslocamento do quadro, sem acumular.
            foreach (var reacao in _reacoes)
            {
                if (reacao.Alvo != null && reacao.Recuo != Vector3.zero && reacao.Duracao > 0f)
                {
                    var forca = Mathf.Clamp01(reacao.Restante / reacao.Duracao);
                    reacao.Alvo.transform.position += reacao.Recuo * forca;
                }
            }

            var tremor = _limite.TremorAtual;
            var camera = Camera.main;
            if (ConfiguracaoDeImpacto.TremorLigado && tremor > 0f && camera != null)
            {
                camera.transform.position += UnityEngine.Random.insideUnitSphere * tremor;
            }
        }

        private void Descongelar()
        {
            if (_congelado)
            {
                Time.timeScale = 1f;
                _congelado = false;
            }
        }

        /// <summary>
        /// Retorno do golpe do próprio jogador que o servidor não confirmou (errou o alvo): o rastro
        /// no chão, no volume do golpe, e o som de ar cortado.
        /// </summary>
        public void GolpeNoVazio(Vector3 pes, float direcaoYGraus)
        {
            RastroDoGolpe.Criar(pes, direcaoYGraus);
            Tocar(SomDeCombate.GolpeNoVazio, 0.5f);
        }

        // Segura a pose de quem tem Animator por RegrasReacao.PausaVisualNoAcertoMs. O relógio do
        // servidor não para: é só a sensação de contato.
        private void SegurarPose(GameObject objeto)
        {
            if (objeto == null || !ConfiguracaoDeImpacto.HitStopLigado)
            {
                return;
            }

            var animator = objeto.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                return;
            }

            var duracao = RegrasReacao.PausaVisualNoAcertoMs / 1000f;
            foreach (var pausa in _pausas)
            {
                if (pausa.Animator == animator)
                {
                    pausa.Restante = duracao;
                    return;
                }
            }

            _pausas.Add(
                new PausaDePose
                {
                    Animator = animator,
                    Restante = duracao,
                    Velocidade = animator.speed,
                }
            );
            animator.speed = 0f;
        }

        private void SoltarPoses(bool todas)
        {
            var dt = Time.unscaledDeltaTime;
            for (var i = _pausas.Count - 1; i >= 0; i--)
            {
                var pausa = _pausas[i];
                pausa.Restante -= dt;
                if (todas || pausa.Restante <= 0f || pausa.Animator == null)
                {
                    if (pausa.Animator != null)
                    {
                        pausa.Animator.speed = pausa.Velocidade;
                    }

                    _pausas.RemoveAt(i);
                }
            }
        }

        private void AoIniciarAtaque(AtaqueIniciado ataque)
        {
            if (ataque.AtacanteId != _personagemId)
            {
                if (AnimacaoDeOutros.EhDeOutro(ataque.AtacanteId, _personagemId))
                {
                    Tocar(SomDeCombate.Golpe, AnimacaoDeOutros.VolumeDeOutros);
                }

                return;
            }

            Tocar(SomDeCombate.Golpe, ataque.Tipo == TipoDeAtaque.Pesado ? 0.8f : 0.5f);
        }

        private void AoAcertarAtaque(AtaqueAcertou ataque)
        {
            if (ataque.Acertos == null)
            {
                return;
            }

            var meu = ataque.AtacanteId == _personagemId;
            var jogador = EntradaNoJogo.Jogador;
            foreach (var acerto in ataque.Acertos)
            {
                if (acerto == null)
                {
                    continue;
                }

                EfeitoDeImpacto efeito;
                var vale = meu
                    ? EscolhaDeEfeito.DoGolpeProprio(ataque.Tipo, acerto.Dano, out efeito)
                    : EscolhaDeEfeito.DoGolpeDeOutro(acerto.Dano, out efeito);
                if (!vale)
                {
                    continue;
                }

                var alvo = RegistroDeAlvos.Achar(acerto.AlvoId);
                var origem = jogador != null ? jogador.transform.position : Vector3.zero;
                Aplicar(efeito, alvo, origem);

                // A pose de quem bateu e de quem levou fica parada por um instante (70 ms).
                SegurarPose(meu ? jogador : null);
                SegurarPose(alvo);
            }
        }

        private void AoMudarRecursos(RecursosMudaram recursos)
        {
            var anterior = _vidaAnterior;
            _vidaAnterior = recursos.VidaAtual;
            if (anterior == null)
            {
                return;
            }

            if (
                EscolhaDeEfeito.DoGolpeRecebido(
                    anterior.Value,
                    recursos.VidaAtual,
                    recursos.VidaMaxima,
                    out var efeito
                )
            )
            {
                _detector.AoPerderVida(Time.unscaledTime);
                Aplicar(efeito, EntradaNoJogo.Jogador, Vector3.zero);
                SegurarPose(EntradaNoJogo.Jogador);
            }
        }

        private void AoIniciarEsquiva(EsquivaIniciada esquiva)
        {
            if (esquiva.PersonagemId != _personagemId)
            {
                if (AnimacaoDeOutros.EhDeOutro(esquiva.PersonagemId, _personagemId))
                {
                    Tocar(SomDeCombate.Esquiva, AnimacaoDeOutros.VolumeDeOutros);
                }

                return;
            }

            _detector.AoEsquivar(Time.unscaledTime);
            Tocar(SomDeCombate.Esquiva, 0.5f);
        }

        private void AoPrepararAtaqueDeInimigo(InimigoPreparaAtaque aviso)
        {
            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null || aviso.Areas == null)
            {
                return;
            }

            var areas = new List<(float x, float z, float raio)>();
            foreach (var area in aviso.Areas)
            {
                if (area != null)
                {
                    areas.Add((area.CentroX, area.CentroZ, area.Raio));
                }
            }

            if (areas.Count == 0)
            {
                // Golpe comum sai no contato do modelo, sem marca no chão (Mobs): raio 0.
                if (aviso.Raio <= 0f)
                {
                    return;
                }

                areas.Add((aviso.CentroX, aviso.CentroZ, aviso.Raio));
            }

            var posicao = jogador.transform.position;
            var dentro = _detector.AoAvisoDeGolpe(
                Time.unscaledTime,
                aviso.PreparoMs / 1000f,
                areas,
                posicao.x,
                posicao.z
            );
            if (dentro && Time.unscaledTime >= _proximoAlerta)
            {
                _proximoAlerta = Time.unscaledTime + IntervaloDoAlertaSegundos;
                AvisosDoJogo.Mostrar(TextoSaiaDaArea, TipoDeAviso.Aviso);
                Tocar(SomDeCombate.AlertaDeArea, 0.6f);
            }
        }

        private void Aplicar(EfeitoDeImpacto efeito, GameObject alvo, Vector3 origemDoGolpe)
        {
            if (alvo != null && _limite.RegistrarFlash(efeito.FlashSegundos))
            {
                Piscar(alvo, efeito, origemDoGolpe);
            }

            var recebido =
                efeito.Origem == OrigemDoImpacto.GolpeRecebido
                || efeito.Origem == OrigemDoImpacto.GolpeRecebidoForte;
            if (recebido)
            {
                _vinheta = Mathf.Max(
                    _vinheta,
                    efeito.Origem == OrigemDoImpacto.GolpeRecebidoForte
                        ? VinhetaForte
                        : VinhetaNormal
                );
            }

            _limite.PedirHitStop(efeito.HitStopSegundos);
            _limite.PedirTremor(efeito.TremorAmplitude, efeito.TremorSegundos);
            Tocar(efeito.Som, efeito.VolumeDoSom);
        }

        private void Piscar(GameObject alvo, EfeitoDeImpacto efeito, Vector3 origemDoGolpe)
        {
            Reacao reacao = null;
            foreach (var existente in _reacoes)
            {
                if (existente.Alvo == alvo)
                {
                    reacao = existente;
                    break;
                }
            }

            if (reacao == null)
            {
                reacao = new Reacao
                {
                    Alvo = alvo,
                    Renderers = alvo.GetComponentsInChildren<Renderer>(),
                };
                _reacoes.Add(reacao);
            }

            reacao.Duracao = efeito.FlashSegundos;
            reacao.Restante = efeito.FlashSegundos;
            var afastar = alvo.transform.position - origemDoGolpe;
            afastar.y = 0f;
            reacao.Recuo =
                efeito.RecuoMetros > 0f && afastar.sqrMagnitude > 0.0001f
                    ? afastar.normalized * efeito.RecuoMetros
                    : Vector3.zero;

            _bloco.Clear();
            _bloco.SetColor(CorBase, CorDoFlash);
            _bloco.SetColor(CorSimples, CorDoFlash);
            foreach (var renderer in reacao.Renderers)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(_bloco);
                }
            }
        }

        private static void Restaurar(Reacao reacao)
        {
            if (reacao.Renderers == null)
            {
                return;
            }

            foreach (var renderer in reacao.Renderers)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }
        }

        private void Tocar(SomDeCombate som, float volume)
        {
            if (
                !ConfiguracaoDeImpacto.SomLigado
                || som == SomDeCombate.Nenhum
                || _fonte == null
                || !_limite.PodeTocarSom()
            )
            {
                return;
            }

            if (!_clipes.TryGetValue(som, out var clipe))
            {
                var amostras = SintetizadorDeSons.Gerar(som);
                clipe = AudioClip.Create(
                    "Combate." + som,
                    amostras.Length,
                    1,
                    SintetizadorDeSons.TaxaDeAmostragem,
                    false
                );
                clipe.SetData(amostras, 0);
                _clipes[som] = clipe;
            }

            _fonte.PlayOneShot(clipe, Mathf.Clamp01(volume) * VolumeBase);
        }
    }
}
