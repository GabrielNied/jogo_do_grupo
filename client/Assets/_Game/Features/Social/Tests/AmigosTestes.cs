using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Social.Tests
{
    /// <summary>Estado local das listas que o servidor manda (sem cena).</summary>
    public class AmigosTestes
    {
        private sealed class ConexaoFalsa : IConexaoDoGrupo
        {
            public List<(ushort Id, object Mensagem)> Enviados { get; } =
                new List<(ushort, object)>();

            public bool Conectado => true;

            public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega)
            {
                Enviados.Add((id, mensagem));
                return true;
            }

            public IDisposable Assinar<T>(ushort id, Action<T> aoReceber) => new Assinatura();

            private sealed class Assinatura : IDisposable
            {
                public void Dispose() { }
            }
        }

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

            var visiveis = historico.Ultimas(8, estado);

            Assert.AreEqual(1, visiveis.Count);
            Assert.AreEqual("Ana", visiveis[0].Nome);
            Assert.IsTrue(estado.TentarIgnorado("CHATO", out var ignorado));
            Assert.AreEqual(7, ignorado.IgnoradoId);
        }

        [Test]
        public void IgnorarEDesignorar_EnviamIntencoesDoContrato()
        {
            var conexao = new ConexaoFalsa();
            var estado = new EstadoDeAmigos(conexao);

            Assert.IsTrue(estado.Ignorar("Chato"));
            Assert.IsTrue(estado.DeixarDeIgnorar(7));

            Assert.AreEqual(SocialMessageIds.IgnorarJogador, conexao.Enviados[0].Id);
            Assert.AreEqual("Chato", ((IgnorarJogador)conexao.Enviados[0].Mensagem).Nome);
            Assert.AreEqual(SocialMessageIds.DeixarDeIgnorar, conexao.Enviados[1].Id);
            Assert.AreEqual(7L, ((DeixarDeIgnorar)conexao.Enviados[1].Mensagem).IgnoradoId);

            estado.Dispose();
        }
    }
}
