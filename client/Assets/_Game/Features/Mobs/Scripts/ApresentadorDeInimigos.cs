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

        /// <summary>Quanto tempo o corpo fica no chão tocando a morte antes de sumir.</summary>
        public const float DuracaoDaMorteSegundos = 1.5f;

        // Ajustes da aura da fortalecida (balanceamento visual).
        private static readonly Color AuraCor = new Color(1f, 0.1f, 0.05f);
        private const float AuraAltura = 0.6f;
        private const float AuraAlcance = 4f;
        private const float AuraIntensidade = 3f;

        private readonly RegistroDeInimigos _registro = new RegistroDeInimigos();
        private readonly Dictionary<long, GameObject> _visuais = new Dictionary<long, GameObject>();
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

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
                ConexaoTempoReal.Assinar<InimigoMorreu>(MobsMessageIds.InimigoMorreu, AoMorrer)
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

        private void AoPrepararAtaque(InimigoPreparaAtaque mensagem) =>
            _registro.PrepararAtaque(mensagem);

        private void AoMorrer(InimigoMorreu mensagem)
        {
            var inimigo = _registro.Morrer(mensagem);
            if (inimigo == null || !_visuais.TryGetValue(inimigo.InimigoId, out var visual))
            {
                return;
            }

            _visuais.Remove(inimigo.InimigoId);
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
