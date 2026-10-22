using JogoDoGrupo.Client.Core.Audio;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class RegrasEfeitosAudioTestes
    {
        [Test]
        public void Sons_de_outros_jogadores_so_tocam_ate_25_metros()
        {
            Assert.IsTrue(RegrasEfeitosAudio.PodeOuvirOutroJogador(0f));
            Assert.IsTrue(RegrasEfeitosAudio.PodeOuvirOutroJogador(12.5f));
            Assert.IsTrue(RegrasEfeitosAudio.PodeOuvirOutroJogador(25f));

            Assert.IsFalse(RegrasEfeitosAudio.PodeOuvirOutroJogador(25.1f));
            Assert.IsFalse(RegrasEfeitosAudio.PodeOuvirOutroJogador(30f));
            Assert.IsFalse(RegrasEfeitosAudio.PodeOuvirOutroJogador(-1f));
        }

        [Test]
        public void Limite_de_16_efeitos_simultaneos_aloca_todas_as_vagas()
        {
            var controle = new ControleDeVagasDeEfeitos(16);

            for (var i = 0; i < 16; i++)
            {
                var id = controle.TentarAlocar(i * 1.5f, out var desalocadoId);
                Assert.IsNotNull(id, $"Efeito {i} deveria ter sido alocado.");
                Assert.IsNull(desalocadoId, $"Efeito {i} não deveria ter desalocado outro.");
            }

            Assert.AreEqual(16, controle.AtivasCount);
        }

        [Test]
        public void Acima_do_limite_corta_o_mais_distante_quando_novo_e_mais_proximo()
        {
            var controle = new ControleDeVagasDeEfeitos(16);

            int? idMaisDistante = null;
            for (var i = 1; i <= 16; i++)
            {
                var id = controle.TentarAlocar(i * 2f, out _);
                if (i == 16)
                {
                    idMaisDistante = id;
                }
            }

            // O mais distante atual está a 32m. Um novo a 1m deve desalojar o de 32m.
            var novoId = controle.TentarAlocar(1f, out var desalocado);
            Assert.IsNotNull(novoId);
            Assert.AreEqual(idMaisDistante, desalocado);
            Assert.AreEqual(16, controle.AtivasCount);
        }

        [Test]
        public void Acima_do_limite_descarta_efeito_novo_se_for_mais_distante()
        {
            var controle = new ControleDeVagasDeEfeitos(16);

            for (var i = 1; i <= 16; i++)
            {
                controle.TentarAlocar(i * 2f, out _);
            }

            // O mais distante está a 32m. Um novo a 35m é descartado.
            var novoId = controle.TentarAlocar(35f, out var desalocado);
            Assert.IsNull(novoId);
            Assert.IsNull(desalocado);
            Assert.AreEqual(16, controle.AtivasCount);
        }

        [Test]
        public void Liberar_vaga_permite_nova_alocacao_sem_desalojar()
        {
            var controle = new ControleDeVagasDeEfeitos(16);

            int? primeiroId = null;
            for (var i = 0; i < 16; i++)
            {
                var id = controle.TentarAlocar(i, out _);
                if (i == 0)
                {
                    primeiroId = id;
                }
            }

            Assert.AreEqual(16, controle.AtivasCount);

            var liberou = controle.Liberar(primeiroId.Value);
            Assert.IsTrue(liberou);
            Assert.AreEqual(15, controle.AtivasCount);

            // Agora pode alocar até um efeito com distância grande sem desalojar
            var novoId = controle.TentarAlocar(50f, out var desalocado);
            Assert.IsNotNull(novoId);
            Assert.IsNull(desalocado);
            Assert.AreEqual(16, controle.AtivasCount);
        }
    }
}
