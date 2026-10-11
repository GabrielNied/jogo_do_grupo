using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Social.Tests
{
    /// <summary>Estado local das listas que o servidor manda (sem cena).</summary>
    public class AmigosTestes
    {
        [Test]
        public void AplicarLista_SubstituiAmigosEPedidos()
        {
            var estado = new EstadoDeAmigos();
            estado.Aplicar(
                new ListaDeAmigos
                {
                    Amigos = new[]
                    {
                        new AmigoDaLista
                        {
                            AmigoId = 1,
                            Nome = "Ana",
                            Online = true,
                            PersonagemOnline = "Ana",
                        },
                    },
                    PedidosRecebidos = new[]
                    {
                        new PedidoDeAmizade { PedidoId = 4, Nome = "Bia" },
                    },
                    PedidosEnviados = new[]
                    {
                        new PedidoDeAmizade { PedidoId = 5, Nome = "Caio" },
                    },
                }
            );

            estado.Aplicar(
                new ListaDeAmigos
                {
                    Amigos = new[]
                    {
                        new AmigoDaLista { AmigoId = 2, Nome = "Duda" },
                    },
                }
            );

            Assert.AreEqual(1, estado.Amigos.Count);
            Assert.AreEqual("Duda", estado.Amigos[0].Nome);
            Assert.AreEqual(0, estado.PedidosRecebidos.Count);
            Assert.AreEqual(0, estado.PedidosEnviados.Count);
        }

        [Test]
        public void AmigoMudouDeEstado_TrocaSoALinhaDaqueleAmigo()
        {
            var estado = new EstadoDeAmigos();
            estado.Aplicar(
                new ListaDeAmigos
                {
                    Amigos = new[]
                    {
                        new AmigoDaLista
                        {
                            AmigoId = 1,
                            Nome = "Ana",
                            Online = false,
                        },
                        new AmigoDaLista
                        {
                            AmigoId = 2,
                            Nome = "Bia",
                            Online = true,
                            PersonagemOnline = "Bia",
                        },
                    },
                }
            );

            estado.Aplicar(
                new AmigoMudouDeEstado
                {
                    AmigoId = 1,
                    Online = true,
                    PersonagemOnline = "AnaDois",
                }
            );

            Assert.IsTrue(estado.Amigos[0].Online);
            Assert.AreEqual("AnaDois", estado.Amigos[0].PersonagemOnline);
            Assert.AreEqual("Ana", estado.Amigos[0].Nome);
            Assert.AreEqual("Bia", estado.Amigos[1].PersonagemOnline);
        }

        [Test]
        public void ListaDeIgnoradosDoServidor_EscondeONomeNoChat()
        {
            var estado = new EstadoDeAmigos();
            var historico = new HistoricoDeChat();
            historico.Adicionar(
                new LinhaDeChat(CanalDaLinha.Cidade, 2, "Chato", "", "spam", 10, false)
            );
            historico.Adicionar(
                new LinhaDeChat(CanalDaLinha.Cidade, 3, "Ana", "", "oi", 20, false)
            );

            estado.Aplicar(
                new JogoDoGrupo.Shared.Features.Social.ListaDeIgnorados
                {
                    Ignorados = new[]
                    {
                        new JogadorIgnorado { IgnoradoId = 7, Nome = "chato" },
                    },
                }
            );

            var visiveis = historico.Ultimas(8, null, estado);

            Assert.AreEqual(1, visiveis.Count);
            Assert.AreEqual("Ana", visiveis[0].Nome);
            Assert.IsTrue(estado.TentarIgnorado("CHATO", out var ignorado));
            Assert.AreEqual(7, ignorado.IgnoradoId);
        }
    }
}
