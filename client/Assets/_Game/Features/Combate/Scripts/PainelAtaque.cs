using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Os números de dano e o aviso de ataque recusado (UI Toolkit montada em código). Só
    /// desenha o que recebe; cores e tamanhos estão em <c>EstiloCombate.uss</c>. O número de um
    /// acerto nasce sobre o alvo, no mundo, e sobe com ele na tela; sem alvo, sobe em torno da mira.
    /// </summary>
    internal sealed class PainelAtaque
    {
        /// <summary>Altura da mira na tela, em fração (de cima para baixo), para o número sem alvo.</summary>
        public const float AlturaDaMira = 0.38f;

        private readonly VisualElement _numeros;
        private readonly Label _aviso;
        private readonly List<Label> _rotulos = new List<Label>();

        public PainelAtaque(VisualElement raiz)
        {
            // Cobre a tela inteira: os números se posicionam em pixels do painel.
            _numeros = new VisualElement { name = "combate.numeros" };
            _numeros.pickingMode = PickingMode.Ignore;
            _numeros.AddToClassList("numeros-de-dano");

            _aviso = new Label { name = "combate.aviso" };
            _aviso.pickingMode = PickingMode.Ignore;
            _aviso.AddToClassList("aviso-de-ataque");
            _aviso.style.display = DisplayStyle.None;

            raiz.Add(_numeros);
            raiz.Add(_aviso);
        }

        public void Mostrar(FilaDeNumeros fila, AvisoTemporario aviso)
        {
            var itens = fila.Itens;
            while (_rotulos.Count < itens.Count)
            {
                var rotulo = new Label();
                rotulo.pickingMode = PickingMode.Ignore;
                rotulo.AddToClassList("dano-numero");
                _numeros.Add(rotulo);
                _rotulos.Add(rotulo);
            }

            var camera = Camera.main;
            var painel = _numeros.panel;
            var tamanho = _numeros.layout.size;
            for (var i = 0; i < _rotulos.Count; i++)
            {
                var rotulo = _rotulos[i];
                if (i >= itens.Count)
                {
                    rotulo.style.display = DisplayStyle.None;
                    continue;
                }

                var numero = itens[i];
                if (!Posicionar(rotulo, numero, camera, painel, tamanho))
                {
                    rotulo.style.display = DisplayStyle.None;
                    continue;
                }

                rotulo.style.display = DisplayStyle.Flex;
                rotulo.text = numero.Texto;
                rotulo.EnableInClassList("dano-pesado", numero.Pesado);
                var visibilidade = 1f - numero.Progresso * numero.Progresso;
                rotulo.style.opacity = visibilidade;
            }

            _aviso.style.display = aviso.Visivel ? DisplayStyle.Flex : DisplayStyle.None;
            if (aviso.Visivel)
            {
                _aviso.text = aviso.Texto;
            }
        }

        // Leva o rótulo ao ponto do número na tela; false se o ponto está atrás da câmera.
        private static bool Posicionar(
            Label rotulo,
            NumeroFlutuante numero,
            Camera camera,
            IPanel painel,
            Vector2 tamanho
        )
        {
            // Centrado no ponto, um pouco acima (o texto nasce sobre a cabeça, não dentro dela).
            rotulo.style.translate = new Translate(
                new Length(-50f, LengthUnit.Percent),
                new Length(-100f, LengthUnit.Percent)
            );

            if (numero.Origem is Vector3 origem && camera != null && painel != null)
            {
                var mundo = ApresentacaoAtaque.PosicaoDoNumero(
                    origem,
                    numero.Progresso,
                    ConfigSensacaoCombate.NumeroSubidaMetros
                );
                if (camera.WorldToViewportPoint(mundo).z <= 0f)
                {
                    return false;
                }

                var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(painel, mundo, camera);
                rotulo.style.left = ponto.x + numero.Deslocamento;
                rotulo.style.top = ponto.y;
                return true;
            }

            rotulo.style.left = tamanho.x * 0.5f + numero.Deslocamento;
            rotulo.style.top =
                tamanho.y * AlturaDaMira
                - ConfigSensacaoCombate.NumeroSubidaPixels * numero.Progresso;
            return true;
        }
    }
}
