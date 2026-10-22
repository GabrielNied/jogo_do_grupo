using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;
using UnityEngine.UIElements;

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

        [Test]
        public void Atualizar_incrementa_segundos_apenas_quando_em_fila()
        {
            var conexao = new ConexaoFalsa();
            using var estado = new EstadoDaFilaDeGrupo(conexao);

            estado.Atualizar(2.5f);
            Assert.AreEqual(0, estado.SegundosNaFila);

            conexao.Simular(
                SocialMessageIds.StatusDaFilaDeGrupo,
                new StatusDaFilaDeGrupo { EmFila = true, Papel = PapelNoGrupo.Tank }
            );

            estado.Atualizar(1.2f);
            Assert.AreEqual(1, estado.SegundosNaFila);

            estado.Atualizar(0.9f);
            Assert.AreEqual(2, estado.SegundosNaFila);
        }

        [Test]
        public void PainelFilaGrupo_descartar_preserva_estado_da_fila()
        {
            var conexao = new ConexaoFalsa();
            using var estado = new EstadoDaFilaDeGrupo(conexao);
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

            var raiz = new VisualElement();
            var painel = new PainelFilaGrupo(raiz, estado);

            var secaoStatus = raiz.Q<VisualElement>("lfg.secao-status");
            Assert.AreEqual(DisplayStyle.Flex, secaoStatus.style.display.value);

            // Simula fechar a janela (chama Dispose do painel)
            painel.Dispose();

            // O estado da fila continua ativo
            Assert.IsTrue(estado.EmFila);
            Assert.AreEqual(45, estado.SegundosNaFila);

            // Reabrir a janela (novo painel) reflete imediatamente a fila ativa
            var novaRaiz = new VisualElement();
            using var novoPainel = new PainelFilaGrupo(novaRaiz, estado);
            var novaSecaoStatus = novaRaiz.Q<VisualElement>("lfg.secao-status");
            Assert.AreEqual(DisplayStyle.Flex, novaSecaoStatus.style.display.value);
        }

        [Test]
        public void IconeFilaGrupoHud_visivel_apenas_quando_em_fila_e_cancela_pelo_botao()
        {
            var conexao = new ConexaoFalsa();
            using var estado = new EstadoDaFilaDeGrupo(conexao);
            var raizHud = new VisualElement();
            using var icone = new IconeFilaGrupoHud(raizHud, estado);

            // Inicialmente oculto
            Assert.AreEqual(DisplayStyle.None, icone.Elemento.style.display.value);

            // Entrou na fila
            conexao.Simular(
                SocialMessageIds.StatusDaFilaDeGrupo,
                new StatusDaFilaDeGrupo
                {
                    EmFila = true,
                    Papel = PapelNoGrupo.Dps,
                    Tanques = 1,
                    Healers = 0,
                    Dps = 2,
                    SegundosNaFila = 75,
                }
            );

            Assert.AreEqual(DisplayStyle.Flex, icone.Elemento.style.display.value);
            var rotuloTempo = icone.Elemento.Q<Label>("social.fila-hud-tempo");
            Assert.AreEqual("01:15", rotuloTempo.text);

            // Cancelar pelo botão sem reabrir a janela
            var botaoCancelar = icone.Elemento.Q<Button>("social.fila-hud-cancelar");
            Assert.NotNull(botaoCancelar);
            Click(botaoCancelar);

            Assert.AreEqual(1, conexao.Enviados.Count);
            Assert.AreEqual(SocialMessageIds.SairDaFilaDeGrupo, conexao.Enviados[0].Id);
        }

        private static void Click(Button botao)
        {
            using var evento = NavigationSubmitEvent.GetPooled();
            evento.target = botao;
            botao.SendEvent(evento);
        }

        [Test]
        public void IconeFilaGrupoHud_atualiza_rotacao_ao_receber_delta_tempo()
        {
            var conexao = new ConexaoFalsa();
            using var estado = new EstadoDaFilaDeGrupo(conexao);
            var raizHud = new VisualElement();
            using var icone = new IconeFilaGrupoHud(raizHud, estado);

            conexao.Simular(
                SocialMessageIds.StatusDaFilaDeGrupo,
                new StatusDaFilaDeGrupo { EmFila = true, Papel = PapelNoGrupo.Tank }
            );

            icone.AtualizarRotacao(0.5f);
            var labelGiro = icone.Elemento.Q<Label>("social.fila-hud-icone");
            Assert.AreEqual(90f, labelGiro.style.rotate.value.angle.value, 0.01f);
        }
    }
}
