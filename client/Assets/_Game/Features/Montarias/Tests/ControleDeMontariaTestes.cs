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
        public void EquiparMontaria_AlteraMontariaEquipada()
        {
            var objeto = new GameObject("ControleTeste");
            try
            {
                var controle = objeto.AddComponent<ControleDeMontaria>();
                var equipadaNotificada = 0;
                controle.MontariaEquipadaMudou += tipo => equipadaNotificada = tipo;

                Assert.AreEqual(RegrasMontaria.TipoPadrao, controle.MontariaEquipada);

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
    }
}
