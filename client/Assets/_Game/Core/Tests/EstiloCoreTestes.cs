using JogoDoGrupo.Client.Core.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// A raiz da interface ignora o mouse para o clique chegar nos painéis que ficam atrás,
    /// como o Fechar do inventário.
    /// </summary>
    public class EstiloCoreTestes
    {
        [Test]
        public void NaoBloquearCliques_IgnoraOPonteiro()
        {
            var elemento = new VisualElement();

            EstiloCore.NaoBloquearCliques(elemento);

            Assert.AreEqual(PickingMode.Ignore, elemento.pickingMode);
        }

        [Test]
        public void NaoBloquearCliques_NuloNaoFalha()
        {
            Assert.DoesNotThrow(() => EstiloCore.NaoBloquearCliques(null));
        }
    }
}
