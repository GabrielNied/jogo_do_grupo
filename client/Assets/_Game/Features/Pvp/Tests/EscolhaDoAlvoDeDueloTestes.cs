using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Pvp.Tests
{
    public sealed class EscolhaDoAlvoDeDueloTestes
    {
        private const float Alcance = 15f;

        [Test]
        public void Escolhe_o_mais_perto_dentro_do_alcance()
        {
            var candidatos = new[]
            {
                new CandidatoDeDuelo(2, "Longe", 12f, 0f),
                new CandidatoDeDuelo(3, "Perto", 3f, 4f),
            };

            var achou = EscolhaDoAlvoDeDuelo.TentarEscolher(
                0f,
                0f,
                1,
                candidatos,
                Alcance,
                out var escolhido
            );

            Assert.IsTrue(achou);
            Assert.AreEqual(3, escolhido.PersonagemId);
        }

        [Test]
        public void Ignora_quem_esta_alem_do_alcance_e_o_proprio_jogador()
        {
            var candidatos = new[]
            {
                new CandidatoDeDuelo(1, "Eu", 0f, 0f),
                new CandidatoDeDuelo(2, "Fora", 15.1f, 0f),
            };

            Assert.IsFalse(
                EscolhaDoAlvoDeDuelo.TentarEscolher(0f, 0f, 1, candidatos, Alcance, out _)
            );
        }

        [Test]
        public void No_limite_exato_do_alcance_ainda_vale()
        {
            var candidatos = new[] { new CandidatoDeDuelo(2, "Limite", 15f, 0f) };

            Assert.IsTrue(
                EscolhaDoAlvoDeDuelo.TentarEscolher(0f, 0f, 1, candidatos, Alcance, out _)
            );
        }

        [Test]
        public void Empate_fica_com_o_menor_id()
        {
            var candidatos = new[]
            {
                new CandidatoDeDuelo(9, "B", 5f, 0f),
                new CandidatoDeDuelo(4, "A", 0f, 5f),
            };

            EscolhaDoAlvoDeDuelo.TentarEscolher(0f, 0f, 1, candidatos, Alcance, out var escolhido);

            Assert.AreEqual(4, escolhido.PersonagemId);
        }

        [Test]
        public void Sem_candidatos_ou_sem_fonte_nao_ha_alvo()
        {
            Assert.IsFalse(EscolhaDoAlvoDeDuelo.TentarEscolher(0f, 0f, 1, null, Alcance, out _));
            FonteDeAlvosDeDuelo.Candidatos = null;
            Assert.AreEqual(0, FonteDeAlvosDeDuelo.Listar().Count);
        }
    }
}
