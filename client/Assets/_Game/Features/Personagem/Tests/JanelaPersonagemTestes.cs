using JogoDoGrupo.Shared.Features.Personagem;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Personagem.Tests
{
    /// <summary>
    /// Janela do personagem (tecla C): a definição registrada no Core e os dados mostrados,
    /// que vêm só do que o servidor informa.
    /// </summary>
    public class JanelaPersonagemTestes
    {
        [Test]
        public void Definicao_UsaATeclaC_ECriaUmaJanelaNovaACadaAbertura()
        {
            var definicao = JanelaPersonagem.CriarDefinicao();

            Assert.AreEqual("personagem.ficha", definicao.Chave);
            Assert.AreEqual("C", definicao.Tecla);
            var primeira = definicao.Criar();
            Assert.IsInstanceOf<JanelaPersonagem>(primeira);
            Assert.AreNotSame(primeira, definicao.Criar());
        }

        [Test]
        public void ProcurarPorId_AchaOPersonagemQueEstaNoMundo()
        {
            var lista = new[]
            {
                new PersonagemResumo { Id = 1, Nome = "Ana" },
                null,
                new PersonagemResumo { Id = 7, Nome = "Bruno" },
            };

            Assert.AreEqual("Bruno", TextosPersonagem.ProcurarPorId(lista, 7).Nome);
        }

        [Test]
        public void ProcurarPorId_SemListaOuSemOId_DevolveNulo()
        {
            var lista = new[]
            {
                new PersonagemResumo { Id = 1, Nome = "Ana" },
            };

            Assert.IsNull(TextosPersonagem.ProcurarPorId(null, 1));
            Assert.IsNull(TextosPersonagem.ProcurarPorId(new PersonagemResumo[0], 1));
            Assert.IsNull(TextosPersonagem.ProcurarPorId(lista, 2));
        }

        [Test]
        public void LinhasDaFicha_MostraNomeRacaENivel_NessaOrdem()
        {
            var personagem = new PersonagemResumo
            {
                Id = 3,
                Nome = "Ana",
                Raca = Raca.Valdren,
                Nivel = 4,
            };

            var linhas = TextosPersonagem.LinhasDaFicha(personagem);

            Assert.AreEqual(3, linhas.Count);
            Assert.AreEqual("Nome", linhas[0].Key);
            Assert.AreEqual("Ana", linhas[0].Value);
            Assert.AreEqual("Raça", linhas[1].Key);
            Assert.AreEqual("Valdren", linhas[1].Value);
            Assert.AreEqual("Nível", linhas[2].Key);
            Assert.AreEqual("4", linhas[2].Value);
        }
    }
}
