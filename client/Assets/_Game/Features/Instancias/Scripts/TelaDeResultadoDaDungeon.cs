using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Instancias;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>
    /// O resultado da corrida (vitória, derrota ou instância fechada) num painel Lumora no alto
    /// da tela. Não pega o mouse (o cursor fica preso durante o jogo): fecha sozinho depois de
    /// <see cref="SegundosNaTela"/> ou com a tecla de interagir.
    /// </summary>
    internal sealed class TelaDeResultadoDaDungeon
    {
        /// <summary>Quanto o painel fica na tela se ninguém o fechar (s).</summary>
        public const float SegundosNaTela = 12f;

        private readonly VisualElement _painel;
        private readonly Label _titulo;
        private readonly Label _detalhe;
        private readonly Label _dica;
        private float _fechaEm;

        public TelaDeResultadoDaDungeon(VisualElement raiz, string rotuloDaTecla)
        {
            _painel = ComponentesVisuais.Painel();
            _painel.name = "InstanciasResultado";
            _painel.pickingMode = PickingMode.Ignore;
            _painel.style.position = Position.Absolute;
            _painel.style.top = Length.Percent(14);
            _painel.style.alignSelf = Align.Center;
            _painel.style.minWidth = 380;
            _painel.style.maxWidth = Length.Percent(80);
            _painel.style.alignItems = Align.Center;

            _titulo = ComponentesVisuais.Texto(string.Empty, titulo: true);
            _detalhe = ComponentesVisuais.Texto(string.Empty);
            _dica = ComponentesVisuais.Texto(rotuloDaTecla + ": continuar");
            foreach (var texto in new[] { _titulo, _detalhe, _dica })
            {
                texto.pickingMode = PickingMode.Ignore;
                texto.style.whiteSpace = WhiteSpace.Normal;
                texto.style.unityTextAlign = TextAnchor.MiddleCenter;
            }

            _painel.Add(_titulo);
            _painel.Add(_detalhe);
            _painel.Add(_dica);
            _painel.style.display = DisplayStyle.None;
            raiz.Add(_painel);
        }

        public bool Visivel => _painel.style.display == DisplayStyle.Flex;

        public void Mostrar(ResultadoDungeon resultado)
        {
            _titulo.text = TextosInstancias.TituloDoResultado(resultado);
            _detalhe.text = TextosInstancias.Resultado(resultado);
            _painel.style.display = DisplayStyle.Flex;
            _fechaEm = Time.unscaledTime + SegundosNaTela;
        }

        public void Mostrar(DungeonEncerrada encerrada)
        {
            if (encerrada == null)
            {
                return;
            }

            _titulo.text = TextosInstancias.TituloDoResultado(
                encerrada.Resultado,
                encerrada.Nivel,
                encerrada.NoTempo
            );
            _detalhe.text = TextosInstancias.DetalheDoResultado(
                encerrada.Resultado,
                encerrada.Nivel,
                encerrada.TempoTotalSegundos,
                encerrada.NoTempo,
                encerrada.ProximoNivelLiberado
            );
            _painel.style.display = DisplayStyle.Flex;
            _fechaEm = Time.unscaledTime + SegundosNaTela;
        }

        public void Fechar() => _painel.style.display = DisplayStyle.None;

        public void Atualizar()
        {
            if (Visivel && Time.unscaledTime >= _fechaEm)
            {
                Fechar();
            }
        }
    }
}
