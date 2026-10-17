using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// O indicador de guarda segurada, logo acima das barras: aparece enquanto a guarda está
    /// de pé e muda de cor quando a stamina está acabando. Só desenha o que recebe; cores e
    /// tamanhos estão em <c>EstiloCombate.uss</c>.
    /// </summary>
    internal sealed class PainelDefesa
    {
        public const string TextoGuarda = "Guarda";
        public const string TextoDefesaInsuficiente =
            ApresentacaoDefesa.TextoDefesaInsuficientePadrao;

        private readonly Label _indicador;

        public PainelDefesa(VisualElement raiz)
        {
            _indicador = new Label(TextoGuarda) { name = "combate.guarda" };
            _indicador.pickingMode = PickingMode.Ignore;
            _indicador.AddToClassList("guarda-indicador");
            _indicador.style.display = DisplayStyle.None;
            raiz.Add(_indicador);
        }

        public void Mostrar(bool segurando, bool staminaBaixo)
        {
            _indicador.style.display = segurando ? DisplayStyle.Flex : DisplayStyle.None;
            _indicador.EnableInClassList("guarda-fraca", staminaBaixo);
        }
    }
}
