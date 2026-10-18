using System;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Componente de apresentação visual de um balão de fala individual no UI Toolkit (Lumora).
    /// Projeta a posição 3D do topo da cabeça do avatar para coordenadas de tela do painel
    /// e controla o ciclo de vida, duração proporcional e esmaecimento suave (fade-out).
    /// </summary>
    public sealed class BalaoDeFalaVisual
    {
        public long PersonagemId { get; }
        public string NomeAutor { get; private set; }
        public string Texto { get; private set; }
        public CanalDeChat Canal { get; private set; }
        public float TempoRestante { get; private set; }
        public float DuracaoTotal { get; private set; }
        public Transform TransformAlvo { get; set; }

        public VisualElement Elemento { get; }
        public Label RotuloAutor { get; }
        public Label RotuloTexto { get; }
        public VisualElement Ponta { get; }

        public BalaoDeFalaVisual(
            long personagemId,
            string nomeAutor,
            string texto,
            CanalDeChat canal,
            Transform transformAlvo,
            VisualElement containerPai
        )
        {
            PersonagemId = personagemId;
            TransformAlvo = transformAlvo;

            Elemento = new VisualElement { name = $"balao_{personagemId}" };
            Elemento.pickingMode = PickingMode.Ignore;
            Elemento.AddToClassList(EstiloSocial.BalaoFala);

            RotuloAutor = new Label();
            RotuloAutor.pickingMode = PickingMode.Ignore;
            RotuloAutor.AddToClassList(EstiloSocial.BalaoAutor);
            Elemento.Add(RotuloAutor);

            RotuloTexto = new Label();
            RotuloTexto.pickingMode = PickingMode.Ignore;
            RotuloTexto.AddToClassList(EstiloSocial.BalaoTexto);
            Elemento.Add(RotuloTexto);

            Ponta = new VisualElement { name = "ponta" };
            Ponta.pickingMode = PickingMode.Ignore;
            Ponta.AddToClassList(EstiloSocial.BalaoPonta);
            Elemento.Add(Ponta);

            containerPai.Add(Elemento);

            DefinirMensagem(nomeAutor, texto, canal);
        }

        /// <summary>
        /// Atualiza o conteúdo textual e renova o temporizador de exibição.
        /// </summary>
        public void DefinirMensagem(string nomeAutor, string texto, CanalDeChat canal)
        {
            NomeAutor = nomeAutor ?? string.Empty;
            Texto = texto ?? string.Empty;
            Canal = canal;

            if (!string.IsNullOrWhiteSpace(NomeAutor))
            {
                RotuloAutor.text = NomeAutor;
                RotuloAutor.style.display = DisplayStyle.Flex;
            }
            else
            {
                RotuloAutor.style.display = DisplayStyle.None;
            }

            RotuloTexto.text = Texto;

            DuracaoTotal = RegrasBaloesDeFala.CalcularDuracao(Texto);
            TempoRestante = DuracaoTotal;
            Elemento.style.opacity = 1f;
        }

        /// <summary>
        /// Atualiza o temporizador, fade-out e posicionamento projetado do balão de fala.
        /// Retorna <c>true</c> se a duração expirou e o balão deve ser removido.
        /// </summary>
        public bool Atualizar(float deltaTime, Camera camera, VisualElement painel)
        {
            TempoRestante -= deltaTime;
            if (TempoRestante <= 0f)
            {
                Elemento.style.display = DisplayStyle.None;
                return true;
            }

            // Animação de fade-out suave nos últimos segundos
            if (TempoRestante < RegrasBaloesDeFala.TempoFadeOutSegundos)
            {
                float alpha = Mathf.Clamp01(
                    TempoRestante / RegrasBaloesDeFala.TempoFadeOutSegundos
                );
                Elemento.style.opacity = alpha;
            }
            else
            {
                Elemento.style.opacity = 1f;
            }

            var iPanel = Elemento.panel ?? painel?.panel;
            if (TransformAlvo == null || camera == null || iPanel == null)
            {
                Elemento.style.display = DisplayStyle.None;
                return false;
            }

            var posMundo =
                TransformAlvo.position
                + new Vector3(0f, RegrasBaloesDeFala.AlturaSobreCabecaMetros, 0f);
            var viewport = camera.WorldToViewportPoint(posMundo);
            bool aFrente = viewport.z > 0f;
            float distancia = Vector3.Distance(camera.transform.position, posMundo);

            if (RegrasBaloesDeFala.DeveEstarVisivel(Canal, distancia, aFrente))
            {
                Elemento.style.display = DisplayStyle.Flex;
                var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(iPanel, posMundo, camera);
                Elemento.style.left = ponto.x;
                Elemento.style.top = ponto.y;
            }
            else
            {
                Elemento.style.display = DisplayStyle.None;
            }

            return false;
        }

        /// <summary>
        /// Remove o elemento da árvore do UI Toolkit.
        /// </summary>
        public void Descartar()
        {
            Elemento.RemoveFromHierarchy();
        }
    }
}
