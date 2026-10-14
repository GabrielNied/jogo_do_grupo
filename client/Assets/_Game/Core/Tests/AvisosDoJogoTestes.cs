using System.Linq;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// Regras das mensagens de sistema: limite de 5 linhas, 6 s cada, repetida em menos de
    /// 2 s vira contador, e a cor (classe) por tipo. O relógio é injetado: nada espera de verdade.
    /// </summary>
    public class AvisosDoJogoTestes
    {
        private double _agora;
        private ListaDeAvisos _lista;

        [SetUp]
        public void Preparar()
        {
            _agora = 10.0;
            _lista = new ListaDeAvisos(() => _agora);
        }

        [Test]
        public void Mostrar_EntraEmbaixo()
        {
            _lista.Mostrar("primeira", TipoDeAviso.Informacao);
            _lista.Mostrar("segunda", TipoDeAviso.Informacao);

            Assert.AreEqual(2, _lista.Linhas.Count);
            Assert.AreEqual("primeira", _lista.Linhas[0].Texto);
            Assert.AreEqual("segunda", _lista.Linhas[1].Texto);
        }

        [Test]
        public void Mostrar_TextoVazio_Ignora()
        {
            Assert.IsFalse(_lista.Mostrar(null, TipoDeAviso.Erro));
            Assert.IsFalse(_lista.Mostrar("", TipoDeAviso.Erro));
            Assert.IsFalse(_lista.Mostrar("   ", TipoDeAviso.Erro));
            Assert.AreEqual(0, _lista.Linhas.Count);
        }

        [Test]
        public void Limite_DeCinco_SaiAMaisAntiga()
        {
            for (var i = 1; i <= ListaDeAvisos.Maximo + 1; i++)
            {
                _lista.Mostrar($"mensagem {i}", TipoDeAviso.Informacao);
            }

            Assert.AreEqual(5, ListaDeAvisos.Maximo);
            Assert.AreEqual(ListaDeAvisos.Maximo, _lista.Linhas.Count);
            Assert.AreEqual("mensagem 2", _lista.Linhas[0].Texto);
            Assert.AreEqual("mensagem 6", _lista.Linhas[4].Texto);
        }

        [Test]
        public void Expiracao_SomeEmSeisSegundos()
        {
            _lista.Mostrar("Inventário cheio", TipoDeAviso.Aviso);

            _agora += 5.9;
            Assert.IsFalse(_lista.Atualizar());
            Assert.AreEqual(1, _lista.Linhas.Count);

            _agora += 0.1;
            Assert.IsTrue(_lista.Atualizar());
            Assert.AreEqual(0, _lista.Linhas.Count);
        }

        [Test]
        public void Expiracao_CadaLinhaNoSeuTempo()
        {
            _lista.Mostrar("antiga", TipoDeAviso.Informacao);
            _agora += 3;
            _lista.Mostrar("nova", TipoDeAviso.Informacao);

            _agora += 3;
            _lista.Atualizar();

            Assert.AreEqual(1, _lista.Linhas.Count);
            Assert.AreEqual("nova", _lista.Linhas[0].Texto);
        }

        [Test]
        public void Repetida_EmMenosDeDoisSegundos_ViraContadorERecomecaOTempo()
        {
            _lista.Mostrar("Vigor insuficiente", TipoDeAviso.Erro);
            _agora += 1.5;
            _lista.Mostrar("Vigor insuficiente", TipoDeAviso.Erro);
            _agora += 1.5;
            _lista.Mostrar("Vigor insuficiente", TipoDeAviso.Erro);

            Assert.AreEqual(1, _lista.Linhas.Count);
            Assert.AreEqual(3, _lista.Linhas[0].Contagem);
            Assert.AreEqual("Vigor insuficiente x3", _lista.Linhas[0].TextoNaTela);

            // 6 s depois da primeira, mas só 3 s depois da última: continua.
            _agora += 3;
            _lista.Atualizar();
            Assert.AreEqual(1, _lista.Linhas.Count);

            _agora += 3;
            _lista.Atualizar();
            Assert.AreEqual(0, _lista.Linhas.Count);
        }

        [Test]
        public void Repetida_DepoisDeDoisSegundos_CriaLinhaNova()
        {
            _lista.Mostrar("Inventário cheio", TipoDeAviso.Aviso);
            _agora += ListaDeAvisos.JanelaDeRepeticao;
            _lista.Mostrar("Inventário cheio", TipoDeAviso.Aviso);

            Assert.AreEqual(2, _lista.Linhas.Count);
            Assert.AreEqual("Inventário cheio", _lista.Linhas[1].TextoNaTela);
        }

        [Test]
        public void MesmoTextoComOutroTipo_NaoSoma()
        {
            _lista.Mostrar("Falhou", TipoDeAviso.Aviso);
            _lista.Mostrar("Falhou", TipoDeAviso.Erro);

            Assert.AreEqual(2, _lista.Linhas.Count);
        }

        [Test]
        public void Repetida_ContaNaLinhaMesmoComOutrasNoMeio()
        {
            _lista.Mostrar("A", TipoDeAviso.Informacao);
            _lista.Mostrar("B", TipoDeAviso.Informacao);
            _lista.Mostrar("A", TipoDeAviso.Informacao);

            Assert.AreEqual(2, _lista.Linhas.Count);
            Assert.AreEqual(2, _lista.Linhas[0].Contagem);
        }

        [TestCase(TipoDeAviso.Informacao, EstiloCore.AvisoInformacao)]
        [TestCase(TipoDeAviso.Aviso, EstiloCore.AvisoAviso)]
        [TestCase(TipoDeAviso.Erro, EstiloCore.AvisoErro)]
        public void Painel_UsaAClasseDoTipo(TipoDeAviso tipo, string classe)
        {
            var regiao = new VisualElement();
            var painel = new PainelDeAvisos(regiao, _lista);

            painel.Mostrar("mensagem", tipo);

            var linha = (Label)painel.Raiz[0];
            Assert.IsTrue(linha.ClassListContains(EstiloCore.Aviso));
            Assert.IsTrue(linha.ClassListContains(classe));
            Assert.AreEqual(1, linha.GetClasses().Count(c => c.StartsWith("aviso-")));
        }

        [Test]
        public void Painel_MostraContadorETiraVencidas()
        {
            var regiao = new VisualElement();
            var painel = new PainelDeAvisos(regiao, _lista);
            Assert.AreSame(regiao, painel.Raiz.parent);
            Assert.AreEqual(PickingMode.Ignore, painel.Raiz.pickingMode);

            painel.Mostrar("Inventário cheio", TipoDeAviso.Aviso);
            painel.Mostrar("Inventário cheio", TipoDeAviso.Aviso);
            Assert.AreEqual(1, painel.Raiz.childCount);
            Assert.AreEqual("Inventário cheio x2", ((Label)painel.Raiz[0]).text);

            _agora += ListaDeAvisos.Duracao;
            painel.Atualizar();
            Assert.AreEqual(0, painel.Raiz.childCount);
        }

        [Test]
        public void Painel_LimiteDeCincoNaTela()
        {
            var painel = new PainelDeAvisos(new VisualElement(), _lista);

            for (var i = 0; i < 8; i++)
            {
                painel.Mostrar($"mensagem {i}", TipoDeAviso.Informacao);
            }

            Assert.AreEqual(ListaDeAvisos.Maximo, painel.Raiz.childCount);
            Assert.AreEqual("mensagem 7", ((Label)painel.Raiz[4]).text);
        }

        [Test]
        public void Mostrar_DisparaOEvento_EIgnoraTextoVazio()
        {
            string recebido = null;
            void Ouvir(string texto, TipoDeAviso tipo) => recebido = texto;
            AvisosDoJogo.Mostrado += Ouvir;
            try
            {
                AvisosDoJogo.Mostrar("Inventário cheio", TipoDeAviso.Aviso);
                Assert.AreEqual("Inventário cheio", recebido);

                recebido = "ainda";
                AvisosDoJogo.Mostrar("  ", TipoDeAviso.Erro);
                Assert.AreEqual("ainda", recebido);
            }
            finally
            {
                AvisosDoJogo.Mostrado -= Ouvir;
            }
        }

        [Test]
        public void CategoriaDeCombate_PodeSerOcultadaSemPerderHistoricoNemErro()
        {
            var antes = PreferenciasDeAvisos.MostrarCombate;
            var painel = new PainelDeAvisos(new VisualElement(), _lista);
            try
            {
                PreferenciasDeAvisos.MostrarCombate = false;
                painel.Mostrar("golpe", TipoDeAviso.Aviso, CategoriaDeAviso.Combate);
                painel.Mostrar("falha", TipoDeAviso.Erro, CategoriaDeAviso.Combate);
                Assert.AreEqual(2, _lista.Linhas.Count);
                Assert.AreEqual(1, painel.Raiz.childCount);

                PreferenciasDeAvisos.MostrarCombate = true;
                painel.AtualizarVisibilidade();
                Assert.AreEqual(2, painel.Raiz.childCount);
            }
            finally
            {
                PreferenciasDeAvisos.MostrarCombate = antes;
            }
        }
    }
}
