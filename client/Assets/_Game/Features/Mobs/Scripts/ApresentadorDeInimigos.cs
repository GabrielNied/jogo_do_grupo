using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Mostra os inimigos comuns no campo: modelo, barra de vida, aviso do golpe, morte e
    /// reaparição. Só reage ao que o servidor manda; dano, alvo e morte não são decididos aqui.
    /// </summary>
    internal sealed class ApresentadorDeInimigos : MonoBehaviour
    {
        /// <summary>Onde o artista põe o prefab de uma criatura: <c>Resources/Mobs/Criatura_&lt;id&gt;</c>.</summary>
        public const string PrefixoDoPrefab = "Mobs/Criatura_";

        /// <summary>Gatilho do Animator tocado na morte (se o modelo tiver Animator).</summary>
        public const string GatilhoDeMorte = "Morrer";

        /// <summary>Booleano do Animator: o modelo anda.</summary>
        public const string ParametroAndando = "Andando";

        /// <summary>Gatilho do Animator tocado uma vez por golpe.</summary>
        public const string GatilhoDeAtaque = "Atacar";

        /// <summary>Parâmetro opcional do Animator: reação ao acerto (se o modelo tiver o clipe).</summary>
        public const string ParametroInterrompido = "Interrompido";

        /// <summary>Estado parado do Animator, para onde o golpe cortado volta.</summary>
        public const string EstadoParado = "Idle";

        /// <summary>Duração mínima do tranco visual ao ser interrompido.</summary>
        public const float DuracaoDaReacaoSegundos = 0.35f;

        /// <summary>Fusão do corte do golpe para o parado.</summary>
        public const float CorteDoGolpeSegundos = 0.05f;

        /// <summary>O quanto o corpo achata no tranco (fração da altura).</summary>
        public const float AchatamentoDaReacao = 0.18f;

        /// <summary>Quanto tempo o corpo fica no chão tocando a morte antes de sumir.</summary>
        public const float DuracaoDaMorteSegundos = 1.5f;

        // Ajustes da aura da fortalecida (balanceamento visual).
        private static readonly Color AuraCor = new Color(1f, 0.1f, 0.05f);
        private const float AuraAltura = 0.6f;
        private const float AuraAlcance = 4f;
        private const float AuraIntensidade = 3f;

        private readonly RegistroDeInimigos _registro = new RegistroDeInimigos();
        private readonly Dictionary<long, GameObject> _visuais = new Dictionary<long, GameObject>();
        private readonly Dictionary<long, Animator> _animadores = new Dictionary<long, Animator>();
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private readonly Dictionary<long, float> _reacoes = new Dictionary<long, float>();

        // Pose segurada no acerto: quanto falta (s) para soltar o inimigo que bateu.
        private readonly Dictionary<long, float> _poses = new Dictionary<long, float>();

        // Velocidade do clipe de ataque de cada inimigo, para o contato cair no fim do preparo.
        private readonly Dictionary<long, float> _velocidadesDoGolpe =
            new Dictionary<long, float>();

        private PainelDeInimigos _painel;

        public void Configurar(PainelDeInimigos painel)
        {
            _painel = painel;

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigosDaArea>(
                    MobsMessageIds.InimigosDaArea,
                    AoReceberArea
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoApareceu>(
                    MobsMessageIds.InimigoApareceu,
                    AoAparecer
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoMudou>(MobsMessageIds.InimigoMudou, AoMudar)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoPreparaAtaque>(
                    MobsMessageIds.InimigoPreparaAtaque,
                    AoPrepararAtaque
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoAcertou>(MobsMessageIds.InimigoAcertou, AoAcertar)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<InimigoMorreu>(MobsMessageIds.InimigoMorreu, AoMorrer)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AcaoInterrompida>(
                    CombateMessageIds.AcaoInterrompida,
                    AoInterromper
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AtaqueAcertou>(
                    CombateMessageIds.AtaqueAcertou,
                    _registro.AplicarAcertos
                )
            );
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            foreach (var visual in _visuais.Values)
            {
                if (visual != null)
                {
                    Destroy(visual);
                }
            }

            _visuais.Clear();
            _animadores.Clear();
        }

        private void Update()
        {
            if (!EntradaNoJogo.NoJogo)
            {
                return;
            }

            _registro.Atualizar(Time.deltaTime);
            foreach (var inimigo in _registro.Todos)
            {
                if (_visuais.TryGetValue(inimigo.InimigoId, out var visual) && visual != null)
                {
                    visual.transform.SetPositionAndRotation(
                        new Vector3(inimigo.X, inimigo.Y, inimigo.Z),
                        Quaternion.Euler(0f, inimigo.RotacaoY, 0f)
                    );
                    visual.transform.localScale = new Vector3(
                        inimigo.Escala,
                        EscalaDaReacao(inimigo.InimigoId, inimigo.Escala),
                        inimigo.Escala
                    );
                    if (_animadores.TryGetValue(inimigo.InimigoId, out var animador))
                    {
                        animador.SetBool(ParametroAndando, inimigo.Animacao.Andando);
                        if (inimigo.Animacao.DisparouAtaque)
                        {
                            animador.SetTrigger(GatilhoDeAtaque);
                            _velocidadesDoGolpe[inimigo.InimigoId] =
                                AnimacaoDoInimigo.VelocidadeDoClipe(
                                    DuracaoDoClipeDeAtaque(animador),
                                    inimigo.PreparoDoGolpeSegundos
                                );
                        }

                        animador.speed = VelocidadeDoAnimator(inimigo);
                    }
                }
            }

            _painel?.Mostrar(_registro.Todos, Camera.main);
        }

        private void AoReceberArea(InimigosDaArea mensagem)
        {
            foreach (var removido in _registro.SubstituirArea(mensagem))
            {
                RemoverVisualSemAnimacao(removido.InimigoId);
            }

            foreach (var inimigo in _registro.Todos)
            {
                CriarVisual(inimigo);
            }
        }

        private void AoAparecer(InimigoApareceu mensagem)
        {
            var anterior = _registro.Aparecer(mensagem);
            if (anterior != null)
            {
                RemoverVisualSemAnimacao(anterior.InimigoId);
            }

            var novo =
                mensagem?.Inimigo == null ? null : _registro.Obter(mensagem.Inimigo.InimigoId);
            if (novo != null)
            {
                CriarVisual(novo);
            }
        }

        private void AoMudar(InimigoMudou mensagem) => _registro.Mudar(mensagem);

        /// <summary>
        /// O golpe encostou: o inimigo que bateu segura a pose por um instante (a sensação de
        /// contato). Só visual; o relógio do servidor não para. A pose de quem levou é do Combate.
        /// </summary>
        private void AoAcertar(InimigoAcertou mensagem)
        {
            if (mensagem != null && _animadores.ContainsKey(mensagem.InimigoId))
            {
                _poses[mensagem.InimigoId] = RegrasReacao.PausaVisualNoAcertoMs / 1000f;
            }
        }

        // 0 enquanto a pose está segurada; no golpe, a do clipe; fora dele, a normal.
        private float VelocidadeDoAnimator(InimigoNoClient inimigo)
        {
            if (_poses.TryGetValue(inimigo.InimigoId, out var restante))
            {
                restante -= Time.unscaledDeltaTime;
                if (restante > 0f)
                {
                    _poses[inimigo.InimigoId] = restante;
                    return 0f;
                }

                _poses.Remove(inimigo.InimigoId);
            }

            if (
                inimigo.Estado == EstadoDeInimigo.Ataque
                && _velocidadesDoGolpe.TryGetValue(inimigo.InimigoId, out var velocidade)
            )
            {
                return velocidade;
            }

            return 1f;
        }

        // Duração do clipe de ataque do modelo; zero se o controlador não tem um (velocidade 1).
        private static float DuracaoDoClipeDeAtaque(Animator animador)
        {
            var controlador = animador.runtimeAnimatorController;
            if (controlador == null)
            {
                return 0f;
            }

            foreach (var clipe in controlador.animationClips)
            {
                var nome = clipe.name.ToLowerInvariant();
                if (nome.Contains("atacar") || nome.Contains("attack"))
                {
                    return clipe.length;
                }
            }

            return 0f;
        }

        private void AoPrepararAtaque(InimigoPreparaAtaque mensagem) =>
            _registro.PrepararAtaque(mensagem);

        /// <summary>
        /// O golpe do inimigo foi cancelado: corta o clipe, apaga o aviso (no registro) e mostra
        /// a reação ao acerto. O dano já não sai no servidor.
        /// </summary>
        private void AoInterromper(AcaoInterrompida mensagem)
        {
            var inimigo = _registro.Interromper(mensagem);
            if (inimigo == null)
            {
                return;
            }

            if (_animadores.TryGetValue(inimigo.InimigoId, out var animador) && animador != null)
            {
                animador.ResetTrigger(GatilhoDeAtaque);
                if (TemParametro(animador, ParametroInterrompido))
                {
                    animador.SetTrigger(ParametroInterrompido);
                }
                else
                {
                    // Sem clipe de reação no modelo: volta ao parado, o que corta o golpe.
                    animador.CrossFadeInFixedTime(EstadoParado, CorteDoGolpeSegundos);
                }
            }

            _reacoes[inimigo.InimigoId] = DuracaoDaReacaoSegundos;
        }

        private static bool TemParametro(Animator animador, string nome)
        {
            foreach (var parametro in animador.parameters)
            {
                if (parametro.name == nome)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Tranco do corpo enquanto a reação dura: achata e volta (0 a 1 e de volta).</summary>
        private float EscalaDaReacao(long inimigoId, float escalaBase)
        {
            if (!_reacoes.TryGetValue(inimigoId, out var restante))
            {
                return escalaBase;
            }

            restante -= Time.deltaTime;
            if (restante <= 0f)
            {
                _reacoes.Remove(inimigoId);
                _poses.Remove(inimigoId);
                _velocidadesDoGolpe.Remove(inimigoId);
                return escalaBase;
            }

            _reacoes[inimigoId] = restante;
            var onda = Mathf.Sin(Mathf.Clamp01(restante / DuracaoDaReacaoSegundos) * Mathf.PI);
            return escalaBase * (1f - AchatamentoDaReacao * onda);
        }

        private void AoMorrer(InimigoMorreu mensagem)
        {
            var inimigo = _registro.Morrer(mensagem);
            if (inimigo == null || !_visuais.TryGetValue(inimigo.InimigoId, out var visual))
            {
                return;
            }

            _visuais.Remove(inimigo.InimigoId);
            _animadores.Remove(inimigo.InimigoId);
            _reacoes.Remove(inimigo.InimigoId);
            _poses.Remove(inimigo.InimigoId);
            _velocidadesDoGolpe.Remove(inimigo.InimigoId);
            if (visual == null)
            {
                return;
            }

            var animador = visual.GetComponentInChildren<Animator>();
            if (animador != null)
            {
                animador.SetTrigger(GatilhoDeMorte);
            }

            Destroy(visual, DuracaoDaMorteSegundos);
        }

        private void RemoverVisualSemAnimacao(long inimigoId)
        {
            if (_visuais.TryGetValue(inimigoId, out var visual))
            {
                _visuais.Remove(inimigoId);
                _animadores.Remove(inimigoId);
                _reacoes.Remove(inimigoId);
                _poses.Remove(inimigoId);
                _velocidadesDoGolpe.Remove(inimigoId);
                if (visual != null)
                {
                    Destroy(visual);
                }
            }
        }

        private void CriarVisual(InimigoNoClient inimigo)
        {
            GameObject visual;
            var prefab = Resources.Load<GameObject>(PrefixoDoPrefab + inimigo.CriaturaId);
            if (prefab != null)
            {
                visual = Instantiate(prefab);
            }
            else
            {
                // Sem prefab do artista ainda: uma cápsula marca o lugar do inimigo.
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var colisor = visual.GetComponent<Collider>();
                if (colisor != null)
                {
                    Destroy(colisor);
                }
            }

            visual.name = "Inimigo " + inimigo.InimigoId;
            visual.transform.localScale = Vector3.one * inimigo.Escala;
            visual.transform.SetPositionAndRotation(
                new Vector3(inimigo.X, inimigo.Y, inimigo.Z),
                Quaternion.Euler(0f, inimigo.RotacaoY, 0f)
            );
            if (inimigo.Fortalecido)
            {
                AdicionarAura(visual);
            }

            _visuais[inimigo.InimigoId] = visual;
            var animador = visual.GetComponentInChildren<Animator>();
            if (animador != null)
            {
                _animadores[inimigo.InimigoId] = animador;
            }
        }

        /// <summary>Aura vermelha da variante fortalecida: uma luz que tinge o chão ao redor.</summary>
        private static void AdicionarAura(GameObject visual)
        {
            var aura = new GameObject("Aura");
            aura.transform.SetParent(visual.transform, false);
            aura.transform.localPosition = Vector3.up * AuraAltura;
            var luz = aura.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = AuraCor;
            luz.range = AuraAlcance;
            luz.intensity = AuraIntensidade;
        }
    }
}
