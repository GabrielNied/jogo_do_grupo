using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Visual
{
    /// <summary>
    /// Componente que controla a apresentação e animação suave de pulsação da aura de tier no mundo 3D
    /// (loot no chão ou arma visível no avatar).
    /// </summary>
    public sealed class AuraDeTierVisual : MonoBehaviour
    {
        public TierItem Tier { get; private set; } = TierItem.D;
        public Light Luz { get; private set; }

        private float _intensidadeBase;
        private float _tempo;

        /// <summary>
        /// Aplica o efeito de aura de tier em um GameObject alvo. Cria a fonte de luz pontual se tiver aura (B+).
        /// </summary>
        public static AuraDeTierVisual Aplicar(GameObject alvo, TierItem tier, bool noChao)
        {
            if (alvo == null)
            {
                return null;
            }

            var aura = alvo.GetComponent<AuraDeTierVisual>();
            if (!ConfigAuraTier.TemAura(tier))
            {
                if (aura != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(aura);
                    }
                    else
                    {
                        DestroyImmediate(aura);
                    }
                }
                return null;
            }

            if (aura == null)
            {
                aura = alvo.AddComponent<AuraDeTierVisual>();
            }

            aura.Configurar(tier, noChao);
            return aura;
        }

        private void Configurar(TierItem tier, bool noChao)
        {
            Tier = tier;
            _intensidadeBase = noChao
                ? ConfigAuraTier.IntensidadeChao(tier)
                : ConfigAuraTier.IntensidadeArma(tier);
            var alcance = noChao
                ? ConfigAuraTier.AlcanceChao(tier)
                : ConfigAuraTier.AlcanceArma(tier);
            var cor = ConfigAuraTier.Cor(tier);

            if (Luz == null)
            {
                var filhoLuz = transform.Find("LuzAuraTier");
                if (filhoLuz == null)
                {
                    var go = new GameObject("LuzAuraTier");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    filhoLuz = go.transform;
                }
                Luz = filhoLuz.GetComponent<Light>();
                if (Luz == null)
                {
                    Luz = filhoLuz.gameObject.AddComponent<Light>();
                }
            }

            Luz.type = LightType.Point;
            Luz.color = cor;
            Luz.intensity = _intensidadeBase;
            Luz.range = alcance;
            Luz.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (Luz == null || _intensidadeBase <= 0f)
            {
                return;
            }

            _tempo += Time.deltaTime * 2.5f;
            // Efeito suave de respiração/pulsação (entre 88% e 112% da intensidade base)
            var fator = 1f + 0.12f * Mathf.Sin(_tempo);
            Luz.intensity = _intensidadeBase * fator;
        }
    }
}
