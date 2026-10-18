using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social.Tests
{
    [TestFixture]
    public class MarcadoresTaticosTestes
    {
        private sealed class ConexaoFalsa : IConexaoDoGrupo
        {
            private readonly Dictionary<ushort, List<Delegate>> _assinantes =
                new Dictionary<ushort, List<Delegate>>();

            public bool Conectado { get; set; } = true;
            public List<(ushort Id, object Mensagem)> Enviados { get; } =
                new List<(ushort, object)>();

            public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega)
            {
                if (!Conectado)
                {
                    return false;
                }

                Enviados.Add((id, mensagem));
                return true;
            }

            public IDisposable Assinar<T>(ushort id, Action<T> aoReceber)
            {
                if (!_assinantes.TryGetValue(id, out var lista))
                {
                    _assinantes[id] = lista = new List<Delegate>();
                }

                lista.Add(aoReceber);
                return new Cancelador(() => lista.Remove(aoReceber));
            }

            public void Receber<T>(ushort id, T mensagem)
            {
                if (_assinantes.TryGetValue(id, out var lista))
                {
                    foreach (var assinante in lista.ToArray())
                    {
                        ((Action<T>)assinante)(mensagem);
                    }
                }
            }

            private sealed class Cancelador : IDisposable
            {
                private readonly Action _aoCancelar;

                public Cancelador(Action aoCancelar) => _aoCancelar = aoCancelar;

                public void Dispose() => _aoCancelar?.Invoke();
            }
        }

        private ConexaoFalsa _conexao;
        private EstadoMarcadoresTaticos _estado;

        [SetUp]
        public void Setup()
        {
            _conexao = new ConexaoFalsa();
            _estado = new EstadoMarcadoresTaticos(_conexao);
        }

        [TearDown]
        public void Teardown()
        {
            _estado.Dispose();
        }

        [Test]
        public void Estado_RecebeMensagemDoServidor_AtualizaMarcadores()
        {
            bool disparouMudou = false;
            _estado.Mudou += () => disparouMudou = true;

            var msg = new MarcadoresDeAlvoMudaram
            {
                Marcadores = new[]
                {
                    new MarcadorAlvoDto { AlvoId = 101, Marcador = TipoMarcadorDeAlvo.Caveira },
                    new MarcadorAlvoDto { AlvoId = 202, Marcador = TipoMarcadorDeAlvo.Cruz },
                },
            };

            _conexao.Receber(SocialMessageIds.MarcadoresDeAlvoMudaram, msg);

            Assert.IsTrue(disparouMudou);
            Assert.AreEqual(2, _estado.Marcadores.Count);
            Assert.AreEqual(TipoMarcadorDeAlvo.Caveira, _estado.ObterMarcador(101));
            Assert.AreEqual(TipoMarcadorDeAlvo.Cruz, _estado.ObterMarcador(202));
            Assert.AreEqual(TipoMarcadorDeAlvo.Nenhum, _estado.ObterMarcador(999));
        }

        [Test]
        public void Estado_PedirDefinirMarcador_EnviaMensagemCorreta()
        {
            var enviado = _estado.PedirDefinirMarcador(105, TipoMarcadorDeAlvo.Escudo);

            Assert.IsTrue(enviado);
            Assert.AreEqual(1, _conexao.Enviados.Count);
            Assert.AreEqual(SocialMessageIds.DefinirMarcadorDeAlvo, _conexao.Enviados[0].Id);

            var pedido = (DefinirMarcadorDeAlvo)_conexao.Enviados[0].Mensagem;
            Assert.AreEqual(105, pedido.AlvoId);
            Assert.AreEqual(TipoMarcadorDeAlvo.Escudo, pedido.Marcador);
        }

        [Test]
        public void Estado_PedirDefinirMarcador_AlvoInvalidoNaoEnvia()
        {
            var enviado = _estado.PedirDefinirMarcador(0, TipoMarcadorDeAlvo.Caveira);

            Assert.IsFalse(enviado);
            Assert.AreEqual(0, _conexao.Enviados.Count);
        }

        [Test]
        public void Estado_Limpar_RemoveTodosMarcadoresEDisparaEvento()
        {
            _estado.DefinirMarcadores(
                new[]
                {
                    new MarcadorAlvoDto { AlvoId = 50, Marcador = TipoMarcadorDeAlvo.Lua },
                }
            );

            Assert.AreEqual(1, _estado.Marcadores.Count);

            bool disparou = false;
            _estado.Mudou += () => disparou = true;
            _estado.Limpar();

            Assert.IsTrue(disparou);
            Assert.AreEqual(0, _estado.Marcadores.Count);
        }

        [Test]
        public void Estado_ReceberRecusa_DisparaEventoRecusado()
        {
            DefinirMarcadorRecusado recebido = null;
            _estado.Recusado += recusa => recebido = recusa;

            var msg = new DefinirMarcadorRecusado
            {
                Motivo = CodigoErroMarcador.ApenasLiderPodeMarcar,
            };

            _conexao.Receber(SocialMessageIds.DefinirMarcadorRecusado, msg);

            Assert.IsNotNull(recebido);
            Assert.AreEqual(CodigoErroMarcador.ApenasLiderPodeMarcar, recebido.Motivo);
        }

        [Test]
        public void ComandoChat_ConverterMarcador_ConverteAliasesCorretamente()
        {
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Caveira,
                ComandoDeMarcadorNoChat.ConverterMarcador("caveira")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Caveira,
                ComandoDeMarcadorNoChat.ConverterMarcador("skull")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Caveira,
                ComandoDeMarcadorNoChat.ConverterMarcador("1")
            );

            Assert.AreEqual(
                TipoMarcadorDeAlvo.Cruz,
                ComandoDeMarcadorNoChat.ConverterMarcador("x")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Cruz,
                ComandoDeMarcadorNoChat.ConverterMarcador("cruz")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Cruz,
                ComandoDeMarcadorNoChat.ConverterMarcador("cross")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Cruz,
                ComandoDeMarcadorNoChat.ConverterMarcador("2")
            );

            Assert.AreEqual(
                TipoMarcadorDeAlvo.Escudo,
                ComandoDeMarcadorNoChat.ConverterMarcador("escudo")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Escudo,
                ComandoDeMarcadorNoChat.ConverterMarcador("shield")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Escudo,
                ComandoDeMarcadorNoChat.ConverterMarcador("3")
            );

            Assert.AreEqual(
                TipoMarcadorDeAlvo.Lua,
                ComandoDeMarcadorNoChat.ConverterMarcador("lua")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Lua,
                ComandoDeMarcadorNoChat.ConverterMarcador("moon")
            );
            Assert.AreEqual(TipoMarcadorDeAlvo.Lua, ComandoDeMarcadorNoChat.ConverterMarcador("4"));

            Assert.AreEqual(
                TipoMarcadorDeAlvo.Estrela,
                ComandoDeMarcadorNoChat.ConverterMarcador("estrela")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Estrela,
                ComandoDeMarcadorNoChat.ConverterMarcador("star")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Estrela,
                ComandoDeMarcadorNoChat.ConverterMarcador("5")
            );

            Assert.AreEqual(
                TipoMarcadorDeAlvo.Nenhum,
                ComandoDeMarcadorNoChat.ConverterMarcador("desconhecido")
            );
            Assert.AreEqual(
                TipoMarcadorDeAlvo.Nenhum,
                ComandoDeMarcadorNoChat.ConverterMarcador("")
            );
        }

        [Test]
        public void ComandoChat_InterpretarMarcar_ExtraiMarcadorEAlvo()
        {
            bool ok = ComandoDeMarcadorNoChat.InterpretarMarcar(
                "caveira 123",
                out var marcador,
                out var alvoId
            );

            Assert.IsTrue(ok);
            Assert.AreEqual(TipoMarcadorDeAlvo.Caveira, marcador);
            Assert.AreEqual(123, alvoId);
        }

        [Test]
        public void ComandoChat_InterpretarMarcar_SemAlvoRetornaZeroParaBuscaDeProximidade()
        {
            bool ok = ComandoDeMarcadorNoChat.InterpretarMarcar(
                "estrela",
                out var marcador,
                out var alvoId
            );

            Assert.IsTrue(ok);
            Assert.AreEqual(TipoMarcadorDeAlvo.Estrela, marcador);
            Assert.AreEqual(0, alvoId);
        }

        [Test]
        public void ComandoChat_InterpretarDesmarcar_ExtraiAlvo()
        {
            bool ok = ComandoDeMarcadorNoChat.InterpretarDesmarcar("456", out var alvoId);

            Assert.IsTrue(ok);
            Assert.AreEqual(456, alvoId);
        }

        [Test]
        public void Apresentador_SincronizacaoComEstado_RemoveElementoQuandoDesmarcado()
        {
            var raizElemento = new VisualElement();
            var go = new GameObject("TesteApresentador");
            var apresentador = go.AddComponent<ApresentadorDeMarcadoresTaticos>();
            apresentador.Configurar(raizElemento, null, _estado);

            _estado.DefinirMarcadores(
                new[]
                {
                    new MarcadorAlvoDto { AlvoId = 10, Marcador = TipoMarcadorDeAlvo.Caveira },
                }
            );

            var alvoGo = new GameObject("Inimigo 10");
            alvoGo.transform.position = new Vector3(0, 0, 5);

            // Atualiza apresentador com localizador falso
            apresentador.Atualizar(id => id == 10 ? alvoGo.transform : null);

            // Agora limpa o estado
            _estado.Limpar();

            Assert.AreEqual(0, apresentador.ElementosAtivos.Count);

            UnityEngine.Object.DestroyImmediate(alvoGo);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
