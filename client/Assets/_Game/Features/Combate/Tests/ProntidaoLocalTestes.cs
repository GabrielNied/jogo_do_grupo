using JogoDoGrupo.Client.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    public class ProntidaoLocalTestes
    {
        [Test]
        public void SemStaminaConhecido_AtaqueEEsquivaSeguem()
        {
            var prontidao = new ProntidaoLocal();

            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Pesado, 0));
            Assert.IsTrue(prontidao.PodeEsquivar(0));
        }

        [Test]
        public void SemStamina_PesadoEEsquivaNaoSeguem_LeveSegue()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarStamina(RegrasEsquiva.CustoDeStamina - 1);

            Assert.IsFalse(prontidao.PodeAtacar(TipoDeAtaque.Pesado, 0));
            Assert.IsFalse(prontidao.PodeEsquivar(0));
            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Leve, 0));
        }

        [Test]
        public void ComStamina_PesadoEEsquivaSeguem()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarStamina(RegrasAtaque.CustoDeStaminaPesado);

            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Pesado, 0));
            Assert.IsTrue(prontidao.PodeEsquivar(0));
        }

        [Test]
        public void Recarga_BloqueiaAteADuracaoAcabar()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarStamina(100);
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);

            var durante = 10 + RegrasAtaque.DuracaoSegundos(TipoDeAtaque.Leve) - 0.01;
            Assert.IsFalse(prontidao.PodeAtacar(TipoDeAtaque.Leve, durante));

            var depois = 10 + RegrasAtaque.DuracaoSegundos(TipoDeAtaque.Leve);
            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Leve, depois));
            Assert.IsTrue(prontidao.PodeEsquivar(depois));
        }

        [Test]
        public void Compromisso_EsquivaSoNaRecuperacao_ECortaOResto()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);

            var noPreparo = 10 + RegrasAtaque.PreparoSegundos(TipoDeAtaque.Leve) - 0.01;
            var naRecuperacao = 10 + RegrasAtaque.PreparoSegundos(TipoDeAtaque.Leve);
            Assert.IsTrue(prontidao.Comprometido(noPreparo));
            Assert.IsFalse(prontidao.PodeEsquivar(noPreparo));
            Assert.IsFalse(prontidao.Comprometido(naRecuperacao));
            Assert.IsTrue(prontidao.PodeEsquivar(naRecuperacao));

            prontidao.MarcarEsquiva(naRecuperacao);
            Assert.AreEqual(naRecuperacao + RegrasEsquiva.DuracaoSegundos, prontidao.LivreEm, 1e-9);
            Assert.IsFalse(prontidao.PodeAtacar(TipoDeAtaque.Leve, naRecuperacao + 0.1));
            Assert.IsFalse(prontidao.PodeEsquivar(naRecuperacao + 0.1));
        }

        [Test]
        public void Reacao_ParaOGolpe_ESeguraAProximaAcaoPor300Ms()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);

            prontidao.MarcarReacao(10.2, RegrasReacao.AtordoamentoNoAcertoMs / 1000.0);

            Assert.IsFalse(prontidao.Comprometido(10.25));
            Assert.IsFalse(prontidao.PodeAtacar(TipoDeAtaque.Leve, 10.4));
            Assert.IsFalse(prontidao.PodeEsquivar(10.4));
            Assert.IsFalse(prontidao.PodeGuardar(10.4));
            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Leve, 10.5));
            Assert.IsTrue(prontidao.PodeEsquivar(10.5));
        }

        [Test]
        public void Guarda_NaoSobeNoPreparo_VaiNaRecuperacao_ECortaOResto()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);

            Assert.IsFalse(prontidao.PodeGuardar(10.1));
            var naRecuperacao = 10 + RegrasAtaque.PreparoSegundos(TipoDeAtaque.Leve) + 0.05;
            Assert.IsTrue(prontidao.PodeGuardar(naRecuperacao));

            prontidao.MarcarGuarda(true, naRecuperacao);

            Assert.IsTrue(prontidao.Guardando);
            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Leve, naRecuperacao));
        }

        [Test]
        public void Reacao_DerrubaAGuarda()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.MarcarGuarda(true, 10);

            prontidao.MarcarReacao(10.1, 0.3);

            Assert.IsFalse(prontidao.Guardando);
        }

        // ── Stamina prevista (#479) ────────────────────────────────────────────────

        [Test]
        public void Regeneracao_PrevistaLiberaOPesadoQuandoAlcancaOCusto()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarStamina(100, 0);
            prontidao.MarcarAtaque(TipoDeAtaque.Pesado, 10); // 80
            prontidao.MarcarAtaque(TipoDeAtaque.Pesado, 12); // 60
            prontidao.MarcarAtaque(TipoDeAtaque.Pesado, 14); // 40
            prontidao.MarcarAtaque(TipoDeAtaque.Pesado, 16); // 20
            prontidao.MarcarAtaque(TipoDeAtaque.Pesado, 18); // 0

            // Sem previsão, o client recusaria por 3 s; com ela, a espera de 2 s e 2 s a 10 por s.
            var agora = 18 + RegrasRecursos.AtrasoRegeneracaoSegundos;
            Assert.IsFalse(prontidao.PodeAtacar(TipoDeAtaque.Pesado, 18 + 1.6));
            Assert.IsFalse(prontidao.PodeAtacar(TipoDeAtaque.Pesado, agora + 1.9));
            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Pesado, agora + 2.0));
            Assert.AreEqual(RegrasAtaque.CustoDeStaminaPesado, prontidao.StaminaEm(agora + 2.0));
        }

        [Test]
        public void Gasto_ReiniciaAEspera_ESegurarAGuardaPausaARegeneracao()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarStamina(100, 0);
            prontidao.MarcarAtaque(TipoDeAtaque.Pesado, 10); // 80, gasto em 10

            Assert.AreEqual(80, prontidao.StaminaEm(11.9));
            Assert.AreEqual(100, prontidao.StaminaEm(15)); // 2 s de espera, depois 10 por segundo.

            prontidao.RegistrarStamina(50, 30);
            prontidao.MarcarGuarda(true, 30);
            Assert.AreEqual(50, prontidao.StaminaEm(40), "guarda segurada: nada regenera");

            prontidao.MarcarGuarda(false, 40);
            Assert.AreEqual(50, prontidao.StaminaEm(41.9), "a espera recomeça ao soltar");
            Assert.AreEqual(70, prontidao.StaminaEm(44), "2 s depois, 10 por segundo");
        }

        [Test]
        public void Regeneracao_UsaOSecundarioDoServidorEmVezDaBaseFixa()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarStamina(50, 0);
            prontidao.RegistrarRegeneracaoDeStamina(15, 0); // base 10 + 5 do equipamento

            // Sem gasto recente a espera de 2 s já passou; a 15 por segundo, 2 s dão 30.
            Assert.AreEqual(80, prontidao.StaminaEm(2));
        }

        [Test]
        public void AplicarDoServidor_AtualizaTaxaDeRegeneracaoEPreservaOTeto()
        {
            var objeto = new GameObject();
            try
            {
                var controle = objeto.AddComponent<ControleRecursos>();
                var prontidao = new ProntidaoLocal();
                controle.Configurar(1, new PainelRecursos(new VisualElement()), prontidao);
                var agora = Time.unscaledTimeAsDouble;

                controle.AplicarDoServidor(
                    new RecursosMudaram
                    {
                        StaminaAtual = 50,
                        StaminaMaxima = 100,
                        RegeneracaoStamina = 15,
                    }
                );

                Assert.AreEqual(65, prontidao.StaminaEm(agora + 1));

                controle.AplicarDoServidor(
                    new RecursosMudaram
                    {
                        StaminaAtual = 95,
                        StaminaMaxima = 100,
                        RegeneracaoStamina = 15,
                    }
                );

                Assert.AreEqual(100, prontidao.StaminaEm(agora + 2));
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void Retrato_ComStaminaAbaixoDoPrevisto_ContaComoGasto()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarStamina(50, 0);

            // Aos 10 s o previsto seria 100; o servidor diz 40: houve um gasto agora.
            prontidao.RegistrarStamina(40, 10);

            Assert.AreEqual(40, prontidao.StaminaEm(11.9));
            Assert.AreEqual(50, prontidao.StaminaEm(13));
        }

        [Test]
        public void Recusa_DesfazOGolpe_EDevolveOStaminaPrevisto()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarStamina(100, 0);
            prontidao.MarcarAtaque(TipoDeAtaque.Pesado, 10);
            Assert.AreEqual(80, prontidao.Stamina);

            prontidao.DesfazerAtaque(10.1);

            Assert.AreEqual(100, prontidao.Stamina);
            Assert.IsFalse(prontidao.Comprometido(10.1));
            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Leve, 10.1));
        }

        // ── Buffer de entrada (#480) ─────────────────────────────────────────────

        [Test]
        public void Clique_A200MsDoFim_EntraNaFilaDoServidor()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);
            var fim = prontidao.LivreEm;

            var resultado = prontidao.ClicarAtaque(TipoDeAtaque.Leve, fim - 0.2);

            Assert.AreEqual(ResultadoDoClique.EnviarNaFila, resultado);
            Assert.AreEqual(TipoDeAtaque.Leve, prontidao.NaFila);
            Assert.IsFalse(prontidao.IniciarDaFila(fim - 0.1, out _), "só começa no fim");
            Assert.IsTrue(prontidao.IniciarDaFila(fim, out var tipo));
            Assert.AreEqual(TipoDeAtaque.Leve, tipo);
        }

        [Test]
        public void Clique_A500MsDoFim_FicaGuardadoESaiAntesDaFilaAbrirNoServidor()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);
            var fim = prontidao.LivreEm;
            var clique = fim - (RegrasAtaque.JanelaDoBufferSegundos - 0.05);

            Assert.AreEqual(
                ResultadoDoClique.Guardado,
                prontidao.ClicarAtaque(TipoDeAtaque.Pesado, clique)
            );
            Assert.IsFalse(prontidao.LiberarAtaqueGuardado(clique + 0.1, out _));

            var saida =
                fim
                - (RegrasAtaque.JanelaDaFilaSegundos - RegrasAtaque.MargemDoEnvioSegundos)
                + 0.001;
            Assert.IsTrue(prontidao.LiberarAtaqueGuardado(saida, out var tipo));
            Assert.AreEqual(TipoDeAtaque.Pesado, tipo);
            Assert.AreEqual(
                ResultadoDoClique.EnviarNaFila,
                prontidao.ClicarAtaque(tipo, saida),
                "e agora cabe na fila do servidor"
            );
        }

        [Test]
        public void Clique_MuitoCedo_OuComPedidoJaGuardado_Descarta()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);
            var fim = prontidao.LivreEm;

            Assert.AreEqual(
                ResultadoDoClique.Descartado,
                prontidao.ClicarAtaque(TipoDeAtaque.Leve, fim - 0.6)
            );

            prontidao.ClicarAtaque(TipoDeAtaque.Leve, fim - 0.45); // Guardado.
            Assert.AreEqual(
                ResultadoDoClique.Descartado,
                prontidao.ClicarAtaque(TipoDeAtaque.Leve, fim - 0.4),
                "um pedido só"
            );
        }

        [Test]
        public void Esquiva_PedidaNoFimDoPreparo_FicaGuardadaESaiQuandoPodeComecar()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);

            var clique = prontidao.AcertoEm - 0.2;
            Assert.AreEqual(ResultadoDoClique.Guardado, prontidao.ClicarEsquiva(clique));
            Assert.IsFalse(prontidao.LiberarEsquivaGuardada(clique + 0.1));
            Assert.IsTrue(prontidao.LiberarEsquivaGuardada(prontidao.AcertoEm));
        }
    }
}
