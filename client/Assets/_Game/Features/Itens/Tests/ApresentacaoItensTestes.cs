using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    /// <summary>
    /// O que o inventário e o baú mostram, montado a partir das respostas do servidor (sem
    /// servidor): lista, ganhos, e os erros (<c>InventarioCheio</c> sem inventar item).
    /// </summary>
    public class ApresentacaoItensTestes
    {
        private static EntradaInventario Entrada(int itemId, string nome)
        {
            return new EntradaInventario { ItemId = itemId, Nome = nome };
        }

        [Test]
        public void FormatarEntrada_MostraItemIdENome()
        {
            var texto = ApresentacaoItens.FormatarEntrada(Entrada(1, "Espada de teste"));

            Assert.AreEqual("[1] Espada de teste", texto);
        }

        [Test]
        public void FormatarEntrada_SemNome_UsaONomeDoCatalogo()
        {
            var texto = ApresentacaoItens.FormatarEntrada(Entrada(CatalogoItens.MoedaDeTeste, ""));

            Assert.AreEqual("[4] " + CatalogoItens.ObterNome(CatalogoItens.MoedaDeTeste), texto);
        }

        [Test]
        public void FormatarEntrada_SemNomeEForaDoCatalogo_NaoQuebra()
        {
            var texto = ApresentacaoItens.FormatarEntrada(Entrada(99, null));

            Assert.AreEqual("[99] Item desconhecido", texto);
        }

        [Test]
        public void MontarLinhas_MantemAOrdemEOsRepetidosEmLinhasSeparadas()
        {
            var linhas = ApresentacaoItens.MontarLinhas(
                new[]
                {
                    Entrada(3, "Poção de teste"),
                    Entrada(3, "Poção de teste"),
                    Entrada(1, "Espada de teste"),
                }
            );

            CollectionAssert.AreEqual(
                new[] { "[3] Poção de teste", "[3] Poção de teste", "[1] Espada de teste" },
                linhas
            );
        }

        [Test]
        public void MontarLinhas_NuloOuVazio_DaListaVazia()
        {
            Assert.IsEmpty(ApresentacaoItens.MontarLinhas(null));
            Assert.IsEmpty(ApresentacaoItens.MontarLinhas(new EntradaInventario[0]));
        }

        [Test]
        public void MontarLinhas_IgnoraEntradaNula()
        {
            var linhas = ApresentacaoItens.MontarLinhas(
                new[] { null, Entrada(2, "Escudo de teste") }
            );

            CollectionAssert.AreEqual(new[] { "[2] Escudo de teste" }, linhas);
        }

        [Test]
        public void MontarLinhas_QuarentaEntradas_MostraTodasSemCortar()
        {
            var entradas = new EntradaInventario[RegrasItens.EspacosInventario];
            for (var i = 0; i < entradas.Length; i++)
            {
                entradas[i] = Entrada(1, "Espada de teste");
            }

            Assert.AreEqual(40, ApresentacaoItens.MontarLinhas(entradas).Count);
        }

        [Test]
        public void TituloDoInventario_MostraOsEspacosUsadosDeQuarenta()
        {
            Assert.AreEqual("Inventário (0/40)", ApresentacaoItens.TituloDoInventario(0));
            Assert.AreEqual("Inventário (40/40)", ApresentacaoItens.TituloDoInventario(40));
        }

        [Test]
        public void InterpretarBau_Sucesso_TrazGanhosEInventarioResultante()
        {
            var corpo = new AbrirBauResponse
            {
                Ganhos = new[] { Entrada(2, "Escudo de teste") },
                Inventario = new[] { Entrada(1, "Espada de teste"), Entrada(2, "Escudo de teste") },
            };

            var resultado = ApresentacaoItens.InterpretarBau(true, null, corpo);

            Assert.IsTrue(resultado.Deu);
            Assert.IsNull(resultado.Aviso);
            CollectionAssert.AreEqual(new[] { "[2] Escudo de teste" }, resultado.Ganhos);
            CollectionAssert.AreEqual(
                new[] { "[1] Espada de teste", "[2] Escudo de teste" },
                resultado.Inventario
            );
        }

        [Test]
        public void InterpretarBau_InventarioCheio_AvisaQueNaoCoubeENaoInventaItem()
        {
            var resultado = ApresentacaoItens.InterpretarBau(
                false,
                CodigosErroItens.InventarioCheio,
                null
            );

            Assert.IsFalse(resultado.Deu);
            Assert.AreEqual(CodigosErroItens.InventarioCheio, resultado.CodigoErro);
            StringAssert.Contains("não cabem", resultado.Aviso);
            StringAssert.Contains("Nada foi guardado", resultado.Aviso);
            Assert.IsEmpty(resultado.Ganhos);
            Assert.IsEmpty(resultado.Inventario);
        }

        [Test]
        public void InterpretarBau_SucessoSemCorpo_ViraErroGenerico()
        {
            var resultado = ApresentacaoItens.InterpretarBau(true, null, null);

            Assert.IsFalse(resultado.Deu);
            Assert.IsNotEmpty(resultado.Aviso);
        }

        [Test]
        public void InterpretarInventario_Sucesso_MontaALista()
        {
            var corpo = new InventarioResponse
            {
                Entradas = new[] { Entrada(4, "Moeda de teste") },
            };

            var resultado = ApresentacaoItens.InterpretarInventario(true, null, corpo);

            Assert.IsTrue(resultado.Deu);
            CollectionAssert.AreEqual(new[] { "[4] Moeda de teste" }, resultado.Inventario);
            Assert.IsEmpty(resultado.Ganhos);
        }

        [Test]
        public void InterpretarInventario_Vazio_DaListaVaziaSemErro()
        {
            var resultado = ApresentacaoItens.InterpretarInventario(
                true,
                null,
                new InventarioResponse()
            );

            Assert.IsTrue(resultado.Deu);
            Assert.IsEmpty(resultado.Inventario);
        }

        [Test]
        public void InterpretarInventario_PersonagemNaoEncontrado_Avisa()
        {
            var resultado = ApresentacaoItens.InterpretarInventario(
                false,
                CodigosErroItens.PersonagemNaoEncontrado,
                null
            );

            Assert.IsFalse(resultado.Deu);
            StringAssert.Contains("Personagem não encontrado", resultado.Aviso);
        }

        [TestCase(CodigosErroCliente.SemConexao)]
        [TestCase(CodigosErroCliente.HttpBloqueado)]
        public void MensagemDeErro_SemConexao_AvisaDaConexao(string codigo)
        {
            StringAssert.Contains("Sem conexão", ApresentacaoItens.MensagemDeErro(codigo, true));
        }

        [Test]
        public void MensagemDeErro_Cancelada_NaoMostraNada()
        {
            Assert.IsNull(ApresentacaoItens.MensagemDeErro(CodigosErroCliente.Cancelada, true));
        }

        [Test]
        public void MensagemDeErro_NaoAutenticado_AvisaDaSessao()
        {
            StringAssert.Contains(
                "sessão",
                ApresentacaoItens.MensagemDeErro(CodigosErro.NaoAutenticado, false)
            );
        }

        [Test]
        public void MensagemDeErro_CodigoDesconhecido_DependeDeBauOuInventario()
        {
            StringAssert.Contains("baú", ApresentacaoItens.MensagemDeErro("Qualquer", true));
            StringAssert.Contains(
                "inventário",
                ApresentacaoItens.MensagemDeErro(CodigosErroCliente.RespostaInvalida, false)
            );
        }
    }
}
