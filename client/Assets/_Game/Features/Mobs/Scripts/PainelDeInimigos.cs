using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// A camada de UI dos inimigos: nome, barra de vida e a marca "Exposto" sobre a cabeça de
    /// cada um. O aviso do golpe no chão é malha no mundo (<see cref="AvisosNoChao"/>). Só
    /// desenha o que recebe; cores e tamanhos estão em
    /// <c>EstiloMobs.uss</c>.
    /// </summary>
    internal sealed class PainelDeInimigos
    {
        public const string CaminhoEstilo = "UI/EstiloMobs";

        public const string ClasseCamada = "inimigos-camada";
        public const string ClasseEtiqueta = "inimigo-etiqueta";
        public const string ClasseEtiquetaFortalecida = "inimigo-etiqueta-fortalecida";
        public const string ClasseNome = "inimigo-nome";
        public const string ClasseBarra = "inimigo-barra";
        public const string ClassePreenchimento = "inimigo-preenchimento";
        public const string ClasseExposto = "inimigo-exposto";

        private readonly VisualElement _camada;
        private readonly Dictionary<long, Etiqueta> _etiquetas = new Dictionary<long, Etiqueta>();
        private readonly List<long> _paraRemover = new List<long>();

        public PainelDeInimigos(VisualElement raiz)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                raiz.styleSheets.Add(estilo);
            }

            _camada = new VisualElement { name = "mobs.camada" };
            _camada.pickingMode = PickingMode.Ignore;
            _camada.AddToClassList(ClasseCamada);

            raiz.Add(_camada);
        }

        /// <summary>
        /// Atualiza etiquetas e avisos para este quadro. <paramref name="camera"/> nula esconde
        /// tudo (sem câmera não há onde projetar).
        /// </summary>
        public void Mostrar(
            IReadOnlyCollection<InimigoNoClient> inimigos,
            Camera camera,
            (float X, float Z)? jogadorNoChao = null
        )
        {
            var ids = new HashSet<long>();
            foreach (var inimigo in inimigos)
            {
                ids.Add(inimigo.InimigoId);
            }

            _paraRemover.Clear();
            foreach (var par in _etiquetas)
            {
                if (!ids.Contains(par.Key))
                {
                    _paraRemover.Add(par.Key);
                }
            }

            foreach (var id in _paraRemover)
            {
                _etiquetas[id].Raiz.RemoveFromHierarchy();
                _etiquetas.Remove(id);
            }

            foreach (var inimigo in inimigos)
            {
                if (
                    jogadorNoChao.HasValue
                    && RegrasInteresseDeInimigos.DistanciaAoJogador(
                        jogadorNoChao.Value.X,
                        jogadorNoChao.Value.Z,
                        inimigo.X,
                        inimigo.Z
                    ) > RegrasInteresseDeInimigos.EtiquetaMetros
                )
                {
                    if (_etiquetas.TryGetValue(inimigo.InimigoId, out var escondida))
                    {
                        escondida.Raiz.style.display = DisplayStyle.None;
                    }

                    continue;
                }

                if (!_etiquetas.TryGetValue(inimigo.InimigoId, out var etiqueta))
                {
                    etiqueta = new Etiqueta(inimigo);
                    _camada.Add(etiqueta.Raiz);
                    _etiquetas[inimigo.InimigoId] = etiqueta;
                }

                etiqueta.Mostrar(inimigo, camera);
            }
        }

        private sealed class Etiqueta
        {
            /// <summary>Altura acima do chão, em metros, onde fica a etiqueta (na escala 1).</summary>
            private const float AlturaDaEtiqueta = 2.1f;

            private readonly VisualElement _preenchimento;
            private readonly Label _exposto;

            public Etiqueta(InimigoNoClient inimigo)
            {
                Raiz = new VisualElement();
                Raiz.pickingMode = PickingMode.Ignore;
                Raiz.AddToClassList(ClasseEtiqueta);
                if (inimigo.Fortalecido)
                {
                    Raiz.AddToClassList(ClasseEtiquetaFortalecida);
                }

                var nome = new Label(inimigo.Rotulo);
                nome.pickingMode = PickingMode.Ignore;
                nome.AddToClassList(ClasseNome);

                var barra = new VisualElement();
                barra.pickingMode = PickingMode.Ignore;
                barra.AddToClassList(ClasseBarra);

                _preenchimento = new VisualElement();
                _preenchimento.pickingMode = PickingMode.Ignore;
                _preenchimento.AddToClassList(ClassePreenchimento);

                barra.Add(_preenchimento);
                _exposto = new Label();
                _exposto.pickingMode = PickingMode.Ignore;
                _exposto.AddToClassList(ClasseExposto);
                _exposto.style.display = DisplayStyle.None;

                Raiz.Add(_exposto);
                Raiz.Add(nome);
                Raiz.Add(barra);
            }

            public VisualElement Raiz { get; }

            public void Mostrar(InimigoNoClient inimigo, Camera camera)
            {
                _preenchimento.style.width = Length.Percent(inimigo.FracaoDeVida * 100f);
                _exposto.style.display = inimigo.Exposto ? DisplayStyle.Flex : DisplayStyle.None;
                if (inimigo.Exposto)
                {
                    _exposto.text = "Exposto x" + inimigo.ExpostoMultiplicador.ToString("0.##");
                }

                var painel = Raiz.panel;
                if (camera == null || painel == null)
                {
                    Raiz.style.display = DisplayStyle.None;
                    return;
                }

                var mundo = new Vector3(
                    inimigo.X,
                    inimigo.Y + AlturaDaEtiqueta * inimigo.Escala,
                    inimigo.Z
                );
                var atras = camera.WorldToViewportPoint(mundo).z <= 0f;
                Raiz.style.display = atras ? DisplayStyle.None : DisplayStyle.Flex;
                if (atras)
                {
                    return;
                }

                var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(painel, mundo, camera);
                Raiz.style.left = ponto.x;
                Raiz.style.top = ponto.y;
            }
        }
    }
}
