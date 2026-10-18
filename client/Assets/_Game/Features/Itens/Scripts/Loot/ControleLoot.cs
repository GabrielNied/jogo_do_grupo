using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Features.Itens.Visual;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens.Loot
{
    /// <summary>Cores do marcador de loot, distintas sem criar materiais por peça.</summary>
    public static class CoresMarcadorDeLoot
    {
        private static readonly Color CorNeutra = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color CorTierB = new Color(0.18f, 0.48f, 0.95f);
        private static readonly Color CorTierA = new Color(0.58f, 0.27f, 0.82f);
        private static readonly Color CorTierS = new Color(0.96f, 0.48f, 0.12f);
        private static readonly Color CorTierSMais = new Color(0.48f, 0.08f, 0.13f);

        /// <summary>Devolve a cor correspondente ao tier; D, C e itens sem tier são neutros.</summary>
        public static Color Para(bool temTier, TierItem tier)
        {
            if (!temTier)
            {
                return CorNeutra;
            }

            if (ConfigAuraTier.TemAura(tier))
            {
                return ConfigAuraTier.Cor(tier);
            }

            return CorNeutra;
        }
    }

    /// <summary>
    /// O loot no mundo, enquanto o jogador está nele (issue #369): um marcador por item do
    /// jogador no chão, visível a até 12 m, a dica "F: pegar" com item ao alcance e o pedido de
    /// coleta com F. Os avisos de coletado e recusado são do <see cref="ServicoDeLoot"/>.
    /// Criado pelo <see cref="ObservadorDoMundo"/> na cena do mundo.
    /// </summary>
    internal sealed class ControleLoot : MonoBehaviour
    {
        /// <summary>Lado do marcador do item, em metros.</summary>
        public const float TamanhoDoMarcador = 0.35f;

        /// <summary>Altura do centro do marcador acima do chão, em metros.</summary>
        public const float AlturaDoMarcador = 0.45f;

        /// <summary>Giro do marcador, em graus por segundo (chama atenção sem piscar).</summary>
        public const float GiroDoMarcador = 90f;

        private const float AlturaDaBusca = 30f;
        private const float AlcanceDaBusca = 80f;

        private static readonly int CorBaseDoMaterial = Shader.PropertyToID("_BaseColor");
        private static readonly int CorDoMaterial = Shader.PropertyToID("_Color");

        private readonly Dictionary<long, GameObject> _marcadores =
            new Dictionary<long, GameObject>();

        private LootNoChaoDoCliente _registro;
        private GameObject _jogador;
        private Label _dica;
        private InputAction _coletar;

        /// <summary>Liga ao jogador e à interface. Chamar antes de ativar.</summary>
        public void Configurar(GameObject jogador, VisualElement raizDaInterface)
        {
            _jogador = jogador;
            _registro = ServicoDeLoot.Registro;

            _dica = new Label(TextosDoLoot.Dica);
            _dica.pickingMode = PickingMode.Ignore;
            _dica.AddToClassList(EstiloCore.Dica);
            _dica.style.display = DisplayStyle.None;
            raizDaInterface.Add(_dica);

            _coletar = new InputAction(
                "Coletar",
                InputActionType.Button,
                TeclasDoJogo.Caminho(TeclasDoJogo.Coletar)
            );
            TeclasDoJogo.Mudou += AtualizarTecla;
            _coletar.AddBinding("<Gamepad>/buttonNorth");

            _registro.Apareceu += CriarMarcador;
            _registro.Saiu += RemoverMarcador;
            foreach (var item in _registro.Itens)
            {
                CriarMarcador(item);
            }

            if (isActiveAndEnabled)
            {
                _coletar.Enable();
            }
        }

        private void OnEnable()
        {
            _coletar?.Enable();
        }

        private void AtualizarTecla() =>
            _coletar?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Coletar));

        private void OnDisable()
        {
            _coletar?.Disable();
        }

        private void OnDestroy()
        {
            TeclasDoJogo.Mudou -= AtualizarTecla;
            if (_registro != null)
            {
                _registro.Apareceu -= CriarMarcador;
                _registro.Saiu -= RemoverMarcador;
            }

            _coletar?.Dispose();
            _coletar = null;
        }

        private void Update()
        {
            if (_registro == null)
            {
                return;
            }

            _registro.Avancar(Time.deltaTime);
            if (_jogador == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            var posicao = _jogador.transform.position;
            var tierMinimo = FiltroDeLoot.TierMinimo;
            foreach (var item in _registro.Itens)
            {
                if (_marcadores.TryGetValue(item.LootId, out var marcador) && marcador != null)
                {
                    var visivel =
                        item.Passa(tierMinimo)
                        && LootNoChaoDoCliente.Visivel(item, posicao.x, posicao.z);
                    if (marcador.activeSelf != visivel)
                    {
                        marcador.SetActive(visivel);
                    }

                    marcador.transform.Rotate(0f, GiroDoMarcador * Time.deltaTime, 0f, Space.World);
                }
            }

            // Com o menu do Esc aberto, nem dica nem coleta; janelas comuns não bloqueiam.
            ItemNoChao maisProximo = null;
            var totalAoAlcance = 0;
            var aoAlcance =
                !JanelasDoJogo.MenuAberto
                && _registro.ObterAoAlcance(
                    posicao.x,
                    posicao.z,
                    tierMinimo,
                    out maisProximo,
                    out totalAoAlcance
                );
            _dica.style.display = aoAlcance ? DisplayStyle.Flex : DisplayStyle.None;
            if (aoAlcance)
            {
                _dica.text = TextosDoLoot.FormatarDica(
                    TeclasDoJogo.Rotulo(TeclasDoJogo.Coletar),
                    maisProximo,
                    totalAoAlcance
                );
            }

            if (aoAlcance && _coletar.WasPressedThisFrame())
            {
                ServicoDeLoot.PedirColeta();
            }
        }

        private void CriarMarcador(ItemNoChao item)
        {
            RemoverMarcador(item);

            var marcador = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marcador.name = $"Loot_{item.LootId}";
            Destroy(marcador.GetComponent<Collider>());
            marcador.transform.SetParent(transform, false);
            marcador.transform.position = new Vector3(
                item.X,
                AlturaDoChao(item.X, item.Z) + AlturaDoMarcador,
                item.Z
            );
            marcador.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
            marcador.transform.localScale = Vector3.one * TamanhoDoMarcador;

            var renderizador = marcador.GetComponent<Renderer>();
            var propriedades = new MaterialPropertyBlock();
            var cor = CoresMarcadorDeLoot.Para(item.TemTier, item.Tier);
            propriedades.SetColor(CorBaseDoMaterial, cor);
            propriedades.SetColor(CorDoMaterial, cor);

            if (item.TemTier && ConfigAuraTier.TemAura(item.Tier))
            {
                AuraDeTierVisual.Aplicar(marcador, item.Tier, noChao: true);
                propriedades.SetColor(Shader.PropertyToID("_EmissionColor"), cor * 1.5f);
            }

            renderizador.SetPropertyBlock(propriedades);
            marcador.SetActive(false);
            _marcadores[item.LootId] = marcador;
        }

        private void RemoverMarcador(ItemNoChao item)
        {
            if (_marcadores.TryGetValue(item.LootId, out var marcador))
            {
                _marcadores.Remove(item.LootId);
                if (marcador != null)
                {
                    Destroy(marcador);
                }
            }
        }

        // O servidor não sabe a altura do terreno: o client assenta o item no chão.
        private float AlturaDoChao(float x, float z)
        {
            var origem = new Vector3(x, AlturaDaBusca, z);
            if (_jogador != null)
            {
                origem.y = _jogador.transform.position.y + AlturaDaBusca;
            }

            return Physics.Raycast(
                origem,
                Vector3.down,
                out var batida,
                AlcanceDaBusca,
                ~0,
                QueryTriggerInteraction.Ignore
            )
                ? batida.point.y
                : origem.y - AlturaDaBusca;
        }
    }
}
