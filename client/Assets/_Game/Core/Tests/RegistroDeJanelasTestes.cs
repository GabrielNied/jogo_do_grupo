using System;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// Registro das janelas do jogo: cada funcionalidade registra a sua, e chave ou tecla
    /// inválida, reservada ou repetida é recusada na hora (não na frente do jogador).
    /// </summary>
    public class RegistroDeJanelasTestes
    {
        private static DefinicaoDeJanela Janela(string chave, string tecla, int ordem = 0)
        {
            return new DefinicaoDeJanela(
                chave,
                "Título",
                tecla,
                ordem,
                () => new JanelaEmConstrucao("")
            );
        }

        [Test]
        public void Registrar_GuardaAJanela_EProcurarAcha()
        {
            var registro = new RegistroDeJanelas();
            var inventario = Janela("itens.inventario", "I");

            registro.Registrar(inventario);

            Assert.AreSame(inventario, registro.Procurar("itens.inventario"));
            Assert.AreEqual(1, registro.Definicoes.Count);
        }

        [Test]
        public void Procurar_ChaveDesconhecidaVaziaOuNula_DevolveNulo()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(Janela("itens.inventario", "I"));

            Assert.IsNull(registro.Procurar("mundo.mapa"));
            Assert.IsNull(registro.Procurar(""));
            Assert.IsNull(registro.Procurar(null));
        }

        [Test]
        public void Definicoes_FicamNaOrdemDaBarra_EEmpateSegueORegistro()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(Janela("mundo.mapa", "M", 40));
            registro.Registrar(Janela("personagem.ficha", "C", 10));
            registro.Registrar(Janela("missoes.registro", "L", 30));
            registro.Registrar(Janela("itens.inventario", "I", 20));
            registro.Registrar(Janela("social.grupo", "O", 30));

            var chaves = new string[registro.Definicoes.Count];
            for (var i = 0; i < chaves.Length; i++)
            {
                chaves[i] = registro.Definicoes[i].Chave;
            }

            CollectionAssert.AreEqual(
                new[]
                {
                    "personagem.ficha",
                    "itens.inventario",
                    "missoes.registro",
                    "social.grupo",
                    "mundo.mapa",
                },
                chaves
            );
        }

        [Test]
        public void Tecla_MinusculaOuComEspaco_ViraMaiuscula()
        {
            Assert.AreEqual("I", Janela("itens.inventario", " i ").Tecla);
        }

        [Test]
        public void Registrar_ChaveRepetida_Recusa()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(Janela("itens.inventario", "I"));

            Assert.Throws<ArgumentException>(() =>
                registro.Registrar(Janela("itens.inventario", "B"))
            );
            Assert.AreEqual(1, registro.Definicoes.Count);
        }

        [Test]
        public void Registrar_TeclaRepetida_Recusa_MesmoEmMinuscula()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(Janela("itens.inventario", "I"));

            Assert.Throws<ArgumentException>(() => registro.Registrar(Janela("outra.janela", "i")));
            Assert.AreEqual(1, registro.Definicoes.Count);
        }

        [TestCase("W")]
        [TestCase("A")]
        [TestCase("S")]
        [TestCase("D")]
        [TestCase("Q")]
        [TestCase("E")]
        [TestCase("R")]
        public void Registrar_TeclaDeOutraAcaoDoJogo_Recusa(string tecla)
        {
            var registro = new RegistroDeJanelas();

            Assert.Throws<ArgumentException>(() => registro.Registrar(Janela("x.janela", tecla)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("IJ")]
        [TestCase("1")]
        [TestCase("Ç")]
        [TestCase("Esc")]
        public void Registrar_TeclaQueNaoEUmaLetraDeAaZ_Recusa(string tecla)
        {
            var registro = new RegistroDeJanelas();

            Assert.Throws<ArgumentException>(() => registro.Registrar(Janela("x.janela", tecla)));
        }

        [Test]
        public void Registrar_SemChaveSemTituloOuSemFabrica_Recusa()
        {
            var registro = new RegistroDeJanelas();

            Assert.Throws<ArgumentNullException>(() => registro.Registrar(null));
            Assert.Throws<ArgumentException>(() => registro.Registrar(Janela("", "I")));
            Assert.Throws<ArgumentException>(() =>
                registro.Registrar(
                    new DefinicaoDeJanela("x.janela", "", "I", 0, () => new JanelaEmConstrucao(""))
                )
            );
            Assert.Throws<ArgumentNullException>(() =>
                registro.Registrar(new DefinicaoDeJanela("x.janela", "Título", "I", 0, null))
            );
            Assert.AreEqual(0, registro.Definicoes.Count);
        }

        [Test]
        public void Limpar_EsvaziaORegistro()
        {
            var registro = new RegistroDeJanelas();
            registro.Registrar(Janela("itens.inventario", "I"));

            registro.Limpar();

            Assert.AreEqual(0, registro.Definicoes.Count);
            Assert.DoesNotThrow(() => registro.Registrar(Janela("itens.inventario", "I")));
        }
    }
}
