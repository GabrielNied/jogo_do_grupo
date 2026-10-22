using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Nome e título dos NPCs sobre a cabeça, em UI Toolkit (nítido em qualquer distância), como
    /// no World of Warcraft: nome em verde (amigável), o papel entre &lt; &gt; embaixo e, perto o
    /// bastante para conversar, a dica "[E] Falar". Substitui a placa 3D de texto. Some longe
    /// da câmera ou atrás dela.
    /// </summary>
    internal sealed class EtiquetasDeNpcs : MonoBehaviour
    {
        private const string CaminhoEstilo = "UI/EstiloNpcs";
        private const float DistanciaMaxima = 40f;

        private VisualElement _camada;
        private readonly Dictionary<NpcNoMundo, Etiqueta> _etiquetas =
            new Dictionary<NpcNoMundo, Etiqueta>();
        private readonly List<NpcNoMundo> _remover = new List<NpcNoMundo>();

        private sealed class Etiqueta
        {
            public VisualElement Raiz;
            public Label Nome;
            public Label Titulo;
            public Label Falar;
        }

        public static EtiquetasDeNpcs Criar(Scene cena)
        {
            var objeto = new GameObject("Npcs.Etiquetas");
            SceneManager.MoveGameObjectToScene(objeto, cena);
            objeto.SetActive(false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel("PainelNpcs", -3.5f);
            objeto.SetActive(true);
            EstiloCore.Aplicar(documento.rootVisualElement);
            var etiquetas = objeto.AddComponent<EtiquetasDeNpcs>();
            etiquetas.Montar(documento.rootVisualElement);
            return etiquetas;
        }

        private void Montar(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
                raiz.styleSheets.Add(estilo);
            _camada = new VisualElement
            {
                name = "npcs.etiquetas",
                pickingMode = PickingMode.Ignore,
            };
            _camada.AddToClassList("npcs-camada");
            raiz.Add(_camada);
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (_camada == null)
                return;

            foreach (var npc in NpcNoMundo.Ativos)
            {
                if (!_etiquetas.TryGetValue(npc, out var etiqueta))
                {
                    etiqueta = Criar(npc);
                    _etiquetas[npc] = etiqueta;
                    _camada.Add(etiqueta.Raiz);
                }

                Posicionar(npc, etiqueta, camera);
            }

            _remover.Clear();
            foreach (var par in _etiquetas)
            {
                if (par.Key == null || !NpcNoMundo.EstaAtivo(par.Key))
                    _remover.Add(par.Key);
            }

            foreach (var npc in _remover)
            {
                _etiquetas[npc].Raiz.RemoveFromHierarchy();
                _etiquetas.Remove(npc);
            }
        }

        private static Etiqueta Criar(NpcNoMundo npc)
        {
            var rotuloTecla = TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir);
            var etiqueta = new Etiqueta
            {
                Raiz = new VisualElement { pickingMode = PickingMode.Ignore },
                Nome = ComponentesHud.Texto(npc.Definicao?.Nome ?? string.Empty, "npc-nome"),
                Titulo = ComponentesHud.Texto(
                    npc.Definicao != null
                        ? "<" + NpcNoMundo.ObterTituloDoPapel(npc.Definicao.Papel) + ">"
                        : string.Empty,
                    "npc-titulo"
                ),
                Falar = ComponentesHud.Texto($"[{rotuloTecla}] Falar", "npc-falar"),
            };
            etiqueta.Raiz.AddToClassList("npc-etiqueta");
            etiqueta.Raiz.Add(etiqueta.Nome);
            etiqueta.Raiz.Add(etiqueta.Titulo);
            etiqueta.Raiz.Add(etiqueta.Falar);
            return etiqueta;
        }

        private static void Posicionar(NpcNoMundo npc, Etiqueta etiqueta, Camera camera)
        {
            var painel = etiqueta.Raiz.panel;
            if (camera == null || painel == null || npc == null)
            {
                etiqueta.Raiz.style.display = DisplayStyle.None;
                return;
            }

            var mundo = npc.transform.position + Vector3.up * NpcNoMundo.AlturaPlaca;
            var viewport = camera.WorldToViewportPoint(mundo);
            var distancia = Vector3.Distance(camera.transform.position, mundo);
            if (viewport.z <= 0f || distancia > DistanciaMaxima)
            {
                etiqueta.Raiz.style.display = DisplayStyle.None;
                return;
            }

            var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(painel, mundo, camera);
            etiqueta.Raiz.style.display = DisplayStyle.Flex;
            etiqueta.Raiz.style.left = ponto.x;
            etiqueta.Raiz.style.top = ponto.y;
            // Mais longe, menor e mais transparente, para a praça cheia não virar uma parede de texto.
            var proximidade = 1f - Mathf.Clamp01((distancia - 8f) / (DistanciaMaxima - 8f));
            etiqueta.Raiz.style.opacity = Mathf.Lerp(0.55f, 1f, proximidade);
            var escala = Mathf.Lerp(0.8f, 1f, proximidade);
            etiqueta.Raiz.style.scale = new Scale(new Vector3(escala, escala, 1f));

            var jogador = EntradaNoJogo.Jogador;
            var posJogador =
                jogador != null ? jogador.transform.position : camera.transform.position;
            var ehOMaisProximo =
                npc.JogadorNoAlcance
                && RegistroDeInteracao.EhOMaisProximo(npc, posJogador.x, posJogador.z);
            etiqueta.Falar.text = $"[{TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir)}] Falar";
            etiqueta.Falar.style.display = ehOMaisProximo ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
