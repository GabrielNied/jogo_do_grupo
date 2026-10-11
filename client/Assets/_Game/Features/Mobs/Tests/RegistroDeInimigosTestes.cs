using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Mobs.Tests
{
    /// <summary>O que o client guarda e mostra dos inimigos, conforme as mensagens do servidor.</summary>
    public class RegistroDeInimigosTestes
    {
        private static InimigoNoMundo Dados(long id, int vida = 180) =>
            new InimigoNoMundo
            {
                InimigoId = id,
                CriaturaId = CatalogoCriaturas.JavaliDoCampo,
                X = 1f,
                Z = 2f,
                Vida = vida,
                VidaMaxima = 180,
            };

        private static RegistroDeInimigos ComInimigos(params long[] ids)
        {
            var registro = new RegistroDeInimigos();
            var area = new InimigosDaArea { Inimigos = new InimigoNoMundo[ids.Length] };
            for (var i = 0; i < ids.Length; i++)
            {
                area.Inimigos[i] = Dados(ids[i]);
            }

            registro.SubstituirArea(area);
            return registro;
        }

        [Test]
        public void SubstituirArea_SubstituiALista_EDevolveQuemSaiu()
        {
            var registro = ComInimigos(1, 2);

            var removidos = registro.SubstituirArea(
                new InimigosDaArea { Inimigos = new[] { Dados(2), Dados(3) } }
            );

            Assert.AreEqual(2, removidos.Count);
            Assert.IsNull(registro.Obter(1));
            Assert.IsNotNull(registro.Obter(2));
            Assert.IsNotNull(registro.Obter(3));
            Assert.AreEqual(2, registro.Quantidade);
        }

        [Test]
        public void Aparecer_CriaUm_ERecriaOExistente()
        {
            var registro = ComInimigos(1);

            var anterior = registro.Aparecer(new InimigoApareceu { Inimigo = Dados(1, 50) });
            var novo = registro.Aparecer(new InimigoApareceu { Inimigo = Dados(9) });

            Assert.IsNotNull(anterior);
            Assert.AreEqual(50, registro.Obter(1).Vida);
            Assert.IsNull(novo);
            Assert.IsNotNull(registro.Obter(9));
        }

        [Test]
        public void Mudar_SequenciaAntigaOuIgual_EDescartada()
        {
            var registro = ComInimigos(1);
            registro.Mudar(
                new InimigoMudou
                {
                    InimigoId = 1,
                    Sequencia = 5,
                    X = 10f,
                    Vida = 100,
                }
            );

            var antiga = registro.Mudar(
                new InimigoMudou
                {
                    InimigoId = 1,
                    Sequencia = 4,
                    X = 99f,
                    Vida = 1,
                }
            );
            var igual = registro.Mudar(
                new InimigoMudou
                {
                    InimigoId = 1,
                    Sequencia = 5,
                    X = 99f,
                    Vida = 1,
                }
            );

            Assert.IsFalse(antiga);
            Assert.IsFalse(igual);
            Assert.AreEqual(10f, registro.Obter(1).AlvoX);
            Assert.AreEqual(100, registro.Obter(1).Vida);
        }

        [Test]
        public void Mudar_SequenciaMaior_AtualizaPoseEstadoEVida()
        {
            var registro = ComInimigos(1);

            var aceita = registro.Mudar(
                new InimigoMudou
                {
                    InimigoId = 1,
                    Sequencia = 1,
                    X = 7f,
                    Y = 1f,
                    Z = 8f,
                    RotacaoY = 90f,
                    Estado = EstadoDeInimigo.Perseguicao,
                    AlvoId = 42,
                    Vida = 180,
                }
            );

            var inimigo = registro.Obter(1);
            Assert.IsTrue(aceita);
            Assert.AreEqual(7f, inimigo.AlvoX);
            Assert.AreEqual(8f, inimigo.AlvoZ);
            Assert.AreEqual(90f, inimigo.AlvoRotacaoY);
            Assert.AreEqual(EstadoDeInimigo.Perseguicao, inimigo.Estado);
            Assert.AreEqual(42, inimigo.AlvoId);
        }

        [Test]
        public void Mudar_InimigoDesconhecido_NaoQuebra()
        {
            var registro = ComInimigos(1);

            Assert.IsFalse(registro.Mudar(new InimigoMudou { InimigoId = 77, Sequencia = 1 }));
            Assert.IsFalse(registro.Mudar(null));
        }

        [Test]
        public void Mudar_VoltaAVidaCheia_AtualizaABarra()
        {
            var registro = ComInimigos(1);
            registro.AplicarAcertos(Acerto(1, 20));
            Assert.AreEqual(20f / 180f, registro.Obter(1).FracaoDeVida, 0.001f);

            registro.Mudar(
                new InimigoMudou
                {
                    InimigoId = 1,
                    Sequencia = 1,
                    Vida = 180,
                }
            );

            Assert.AreEqual(1f, registro.Obter(1).FracaoDeVida, 0.001f);
        }

        [Test]
        public void Morrer_RemoveOInimigo_EOAviso()
        {
            var registro = ComInimigos(1);
            registro.PrepararAtaque(new InimigoPreparaAtaque { InimigoId = 1, PreparoMs = 1000 });

            var morto = registro.Morrer(new InimigoMorreu { InimigoId = 1 });

            Assert.IsNotNull(morto);
            Assert.IsNull(morto.Aviso);
            Assert.IsNull(registro.Obter(1));
        }

        [Test]
        public void Morrer_InimigoDesconhecido_NaoQuebra()
        {
            var registro = ComInimigos(1);

            Assert.IsNull(registro.Morrer(new InimigoMorreu { InimigoId = 500 }));
            Assert.IsNull(registro.Morrer(null));
            Assert.AreEqual(1, registro.Quantidade);
        }

        [Test]
        public void AplicarAcertos_BarraSegueAVidaRestanteDoServidor()
        {
            var registro = ComInimigos(1, 2);

            registro.AplicarAcertos(Acerto(1, 90));

            Assert.AreEqual(0.5f, registro.Obter(1).FracaoDeVida, 0.001f);
            Assert.AreEqual(1f, registro.Obter(2).FracaoDeVida, 0.001f);
        }

        [Test]
        public void AplicarAcertos_AlvoQueNaoEInimigo_EIgnorado()
        {
            var registro = ComInimigos(1);

            Assert.DoesNotThrow(() => registro.AplicarAcertos(Acerto(999, 0)));
            Assert.DoesNotThrow(() => registro.AplicarAcertos(null));
            Assert.AreEqual(180, registro.Obter(1).Vida);
        }

        [Test]
        public void Aviso_ComecaNoPreparo_ESomeNoFim()
        {
            var registro = ComInimigos(1);
            var inimigo = registro.Obter(1);

            var ok = registro.PrepararAtaque(
                new InimigoPreparaAtaque
                {
                    InimigoId = 1,
                    PreparoMs = 1000,
                    CentroX = 3f,
                    CentroZ = 4f,
                    Raio = 1.5f,
                }
            );
            registro.Atualizar(0.5f);

            Assert.IsTrue(ok);
            Assert.IsNotNull(inimigo.Aviso);
            Assert.AreEqual(0.5f, inimigo.Aviso.Progresso, 0.001f);
            Assert.AreEqual(1.5f, inimigo.Aviso.Raio);

            registro.Atualizar(0.6f);

            Assert.IsNull(inimigo.Aviso);
        }

        [Test]
        public void PrepararAtaque_InimigoDesconhecido_EIgnorado()
        {
            var registro = ComInimigos(1);

            Assert.IsFalse(registro.PrepararAtaque(new InimigoPreparaAtaque { InimigoId = 5 }));
            Assert.IsFalse(registro.PrepararAtaque(null));
        }

        [Test]
        public void Atualizar_SuavizaAPoseEmDirecaoAoServidor()
        {
            var registro = ComInimigos(1);
            registro.Mudar(
                new InimigoMudou
                {
                    InimigoId = 1,
                    Sequencia = 1,
                    X = 11f,
                    Z = 2f,
                }
            );

            registro.Atualizar(0.05f);
            var meio = registro.Obter(1).X;
            for (var i = 0; i < 100; i++)
            {
                registro.Atualizar(0.05f);
            }

            Assert.Greater(meio, 1f);
            Assert.Less(meio, 11f);
            Assert.AreEqual(11f, registro.Obter(1).X, 0.01f);
        }

        [Test]
        public void Inimigo_Fortalecido_UsaEscalaDasRegras()
        {
            var dados = Dados(1);
            dados.Variante = VarianteDeCriatura.Fortalecida;

            var inimigo = new InimigoNoClient(dados);

            Assert.IsTrue(inimigo.Fortalecido);
            Assert.AreEqual(RegrasCriaturas.EscalaFortalecida, inimigo.Escala);
            Assert.AreEqual(1f, new InimigoNoClient(Dados(2)).Escala);
        }

        [Test]
        public void Rotulo_UsaNomeENivelDoCatalogo_ECriaturaDesconhecidaCaiNoId()
        {
            Assert.AreEqual("Javali do Campo Nv 3", new InimigoNoClient(Dados(1)).Rotulo);

            var desconhecida = Dados(2);
            desconhecida.CriaturaId = 9999;
            Assert.AreEqual("Criatura 9999", new InimigoNoClient(desconhecida).Rotulo);
        }

        [Test]
        public void FracaoDeVida_MaximoZeroOuVidaFora_NaoEstoura()
        {
            var dados = Dados(1);
            dados.VidaMaxima = 0;
            Assert.AreEqual(0f, new InimigoNoClient(dados).FracaoDeVida);

            var acima = Dados(2, 500);
            Assert.AreEqual(1f, new InimigoNoClient(acima).FracaoDeVida);
        }

        private static AtaqueAcertou Acerto(long alvoId, int vidaRestante) =>
            new AtaqueAcertou
            {
                AtacanteId = 1,
                Acertos = new[]
                {
                    new AcertoDeAtaque
                    {
                        AlvoId = alvoId,
                        Dano = 1,
                        VidaRestante = vidaRestante,
                        VidaMaxima = 180,
                    },
                },
            };
    }
}
