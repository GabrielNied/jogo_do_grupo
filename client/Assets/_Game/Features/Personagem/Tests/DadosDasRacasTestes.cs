using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Personagem;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Personagem.Tests
{
    /// <summary>A tela de criação lista todas as raças do enum, cada uma com seus textos.</summary>
    public class DadosDasRacasTestes
    {
        [Test]
        public void TodaRacaDoEnumTemDadosCompletos()
        {
            foreach (Raca raca in Enum.GetValues(typeof(Raca)))
            {
                var dados = DadosDasRacas.Procurar(raca);

                Assert.IsNotNull(dados, "Sem dados para " + raca);
                var textos = new[]
                {
                    dados.Nome,
                    dados.Povo,
                    dados.Aparencia,
                    dados.Traco,
                    dados.SkillRacial,
                    dados.Texto,
                };
                foreach (var texto in textos)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(texto), "Texto vazio em " + raca);
                }
            }
        }

        [Test]
        public void NomesDasRacasSaoDiferentes_ENaoCaemNoNomeDoCodigo()
        {
            var vistos = new HashSet<string>();
            foreach (Raca raca in Enum.GetValues(typeof(Raca)))
            {
                var nome = TextosPersonagem.NomeDaRaca(raca);

                Assert.IsTrue(vistos.Add(nome), "Nome repetido: " + nome);
                Assert.AreEqual(DadosDasRacas.Procurar(raca).Nome, nome);
            }
        }

        [Test]
        public void SoExisteDadoParaRacaDoEnum()
        {
            Assert.AreEqual(Enum.GetValues(typeof(Raca)).Length, DadosDasRacas.Todas.Length);
            Assert.IsNull(DadosDasRacas.Procurar((Raca)0));
        }
    }
}
