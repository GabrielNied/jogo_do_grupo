using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// A camada de UI dos inimigos: nome e barra de vida sobre a cabeça de cada um e o círculo
    /// de aviso do golpe no chão. Só desenha o que recebe; cores e tamanhos estão em
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
        public const string ClasseCamadaDeAviso = "inimigo-aviso";

        /// <summary>Quantos lados tem o polígono que representa o círculo do aviso.</summary>
        private const int LadosDoCirculo = 40;

        private static readonly CustomStyleProperty<Color> CorDoAviso =
            new CustomStyleProperty<Color>("--cor-aviso");

        private static readonly CustomStyleProperty<Color> CorDoPreenchimento =
            new CustomStyleProperty<Color>("--cor-aviso-preenchimento");

        private readonly VisualElement _camada;
        private readonly VisualElement _camadaDeAviso;
        private readonly Dictionary<long, Etiqueta> _etiquetas = new Dictionary<long, Etiqueta>();
        private readonly List<long> _paraRemover = new List<long>();

        private Color _corDoAviso = new Color(1f, 0f, 0f, 0.8f);
        private Color _corDoPreenchimento = new Color(1f, 0f, 0f, 0.3f);
        private IReadOnlyCollection<InimigoNoClient> _inimigos;
        private Camera _camera;

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

            _camadaDeAviso = new VisualElement { name = "mobs.aviso" };
            _camadaDeAviso.pickingMode = PickingMode.Ignore;
            _camadaDeAviso.AddToClassList(ClasseCamadaDeAviso);
            _camadaDeAviso.RegisterCallback<CustomStyleResolvedEvent>(AoResolverEstilo);
            _camadaDeAviso.generateVisualContent += DesenharAvisos;
            _camada.Add(_camadaDeAviso);

            raiz.Add(_camada);
        }

        /// <summary>
        /// Atualiza etiquetas e avisos para este quadro. <paramref name="camera"/> nula esconde
        /// tudo (sem câmera não há onde projetar).
        /// </summary>
        public void Mostrar(IReadOnlyCollection<InimigoNoClient> inimigos, Camera camera)
        {
            _inimigos = inimigos;
            _camera = camera;
            _paraRemover.Clear();
            foreach (var par in _etiquetas)
            {
                if (!Contem(inimigos, par.Key))
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
                if (!_etiquetas.TryGetValue(inimigo.InimigoId, out var etiqueta))
                {
                    etiqueta = new Etiqueta(inimigo);
                    _camada.Add(etiqueta.Raiz);
                    _etiquetas[inimigo.InimigoId] = etiqueta;
                }

                etiqueta.Mostrar(inimigo, camera);
            }

            _camadaDeAviso.MarkDirtyRepaint();
        }

        private static bool Contem(IReadOnlyCollection<InimigoNoClient> inimigos, long id)
        {
            foreach (var inimigo in inimigos)
            {
                if (inimigo.InimigoId == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void AoResolverEstilo(CustomStyleResolvedEvent evento)
        {
            if (evento.customStyle.TryGetValue(CorDoAviso, out var contorno))
            {
                _corDoAviso = contorno;
            }

            if (evento.customStyle.TryGetValue(CorDoPreenchimento, out var preenchimento))
            {
                _corDoPreenchimento = preenchimento;
            }
        }

        private void DesenharAvisos(MeshGenerationContext contexto)
        {
            if (_inimigos == null || _camera == null)
            {
                return;
            }

            var painel = _camadaDeAviso.panel;
            if (painel == null)
            {
                return;
            }

            var pintor = contexto.painter2D;
            foreach (var inimigo in _inimigos)
            {
                var aviso = inimigo.Aviso;
                if (aviso == null)
                {
                    continue;
                }

                // O preenchimento cresce do centro até a borda: o golpe cai quando enche.
                DesenharCirculo(pintor, painel, aviso, inimigo.AlvoY, 1f, _corDoAviso, false);
                DesenharCirculo(
                    pintor,
                    painel,
                    aviso,
                    inimigo.AlvoY,
                    aviso.Progresso,
                    _corDoPreenchimento,
                    true
                );
            }
        }

        private void DesenharCirculo(
            Painter2D pintor,
            IPanel painel,
            AvisoDeGolpe aviso,
            float alturaDoChao,
            float fracaoDoRaio,
            Color cor,
            bool preencher
        )
        {
            var raio = aviso.Raio * fracaoDoRaio;
            if (raio <= 0f)
            {
                return;
            }

            pintor.BeginPath();
            for (var i = 0; i < LadosDoCirculo; i++)
            {
                var angulo = i * Mathf.PI * 2f / LadosDoCirculo;
                var mundo = new Vector3(
                    aviso.CentroX + Mathf.Sin(angulo) * raio,
                    alturaDoChao,
                    aviso.CentroZ + Mathf.Cos(angulo) * raio
                );
                var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(painel, mundo, _camera);
                var local = _camadaDeAviso.WorldToLocal(ponto);
                if (i == 0)
                {
                    pintor.MoveTo(local);
                }
                else
                {
                    pintor.LineTo(local);
                }
            }

            pintor.ClosePath();
            if (preencher)
            {
                pintor.fillColor = cor;
                pintor.Fill();
            }
            else
            {
                pintor.strokeColor = cor;
                pintor.lineWidth = 3f;
                pintor.Stroke();
            }
        }

        private sealed class Etiqueta
        {
            /// <summary>Altura acima do chão, em metros, onde fica a etiqueta (na escala 1).</summary>
            private const float AlturaDaEtiqueta = 2.1f;

            private readonly VisualElement _preenchimento;

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
                Raiz.Add(nome);
                Raiz.Add(barra);
            }

            public VisualElement Raiz { get; }

            public void Mostrar(InimigoNoClient inimigo, Camera camera)
            {
                _preenchimento.style.width = Length.Percent(inimigo.FracaoDeVida * 100f);

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
