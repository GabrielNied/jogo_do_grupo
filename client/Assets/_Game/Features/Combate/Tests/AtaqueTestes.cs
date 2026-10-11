using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    /// <summary>Intenção, números de dano, avisos e barras do ataque (sem regra de dano).</summary>
    public class AtaqueTestes
    {
        // ── Intenção ────────────────────────────────────────────────────────────

        [Test]
        public void Criar_NumeraEmOrdemCrescente()
        {
            var gerador = new GeradorDeIntencoes();

            var primeira = gerador.Criar(TipoDeAtaque.Leve, 10f);
            var segunda = gerador.Criar(TipoDeAtaque.Pesado, 20f);

            Assert.AreEqual(1, primeira.Sequencia);
            Assert.AreEqual(2, segunda.Sequencia);
            Assert.AreEqual(TipoDeAtaque.Leve, primeira.Tipo);
            Assert.AreEqual(TipoDeAtaque.Pesado, segunda.Tipo);
            Assert.AreEqual(20f, segunda.DirecaoY);
        }

        [TestCase(0f, 0f)]
        [TestCase(90f, 90f)]
        [TestCase(360f, 0f)]
        [TestCase(450f, 90f)]
        [TestCase(-90f, 270f)]
        [TestCase(float.NaN, 0f)]
        [TestCase(float.PositiveInfinity, 0f)]
        public void NormalizarDirecao_LevaPara0A360(float entrada, float esperado)
        {
            Assert.AreEqual(esperado, GeradorDeIntencoes.NormalizarDirecao(entrada), 0.001f);
        }

        // ── Texto ──────────────────────────────────────────────────────────────

        [Test]
        public void TextoDoDano_MostraOValorDoServidor_NuncaNegativo()
        {
            Assert.AreEqual("42", ApresentacaoAtaque.TextoDoDano(42));
            Assert.AreEqual("0", ApresentacaoAtaque.TextoDoDano(-5));
        }

        [Test]
        public void TextoDaRecusa_TraduzCadaCodigo()
        {
            Assert.AreEqual(
                ApresentacaoAtaque.TextoVigorInsuficiente,
                ApresentacaoAtaque.TextoDaRecusa(CodigosErroCombate.VigorInsuficiente)
            );
            Assert.AreEqual(
                ApresentacaoAtaque.TextoAcaoEmAndamento,
                ApresentacaoAtaque.TextoDaRecusa(CodigosErroCombate.AcaoEmAndamento)
            );
            Assert.AreEqual(
                ApresentacaoAtaque.TextoVoceEstaCaido,
                ApresentacaoAtaque.TextoDaRecusa(CodigosErroCombate.VoceEstaCaido)
            );
        }

        [Test]
        public void TextoDaRecusa_CodigoDesconhecidoOuNulo_TextoGenerico()
        {
            Assert.AreEqual(
                ApresentacaoAtaque.TextoRecusaGenerica,
                ApresentacaoAtaque.TextoDaRecusa("Inventado")
            );
            Assert.AreEqual(
                ApresentacaoAtaque.TextoRecusaGenerica,
                ApresentacaoAtaque.TextoDaRecusa(null)
            );
        }

        [Test]
        public void MostraODano_SoDoProprioJogador()
        {
            Assert.IsTrue(ApresentacaoAtaque.MostraODano(7, 7));
            Assert.IsFalse(ApresentacaoAtaque.MostraODano(8, 7));
            Assert.IsFalse(ApresentacaoAtaque.MostraODano(0, 0));
        }

        // ── Números de dano ────────────────────────────────────────────────────

        [Test]
        public void Fila_NumeroSomeDepoisDeUmSegundo()
        {
            var fila = new FilaDeNumeros();
            fila.Adicionar("10", false, 0f);

            fila.Atualizar(0.5f);
            Assert.AreEqual(1, fila.Itens.Count);
            Assert.AreEqual(0.5f, fila.Itens[0].Progresso, 0.001f);

            fila.Atualizar(0.5f);
            Assert.AreEqual(0, fila.Itens.Count);
        }

        [Test]
        public void Fila_PassandoDoMaximo_OsMaisAntigosSomemPrimeiro()
        {
            var fila = new FilaDeNumeros();
            for (var i = 0; i < FilaDeNumeros.Maximo + 3; i++)
            {
                fila.Adicionar(i.ToString(), false, 0f);
            }

            Assert.AreEqual(FilaDeNumeros.Maximo, fila.Itens.Count);
            Assert.AreEqual("3", fila.Itens[0].Texto);
            Assert.AreEqual(
                (FilaDeNumeros.Maximo + 2).ToString(),
                fila.Itens[fila.Itens.Count - 1].Texto
            );
        }

        [Test]
        public void Fila_GuardaSePesadoEODeslocamento()
        {
            var fila = new FilaDeNumeros();
            fila.Adicionar("44", true, -35f);

            Assert.IsTrue(fila.Itens[0].Pesado);
            Assert.AreEqual(-35f, fila.Itens[0].Deslocamento);
        }

        [Test]
        public void Fila_Limpar_EsvaziaTudo()
        {
            var fila = new FilaDeNumeros();
            fila.Adicionar("1", false, 0f);

            fila.Limpar();

            Assert.AreEqual(0, fila.Itens.Count);
        }

        // ── Aviso de recusa ────────────────────────────────────────────────────

        [Test]
        public void Aviso_VisivelPorDoisSegundosESome()
        {
            var aviso = new AvisoTemporario();
            Assert.IsFalse(aviso.Visivel);

            aviso.Mostrar("Vigor insuficiente");
            Assert.IsTrue(aviso.Visivel);
            Assert.AreEqual("Vigor insuficiente", aviso.Texto);

            aviso.Atualizar(AvisoTemporario.DuracaoSegundos - 0.1f);
            Assert.IsTrue(aviso.Visivel);

            aviso.Atualizar(0.2f);
            Assert.IsFalse(aviso.Visivel);
            Assert.AreEqual("", aviso.Texto);
        }

        [Test]
        public void Aviso_NovoAvisoRecomecaOTempo()
        {
            var aviso = new AvisoTemporario();
            aviso.Mostrar("a");
            aviso.Atualizar(1.5f);

            aviso.Mostrar("b");
            aviso.Atualizar(1.5f);

            Assert.IsTrue(aviso.Visivel);
            Assert.AreEqual("b", aviso.Texto);
        }

        // ── Animação e barras ──────────────────────────────────────────────────

        [Test]
        public void ParametroDe_PesadoELeveTemTriggersProprios()
        {
            Assert.AreEqual(
                AnimadorDeAtaque.ParametroAtaquePesado,
                AnimadorDeAtaque.ParametroDe(TipoDeAtaque.Pesado)
            );
            Assert.AreEqual(
                AnimadorDeAtaque.ParametroAtaqueLeve,
                AnimadorDeAtaque.ParametroDe(TipoDeAtaque.Leve)
            );
        }

        [Test]
        public void Tocar_SemAnimator_NaoFazNada()
        {
            Assert.IsFalse(AnimadorDeAtaque.Tocar(null, TipoDeAtaque.Leve));
        }

        [Test]
        public void TentarMontar_RecursosMudaram_CopiaOsNumerosDoServidor()
        {
            var mensagem = new RecursosMudaram
            {
                VidaAtual = 70,
                VidaMaxima = 100,
                VigorAtual = 10,
                VigorMaximo = 50,
                EnergiaAtual = 0,
                EnergiaMaxima = 20,
            };

            var ok = ApresentacaoCombate.TentarMontar(
                mensagem,
                out var vida,
                out var vigor,
                out var energia
            );

            Assert.IsTrue(ok);
            Assert.AreEqual("Vida 70 / 100", vida.Texto);
            Assert.AreEqual("Vigor 10 / 50", vigor.Texto);
            Assert.AreEqual("Energia 0 / 20", energia.Texto);
        }

        [Test]
        public void TentarMontar_RecursosMudaramNulo_NaoMonta()
        {
            Assert.IsFalse(
                ApresentacaoCombate.TentarMontar((RecursosMudaram)null, out _, out _, out _)
            );
        }
    }
}
