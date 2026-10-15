using System;
using JogoDoGrupo.Client.Core.Jogo;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class SintoniaTestes
    {
        private static float _pausa;
        private static float _tremor;

        [SetUp]
        public void Preparar()
        {
            Sintonia.Limpar();
            _pausa = 0.07f;
            _tremor = 0.05f;
            Sintonia.Registrar("Combate", "Pausa", () => _pausa, v => _pausa = v, 0f, 0.3f, "s");
            Sintonia.Registrar("Combate", "Tremor", () => _tremor, v => _tremor = v, 0f, 0.2f);
        }

        [TearDown]
        public void Limpar() => Sintonia.Limpar();

        [Test]
        public void DefinirMudaOValorDentroDosLimitesEAvisa()
        {
            var avisos = 0;
            Action contar = () => avisos++;
            Sintonia.Mudou += contar;
            try
            {
                Assert.IsTrue(Sintonia.Definir("Combate.Pausa", 0.1f));
                Assert.AreEqual(0.1f, _pausa, 0.0001f);

                Assert.IsTrue(Sintonia.Definir("Combate.Pausa", 5f));
                Assert.AreEqual(0.3f, _pausa, 0.0001f);

                Assert.IsFalse(Sintonia.Definir("Combate.NaoExiste", 1f));
                Assert.AreEqual(2, avisos);
            }
            finally
            {
                Sintonia.Mudou -= contar;
            }
        }

        [Test]
        public void AlteradosListaSoOQueSaiuDoPadraoComPontoDecimal()
        {
            Assert.AreEqual("", Sintonia.Alterados());

            Sintonia.Definir("Combate.Tremor", 0.125f);

            Assert.AreEqual("Combate.Tremor = 0.125 (padrão 0.05)\n", Sintonia.Alterados());
        }

        [Test]
        public void RestaurarTodosVoltaAoPadrao()
        {
            Sintonia.Definir("Combate.Pausa", 0.2f);
            Sintonia.Definir("Combate.Tremor", 0f);

            Sintonia.RestaurarTodos();

            Assert.AreEqual(0.07f, _pausa, 0.0001f);
            Assert.AreEqual(0.05f, _tremor, 0.0001f);
            Assert.IsFalse(Sintonia.Procurar("Combate.Pausa").Alterado);
        }

        [Test]
        public void RegistrarDeNovoSubstituiSemDuplicar()
        {
            Sintonia.Registrar("Combate", "Pausa", () => _pausa, v => _pausa = v, 0f, 1f);

            Assert.AreEqual(2, Sintonia.Ajustes.Count);
            Assert.AreEqual(1f, Sintonia.Procurar("Combate.Pausa").Maximo);
        }

        [Test]
        public void RegistroInvalidoFalha()
        {
            Assert.Throws<ArgumentException>(() =>
                Sintonia.Registrar("", "X", () => 0f, _ => { }, 0f, 1f)
            );
            Assert.Throws<ArgumentException>(() =>
                Sintonia.Registrar("G", "X", () => 0f, _ => { }, 2f, 1f)
            );
            Assert.Throws<ArgumentNullException>(() =>
                Sintonia.Registrar("G", "X", null, _ => { }, 0f, 1f)
            );
        }
    }
}
