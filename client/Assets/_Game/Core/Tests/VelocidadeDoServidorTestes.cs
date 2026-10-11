using JogoDoGrupo.Client.Core.Jogo.Controle;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Tests
{
    /// <summary>
    /// Conectado, o Inspector só vira pedido quando difere do que o servidor deu (#263).
    /// </summary>
    public class VelocidadeDoServidorTestes
    {
        [Test]
        public void Igual_ao_servidor_nao_e_pedido()
        {
            Assert.IsFalse(ControleTerceiraPessoa.PedidoDiferente(new Vector2(2f, 5.5f), 2f, 5.5f));
        }

        [Test]
        public void Correr_ou_andar_diferente_e_pedido()
        {
            Assert.IsTrue(ControleTerceiraPessoa.PedidoDiferente(new Vector2(2f, 5.5f), 2f, 10f));
            Assert.IsTrue(ControleTerceiraPessoa.PedidoDiferente(new Vector2(2f, 5.5f), 3f, 5.5f));
        }

        [Test]
        public void Aplicar_do_servidor_vale_no_movimento_e_offline_volta_ao_inspector()
        {
            var objeto = new GameObject("jogador-teste");
            try
            {
                var controle = objeto.AddComponent<ControleTerceiraPessoa>();

                controle.AplicarVelocidadesDoServidor(3f, 12f);

                Assert.IsTrue(controle.VelocidadesDoServidor);
                Assert.AreEqual(12f, controle.Movimento.VelocidadeCorrer);
                Assert.AreEqual(3f, controle.Movimento.VelocidadeAndar);

                controle.SoltarVelocidadesDoServidor();

                Assert.IsFalse(controle.VelocidadesDoServidor);
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }
    }
}
