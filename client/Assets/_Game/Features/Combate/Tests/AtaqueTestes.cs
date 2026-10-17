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
                ApresentacaoAtaque.TextoStaminaInsuficiente,
                ApresentacaoAtaque.TextoDaRecusa(CodigosErroCombate.StaminaInsuficiente)
            );
            Assert.AreEqual(
                ApresentacaoAtaque.TextoAcaoEmAndamento,
                ApresentacaoAtaque.TextoDaRecusa(CodigosErroCombate.AcaoEmAndamento)
            );
            Assert.AreEqual(
                ApresentacaoAtaque.TextoVoceEstaCaido,
                ApresentacaoAtaque.TextoDaRecusa(CodigosErroCombate.VoceEstaCaido)
            );
            Assert.AreEqual(
                ApresentacaoAtaque.TextoVoceEstaAtordoado,
                ApresentacaoAtaque.TextoDaRecusa(CodigosErroCombate.VoceEstaAtordoado)
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
        public void MostraODano_ProprioJogadorEOuAliado()
        {
            Assert.IsTrue(ApresentacaoAtaque.MostraODano(7, 7));
            Assert.IsFalse(ApresentacaoAtaque.MostraODano(8, 7));
            Assert.IsFalse(ApresentacaoAtaque.MostraODano(0, 0));

            // Com consulta de aliado
            Assert.IsTrue(ApresentacaoAtaque.MostraODano(8, 7, atacanteId => atacanteId == 8));
            Assert.IsFalse(ApresentacaoAtaque.MostraODano(9, 7, atacanteId => atacanteId == 8));
            Assert.IsFalse(ApresentacaoAtaque.MostraODano(8, 7, null));
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

            aviso.Mostrar("Stamina insuficiente");
            Assert.IsTrue(aviso.Visivel);
            Assert.AreEqual("Stamina insuficiente", aviso.Texto);

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
                StaminaAtual = 10,
                StaminaMaxima = 50,
                ManaAtual = 0,
                ManaMaxima = 20,
            };

            var ok = ApresentacaoCombate.TentarMontar(
                mensagem,
                out var vida,
                out var stamina,
                out var mana
            );

            Assert.IsTrue(ok);
            Assert.AreEqual("Vida 70 / 100", vida.Texto);
            Assert.AreEqual("Stamina 10 / 50", stamina.Texto);
            Assert.AreEqual("Mana 0 / 20", mana.Texto);
        }

        [Test]
        public void TentarMontar_RecursosMudaramNulo_NaoMonta()
        {
            Assert.IsFalse(
                ApresentacaoCombate.TentarMontar((RecursosMudaram)null, out _, out _, out _)
            );
        }

        [Test]
        public void GirarParaAMira_NaoPassaDoTetoDoAtaque()
        {
            // Pesado: 60°/s. Em 0,5 s só 30° dos 180° pedidos.
            Assert.AreEqual(
                30f,
                ApresentacaoAtaque.GirarParaAMira(0f, 180f, TipoDeAtaque.Pesado, 0.5f),
                1e-3f
            );
            // Leve: 240°/s. Em 0,25 s chega aos 60° pedidos.
            Assert.AreEqual(
                60f,
                ApresentacaoAtaque.GirarParaAMira(0f, 60f, TipoDeAtaque.Leve, 0.25f),
                1e-3f
            );
            // Pelo menor caminho: de 350° para 10° é horário, e termina entre 0 e 360.
            Assert.AreEqual(
                0f,
                ApresentacaoAtaque.GirarParaAMira(350f, 10f, TipoDeAtaque.Pesado, 10f / 60f),
                1e-3f
            );
        }

        [Test]
        public void VelocidadeDuranteGolpe_E40PorCentoDaCorrida()
        {
            Assert.AreEqual(2.2f, ApresentacaoAtaque.VelocidadeDuranteGolpe(5.5f), 1e-4f);
        }

        [Test]
        public void GolpeForte_PesadoOuTerceiroLeve()
        {
            Assert.IsTrue(ApresentacaoAtaque.GolpeForte(TipoDeAtaque.Pesado, 0));
            Assert.IsTrue(ApresentacaoAtaque.GolpeForte(TipoDeAtaque.Leve, 2));
            Assert.IsFalse(ApresentacaoAtaque.GolpeForte(TipoDeAtaque.Leve, 1));
        }

        // ── Clique e segurar para pesado (0,3 s) ───────────────────────────

        [Test]
        public void SegurarParaPesado_SoltarAntesDoLimiar_DisparaAtaqueLeve()
        {
            var proc = new ProcessadorDeCliqueAtaque();

            // Pressionou no frame t=1.0: ainda não dispara
            var disparou = proc.Atualizar(
                apertouNesteQuadro: true,
                segurandoBotao: true,
                podeAtacar: true,
                agora: 1.0,
                out var golpe
            );
            Assert.IsFalse(disparou);
            Assert.IsNull(golpe);
            Assert.IsTrue(proc.Segurando);

            // Mantém pressionado por 0,15 s (menos que 0,3 s): não dispara
            disparou = proc.Atualizar(
                apertouNesteQuadro: false,
                segurandoBotao: true,
                podeAtacar: true,
                agora: 1.15,
                out golpe
            );
            Assert.IsFalse(disparou);
            Assert.IsNull(golpe);

            // Solta aos 0,2 s: dispara leve
            disparou = proc.Atualizar(
                apertouNesteQuadro: false,
                segurandoBotao: false,
                podeAtacar: true,
                agora: 1.20,
                out golpe
            );
            Assert.IsTrue(disparou);
            Assert.AreEqual(TipoDeAtaque.Leve, golpe);
            Assert.IsFalse(proc.Segurando);
        }

        [Test]
        public void SegurarParaPesado_AlcancandoLimiarExato_DisparaAtaquePesado()
        {
            var proc = new ProcessadorDeCliqueAtaque();
            const double inicio = 2.0;

            // Início do clique
            proc.Atualizar(true, true, true, inicio, out _);

            // Logo antes do limiar (0,299 s): nada
            var disparouAntes = proc.Atualizar(
                false,
                true,
                true,
                inicio + RegrasAtaque.SegurarParaPesadoSegundos - 0.001,
                out var golpeAntes
            );
            Assert.IsFalse(disparouAntes);
            Assert.IsNull(golpeAntes);

            // Ao atingir 0,3 s exatos: dispara pesado imediatamente
            var disparou = proc.Atualizar(
                false,
                true,
                true,
                inicio + RegrasAtaque.SegurarParaPesadoSegundos,
                out var golpe
            );
            Assert.IsTrue(disparou);
            Assert.AreEqual(TipoDeAtaque.Pesado, golpe);
            Assert.IsTrue(proc.PesadoDoClique);
        }

        [Test]
        public void SegurarParaPesado_SoltarAposDisparoDePesado_NaoDisparaSegundoAtaque()
        {
            var proc = new ProcessadorDeCliqueAtaque();

            // Pressiona e segura por 0,35 s (passou do limiar)
            proc.Atualizar(true, true, true, 0.0, out _);
            var disparouPesado = proc.Atualizar(false, true, true, 0.35, out var golpePesado);
            Assert.IsTrue(disparouPesado);
            Assert.AreEqual(TipoDeAtaque.Pesado, golpePesado);

            // Continua segurando: não dispara de novo
            var disparouDurante = proc.Atualizar(false, true, true, 0.50, out var golpeDurante);
            Assert.IsFalse(disparouDurante);
            Assert.IsNull(golpeDurante);

            // Solta o botão após pesado: nenhum ataque adicional é emitido
            var disparouAoSoltar = proc.Atualizar(false, false, true, 0.60, out var golpeAoSoltar);
            Assert.IsFalse(disparouAoSoltar);
            Assert.IsNull(golpeAoSoltar);
            Assert.IsFalse(proc.Segurando);
        }

        [Test]
        public void SegurarParaPesado_QuandoNaoPodeAtacar_IgnoraClique()
        {
            var proc = new ProcessadorDeCliqueAtaque();

            // Clicou com menu aberto ou desconectado: não registra
            var disparou = proc.Atualizar(true, true, false, 1.0, out var golpe);
            Assert.IsFalse(disparou);
            Assert.IsNull(golpe);
            Assert.IsFalse(proc.Segurando);
        }

        [Test]
        public void SegurarParaPesado_PerdeCondicaoDeAtaqueDuranteHold_CancelaSemDisparar()
        {
            var proc = new ProcessadorDeCliqueAtaque();

            // Começa segurando em estado válido
            proc.Atualizar(true, true, true, 1.0, out _);
            Assert.IsTrue(proc.Segurando);

            // Aos 0,15 s abriu um menu (PodeAtacar = false): cancela o clique
            var disparouCancel = proc.Atualizar(false, true, false, 1.15, out var golpeCancel);
            Assert.IsFalse(disparouCancel);
            Assert.IsNull(golpeCancel);
            Assert.IsFalse(proc.Segurando);

            // Mesmo se passar de 0,3 s segurando com o menu aberto, nada sai
            var disparouDepois = proc.Atualizar(false, true, false, 1.40, out var golpeDepois);
            Assert.IsFalse(disparouDepois);
            Assert.IsNull(golpeDepois);
        }

        [Test]
        public void SegurarParaPesado_CliquesConsecutivos_LeveDepoisPesado()
        {
            var proc = new ProcessadorDeCliqueAtaque();

            // Primeiro clique: rápido (leve)
            proc.Atualizar(true, true, true, 1.0, out _);
            var primeiro = proc.Atualizar(false, false, true, 1.1, out var golpe1);
            Assert.IsTrue(primeiro);
            Assert.AreEqual(TipoDeAtaque.Leve, golpe1);

            // Segundo clique: mantido até pesado
            proc.Atualizar(true, true, true, 2.0, out _);
            var segundo = proc.Atualizar(false, true, true, 2.3, out var golpe2);
            Assert.IsTrue(segundo);
            Assert.AreEqual(TipoDeAtaque.Pesado, golpe2);

            // Solta segundo clique
            proc.Atualizar(false, false, true, 2.5, out var golpe3);
            Assert.IsNull(golpe3);
        }
    }
}
