using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Bússola no topo da tela, como no New World: a faixa mostra para onde a câmera olha
    /// (pontos cardeais e marcas a cada 15°) e os marcadores que as funcionalidades pedirem
    /// (missão, grupo, lugares), sem precisar abrir o mapa. O norte é o +Z do mundo, o mesmo
    /// do minimapa.
    /// </summary>
    public sealed class Bussola
    {
        private const float CampoDeVisaoGraus = 180f;
        private const int PassoDasMarcas = 15;

        private static readonly string[] Cardeais = { "N", "NE", "L", "SE", "S", "SO", "O", "NO" };

        private readonly VisualElement _faixa;
        private readonly List<(VisualElement Elemento, float Angulo)> _fixos =
            new List<(VisualElement, float)>();
        private readonly Dictionary<string, Marcador> _marcadores =
            new Dictionary<string, Marcador>();
        private readonly Label _graus;

        private sealed class Marcador
        {
            public VisualElement Elemento;
            public Vector3 Posicao;
        }

        internal Bussola(VisualElement regiao)
        {
            Raiz = new VisualElement { name = "core.bussola", pickingMode = PickingMode.Ignore };
            Raiz.AddToClassList("hud-bussola-corpo");
            _faixa = new VisualElement { pickingMode = PickingMode.Ignore };
            _faixa.AddToClassList("hud-bussola-faixa");
            Raiz.Add(_faixa);

            for (var grau = 0; grau < 360; grau += PassoDasMarcas)
            {
                VisualElement elemento;
                if (grau % 45 == 0)
                {
                    var rotulo = ComponentesHud.Texto(Cardeais[grau / 45], "hud-bussola-cardeal");
                    rotulo.EnableInClassList("hud-bussola-norte", grau == 0);
                    rotulo.EnableInClassList("hud-bussola-colateral", grau % 90 != 0);
                    elemento = rotulo;
                }
                else
                {
                    elemento = new VisualElement { pickingMode = PickingMode.Ignore };
                    elemento.AddToClassList("hud-bussola-marca");
                }

                _faixa.Add(elemento);
                _fixos.Add((elemento, grau));
            }

            var centro = new VisualElement { pickingMode = PickingMode.Ignore };
            centro.AddToClassList("hud-bussola-centro");
            Raiz.Add(centro);
            _graus = ComponentesHud.Texto(string.Empty, "hud-bussola-graus");
            Raiz.Add(_graus);
            regiao.Add(Raiz);
            Raiz.schedule.Execute(Atualizar).Every(16);
        }

        public VisualElement Raiz { get; }

        /// <summary>
        /// Põe (ou move) um marcador na bússola. <paramref name="classe"/> dá a cor/forma
        /// (<c>hud-bussola-missao</c>, <c>hud-bussola-grupo</c>, <c>hud-bussola-lugar</c>).
        /// </summary>
        public void Marcar(string id, Vector3 posicao, string classe, string texto = null)
        {
            if (string.IsNullOrEmpty(id))
                return;
            if (!_marcadores.TryGetValue(id, out var marcador))
            {
                var elemento = new Label(texto ?? string.Empty)
                {
                    pickingMode = PickingMode.Ignore,
                };
                elemento.AddToClassList("hud-bussola-marcador");
                if (!string.IsNullOrEmpty(classe))
                    elemento.AddToClassList(classe);
                _faixa.Add(elemento);
                marcador = new Marcador { Elemento = elemento };
                _marcadores[id] = marcador;
            }

            marcador.Posicao = posicao;
        }

        public void Desmarcar(string id)
        {
            if (id != null && _marcadores.TryGetValue(id, out var marcador))
            {
                marcador.Elemento.RemoveFromHierarchy();
                _marcadores.Remove(id);
            }
        }

        private void Atualizar()
        {
            var camera = Camera.main;
            var largura = _faixa.resolvedStyle.width;
            if (camera == null || float.IsNaN(largura) || largura <= 0f)
                return;

            var direcao = camera.transform.forward;
            var olhar = Mathf.Atan2(direcao.x, direcao.z) * Mathf.Rad2Deg;
            _graus.text = Mathf.RoundToInt(Mathf.Repeat(olhar, 360f)) + "°";
            foreach (var (elemento, angulo) in _fixos)
                Posicionar(elemento, angulo, olhar, largura);

            var origem = camera.transform.position;
            foreach (var marcador in _marcadores.Values)
            {
                var delta = marcador.Posicao - origem;
                var angulo = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                Posicionar(marcador.Elemento, angulo, olhar, largura, true);
            }
        }

        private static void Posicionar(
            VisualElement elemento,
            float angulo,
            float olhar,
            float largura,
            bool presoNaBorda = false
        )
        {
            var diferenca = Mathf.DeltaAngle(olhar, angulo);
            var meio = CampoDeVisaoGraus / 2f;
            if (!presoNaBorda && Mathf.Abs(diferenca) > meio)
            {
                elemento.style.display = DisplayStyle.None;
                return;
            }

            diferenca = Mathf.Clamp(diferenca, -meio, meio);
            var x = largura / 2f + diferenca / meio * (largura / 2f);
            elemento.style.display = DisplayStyle.Flex;
            elemento.style.left = x;
            // Mais apagado longe do centro, como uma faixa curva.
            elemento.style.opacity = Mathf.Lerp(1f, 0.25f, Mathf.Abs(diferenca) / meio);
        }
    }
}
