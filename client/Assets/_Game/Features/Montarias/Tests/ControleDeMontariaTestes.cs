using JogoDoGrupo.Shared.Features.Montarias;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Montarias.Tests
{
    public class ControleDeMontariaTestes
    {
        [Test]
        public void EstadoInicial_NaoEstaMontado()
        {
            var objeto = new GameObject("ControleTeste");
            try
            {
                var controle = objeto.AddComponent<ControleDeMontaria>();
                Assert.IsFalse(controle.MontadoLocal);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void AoReceberEstadoMontaria_AtualizaEstadoLocal()
        {
            var objeto = new GameObject("ControleTeste");
            try
            {
                var controle = objeto.AddComponent<ControleDeMontaria>();
                var mudou = false;
                controle.EstadoMudou += m => mudou = true;

                controle.AoReceberEstadoMontaria(
                    new EstadoMontaria
                    {
                        PersonagemId = 0, // EntradaNoJogo.PersonagemId padrão quando offline
                        Montado = true,
                        TipoMontaria = 1,
                    }
                );

                Assert.IsTrue(controle.MontadoLocal);
                Assert.IsTrue(mudou);

                controle.AoReceberEstadoMontaria(
                    new EstadoMontaria
                    {
                        PersonagemId = 0,
                        Montado = false,
                        TipoMontaria = 0,
                    }
                );

                Assert.IsFalse(controle.MontadoLocal);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void Colecao_PossuiMontariaPadraoInicialmente()
        {
            var objeto = new GameObject("ControleTeste");
            try
            {
                var controle = objeto.AddComponent<ControleDeMontaria>();
                Assert.IsTrue(controle.PossuiMontaria(RegrasMontaria.TipoCastanho));
                Assert.IsFalse(controle.PossuiMontaria(RegrasMontaria.TipoNoturno));
                CollectionAssert.Contains(
                    controle.ColecaoDesbloqueada,
                    RegrasMontaria.TipoCastanho
                );
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void EquiparMontaria_BloqueadaNaoEquipa_DesbloqueadaAlteraEquipada()
        {
            var objeto = new GameObject("ControleTeste");
            try
            {
                var controle = objeto.AddComponent<ControleDeMontaria>();
                var equipadaNotificada = 0;
                controle.MontariaEquipadaMudou += tipo => equipadaNotificada = tipo;

                Assert.AreEqual(RegrasMontaria.TipoPadrao, controle.MontariaEquipada);

                // Tentar equipar bloqueada não deve mudar nada
                controle.EquiparMontaria(RegrasMontaria.TipoNoturno);
                Assert.AreEqual(RegrasMontaria.TipoPadrao, controle.MontariaEquipada);
                Assert.AreEqual(0, equipadaNotificada);

                // Desbloqueia na coleção
                var colecaoNotificada = false;
                controle.ColecaoMudou += () => colecaoNotificada = true;
                controle.AoReceberMontariaDesbloqueada(
                    new MontariaDesbloqueada
                    {
                        TipoMontaria = RegrasMontaria.TipoNoturno,
                        Nome = "Corcel Noturno",
                    }
                );

                Assert.IsTrue(controle.PossuiMontaria(RegrasMontaria.TipoNoturno));
                Assert.IsTrue(colecaoNotificada);

                // Agora equipar deve funcionar
                controle.EquiparMontaria(RegrasMontaria.TipoNoturno);
                Assert.AreEqual(RegrasMontaria.TipoNoturno, controle.MontariaEquipada);
                Assert.AreEqual(RegrasMontaria.TipoNoturno, equipadaNotificada);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void AoReceberColecaoMontarias_AtualizaColecaoENotifica()
        {
            var objeto = new GameObject("ControleTeste");
            try
            {
                var controle = objeto.AddComponent<ControleDeMontaria>();
                var mudou = false;
                controle.ColecaoMudou += () => mudou = true;

                controle.AoReceberColecaoMontarias(
                    new ColecaoMontarias
                    {
                        MontariasDesbloqueadas = new[]
                        {
                            RegrasMontaria.TipoCastanho,
                            RegrasMontaria.TipoNoturno,
                        },
                    }
                );

                Assert.IsTrue(mudou);
                Assert.IsTrue(controle.PossuiMontaria(RegrasMontaria.TipoNoturno));
                CollectionAssert.Contains(controle.ColecaoDesbloqueada, RegrasMontaria.TipoNoturno);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void Invocacao_IniciarECancelar_AtualizaEstadoCorretamente()
        {
            var objeto = new GameObject("ControleTeste");
            try
            {
                var controle = objeto.AddComponent<ControleDeMontaria>();
                var invocandoNotificado = false;
                controle.InvocacaoProgresso += (invocando, _, _) => invocandoNotificado = invocando;

                Assert.IsFalse(controle.Invocando);

                controle.IniciarInvocacao();
                Assert.IsTrue(controle.Invocando);
                Assert.IsTrue(invocandoNotificado);

                controle.CancelarInvocacao();
                Assert.IsFalse(controle.Invocando);
                Assert.IsFalse(invocandoNotificado);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void DeveProcessarAtalhoMontaria_QuandoDigitandoTextoOuMenuAberto_IgnoraAtalho()
        {
            // Tecla apertada, sem menu e sem digitação -> processa
            Assert.IsTrue(ControleDeMontaria.DeveProcessarAtalhoMontaria(true, false, false));

            // Tecla não apertada -> não processa
            Assert.IsFalse(ControleDeMontaria.DeveProcessarAtalhoMontaria(false, false, false));

            // Menu do Esc aberto -> não processa
            Assert.IsFalse(ControleDeMontaria.DeveProcessarAtalhoMontaria(true, true, false));

            // Digitando em campo de texto (chat, reporte, busca) -> não processa
            Assert.IsFalse(ControleDeMontaria.DeveProcessarAtalhoMontaria(true, false, true));

            // Menu aberto E digitando -> não processa
            Assert.IsFalse(ControleDeMontaria.DeveProcessarAtalhoMontaria(true, true, true));
        }
    }
}
