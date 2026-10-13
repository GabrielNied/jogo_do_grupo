using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens.Loot
{
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

        private static readonly Color CorDoMarcador = new Color(1f, 0.82f, 0.3f);

        private readonly Dictionary<long, GameObject> _marcadores =
            new Dictionary<long, GameObject>();

        private LootNoChaoDoCliente _registro;
        private GameObject _jogador;
        private Label _dica;
        private InputAction _coletar;
        private Material _material;

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

            _coletar = new InputAction("Coletar", InputActionType.Button, "<Keyboard>/f");
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

        private void OnDisable()
        {
            _coletar?.Disable();
        }

        private void OnDestroy()
        {
            if (_registro != null)
            {
                _registro.Apareceu -= CriarMarcador;
                _registro.Saiu -= RemoverMarcador;
            }

            _coletar?.Dispose();
            _coletar = null;
            if (_material != null)
            {
                Destroy(_material);
            }
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

            // Com janela ou menu do Esc aberto, nem dica nem coleta por baixo.
            var aoAlcance =
                !JanelasDoJogo.AlgoAberto
                && _registro.AlgumAoAlcance(posicao.x, posicao.z, tierMinimo);
            _dica.style.display = aoAlcance ? DisplayStyle.Flex : DisplayStyle.None;
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
            if (_material == null)
            {
                _material = new Material(renderizador.sharedMaterial) { color = CorDoMarcador };
            }

            renderizador.sharedMaterial = _material;
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
