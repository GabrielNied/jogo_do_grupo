using JogoDoGrupo.Shared.Features.Mobs;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Mobs.Tests
{
    /// <summary>O buffer de snapshots e o movimento suave do inimigo no client.</summary>
    public class MovimentoSuaveDoInimigoTestes
    {
        private const float Quadro = 1f / 60f;
        private const float Envio = 0.1f;

        private static SnapshotDePose Pacote(
            double tempo,
            float x,
            float z = 0f,
            int etiqueta = 0
        ) => new SnapshotDePose(tempo, x, 0f, z, 0f, etiqueta);

        [Test]
        public void Amostrar_EntreDoisPacotes_Interpola()
        {
            var buffer = new InterpoladorDeSnapshots();
            buffer.Adicionar(Pacote(1.0, 0f));
            buffer.Adicionar(Pacote(1.1, 1f));

            var pose = buffer.Amostrar(1.05);

            Assert.AreEqual(0.5f, pose.X, 0.001f);
            Assert.AreEqual(10f, pose.Velocidade, 0.001f);
            Assert.IsFalse(pose.Extrapolando);
        }

        [Test]
        public void Amostrar_RotacaoPeloCaminhoCurto()
        {
            var buffer = new InterpoladorDeSnapshots();
            buffer.Adicionar(new SnapshotDePose(0, 0f, 0f, 0f, 350f, 0));
            buffer.Adicionar(new SnapshotDePose(1, 0f, 0f, 0f, 10f, 0));

            Assert.AreEqual(360f, buffer.Amostrar(0.5).RotacaoY, 0.001f);
        }

        [Test]
        public void Amostrar_SemPacoteNovo_ExtrapolaNoMaximoOLimite()
        {
            var buffer = new InterpoladorDeSnapshots(0.1f);
            buffer.Adicionar(Pacote(1.0, 0f));
            buffer.Adicionar(Pacote(1.1, 1f));

            var dentro = buffer.Amostrar(1.15);
            var alem = buffer.Amostrar(2.0);

            Assert.IsTrue(dentro.Extrapolando);
            Assert.AreEqual(1.5f, dentro.X, 0.001f);
            Assert.AreEqual(2f, alem.X, 0.001f, "para no limite de 100 ms (10 m/s)");
            Assert.AreEqual(0f, alem.Velocidade);
        }

        [Test]
        public void Adicionar_ForaDeOrdemOuRepetido_Descarta()
        {
            var buffer = new InterpoladorDeSnapshots();
            Assert.IsTrue(buffer.Adicionar(Pacote(1.0, 0f)));
            Assert.IsTrue(buffer.Adicionar(Pacote(1.2, 2f)));

            Assert.IsFalse(buffer.Adicionar(Pacote(1.1, 9f)));
            Assert.IsFalse(buffer.Adicionar(Pacote(1.2, 9f)));

            Assert.AreEqual(2, buffer.Quantidade);
            Assert.AreEqual(1f, buffer.Amostrar(1.1).X, 0.001f);
        }

        [Test]
        public void Amostrar_Teletransporte_PulaSemDeslizarNemContarVelocidade()
        {
            var buffer = new InterpoladorDeSnapshots(0.1f, 5f);
            buffer.Adicionar(Pacote(1.0, 0f));
            buffer.Adicionar(Pacote(1.1, 40f));

            var antes = buffer.Amostrar(1.05);
            var depois = buffer.Amostrar(1.1);

            Assert.AreEqual(0f, antes.X);
            Assert.AreEqual(0f, antes.Velocidade);
            Assert.AreEqual(40f, depois.X, 0.001f);
        }

        [Test]
        public void Inimigo_VelocidadeConstanteDoServidor_NaoTrocaAndarEParado()
        {
            const float velocidade = 5.5f;
            var inimigo = new InimigoNoClient(
                new InimigoNoMundo { InimigoId = 1, Estado = EstadoDeInimigo.Perseguicao }
            );
            var proximoEnvio = 0f;
            var x = 0f;
            var trocas = 0;
            var andava = false;
            var maiorPasso = 0f;
            var menorPasso = float.MaxValue;
            var xAnterior = 0f;

            for (var tempo = 0f; tempo < 4f; tempo += Quadro)
            {
                if (tempo >= proximoEnvio)
                {
                    x = tempo * velocidade;
                    inimigo.ReceberPose(x, 0f, 0f, 90f, EstadoDeInimigo.Perseguicao);
                    proximoEnvio += Envio;
                }

                inimigo.Atualizar(Quadro);
                if (tempo > 1f)
                {
                    var passo = inimigo.X - xAnterior;
                    maiorPasso = System.Math.Max(maiorPasso, passo);
                    menorPasso = System.Math.Min(menorPasso, passo);
                    if (inimigo.Animacao.Andando != andava)
                    {
                        trocas++;
                        andava = inimigo.Animacao.Andando;
                    }
                }

                xAnterior = inimigo.X;
            }

            Assert.IsTrue(inimigo.Animacao.Andando);
            Assert.LessOrEqual(trocas, 1, "anda e continua andando");
            Assert.AreEqual(velocidade, inimigo.Animacao.Velocidade, 0.05f);
            Assert.AreEqual(
                maiorPasso,
                menorPasso,
                0.01f,
                "a pose avança o mesmo tanto a cada quadro"
            );
        }

        [Test]
        public void Inimigo_PacoteDePerseguicaoPerdido_NaoParaAAnimacao()
        {
            const float velocidade = 5f;
            var inimigo = new InimigoNoClient(
                new InimigoNoMundo { InimigoId = 1, Estado = EstadoDeInimigo.Perseguicao }
            );
            var proximoEnvio = 0f;
            var envio = 0;
            var pararAndando = false;

            for (var tempo = 0f; tempo < 3f; tempo += Quadro)
            {
                if (tempo >= proximoEnvio)
                {
                    envio++;
                    proximoEnvio += Envio;
                    if (envio != 15)
                    {
                        inimigo.ReceberPose(
                            tempo * velocidade,
                            0f,
                            0f,
                            0f,
                            EstadoDeInimigo.Perseguicao
                        );
                    }
                }

                inimigo.Atualizar(Quadro);
                if (tempo > 1f && !inimigo.Animacao.Andando)
                {
                    pararAndando = true;
                }
            }

            Assert.IsFalse(pararAndando);
        }

        [Test]
        public void Inimigo_Golpe_ComecaComOModeloParado()
        {
            var inimigo = new InimigoNoClient(
                new InimigoNoMundo { InimigoId = 1, Estado = EstadoDeInimigo.Perseguicao }
            );
            var proximoEnvio = 0f;
            var pararEm = 2f;
            var golpeEm = -1f;
            var xNoGolpe = 0f;
            var xParado = 0f;

            for (var tempo = 0f; tempo < 3f; tempo += Quadro)
            {
                if (tempo >= proximoEnvio)
                {
                    proximoEnvio += Envio;
                    var parado = tempo >= pararEm;
                    var x = (parado ? pararEm : tempo) * 5f;
                    xParado = pararEm * 5f;
                    inimigo.ReceberPose(
                        x,
                        0f,
                        0f,
                        0f,
                        parado ? EstadoDeInimigo.Ataque : EstadoDeInimigo.Perseguicao
                    );
                    if (parado && inimigo.GolpesPreparados == 0)
                    {
                        // O servidor avisa o preparo no mesmo instante em que o inimigo para.
                        inimigo.GolpesPreparados++;
                    }
                }

                inimigo.Atualizar(Quadro);
                if (inimigo.Animacao.DisparouAtaque && golpeEm < 0f)
                {
                    golpeEm = tempo;
                    xNoGolpe = inimigo.X;
                }
            }

            Assert.Greater(golpeEm, pararEm);
            Assert.AreEqual(xParado, xNoGolpe, 0.1f, "chegou ao ponto onde o servidor parou");
            Assert.AreEqual(EstadoDeInimigo.Ataque, inimigo.EstadoMostrado);
        }
    }
}
