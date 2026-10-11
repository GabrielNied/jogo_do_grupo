using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Personagem;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem.Tests
{
    /// <summary>
    /// Visual registrado no Core: cada raça tem o seu prefab em <c>Resources</c>, fora de cena
    /// (o Core instancia) e com Animator.
    /// </summary>
    public class RegistroVisualJogadorTestes
    {
        [TearDown]
        public void Limpar()
        {
            RegistroVisualJogador.LembrarRacas(null);
        }

        [Test]
        public void CarregarVisual_CadaRacaTemOPrefabProprio_ForaDeCena()
        {
            var vistos = new HashSet<GameObject>();
            long id = 1;
            foreach (Raca raca in Enum.GetValues(typeof(Raca)))
            {
                RegistroVisualJogador.LembrarRacas(
                    new[]
                    {
                        new PersonagemResumo { Id = id, Raca = raca },
                    }
                );

                var visual = RegistroVisualJogador.CarregarVisual(id);

                Assert.IsNotNull(
                    visual,
                    "Resources/"
                        + RegistroVisualJogador.CaminhoDoPrefab(raca)
                        + " não foi encontrado."
                );
                Assert.IsFalse(visual.scene.IsValid(), "Deve ser o prefab; o Core instancia.");
                Assert.IsNotNull(visual.GetComponentInChildren<Animator>());
                Assert.IsTrue(vistos.Add(visual), "Prefab repetido para " + raca);
                id++;
            }
        }

        [Test]
        public void CarregarVisual_PersonagemDesconhecido_UsaARacaPadrao()
        {
            var esperado = Resources.Load<GameObject>(
                RegistroVisualJogador.CaminhoDoPrefab(RegistroVisualJogador.RacaPadrao)
            );

            Assert.AreSame(esperado, RegistroVisualJogador.CarregarVisual(99));
        }

        [Test]
        public void LembrarRacas_SubstituiAListaAnterior()
        {
            RegistroVisualJogador.LembrarRacas(
                new[]
                {
                    new PersonagemResumo { Id = 5, Raca = Raca.Silvano },
                }
            );
            var silvano = RegistroVisualJogador.CarregarVisual(5);

            RegistroVisualJogador.LembrarRacas(
                new[]
                {
                    new PersonagemResumo { Id = 6, Raca = Raca.Granitico },
                }
            );

            Assert.AreNotSame(silvano, RegistroVisualJogador.CarregarVisual(5));
        }
    }
}
