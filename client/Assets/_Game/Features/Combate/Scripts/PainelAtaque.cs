using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Os números de dano e o aviso de ataque recusado (UI Toolkit montada em código). Só
    /// desenha o que recebe; cores e tamanhos estão em <c>EstiloCombate.uss</c>.
    /// </summary>
    internal sealed class PainelAtaque
    {
        /// <summary>Quanto o número sobe até sumir, em pixels de referência.</summary>
        public const float SubidaPixels = 90f;

        private readonly VisualElement _numeros;
        private readonly Label _aviso;
        private readonly List<Label> _rotulos = new List<Label>();

        public PainelAtaque(VisualElement raiz)
        {
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

            for (var i = 0; i < _rotulos.Count; i++)
            {
                var rotulo = _rotulos[i];
                if (i >= itens.Count)
                {
                    rotulo.style.display = DisplayStyle.None;
                    continue;
                }

                var numero = itens[i];
                rotulo.style.display = DisplayStyle.Flex;
                rotulo.text = numero.Texto;
                rotulo.EnableInClassList("dano-pesado", numero.Pesado);
                rotulo.style.translate = new Translate(
                    numero.Deslocamento,
                    -SubidaPixels * numero.Progresso
                );
                var visibilidade = 1f - numero.Progresso * numero.Progresso;
                rotulo.style.opacity = visibilidade;
            }

            _aviso.style.display = aviso.Visivel ? DisplayStyle.Flex : DisplayStyle.None;
            if (aviso.Visivel)
            {
                _aviso.text = aviso.Texto;
            }
        }
    }
}
