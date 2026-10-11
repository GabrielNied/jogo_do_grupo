using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem.Tests
{
    /// <summary>
    /// Visual registrado no Core: o prefab humano existe em <c>Resources</c>, fora de cena (o
    /// Core instancia) e com Animator.
    /// </summary>
    public class RegistroVisualJogadorTestes
    {
        [Test]
        public void CarregarVisual_DevolveOPrefabHumano_ForaDeCena()
        {
            var visual = RegistroVisualJogador.CarregarVisual(1);

            Assert.IsNotNull(visual, "Resources/Personagem/Jogador_Humano não foi encontrado.");
            Assert.IsFalse(visual.scene.IsValid(), "Deve ser o prefab; o Core instancia.");
        }

        [Test]
        public void CarregarVisual_TemAnimatorEIndependeDoId()
        {
            var primeiro = RegistroVisualJogador.CarregarVisual(1);
            var segundo = RegistroVisualJogador.CarregarVisual(99);

            Assert.AreSame(primeiro, segundo);
            Assert.IsNotNull(primeiro.GetComponentInChildren<Animator>());
        }
    }
}
