using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Features.Mundo.Presenca;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class PresencaTestes
    {
        private sealed class ConexaoFalsa : IConexaoDePresenca
        {
            private readonly Dictionary<ushort, List<Delegate>> _assinantes =
                new Dictionary<ushort, List<Delegate>>();

            public bool Conectado { get; set; }
            public List<(ushort Id, object Mensagem, EntregaTempoReal Entrega)> Enviados { get; } =
                new List<(ushort, object, EntregaTempoReal)>();

            public event Action<EstadoTempoReal> EstadoMudou;

            public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega)
            {
                if (!Conectado)
                {
                    return false;
                }

                Enviados.Add((id, mensagem, entrega));
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

            public void Mudar(EstadoTempoReal estado)
            {
                Conectado = estado == EstadoTempoReal.Conectado;
                EstadoMudou?.Invoke(estado);
            }

            public int Contar(ushort id)
            {
                var total = 0;
                foreach (var enviado in Enviados)
                {
                    if (enviado.Id == id)
                    {
                        total++;
                    }
                }

                return total;
            }

            private sealed class Cancelador : IDisposable
            {
                private readonly Action _acao;

                public Cancelador(Action acao) => _acao = acao;

                public void Dispose() => _acao();
            }
        }

        private static JogadorVisivel Jogador(long id, float x = 0f, float z = 0f) =>
            new JogadorVisivel
            {
                PersonagemId = id,
                X = x,
                Z = z,
            };

        [Test]
        public void Limitador_PermiteDezEnviosPorSegundo()
        {
            var limitador = new LimitadorDeTaxa(10f);
            var envios = 0;
            for (var i = 0; i < 60; i++)
            {
                if (limitador.Deve(1f / 60f))
                {
                    envios++;
                }
            }

            Assert.That(envios, Is.InRange(9, 11));
        }

        [Test]
        public void Limitador_QuadroLongoValeUmEnvioSo()
        {
            var limitador = new LimitadorDeTaxa(10f);
            Assert.IsTrue(limitador.Deve(1f));
            Assert.IsFalse(limitador.Deve(0.01f));
        }

        [Test]
        public void Avatar_SegueOAlvoSemUltrapassar()
        {
            var avatar = new AvatarRemoto(2, Vector3.zero, 0f);
            avatar.DefinirAlvo(new Vector3(4f, 0f, 0f), 90f);

            avatar.Atualizar(0.05f);
            var primeiro = avatar.Posicao.x;
            Assert.That(primeiro, Is.GreaterThan(0f).And.LessThan(4f));

            for (var i = 0; i < 100; i++)
            {
                avatar.Atualizar(0.05f);
            }

            Assert.That(avatar.Posicao.x, Is.EqualTo(4f).Within(0.01f));
            Assert.That(avatar.RotacaoY, Is.EqualTo(90f).Within(0.5f));
        }

        [Test]
        public void Avatar_DistanciaGrandeSalta()
        {
            var avatar = new AvatarRemoto(2, Vector3.zero, 0f);
            avatar.DefinirAlvo(new Vector3(50f, 0f, 0f), 0f);
            avatar.Atualizar(0.016f);
            Assert.That(avatar.Posicao.x, Is.EqualTo(50f));
        }

        [Test]
        public void Avatar_RotacaoTomaOCaminhoCurto()
        {
            var avatar = new AvatarRemoto(2, Vector3.zero, 350f);
            avatar.DefinirAlvo(Vector3.zero, 10f);
            avatar.Atualizar(0.05f);
            Assert.That(Mathf.DeltaAngle(avatar.RotacaoY, 350f), Is.LessThan(0f));
        }

        [Test]
        public void Registro_EstadoDaCidadeCriaOsOutrosEIgnoraEu()
        {
            var registro = new RegistroDeAvatares(1);
            var criados = new List<long>();
            registro.Criado += a => criados.Add(a.PersonagemId);

            registro.Aplicar(
                new EstadoDaCidade
                {
                    Eu = Jogador(1),
                    Outros = new[] { Jogador(2, 3f), Jogador(3), Jogador(1) },
                }
            );

            Assert.That(registro.Quantidade, Is.EqualTo(2));
            Assert.That(criados, Is.EquivalentTo(new long[] { 2, 3 }));
            registro.TentarObter(2, out var dois);
            Assert.That(dois.Posicao.x, Is.EqualTo(3f));
        }

        [Test]
        public void Registro_NovoSnapshotRemoveQuemSumiu()
        {
            var registro = new RegistroDeAvatares(1);
            var removidos = new List<long>();
            registro.Removido += a => removidos.Add(a.PersonagemId);
            registro.Aplicar(new EstadoDaCidade { Outros = new[] { Jogador(2), Jogador(3) } });

            registro.Aplicar(new EstadoDaCidade { Outros = new[] { Jogador(3) } });

            Assert.That(removidos, Is.EqualTo(new long[] { 2 }));
            Assert.That(registro.Quantidade, Is.EqualTo(1));
        }

        [Test]
        public void Registro_EntrouSaiuEMover()
        {
            var registro = new RegistroDeAvatares(1);
            registro.Entrou(Jogador(2));
            registro.Entrou(Jogador(2));
            Assert.That(registro.Quantidade, Is.EqualTo(1));

            registro.Mover(new PosicaoJogador { PersonagemId = 2, X = 7f });
            registro.TentarObter(2, out var avatar);
            Assert.That(avatar.Alvo.x, Is.EqualTo(7f));

            registro.Saiu(2);
            registro.Saiu(2);
            Assert.That(registro.Quantidade, Is.EqualTo(0));
        }

        [Test]
        public void Registro_MoverIgnoraOProprioJogador()
        {
            var registro = new RegistroDeAvatares(1);
            registro.Mover(new PosicaoJogador { PersonagemId = 1, X = 5f });
            registro.Entrou(Jogador(1));
            Assert.That(registro.Quantidade, Is.EqualTo(0));
        }

        [Test]
        public void Presenca_EntraNaCidadeACadaConexao()
        {
            var conexao = new ConexaoFalsa();
            using var presenca = new PresencaNaCidade(conexao, 1);
            Assert.That(conexao.Contar(MundoMessageIds.EntrarNaCidade), Is.EqualTo(0));

            conexao.Mudar(EstadoTempoReal.Conectado);
            conexao.Mudar(EstadoTempoReal.Reconectando);
            conexao.Mudar(EstadoTempoReal.Conectado);

            Assert.That(conexao.Contar(MundoMessageIds.EntrarNaCidade), Is.EqualTo(2));
        }

        [Test]
        public void Presenca_JaConectadaEntraNaCriacao()
        {
            var conexao = new ConexaoFalsa { Conectado = true };
            using var presenca = new PresencaNaCidade(conexao, 1);
            Assert.That(conexao.Contar(MundoMessageIds.EntrarNaCidade), Is.EqualTo(1));
        }

        [Test]
        public void Presenca_AplicaMensagensDoServidor()
        {
            var conexao = new ConexaoFalsa { Conectado = true };
            using var presenca = new PresencaNaCidade(conexao, 1);

            conexao.Receber(
                MundoMessageIds.EstadoDaCidade,
                new EstadoDaCidade { Eu = Jogador(1), Outros = new[] { Jogador(2) } }
            );
            conexao.Receber(
                MundoMessageIds.JogadorEntrou,
                new JogadorEntrou { Jogador = Jogador(3) }
            );
            Assert.That(presenca.Avatares.Quantidade, Is.EqualTo(2));

            conexao.Receber(
                MundoMessageIds.PosicaoJogador,
                new PosicaoJogador { PersonagemId = 3, X = 9f }
            );
            presenca.Avatares.TentarObter(3, out var tres);
            Assert.That(tres.Alvo.x, Is.EqualTo(9f));

            conexao.Receber(MundoMessageIds.JogadorSaiu, new JogadorSaiu { PersonagemId = 2 });
            Assert.That(presenca.Avatares.Quantidade, Is.EqualTo(1));
        }

        [Test]
        public void Presenca_QuedaLimpaOsAvatares()
        {
            var conexao = new ConexaoFalsa { Conectado = true };
            using var presenca = new PresencaNaCidade(conexao, 1);
            conexao.Receber(
                MundoMessageIds.EstadoDaCidade,
                new EstadoDaCidade { Outros = new[] { Jogador(2) } }
            );

            conexao.Mudar(EstadoTempoReal.Reconectando);

            Assert.That(presenca.Avatares.Quantidade, Is.EqualTo(0));
        }

        [Test]
        public void Presenca_PosicaoDoProprioJogadorViraCorrecao()
        {
            var conexao = new ConexaoFalsa { Conectado = true };
            using var presenca = new PresencaNaCidade(conexao, 1);
            Vector3? recebida = null;
            presenca.PosicaoDoServidor += (posicao, _) => recebida = posicao;

            conexao.Receber(
                MundoMessageIds.PosicaoJogador,
                new PosicaoJogador
                {
                    PersonagemId = 1,
                    X = 2f,
                    Z = 3f,
                }
            );

            Assert.That(recebida, Is.EqualTo(new Vector3(2f, 0f, 3f)));
            Assert.That(presenca.Avatares.Quantidade, Is.EqualTo(0));
        }

        [Test]
        public void Presenca_EnviaPosicaoEmTaxaLimitadaESequenciada()
        {
            var conexao = new ConexaoFalsa { Conectado = true };
            using var presenca = new PresencaNaCidade(conexao, 1);

            for (var i = 0; i < 60; i++)
            {
                presenca.Atualizar(1f / 60f, new Vector3(i, 0f, 0f), 45f);
            }

            var total = conexao.Contar(MundoMessageIds.IntencaoDeMovimento);
            Assert.That(total, Is.InRange(9, 11));

            var ultimo = conexao.Enviados[conexao.Enviados.Count - 1];
            Assert.That(ultimo.Entrega, Is.EqualTo(EntregaTempoReal.Sequenciada));
            var intencao = (IntencaoDeMovimento)ultimo.Mensagem;
            Assert.That(intencao.Sequencia, Is.EqualTo(total));
            Assert.That(intencao.RotacaoY, Is.EqualTo(45f));
        }

        [Test]
        public void Presenca_NaoEnviaPosicaoDesconectada()
        {
            var conexao = new ConexaoFalsa();
            using var presenca = new PresencaNaCidade(conexao, 1);
            presenca.Atualizar(1f, Vector3.zero, 0f);
            Assert.That(conexao.Enviados, Is.Empty);
        }

        [Test]
        public void Presenca_DescartarCancelaAssinaturas()
        {
            var conexao = new ConexaoFalsa { Conectado = true };
            var presenca = new PresencaNaCidade(conexao, 1);
            presenca.Dispose();

            conexao.Receber(
                MundoMessageIds.JogadorEntrou,
                new JogadorEntrou { Jogador = Jogador(2) }
            );
            conexao.Mudar(EstadoTempoReal.Conectado);

            Assert.That(presenca.Avatares.Quantidade, Is.EqualTo(0));
            Assert.That(conexao.Contar(MundoMessageIds.EntrarNaCidade), Is.EqualTo(1));
        }
    }
}
