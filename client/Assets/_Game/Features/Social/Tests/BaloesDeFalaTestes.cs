using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social.Tests
{
    public class BaloesDeFalaTestes
    {
        private VisualElement _camada;
        private GameObject _objetoGerenciador;
        private GerenciadorBaloesDeFala _gerenciador;
        private GameObject _avatarTeste;

        [SetUp]
        public void Preparar()
        {
            _camada = new VisualElement { name = "CamadaTeste" };
            _objetoGerenciador = new GameObject("GerenciadorTeste");
            _gerenciador = _objetoGerenciador.AddComponent<GerenciadorBaloesDeFala>();
            _gerenciador.Configurar(_camada);

            _avatarTeste = new GameObject("Jogador_42");
            _avatarTeste.transform.position = new Vector3(0, 0, 5);
        }

        [TearDown]
        public void Finalizar()
        {
            if (_gerenciador != null)
            {
                _gerenciador.DescartarTudo();
            }

            if (_objetoGerenciador != null)
            {
                Object.DestroyImmediate(_objetoGerenciador);
            }

            if (_avatarTeste != null)
            {
                Object.DestroyImmediate(_avatarTeste);
            }
        }

        [Test]
        public void BalaoDeFalaVisual_CriacaoEClasses_AplicaEstilosLumora()
        {
            var balao = new BalaoDeFalaVisual(
                1,
                "Aldor",
                "Olá mundo!",
                CanalDeChat.Cidade,
                _avatarTeste.transform,
                _camada
            );

            Assert.IsTrue(balao.Elemento.ClassListContains(EstiloSocial.BalaoFala));
            Assert.IsTrue(balao.RotuloAutor.ClassListContains(EstiloSocial.BalaoAutor));
            Assert.IsTrue(balao.RotuloTexto.ClassListContains(EstiloSocial.BalaoTexto));
            Assert.IsTrue(balao.Ponta.ClassListContains(EstiloSocial.BalaoPonta));
            Assert.AreEqual("Aldor", balao.RotuloAutor.text);
            Assert.AreEqual("Olá mundo!", balao.RotuloTexto.text);
            Assert.AreEqual(RegrasBaloesDeFala.CalcularDuracao("Olá mundo!"), balao.DuracaoTotal);
            Assert.AreEqual(1f, balao.Elemento.style.opacity.value);

            balao.Descartar();
        }

        [Test]
        public void BalaoDeFalaVisual_NovaMensagem_RenovaTempoEOpacidade()
        {
            var balao = new BalaoDeFalaVisual(
                1,
                "Aldor",
                "Mensagem inicial",
                CanalDeChat.Cidade,
                _avatarTeste.transform,
                _camada
            );

            // Simula passagem de tempo até quase expirar
            balao.Atualizar(balao.DuracaoTotal - 0.2f, null, _camada);
            Assert.Less(balao.Elemento.style.opacity.value, 1f);

            // Envia nova mensagem
            balao.DefinirMensagem("Aldor", "Nova mensagem!", CanalDeChat.Cidade);
            Assert.AreEqual("Nova mensagem!", balao.RotuloTexto.text);
            Assert.AreEqual(balao.DuracaoTotal, balao.TempoRestante);
            Assert.AreEqual(1f, balao.Elemento.style.opacity.value);

            balao.Descartar();
        }

        [Test]
        public void BalaoDeFalaVisual_FadeOut_EsmaeceSuavementeNosUltimosSegundos()
        {
            var balao = new BalaoDeFalaVisual(
                1,
                "Aldor",
                "Teste fade",
                CanalDeChat.Cidade,
                _avatarTeste.transform,
                _camada
            );

            // Avança até a metade do fade out (0.4s restantes de 0.8s)
            float tempoAteMeioFade =
                balao.DuracaoTotal - (RegrasBaloesDeFala.TempoFadeOutSegundos * 0.5f);
            balao.Atualizar(tempoAteMeioFade, null, _camada);

            Assert.AreEqual(0.5f, balao.Elemento.style.opacity.value, 0.05f);

            balao.Descartar();
        }

        [Test]
        public void BalaoDeFalaVisual_Expirado_RetornaTrueEFecha()
        {
            var balao = new BalaoDeFalaVisual(
                1,
                "Aldor",
                "Mensagem rápida",
                CanalDeChat.Cidade,
                _avatarTeste.transform,
                _camada
            );

            bool expirou = balao.Atualizar(balao.DuracaoTotal + 0.1f, null, _camada);
            Assert.IsTrue(expirou);
            Assert.AreEqual(DisplayStyle.None, balao.Elemento.style.display.value);

            balao.Descartar();
        }

        [Test]
        public void Gerenciador_ProcessarMensagem_CanalCidadeEGrupo_CriaBalao()
        {
            var msgCidade = new MensagemDeChat
            {
                RemetenteId = 42,
                NomeDoRemetente = "Guerreiro",
                Texto = "Vamos para a dungeon!",
                Canal = CanalDeChat.Cidade,
                HoraUnixMs = 1000,
            };

            _gerenciador.ProcessarMensagem(msgCidade, _ => _avatarTeste.transform);
            Assert.AreEqual(1, _gerenciador.BaloesAtivos.Count);
            Assert.IsTrue(_gerenciador.BaloesAtivos.ContainsKey(42));
            Assert.AreEqual("Vamos para a dungeon!", _gerenciador.BaloesAtivos[42].Texto);

            var msgGrupo = new MensagemDeChat
            {
                RemetenteId = 43,
                NomeDoRemetente = "Mago",
                Texto = "Pronto!",
                Canal = CanalDeChat.Grupo,
                HoraUnixMs = 1000,
            };

            _gerenciador.ProcessarMensagem(msgGrupo, _ => _avatarTeste.transform);
            Assert.AreEqual(2, _gerenciador.BaloesAtivos.Count);
            Assert.IsTrue(_gerenciador.BaloesAtivos.ContainsKey(43));
        }

        [Test]
        public void Gerenciador_ProcessarMensagem_Sussurro_NaoCriaBalao()
        {
            var msgSussurro = new MensagemDeChat
            {
                RemetenteId = 99,
                NomeDoRemetente = "Espiao",
                Texto = "Mensagem secreta privada",
                Canal = CanalDeChat.Sussurro,
                HoraUnixMs = 1000,
            };

            _gerenciador.ProcessarMensagem(msgSussurro, _ => _avatarTeste.transform);
            Assert.AreEqual(0, _gerenciador.BaloesAtivos.Count);
        }

        [Test]
        public void Gerenciador_ProcessarMensagem_MesmoAutor_AtualizaExistenteSemDuplicar()
        {
            var msg1 = new MensagemDeChat
            {
                RemetenteId = 42,
                NomeDoRemetente = "Aldor",
                Texto = "Primeira frase",
                Canal = CanalDeChat.Cidade,
            };

            var msg2 = new MensagemDeChat
            {
                RemetenteId = 42,
                NomeDoRemetente = "Aldor",
                Texto = "Segunda frase imediata",
                Canal = CanalDeChat.Cidade,
            };

            _gerenciador.ProcessarMensagem(msg1, _ => _avatarTeste.transform);
            Assert.AreEqual(1, _gerenciador.BaloesAtivos.Count);
            Assert.AreEqual("Primeira frase", _gerenciador.BaloesAtivos[42].Texto);

            _gerenciador.ProcessarMensagem(msg2, _ => _avatarTeste.transform);
            Assert.AreEqual(1, _gerenciador.BaloesAtivos.Count);
            Assert.AreEqual("Segunda frase imediata", _gerenciador.BaloesAtivos[42].Texto);
        }

        [Test]
        public void Gerenciador_Atualizar_DescartaExpirados()
        {
            var msg = new MensagemDeChat
            {
                RemetenteId = 42,
                NomeDoRemetente = "Aldor",
                Texto = "Vou sumir em breve",
                Canal = CanalDeChat.Cidade,
            };

            _gerenciador.ProcessarMensagem(msg, _ => _avatarTeste.transform);
            Assert.AreEqual(1, _gerenciador.BaloesAtivos.Count);

            // Passa o tempo inteiro de duração
            float duracao = _gerenciador.BaloesAtivos[42].DuracaoTotal;
            _gerenciador.Atualizar(duracao + 0.1f);

            Assert.AreEqual(0, _gerenciador.BaloesAtivos.Count);
        }
    }
}
