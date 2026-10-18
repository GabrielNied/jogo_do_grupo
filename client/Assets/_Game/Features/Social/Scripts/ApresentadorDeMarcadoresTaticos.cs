using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Apresenta visualmente os marcadores táticos de grupo sobre os alvos e inimigos no mundo 3D.
    /// </summary>
    public sealed class ApresentadorDeMarcadoresTaticos : MonoBehaviour
    {
        public const string NomeObjeto = "Social.MarcadoresTaticos";
        public const float AlturaDoMarcadorMetros = 2.8f;
        public const float DistanciaMaximaMetros = 50f;

        private readonly Dictionary<long, Label> _elementos = new Dictionary<long, Label>();
        private readonly List<long> _paraRemover = new List<long>();

        private VisualElement _camada;
        private Camera _camera;
        private EstadoMarcadoresTaticos _estado;

        public IReadOnlyDictionary<long, Label> ElementosAtivos => _elementos;

        public void Configurar(
            VisualElement camada,
            Camera camera = null,
            EstadoMarcadoresTaticos estado = null
        )
        {
            _camada = camada;
            _camera = camera;
            _estado = estado ?? EstadoMarcadoresTaticos.Instancia;
            if (_estado != null)
            {
                _estado.Mudou += AoMudarMarcadores;
            }
        }

        private void OnDestroy()
        {
            if (_estado != null)
            {
                _estado.Mudou -= AoMudarMarcadores;
            }

            DescartarTudo();
        }

        private void LateUpdate()
        {
            Atualizar(null);
        }

        private void AoMudarMarcadores()
        {
            // Sincroniza alvos removidos
            _paraRemover.Clear();
            foreach (var alvoId in _elementos.Keys)
            {
                if (_estado == null || !_estado.Marcadores.ContainsKey(alvoId))
                {
                    _paraRemover.Add(alvoId);
                }
            }

            foreach (var id in _paraRemover)
            {
                if (_elementos.TryGetValue(id, out var rotulo))
                {
                    rotulo.RemoveFromHierarchy();
                    _elementos.Remove(id);
                }
            }
        }

        /// <summary>
        /// Atualiza as posições de tela de todos os marcadores táticos ativos.
        /// </summary>
        public void Atualizar(Func<long, Transform> localizadorAlvo = null)
        {
            if (_camada == null || _estado == null)
            {
                return;
            }

            var cam = _camera != null ? _camera : Camera.main;
            var iPanel = _camada.panel;
            if (cam == null || iPanel == null)
            {
                foreach (var rotulo in _elementos.Values)
                {
                    rotulo.style.display = DisplayStyle.None;
                }
                return;
            }

            foreach (var par in _estado.Marcadores)
            {
                long alvoId = par.Key;
                TipoMarcadorDeAlvo marcador = par.Value;

                var transformAlvo =
                    localizadorAlvo != null ? localizadorAlvo(alvoId) : ResolverAlvo(alvoId);

                if (transformAlvo == null)
                {
                    if (_elementos.TryGetValue(alvoId, out var rInvisivel))
                    {
                        rInvisivel.style.display = DisplayStyle.None;
                    }
                    continue;
                }

                var posMundo = transformAlvo.position + new Vector3(0f, AlturaDoMarcadorMetros, 0f);
                var viewport = cam.WorldToViewportPoint(posMundo);
                bool aFrente = viewport.z > 0f;
                float distancia = Vector3.Distance(cam.transform.position, posMundo);

                if (aFrente && distancia <= DistanciaMaximaMetros)
                {
                    if (!_elementos.TryGetValue(alvoId, out var rotulo))
                    {
                        rotulo = CriarRotulo(alvoId);
                        _elementos[alvoId] = rotulo;
                    }

                    ConfigurarRotulo(rotulo, marcador);
                    rotulo.style.display = DisplayStyle.Flex;
                    var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(
                        iPanel,
                        posMundo,
                        cam
                    );
                    rotulo.style.left = ponto.x;
                    rotulo.style.top = ponto.y;
                }
                else
                {
                    if (_elementos.TryGetValue(alvoId, out var rLonge))
                    {
                        rLonge.style.display = DisplayStyle.None;
                    }
                }
            }
        }

        private Label CriarRotulo(long alvoId)
        {
            var rotulo = new Label { name = $"marcador_{alvoId}" };
            rotulo.pickingMode = PickingMode.Ignore;
            rotulo.AddToClassList(EstiloSocial.MarcadorTatico);
            _camada.Add(rotulo);
            return rotulo;
        }

        private static void ConfigurarRotulo(Label rotulo, TipoMarcadorDeAlvo marcador)
        {
            rotulo.RemoveFromClassList(EstiloSocial.MarcadorCaveira);
            rotulo.RemoveFromClassList(EstiloSocial.MarcadorCruz);
            rotulo.RemoveFromClassList(EstiloSocial.MarcadorEscudo);
            rotulo.RemoveFromClassList(EstiloSocial.MarcadorLua);
            rotulo.RemoveFromClassList(EstiloSocial.MarcadorEstrela);

            switch (marcador)
            {
                case TipoMarcadorDeAlvo.Caveira:
                    rotulo.text = "💀";
                    rotulo.AddToClassList(EstiloSocial.MarcadorCaveira);
                    break;
                case TipoMarcadorDeAlvo.Cruz:
                    rotulo.text = "❌";
                    rotulo.AddToClassList(EstiloSocial.MarcadorCruz);
                    break;
                case TipoMarcadorDeAlvo.Escudo:
                    rotulo.text = "🛡";
                    rotulo.AddToClassList(EstiloSocial.MarcadorEscudo);
                    break;
                case TipoMarcadorDeAlvo.Lua:
                    rotulo.text = "🌙";
                    rotulo.AddToClassList(EstiloSocial.MarcadorLua);
                    break;
                case TipoMarcadorDeAlvo.Estrela:
                    rotulo.text = "⭐";
                    rotulo.AddToClassList(EstiloSocial.MarcadorEstrela);
                    break;
                default:
                    rotulo.text = string.Empty;
                    break;
            }
        }

        public static Transform ResolverAlvo(long alvoId)
        {
            if (alvoId <= 0)
            {
                return null;
            }

            var inimigo = GameObject.Find("Inimigo " + alvoId);
            if (inimigo != null)
            {
                return inimigo.transform;
            }

            var inimigoSub = GameObject.Find("Inimigo_" + alvoId);
            if (inimigoSub != null)
            {
                return inimigoSub.transform;
            }

            var jogador = GameObject.Find("Jogador_" + alvoId);
            if (jogador != null)
            {
                return jogador.transform;
            }

            if (
                EntradaNoJogo.NoJogo
                && EntradaNoJogo.PersonagemId == alvoId
                && EntradaNoJogo.Jogador != null
            )
            {
                return EntradaNoJogo.Jogador.transform;
            }

            return null;
        }

        public void DescartarTudo()
        {
            foreach (var rotulo in _elementos.Values)
            {
                rotulo.RemoveFromHierarchy();
            }
            _elementos.Clear();
            _paraRemover.Clear();
        }

        public static ApresentadorDeMarcadoresTaticos Criar(Scene cena)
        {
            var raiz = new GameObject(NomeObjeto);
            raiz.SetActive(false);
            SceneManager.MoveGameObjectToScene(raiz, cena);

            var documento = raiz.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelMarcadoresTaticos",
                -2.8f
            );
            raiz.SetActive(true);

            EstiloCore.Aplicar(documento.rootVisualElement);
            EstiloSocial.Aplicar(documento.rootVisualElement);

            var camada = new VisualElement { name = "SocialMarcadoresCamada" };
            camada.pickingMode = PickingMode.Ignore;
            camada.AddToClassList(EstiloSocial.MarcadorCamada);
            documento.rootVisualElement.Add(camada);

            var apresentador = raiz.AddComponent<ApresentadorDeMarcadoresTaticos>();
            apresentador.Configurar(camada);

            return apresentador;
        }
    }
}
