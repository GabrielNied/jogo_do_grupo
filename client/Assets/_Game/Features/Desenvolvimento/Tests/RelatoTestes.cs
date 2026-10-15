using System.Collections.Generic;
using JogoDoGrupo.Client.Features.Desenvolvimento.Relato;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Tests
{
    public class RelatoTestes
    {
        [Test]
        public void EspalhadosIncluiPrimeiroEUltimo()
        {
            CollectionAssert.AreEqual(new[] { 0, 3, 6, 9 }, EscolhaDeQuadros.Espalhados(10, 4));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, EscolhaDeQuadros.Espalhados(3, 12));
            CollectionAssert.IsEmpty(EscolhaDeQuadros.Espalhados(0, 12));
        }

        [Test]
        public void EmVoltaDeixaQuadrosAntesEDeslizaNasPontas()
        {
            CollectionAssert.AreEqual(
                new[] { 47, 48, 49, 50, 51 },
                EscolhaDeQuadros.EmVolta(100, 50, 5, 3)
            );
            CollectionAssert.AreEqual(
                new[] { 0, 1, 2, 3, 4 },
                EscolhaDeQuadros.EmVolta(100, 1, 5, 3)
            );
            CollectionAssert.AreEqual(
                new[] { 95, 96, 97, 98, 99 },
                EscolhaDeQuadros.EmVolta(100, 99, 5, 3)
            );
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, EscolhaDeQuadros.EmVolta(3, 2, 5, 3));
        }

        [Test]
        public void CentroDoImpactoEhOQuadroDoUltimoAcerto()
        {
            var quadros = Quadros(10);
            var eventos = new List<EventoDeRede>
            {
                new EventoDeRede(1.02, 6002, "AtaqueAcertou", "{}"),
                new EventoDeRede(1.055, 6002, "AtaqueAcertou", "{}"),
                new EventoDeRede(1.08, 4000, "Outra", "{}"),
            };

            Assert.AreEqual(
                5,
                EscolhaDeQuadros.CentroDoImpacto(quadros, eventos, new[] { (ushort)6002 })
            );
            Assert.AreEqual(
                9,
                EscolhaDeQuadros.CentroDoImpacto(quadros, eventos, new[] { (ushort)1 })
            );
        }

        [Test]
        public void LinhaDoTempoMostraSoAsMudancasEmOrdem()
        {
            var quadros = Quadros(6);
            quadros[1].Entrada = "↓Mouse 1";
            for (var i = 3; i < quadros.Count; i++)
            {
                quadros[i].TimeScale = 0.05f;
                quadros[i].Jogador.Velocidade = 0f;
                quadros[i].Jogador.Clipe = i >= 4 ? "Golpe" : "Parado";
            }

            quadros[4].Solavanco = 0.02f;
            var eventos = new List<EventoDeRede>
            {
                new EventoDeRede(1.025, 6002, "AtaqueAcertou", "{\"Dano\":7}"),
            };

            var linhas = LinhaDoTempo.Montar(quadros, eventos).TrimEnd('\n').Split('\n');

            StringAssert.Contains("início", linhas[0]);
            StringAssert.Contains("q001  entrada  ↓Mouse 1", linhas[1]);
            StringAssert.Contains("q002  rede     AtaqueAcertou {\"Dano\":7}", linhas[2]);
            StringAssert.Contains("timeScale 1 → 0.05", linhas[3]);
            StringAssert.Contains("jogador  speed 1 → 0", linhas[4]);
            StringAssert.Contains("jogador  clipe Parado → Golpe", linhas[5]);
            StringAssert.Contains("câmera   tremor começou (2 cm)", linhas[6]);
            StringAssert.Contains("    50 ms  q005  câmera   tremor parou (pico 2 cm)", linhas[7]);
            Assert.AreEqual(8, linhas.Length);
        }

        [Test]
        public void MensagemFrequenteViraTotal()
        {
            var quadros = Quadros(3);
            var eventos = new List<EventoDeRede>();
            for (var i = 0; i <= ConfigRelato.MensagensFrequentes; i++)
            {
                eventos.Add(new EventoDeRede(1.0, 1001, "PosicaoJogador", "{}"));
            }

            var texto = LinhaDoTempo.Montar(quadros, eventos);

            StringAssert.DoesNotContain("rede", texto);
            StringAssert.Contains(
                $"frequentes (omitidas): PosicaoJogador ×{ConfigRelato.MensagensFrequentes + 1}",
                texto
            );
        }

        // Quadros a cada 10 ms a partir de 1 s, jogador parado com speed 1.
        private static List<AmostraDeQuadro> Quadros(int quantos)
        {
            var quadros = new List<AmostraDeQuadro>();
            for (var i = 0; i < quantos; i++)
            {
                quadros.Add(
                    new AmostraDeQuadro
                    {
                        Frame = 100 + i,
                        Tempo = 1.0 + i * 0.01,
                        TimeScale = 1f,
                        Jogador = new EstadoDoAnimator
                        {
                            Existe = true,
                            Objeto = "Jogador",
                            Clipe = "Parado",
                            Velocidade = 1f,
                        },
                    }
                );
            }

            return quadros;
        }
    }
}
