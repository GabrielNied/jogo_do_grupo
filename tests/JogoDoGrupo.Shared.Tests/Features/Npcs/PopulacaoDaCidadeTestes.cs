using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Npcs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Npcs
{
    public class PopulacaoDaCidadeTestes
    {
        [Fact]
        public void Rota_AnduraNaVelocidadeEPausaNosPontos()
        {
            var rota = new RotaDeNpc(
                new[] { new PontoDeRota(0f, 0f, 2f), new PontoDeRota(10f, 0f, 3f) },
                ModoDeRota.Ciclo,
                2f
            );

            // Ciclo: 2 s parado + 5 s de ida + 3 s parado + 5 s de volta.
            Assert.Equal(15f, rota.DuracaoDoCiclo, 3);

            var parado = rota.Amostrar(1.0);
            Assert.True(parado.Parado);
            Assert.Equal(0f, parado.X, 3);

            var meio = rota.Amostrar(2.0 + 2.5);
            Assert.False(meio.Parado);
            Assert.Equal(5f, meio.X, 3);
            Assert.Equal(90f, meio.RotacaoY, 3);

            var noFim = rota.Amostrar(2.0 + 5.0 + 1.0);
            Assert.True(noFim.Parado);
            Assert.Equal(10f, noFim.X, 3);

            var volta = rota.Amostrar(10.0 + 2.5);
            Assert.False(volta.Parado);
            Assert.Equal(5f, volta.X, 3);
            Assert.Equal(-90f, volta.RotacaoY, 3);
        }

        [Fact]
        public void Rota_VaiEVolta_RetornaPeloMesmoCaminho()
        {
            var rota = new RotaDeNpc(
                new[] { new PontoDeRota(0f, 0f), new PontoDeRota(4f, 0f), new PontoDeRota(4f, 4f) },
                ModoDeRota.VaiEVolta,
                1f
            );

            Assert.Equal(16f, rota.DuracaoDoCiclo, 3);
            // Na volta, passa de novo por (4, 0) antes de chegar a (0, 0).
            var tempoVolta = rota.Amostrar(8.0 + 2.0);
            Assert.Equal(4f, tempoVolta.X, 3);
            Assert.Equal(2f, tempoVolta.Z, 3);
            var depois = rota.Amostrar(12.0 + 2.0);
            Assert.Equal(2f, depois.X, 3);
            Assert.Equal(0f, depois.Z, 3);
        }

        [Fact]
        public void Rota_DeUmPonto_FicaParadaOlhandoOAlvo()
        {
            var rota = new RotaDeNpc(
                new[] { new PontoDeRota(1f, 1f, 0f, false, true, 1f, 5f) },
                ModoDeRota.Ciclo,
                1f
            );

            var a = rota.Amostrar(0);
            var b = rota.Amostrar(12345.6, 77f);
            Assert.True(a.Parado && b.Parado);
            Assert.Equal(1f, b.X, 3);
            Assert.Equal(0f, b.RotacaoY, 3);
        }

        [Fact]
        public void Rota_TempoNegativoOuFaseNaoQuebram()
        {
            var rota = PopulacaoDaCidade.Todos[2].Rota;
            var amostra = rota.Amostrar(-1000.5, -3f);
            Assert.False(float.IsNaN(amostra.X));
            Assert.False(float.IsNaN(amostra.Z));
        }

        [Fact]
        public void Populacao_TemIdsUnicosForaDoCatalogoEFalasCurtas()
        {
            var ids = new HashSet<int>();
            foreach (var habitante in PopulacaoDaCidade.Todos)
            {
                Assert.True(ids.Add(habitante.Id), "id repetido " + habitante.Id);
                Assert.True(habitante.Id >= PopulacaoDaCidade.PrimeiroId);
                Assert.Null(CatalogoNpcs.Obter(habitante.Id));
                Assert.False(string.IsNullOrWhiteSpace(habitante.Nome));
                Assert.NotEmpty(habitante.Falas);
                foreach (var fala in habitante.Falas)
                {
                    Assert.InRange(fala.Length, 1, ConfigPopulacaoDaCidade.TamanhoMaximoDaFala);
                }
            }
        }

        [Fact]
        public void Populacao_TemGuardasEMoradores_ComGuardasParadosNosPortoes()
        {
            var guardas = 0;
            var moradores = 0;
            var parados = 0;
            foreach (var habitante in PopulacaoDaCidade.Todos)
            {
                if (habitante.Papel == PapelNpc.Guarda)
                {
                    guardas++;
                    if (habitante.Rota.DuracaoDoCiclo <= 0f)
                    {
                        parados++;
                    }
                }
                else
                {
                    Assert.Equal(PapelNpc.Morador, habitante.Papel);
                    moradores++;
                    Assert.True(habitante.Rota.DuracaoDoCiclo > 0f, "morador deve andar");
                }
            }

            Assert.True(guardas >= 4);
            Assert.True(moradores >= 4);
            Assert.True(parados >= 2);
        }

        [Fact]
        public void Populacao_RotasFicamDentroDaCidadeMurada()
        {
            foreach (var habitante in PopulacaoDaCidade.Todos)
            {
                for (var i = 0; i < habitante.Rota.QuantidadeDePontos; i++)
                {
                    var ponto = habitante.Rota.Ponto(i);
                    Assert.InRange(ponto.X, -34f, 34f);
                    Assert.InRange(ponto.Z, -34f, 34f);
                    // Ninguém atravessa a fonte da praça (0, 0).
                    Assert.True(Math.Abs(ponto.X) > 1f || Math.Abs(ponto.Z) > 1f);
                }
            }
        }

        [Fact]
        public void Populacao_ParesConversamJuntosEPertoUmDoOutro()
        {
            foreach (var a in PopulacaoDaCidade.Todos)
            {
                if (a.ParceiroId == 0)
                {
                    continue;
                }

                var b = PopulacaoDaCidade.Obter(a.ParceiroId);
                Assert.NotNull(b);
                Assert.Equal(a.Id, b.ParceiroId);
                Assert.NotEqual(a.Lado, b.Lado);
                Assert.Equal(a.Rota.DuracaoDoCiclo, b.Rota.DuracaoDoCiclo, 3);

                var emConversa = 0;
                for (var t = 0.0; t < a.Rota.DuracaoDoCiclo * 2; t += 0.5)
                {
                    var amostraA = a.Rota.Amostrar(t, a.Fase);
                    var amostraB = b.Rota.Amostrar(t, b.Fase);
                    Assert.Equal(amostraA.Conversando, amostraB.Conversando);
                    if (!amostraA.Conversando)
                    {
                        continue;
                    }

                    emConversa++;
                    var dx = amostraA.X - amostraB.X;
                    var dz = amostraA.Z - amostraB.Z;
                    Assert.True(dx * dx + dz * dz <= 3f * 3f, "o par conversa a mais de 3 m");
                }

                Assert.True(emConversa > 0);
            }
        }
    }
}
