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
        private readonly List<GameObject> _criados = new List<GameObject>();

        [TearDown]
        public void Limpar()
        {
            RegistroVisualJogador.LembrarRacas(null);
            foreach (var go in _criados)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            _criados.Clear();
        }

        [Test]
        public void CarregarVisual_CadaRacaTemOPrefabProprio()
        {
            var vistos = new HashSet<string>();
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
                _criados.Add(visual);

                Assert.IsNotNull(
                    visual,
                    "Resources/"
                        + RegistroVisualJogador.CaminhoDoPrefab(raca)
                        + " não foi encontrado."
                );
                Assert.IsTrue(
                    visual.scene.IsValid(),
                    "Deve ser a instância com a aparência aplicada."
                );
                Assert.IsNotNull(visual.GetComponentInChildren<Animator>());
                var nomeBase = visual.name.Replace("(Clone)", "").Trim();
                Assert.IsTrue(vistos.Add(nomeBase), "Prefab repetido para " + raca);
                id++;
            }
        }

        [Test]
        public void CarregarVisual_PersonagemDesconhecido_UsaARacaPadrao()
        {
            var esperado = Resources.Load<GameObject>(
                RegistroVisualJogador.CaminhoDoPrefab(RegistroVisualJogador.RacaPadrao)
            );

            var visual = RegistroVisualJogador.CarregarVisual(99);
            _criados.Add(visual);

            Assert.IsNotNull(visual);
            StringAssert.StartsWith(esperado.name, visual.name);
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
            _criados.Add(silvano);

            RegistroVisualJogador.LembrarRacas(
                new[]
                {
                    new PersonagemResumo { Id = 6, Raca = Raca.Granitico },
                }
            );

            var novo = RegistroVisualJogador.CarregarVisual(5);
            _criados.Add(novo);
            Assert.AreNotSame(silvano, novo);
        }
    }
}
