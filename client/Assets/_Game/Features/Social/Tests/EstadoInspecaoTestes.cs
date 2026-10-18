using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Social.Tests
{
    public class EstadoInspecaoTestes
    {
        private sealed class ConexaoFalsa : IConexaoDoGrupo
        {
            public readonly List<(ushort Id, object Mensagem)> Enviadas =
                new List<(ushort, object)>();
            private readonly Dictionary<ushort, List<Delegate>> _assinaturas =
                new Dictionary<ushort, List<Delegate>>();

            public bool Conectado => true;

            public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega)
            {
                Enviadas.Add((id, mensagem));
                return true;
            }

            public IDisposable Assinar<T>(ushort id, Action<T> aoReceber)
            {
                if (!_assinaturas.TryGetValue(id, out var lista))
                {
                    lista = new List<Delegate>();
                    _assinaturas[id] = lista;
                }

                lista.Add(aoReceber);
                return new Descarte(() => lista.Remove(aoReceber));
            }

            public void Simular<T>(ushort id, T mensagem)
            {
                if (_assinaturas.TryGetValue(id, out var lista))
                {
                    foreach (var d in lista.ToArray())
                    {
                        ((Action<T>)d)(mensagem);
                    }
                }
            }

            private sealed class Descarte : IDisposable
            {
                private readonly Action _acao;

                public Descarte(Action acao) => _acao = acao;

                public void Dispose() => _acao?.Invoke();
            }
        }

        private ConexaoFalsa _conexao;
        private EstadoInspecao _estado;

        [SetUp]
        public void Setup()
        {
            _conexao = new ConexaoFalsa();
            _estado = new EstadoInspecao(_conexao);
        }

        [TearDown]
        public void Teardown()
        {
            _estado.Dispose();
        }

        [Test]
        public void PedirPorId_EnviaMensagemCorreta()
        {
            var enviado = _estado.Pedir(42);

            Assert.IsTrue(enviado);
            Assert.AreEqual(1, _conexao.Enviadas.Count);
            Assert.AreEqual(SocialMessageIds.PedirInspecao, _conexao.Enviadas[0].Id);
            var pedido = (PedirInspecao)_conexao.Enviadas[0].Mensagem;
            Assert.AreEqual(42, pedido.PersonagemId);
        }

        [Test]
        public void PedirPorNome_EnviaMensagemCorreta()
        {
            var enviado = _estado.PedirPorNome("Heroi");

            Assert.IsTrue(enviado);
            Assert.AreEqual(1, _conexao.Enviadas.Count);
            Assert.AreEqual(SocialMessageIds.PedirInspecao, _conexao.Enviadas[0].Id);
            var pedido = (PedirInspecao)_conexao.Enviadas[0].Mensagem;
            Assert.AreEqual("Heroi", pedido.Nome);
        }

        [Test]
        public void AoReceberRetrato_AtualizaEstadoEDisparaEvento()
        {
            RetratoInspecaoResponse eventoRetrato = null;
            _estado.RecebeuRetrato += r => eventoRetrato = r;

            var retrato = new RetratoInspecaoResponse
            {
                PersonagemId = 10,
                Nome = "Cavaleiro",
                Nivel = 25,
                Atributos = new AtributosDosEquipamentos { DefesaFisica = 100 },
            };

            _conexao.Simular(SocialMessageIds.RetratoInspecaoResponse, retrato);

            Assert.IsNotNull(_estado.UltimoRetrato);
            Assert.AreEqual("Cavaleiro", _estado.UltimoRetrato.Nome);
            Assert.AreEqual(25, _estado.UltimoRetrato.Nivel);
            Assert.AreEqual(eventoRetrato, _estado.UltimoRetrato);
        }

        [Test]
        public void AoReceberRecusa_RegistraErroEMensagemClara()
        {
            InspecaoRecusada eventoRecusa = null;
            _estado.Recusada += r => eventoRecusa = r;

            var recusa = new InspecaoRecusada
            {
                PersonagemId = 99,
                Motivo = CodigoErroInspecao.AlvoOffline,
            };

            _conexao.Simular(SocialMessageIds.InspecaoRecusada, recusa);

            Assert.AreEqual(CodigoErroInspecao.AlvoOffline, _estado.UltimoErro);
            Assert.AreEqual("O jogador está offline.", _estado.UltimaMensagemErro);
            Assert.IsNotNull(eventoRecusa);
            Assert.AreEqual(99, eventoRecusa.PersonagemId);
        }
    }
}
