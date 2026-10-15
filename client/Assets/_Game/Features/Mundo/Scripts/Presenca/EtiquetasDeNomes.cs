using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// O nome de cada outro jogador sobre a cabeça, em UI Toolkit (Lumora UI v1): um rótulo por
    /// avatar num painel só do Mundo, posicionado pela câmera a cada quadro. Cores e tamanhos
    /// estão em <c>EstiloNomesDeJogadores.uss</c>.
    /// </summary>
    internal sealed class EtiquetasDeNomes
    {
        public const string CaminhoEstilo = "UI/EstiloNomesDeJogadores";
        public const string ClasseCamada = "nomes-camada";
        public const string ClasseNome = "nome-jogador";

        /// <summary>Altura (m) acima dos pés em que o nome fica.</summary>
        public const float AlturaDaEtiqueta = 2.2f;

        /// <summary>Acima desta distância (m) da câmera o nome some: não seria legível.</summary>
        public const float DistanciaMaxima = 45f;

        private readonly VisualElement _camada;
        private readonly Dictionary<long, Label> _etiquetas = new Dictionary<long, Label>();
        private readonly HashSet<long> _vistos = new HashSet<long>();
        private readonly List<long> _paraRemover = new List<long>();

        public EtiquetasDeNomes(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
            }

            _camada = new VisualElement { name = "mundo.nomes" };
            _camada.pickingMode = PickingMode.Ignore;
            _camada.AddToClassList(ClasseCamada);
            raiz.Add(_camada);
        }

        /// <summary>O texto mostrado: o nome, ou <c>null</c> se ainda não se sabe (sem etiqueta).</summary>
        public static string TextoDaEtiqueta(string nome) =>
            string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();

        /// <summary>
        /// Se o nome aparece: precisa de nome, estar à frente da câmera e perto o bastante.
        /// </summary>
        public static bool Visivel(string nome, float distanciaDaCamera, bool aFrente) =>
            TextoDaEtiqueta(nome) != null && aFrente && distanciaDaCamera <= DistanciaMaxima;

        /// <summary>
        /// Atualiza as etiquetas deste quadro. <paramref name="camera"/> nula esconde tudo.
        /// </summary>
        public void Mostrar(IEnumerable<AvatarRemoto> avatares, Camera camera)
        {
            _vistos.Clear();
            foreach (var avatar in avatares)
            {
                _vistos.Add(avatar.PersonagemId);
                var texto = TextoDaEtiqueta(avatar.Nome);
                if (texto == null)
                {
                    Esconder(avatar.PersonagemId);
                    continue;
                }

                if (!_etiquetas.TryGetValue(avatar.PersonagemId, out var etiqueta))
                {
                    etiqueta = new Label();
                    etiqueta.pickingMode = PickingMode.Ignore;
                    etiqueta.AddToClassList(ClasseNome);
                    _camada.Add(etiqueta);
                    _etiquetas[avatar.PersonagemId] = etiqueta;
                }

                if (etiqueta.text != texto)
                {
                    etiqueta.text = texto;
                }

                Posicionar(etiqueta, avatar, camera);
            }

            _paraRemover.Clear();
            foreach (var par in _etiquetas)
            {
                if (!_vistos.Contains(par.Key))
                {
                    _paraRemover.Add(par.Key);
                }
            }

            foreach (var id in _paraRemover)
            {
                _etiquetas[id].RemoveFromHierarchy();
                _etiquetas.Remove(id);
            }
        }

        private void Esconder(long personagemId)
        {
            if (_etiquetas.TryGetValue(personagemId, out var etiqueta))
            {
                etiqueta.style.display = DisplayStyle.None;
            }
        }

        private static void Posicionar(Label etiqueta, AvatarRemoto avatar, Camera camera)
        {
            var painel = etiqueta.panel;
            if (camera == null || painel == null)
            {
                etiqueta.style.display = DisplayStyle.None;
                return;
            }

            var mundo = avatar.Posicao + new Vector3(0f, AlturaDaEtiqueta, 0f);
            var aFrente = camera.WorldToViewportPoint(mundo).z > 0f;
            var distancia = Vector3.Distance(camera.transform.position, mundo);
            if (!Visivel(avatar.Nome, distancia, aFrente))
            {
                etiqueta.style.display = DisplayStyle.None;
                return;
            }

            etiqueta.style.display = DisplayStyle.Flex;
            var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(painel, mundo, camera);
            etiqueta.style.left = ponto.x;
            etiqueta.style.top = ponto.y;
        }

        /// <summary>
        /// Cria o painel das etiquetas como filho de <paramref name="pai"/> (some com a cena do
        /// mundo). Abaixo da interface do Core e do HUD de inimigos.
        /// </summary>
        public static EtiquetasDeNomes Criar(Transform pai)
        {
            var objeto = new GameObject("Mundo.NomesDeJogadores");
            objeto.SetActive(false);
            objeto.transform.SetParent(pai, false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelNomesDeJogadores",
                -4f
            );
            objeto.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);
            var container = new VisualElement { name = "MundoNomesInterface" };
            container.AddToClassList(EstiloCore.RaizInterface);
            container.pickingMode = PickingMode.Ignore;
            documento.rootVisualElement.Add(container);
            return new EtiquetasDeNomes(container);
        }
    }
}
