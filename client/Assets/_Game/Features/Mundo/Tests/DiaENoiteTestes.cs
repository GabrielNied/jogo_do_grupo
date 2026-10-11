using JogoDoGrupo.Client.Features.Mundo.DiaENoite;
using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Tests
{
    public class DiaENoiteTestes
    {
        private static HoraDoMundo Hora(float hora, float duracao = 1200f) =>
            new HoraDoMundo { Hora = hora, DuracaoDoDiaSegundos = duracao };

        // ----- Relógio -----

        [Test]
        public void Sem_hora_do_servidor_o_relogio_nao_inventa_nem_anda()
        {
            var relogio = new RelogioDoMundoNoClient();
            relogio.Avancar(300f);

            Assert.IsFalse(relogio.TemHora);
            Assert.AreEqual(RegrasDiaENoite.HoraInicial, relogio.Hora, 1e-4f);
        }

        [Test]
        public void A_mesma_hora_do_servidor_da_a_mesma_hora_em_dois_clientes()
        {
            var a = new RelogioDoMundoNoClient();
            var b = new RelogioDoMundoNoClient();
            a.Receber(Hora(9.5f));
            b.Receber(Hora(9.5f));
            a.Avancar(30f);
            b.Avancar(10f);
            b.Avancar(20f);

            Assert.AreEqual(a.Hora, b.Hora, 1e-4f);
        }

        [Test]
        public void Entre_mensagens_a_hora_anda_no_ritmo_do_servidor()
        {
            var relogio = new RelogioDoMundoNoClient();
            relogio.Receber(Hora(8f, 1200f));

            relogio.Avancar(50f); // uma hora do mundo num dia de 20 min
            Assert.AreEqual(9f, relogio.Hora, 1e-3f);
        }

        [Test]
        public void Um_dia_inteiro_volta_a_mesma_hora_e_a_meia_noite_da_a_volta()
        {
            var relogio = new RelogioDoMundoNoClient();
            relogio.Receber(Hora(23f, 240f)); // uma hora = 10 s

            relogio.Avancar(20f);
            Assert.AreEqual(1f, relogio.Hora, 1e-3f);

            relogio.Avancar(240f);
            Assert.AreEqual(1f, relogio.Hora, 1e-3f);
        }

        [Test]
        public void Nova_mensagem_acerta_a_hora_e_tempo_invalido_nao_anda()
        {
            var relogio = new RelogioDoMundoNoClient();
            relogio.Receber(Hora(10f));
            relogio.Avancar(500f);
            relogio.Receber(Hora(10f));
            Assert.AreEqual(10f, relogio.Hora, 1e-4f);

            relogio.Avancar(-5f);
            relogio.Avancar(float.NaN);
            relogio.Avancar(float.PositiveInfinity);
            Assert.AreEqual(10f, relogio.Hora, 1e-4f);

            relogio.Receber(null);
            Assert.AreEqual(10f, relogio.Hora, 1e-4f);
        }

        [Test]
        public void Duracao_invalida_do_servidor_usa_a_padrao()
        {
            var relogio = new RelogioDoMundoNoClient();
            relogio.Receber(Hora(8f, 0f));
            relogio.Avancar(RegrasDiaENoite.DuracaoDoDiaSegundos / 24f);

            Assert.AreEqual(9f, relogio.Hora, 1e-3f);
        }

        // ----- Escuridão e lanternas -----

        [Test]
        public void Dia_claro_noite_fechada_e_troca_suave_no_crepusculo()
        {
            Assert.AreEqual(0f, CicloVisual.Escuridao(12f));
            Assert.AreEqual(0f, CicloVisual.Escuridao(7f));
            Assert.AreEqual(1f, CicloVisual.Escuridao(0f));
            Assert.AreEqual(1f, CicloVisual.Escuridao(22f));
            Assert.AreEqual(1f, CicloVisual.Escuridao(3f));

            Assert.AreEqual(0.5f, CicloVisual.Escuridao(RegrasDiaENoite.InicioDaNoite), 1e-3f);
            Assert.AreEqual(0.5f, CicloVisual.Escuridao(RegrasDiaENoite.FimDaNoite), 1e-3f);

            // Anoitecer só escurece; amanhecer só clareia.
            var anterior = 0f;
            for (var h = 18.5f; h <= 21.5f; h += 0.25f)
            {
                var agora = CicloVisual.Escuridao(h);
                Assert.GreaterOrEqual(agora, anterior, $"Clareou às {h}h.");
                anterior = agora;
            }

            for (var h = 3.5f; h <= 6.5f; h += 0.25f)
            {
                var agora = CicloVisual.Escuridao(h);
                Assert.LessOrEqual(agora, anterior, $"Escureceu às {h}h.");
                anterior = agora;
            }
        }

        [Test]
        public void Lanterna_acesa_so_na_noite()
        {
            Assert.IsFalse(LuzDaLanterna.Acesa(12f));
            Assert.IsFalse(LuzDaLanterna.Acesa(19.9f));
            Assert.IsTrue(LuzDaLanterna.Acesa(20f));
            Assert.IsTrue(LuzDaLanterna.Acesa(0f));
            Assert.IsTrue(LuzDaLanterna.Acesa(4.9f));
            Assert.IsFalse(LuzDaLanterna.Acesa(5f));
        }

        [Test]
        public void Crepusculo_escurece_ceu_e_nevoa_aos_poucos()
        {
            var dia = AmbienteDosBiomas.ComHora(AmbienteDosBiomas.Padrao, 12f);
            var crepusculo = AmbienteDosBiomas.ComHora(AmbienteDosBiomas.Padrao, 19.6f);
            var noite = AmbienteDosBiomas.ComHora(AmbienteDosBiomas.Padrao, 23f);

            Assert.Less(crepusculo.Ceu.grayscale, dia.Ceu.grayscale);
            Assert.Greater(crepusculo.Ceu.grayscale, noite.Ceu.grayscale);
            Assert.Less(noite.Nevoa.grayscale, dia.Nevoa.grayscale);
            Assert.Less(noite.ForcaDoSol, crepusculo.ForcaDoSol);
            Assert.Greater(noite.ForcaDoSol, 0f, "A lua ainda ilumina um pouco.");
        }

        // ----- Caminho da luz -----

        [Test]
        public void Ao_meio_do_dia_o_sol_fica_no_angulo_calibrado()
        {
            var meioDoDia = (RegrasDiaENoite.FimDaNoite + RegrasDiaENoite.InicioDaNoite) / 2f;

            Assert.AreEqual(
                ConfigIluminacao.AlturaDoSol,
                CicloVisual.AlturaDaLuz(meioDoDia),
                1e-3f
            );
            Assert.AreEqual(
                Mathf.Repeat(ConfigIluminacao.GiroDoSol, 360f),
                CicloVisual.GiroDaLuz(meioDoDia),
                1e-3f
            );
        }

        [Test]
        public void A_luz_nunca_desce_abaixo_do_minimo_e_o_giro_nao_pula()
        {
            var anterior = CicloVisual.GiroDaLuz(0f);
            for (var h = 0.05f; h < 24f; h += 0.05f)
            {
                var altura = CicloVisual.AlturaDaLuz(h);
                Assert.GreaterOrEqual(altura, ConfigDiaENoite.AlturaMinima - 1e-3f, $"{h}h");
                Assert.LessOrEqual(altura, 90f, $"{h}h");

                var giro = CicloVisual.GiroDaLuz(h);
                Assert.Less(
                    Mathf.Abs(Mathf.DeltaAngle(anterior, giro)),
                    5f,
                    $"Giro pulou às {h}h."
                );
                anterior = giro;
            }
        }

        [Test]
        public void Girar_luzes_poe_o_preenchimento_do_lado_oposto()
        {
            var sol = new GameObject("Sol").AddComponent<Light>();
            var preenchimento = new GameObject("Preenchimento").AddComponent<Light>();
            try
            {
                foreach (var hora in new[] { 7f, 12.5f, 18f, 23f, 2f })
                {
                    AmbienteDosBiomas.GirarLuzes(hora, sol, preenchimento);
                    var deSol = Vector3.ProjectOnPlane(sol.transform.forward, Vector3.up);
                    var dePreenchimento = Vector3.ProjectOnPlane(
                        preenchimento.transform.forward,
                        Vector3.up
                    );
                    Assert.Less(
                        Vector3.Dot(deSol.normalized, dePreenchimento.normalized),
                        -0.99f,
                        $"{hora}h"
                    );
                    Assert.Less(
                        sol.transform.forward.y,
                        0f,
                        $"A luz aponta para baixo às {hora}h."
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(sol.gameObject);
                Object.DestroyImmediate(preenchimento.gameObject);
            }
        }
    }
}
