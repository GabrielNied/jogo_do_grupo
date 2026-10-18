using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Social.Tests
{
    public class FilaGrupoTestes
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

            public void Simular<T>(ushort id, T mensagem)
            {
                if (_assinantes.TryGetValue(id, out var lista))
                {
                    foreach (var d in lista.ToArray())
                    {
                        ((Action<T>)d)(mensagem);
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

        [Test]
        public void EntrarNaFila_envia_mensagem_para_o_servidor()
        {
            var conexao = new ConexaoFalsa();
            using var estado = new EstadoDaFilaDeGrupo(conexao);

            estado.Entrar(PapelNoGrupo.Tank);

            Assert.AreEqual(1, conexao.Enviados.Count);
            Assert.AreEqual(SocialMessageIds.EntrarNaFilaDeGrupo, conexao.Enviados[0].Id);
            var msg = conexao.Enviados[0].Mensagem as EntrarNaFilaDeGrupo;
            Assert.NotNull(msg);
            Assert.AreEqual(PapelNoGrupo.Tank, msg.Papel);
        }

        [Test]
        public void SairDaFila_envia_mensagem_para_o_servidor()
        {
            var conexao = new ConexaoFalsa();
            using var estado = new EstadoDaFilaDeGrupo(conexao);

            estado.Sair();

            Assert.AreEqual(1, conexao.Enviados.Count);
            Assert.AreEqual(SocialMessageIds.SairDaFilaDeGrupo, conexao.Enviados[0].Id);
            Assert.IsInstanceOf<SairDaFilaDeGrupo>(conexao.Enviados[0].Mensagem);
        }

        [Test]
        public void AoReceberStatus_atualiza_estado_local()
        {
            var conexao = new ConexaoFalsa();
            using var estado = new EstadoDaFilaDeGrupo(conexao);
            var mudouDisparado = false;
            estado.Mudou += () => mudouDisparado = true;

            conexao.Simular(
                SocialMessageIds.StatusDaFilaDeGrupo,
                new StatusDaFilaDeGrupo
                {
                    EmFila = true,
                    Papel = PapelNoGrupo.Healer,
                    Tanques = 1,
                    Healers = 1,
                    Dps = 2,
                    SegundosNaFila = 45,
                }
            );

            Assert.IsTrue(mudouDisparado);
            Assert.IsTrue(estado.EmFila);
            Assert.AreEqual(PapelNoGrupo.Healer, estado.Papel);
            Assert.AreEqual(1, estado.Tanques);
            Assert.AreEqual(1, estado.Healers);
            Assert.AreEqual(2, estado.Dps);
            Assert.AreEqual(45, estado.SegundosNaFila);
        }

        [Test]
        public void AoReceberGrupoFormado_desliga_fila_e_dispara_evento()
        {
            var conexao = new ConexaoFalsa();
            using var estado = new EstadoDaFilaDeGrupo(conexao);
            GrupoDeFilaFormado formadoRecebido = null;
            estado.GrupoFormado += f => formadoRecebido = f;

            conexao.Simular(
                SocialMessageIds.StatusDaFilaDeGrupo,
                new StatusDaFilaDeGrupo { EmFila = true, Papel = PapelNoGrupo.Dps }
            );
            Assert.IsTrue(estado.EmFila);

            conexao.Simular(
                SocialMessageIds.GrupoDeFilaFormado,
                new GrupoDeFilaFormado
                {
                    GrupoId = 999,
                    SeuPapel = PapelNoGrupo.Dps,
                    Mensagem = "Grupo formado!",
                }
            );

            Assert.IsFalse(estado.EmFila);
            Assert.NotNull(formadoRecebido);
            Assert.AreEqual(999, formadoRecebido.GrupoId);
            Assert.AreEqual("Grupo formado!", estado.UltimoAviso);
        }
    }
}
