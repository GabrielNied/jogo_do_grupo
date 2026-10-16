using JogoDoGrupo.Shared.Features.Mobs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs
{
    public class RegrasAmeacaTestes
    {
        [Fact]
        public void Calculo_de_dano_gera_um_de_ameaca_por_ponto_e_triplica_com_tank()
        {
            Assert.Equal(100.0, RegrasAmeaca.CalcularAmeacaDeDano(100, eTank: false));
            Assert.Equal(300.0, RegrasAmeaca.CalcularAmeacaDeDano(100, eTank: true));
            Assert.Equal(0.0, RegrasAmeaca.CalcularAmeacaDeDano(0));
            Assert.Equal(0.0, RegrasAmeaca.CalcularAmeacaDeDano(-10));
        }

        [Fact]
        public void Calculo_de_cura_gera_meio_por_ponto_dividido_entre_inimigos()
        {
            Assert.Equal(50.0, RegrasAmeaca.CalcularAmeacaDeCura(100, inimigosEmCombate: 1));
            Assert.Equal(25.0, RegrasAmeaca.CalcularAmeacaDeCura(100, inimigosEmCombate: 2));
            Assert.Equal(
                75.0,
                RegrasAmeaca.CalcularAmeacaDeCura(100, inimigosEmCombate: 2, eTank: true)
            );
            Assert.Equal(0.0, RegrasAmeaca.CalcularAmeacaDeCura(0));
        }

        [Fact]
        public void Supera_margem_respeita_110_por_cento_perto_e_130_por_cento_longe()
        {
            // Perto (110%)
            Assert.False(RegrasAmeaca.SuperaMargem(100.0, 110.0, estaNoAlcance: true));
            Assert.True(RegrasAmeaca.SuperaMargem(100.0, 110.1, estaNoAlcance: true));

            // Longe (130%)
            Assert.False(RegrasAmeaca.SuperaMargem(100.0, 130.0, estaNoAlcance: false));
            Assert.True(RegrasAmeaca.SuperaMargem(100.0, 130.1, estaNoAlcance: false));
        }

        [Fact]
        public void Sem_ameaca_positiva_escolhe_o_mais_proximo()
        {
            long[] candidatos = [10, 20];
            double[] distancias = [5.0, 2.0];
            double[] ameacas = [0.0, 0.0];

            long escolhido = RegrasAmeaca.EscolherAlvo(
                alvoAtual: RegrasInimigo.SemAlvo,
                candidatos: candidatos,
                distanciasMetros: distancias,
                ameacas: ameacas,
                alcanceDeAtaqueMetros: 2.0
            );

            Assert.Equal(20, escolhido);
        }

        [Fact]
        public void Quem_bate_mais_vira_o_alvo_so_depois_de_passar_a_margem()
        {
            long[] candidatos = [1, 2];
            double[] distancias = [1.5, 1.5]; // ambos perto
            double alcance = 2.0;

            // Alvo 1 tem 100 de ameaça. Candidato 2 tem 105 (não passa dos 110)
            double[] ameacas1 = [100.0, 105.0];
            long escolhido1 = RegrasAmeaca.EscolherAlvo(
                1,
                candidatos,
                distancias,
                ameacas1,
                alcance
            );
            Assert.Equal(1, escolhido1);

            // Candidato 2 passa para 111 (passa de 110%)
            double[] ameacas2 = [100.0, 111.0];
            long escolhido2 = RegrasAmeaca.EscolherAlvo(
                1,
                candidatos,
                distancias,
                ameacas2,
                alcance
            );
            Assert.Equal(2, escolhido2);
        }

        [Fact]
        public void Margem_longe_exige_130_por_cento()
        {
            long[] candidatos = [1, 2];
            double[] distancias = [1.5, 5.0]; // 1 perto, 2 longe
            double alcance = 2.0;

            // 2 tem 125 de ameaça (passaria de 110%, mas está longe, precisa de 130%)
            double[] ameacas1 = [100.0, 125.0];
            long escolhido1 = RegrasAmeaca.EscolherAlvo(
                1,
                candidatos,
                distancias,
                ameacas1,
                alcance
            );
            Assert.Equal(1, escolhido1);

            // 2 sobe para 131 de ameaça
            double[] ameacas2 = [100.0, 131.0];
            long escolhido2 = RegrasAmeaca.EscolherAlvo(
                1,
                candidatos,
                distancias,
                ameacas2,
                alcance
            );
            Assert.Equal(2, escolhido2);
        }

        [Fact]
        public void Empate_de_ameaca_nao_troca_o_alvo_atual()
        {
            long[] candidatos = [1, 2];
            double[] distancias = [1.5, 1.5];
            double[] ameacas = [100.0, 100.0];

            long escolhido = RegrasAmeaca.EscolherAlvo(1, candidatos, distancias, ameacas, 2.0);
            Assert.Equal(1, escolhido);
        }

        [Fact]
        public void Tank_com_triplo_de_ameaca_segura_alvo_contra_dps_com_o_dobro_de_dano()
        {
            var tabela = new TabelaDeAmeaca();
            long dps = 1;
            long tank = 2;

            // DPS causa 100 de dano
            tabela.AdicionarDano(dps, 100, eTank: false);
            // Tank causa 50 de dano (metade do DPS, ou DPS dá o dobro do Tank)
            tabela.AdicionarDano(tank, 50, eTank: true);

            Assert.Equal(100.0, tabela.ObterAmeaca(dps));
            Assert.Equal(150.0, tabela.ObterAmeaca(tank));

            long[] candidatos = [dps, tank];
            double[] distancias = [1.5, 1.5];
            long escolhido = tabela.EscolherAlvo(
                RegrasInimigo.SemAlvo,
                candidatos,
                distancias,
                2.0,
                0
            );

            Assert.Equal(tank, escolhido);
        }

        [Fact]
        public void Provocar_vira_o_alvo_na_hora_iguala_maior_ameaca_e_segura_por_4_segundos()
        {
            var tabela = new TabelaDeAmeaca();
            long dps = 1;
            long tank = 2;

            tabela.AdicionarDano(dps, 200, eTank: false);
            tabela.AdicionarDano(tank, 20, eTank: true);

            long[] candidatos = [dps, tank];
            double[] distancias = [1.5, 1.5];

            // Antes de provocar, o alvo é o DPS
            long antes = tabela.EscolherAlvo(
                RegrasInimigo.SemAlvo,
                candidatos,
                distancias,
                2.0,
                1000
            );
            Assert.Equal(dps, antes);

            // Tank provoca aos 1000 ms
            tabela.Provocar(tank, agoraMs: 1000);

            // Ameaça do tank igualou ao topo (200)
            Assert.Equal(200.0, tabela.ObterAmeaca(tank));

            // Vira o alvo na hora
            long durante = tabela.EscolherAlvo(dps, candidatos, distancias, 2.0, 1000);
            Assert.Equal(tank, durante);

            // Aos 3000 ms, mesmo que DPS cause mais 100 de dano (total 300 ameaça), tank segue como alvo forçado
            tabela.AdicionarDano(dps, 100, eTank: false);
            long aindaProvocado = tabela.EscolherAlvo(tank, candidatos, distancias, 2.0, 3000);
            Assert.Equal(tank, aindaProvocado);

            // Aos 5001 ms (após os 4 s = 4000 ms), a provocação expirou.
            // DPS tem 300 ameaça vs tank 200 ameaça (300 > 200 * 1.10 = 220), então DPS recupera o agro.
            long expirado = tabela.EscolherAlvo(tank, candidatos, distancias, 2.0, 5001);
            Assert.Equal(dps, expirado);
        }

        [Fact]
        public void Limpar_e_remover_resetam_pontos_e_provocacao()
        {
            var tabela = new TabelaDeAmeaca();
            tabela.AdicionarDano(1, 100);
            tabela.Provocar(1, 1000);

            Assert.True(tabela.TemAmeaca);
            Assert.True(tabela.EstaProvocado(2000, out _));

            tabela.Remover(1);
            Assert.Equal(0.0, tabela.ObterAmeaca(1));
            Assert.False(tabela.EstaProvocado(2000, out _));

            tabela.AdicionarDano(2, 50);
            tabela.Limpar();
            Assert.False(tabela.TemAmeaca);
            Assert.Equal(0.0, tabela.ObterAmeaca(2));
        }

        [Fact]
        public void RemoverOnde_remove_apenas_quem_satisfaz_condicao_e_limpa_provocador()
        {
            var tabela = new TabelaDeAmeaca();
            tabela.AdicionarDano(10, 100);
            tabela.AdicionarDano(20, 200);
            tabela.AdicionarDano(30, 300);
            tabela.Provocar(20, 1000);

            // Remove quem tem id <= 20
            tabela.RemoverOnde(id => id <= 20);

            Assert.True(tabela.TemAmeaca);
            Assert.Equal(0.0, tabela.ObterAmeaca(10));
            Assert.Equal(0.0, tabela.ObterAmeaca(20));
            Assert.Equal(300.0, tabela.ObterAmeaca(30));
            // Provocador (20) foi removido
            Assert.False(tabela.EstaProvocado(2000, out _));

            // Teste da sobrecarga com contexto
            var limite = 25;
            tabela.RemoverOnde((id, lim) => id > lim, limite);
            Assert.False(tabela.TemAmeaca);
            Assert.Equal(0.0, tabela.ObterAmeaca(30));
        }
    }
}
