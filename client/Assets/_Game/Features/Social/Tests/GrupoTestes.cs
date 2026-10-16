using System;
using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social.Tests
{
    /// <summary>
    /// O client do grupo: manda só intenções, mostra a lista na ordem do servidor (líder e
    /// offline marcados), os convites e os avisos. Sem rede: a conexão é falsa.
    /// </summary>
    public class GrupoTestes
    {
        private const long Eu = 1;

        private sealed class ConexaoFalsa : IConexaoDoGrupo
        {
            private readonly Dictionary<ushort, List<Delegate>> _assinantes =
                new Dictionary<ushort, List<Delegate>>();

            public bool Conectado { get; set; } = true;
            public List<(ushort Id, object Mensagem)> Enviados { get; } =
                new List<(ushort, object)>();

            public bool Enviar<T>(ushort id, T mensagem, EntregaTempoReal entrega)
            {
                if (!Conectado)
                {
                    return false;
                }

                Enviados.Add((id, mensagem));
                return true;
            }

            public IDisposable Assinar<T>(ushort id, Action<T> aoReceber)
            {
                if (!_assinantes.TryGetValue(id, out var lista))
                {
                    _assinantes[id] = lista = new List<Delegate>();
                }

                lista.Add(aoReceber);
                return new Cancelador(() => lista.Remove(aoReceber));
            }

            public void Receber<T>(ushort id, T mensagem)
            {
                if (_assinantes.TryGetValue(id, out var lista))
                {
                    foreach (var assinante in lista.ToArray())
                    {
                        ((Action<T>)assinante)(mensagem);
                    }
                }
            }

            public int Assinantes => _assinantes.Values.Sum(l => l.Count);

            private sealed class Cancelador : IDisposable
            {
                private readonly Action _acao;

                public Cancelador(Action acao) => _acao = acao;

                public void Dispose() => _acao();
            }
        }

        private ConexaoFalsa _conexao;
        private EstadoDoGrupo _estado;
        private GameObject _objeto;
        private PanelSettings _configuracao;
        private VisualElement _raiz;

        [SetUp]
        public void Preparar()
        {
            _conexao = new ConexaoFalsa();
            _estado = new EstadoDoGrupo(_conexao);
            _estado.DefinirPersonagem(Eu);

            // Os botões só recebem eventos presos a um painel de verdade.
            _configuracao = ScriptableObject.CreateInstance<PanelSettings>();
            _objeto = new GameObject("GrupoTestes");
            var documento = _objeto.AddComponent<UIDocument>();
            documento.panelSettings = _configuracao;
            _raiz = documento.rootVisualElement;
        }

        [TearDown]
        public void Limpar()
        {
            _estado.Dispose();
            UnityEngine.Object.DestroyImmediate(_objeto);
            UnityEngine.Object.DestroyImmediate(_configuracao);
        }

        private static MembroDoGrupo Membro(
            long id,
            string nome,
            bool online = true,
            int vida = 100,
            int vidaMaxima = 100
        ) =>
            new MembroDoGrupo
            {
                PersonagemId = id,
                Nome = nome,
                Online = online,
                Vida = vida,
                VidaMaxima = vidaMaxima,
            };

        private void ReceberLista(long liderId, params MembroDoGrupo[] membros) =>
            _conexao.Receber(
                SocialMessageIds.ListaDoGrupo,
                new ListaDoGrupo
                {
                    GrupoId = membros.Length == 0 ? 0 : 10,
                    LiderId = liderId,
                    Membros = membros,
                }
            );

        private void ReceberConvite(long id, string nome, int validade = 60) =>
            _conexao.Receber(
                SocialMessageIds.ConviteRecebido,
                new ConviteRecebido
                {
                    ConviteId = id,
                    NomeDoConvidante = nome,
                    ValidadeSegundos = validade,
                }
            );

        private T Ultimo<T>(ushort id) => (T)_conexao.Enviados.Last(e => e.Id == id).Mensagem;

        // ── Lista do grupo ──────────────────────────────────────────────────────────

        [Test]
        public void Lista_MostraAOrdemDoServidor_ComLiderEOfflineMarcados()
        {
            ReceberLista(
                7,
                Membro(7, "Zeca"),
                Membro(Eu, "Ana"),
                Membro(3, "Bia", online: false),
                Membro(5, "Caio")
            );

            var linhas = ApresentacaoGrupo.Linhas(_estado);

            CollectionAssert.AreEqual(
                new[] { "Zeca", "Ana", "Bia", "Caio" },
                linhas.Select(l => l.Nome).ToArray()
            );
            CollectionAssert.AreEqual(
                new[] { true, false, false, false },
                linhas.Select(l => l.Lider).ToArray()
            );
            CollectionAssert.AreEqual(
                new[] { true, true, false, true },
                linhas.Select(l => l.Online).ToArray()
            );
            Assert.AreEqual("Zeca" + ApresentacaoGrupo.SufixoLider, linhas[0].Texto);
            Assert.AreEqual("Ana" + ApresentacaoGrupo.SufixoVoce, linhas[1].Texto);
            Assert.AreEqual("Bia" + ApresentacaoGrupo.SufixoOffline, linhas[2].Texto);
            Assert.AreEqual("Caio", linhas[3].Texto);
        }

        [Test]
        public void Lista_ProximaListaSubstituiAAnterior_ESemMembrosEhSemGrupo()
        {
            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia"));
            Assert.IsTrue(_estado.EmGrupo);
            Assert.IsTrue(_estado.SouLider);

            ReceberLista(0);

            Assert.IsFalse(_estado.EmGrupo);
            Assert.IsFalse(_estado.SouLider);
            Assert.AreEqual(0, ApresentacaoGrupo.Linhas(_estado).Count);
        }

        [Test]
        public void Lista_SoOLiderExpulsaEPassaALideranca_ENuncaEmSiNemNoLider()
        {
            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia"), Membro(3, "Caio", online: false));

            var linhas = ApresentacaoGrupo.Linhas(_estado);

            Assert.IsFalse(linhas[0].PodeExpulsar);
            Assert.IsFalse(linhas[0].PodePassarLideranca);
            Assert.IsTrue(linhas[1].PodeExpulsar);
            Assert.IsTrue(linhas[1].PodePassarLideranca);
            Assert.IsTrue(linhas[2].PodeExpulsar, "expulsar quem está offline é permitido");
            Assert.IsFalse(linhas[2].PodePassarLideranca, "a liderança vai para quem está online");
        }

        [Test]
        public void Lista_MembroComumNaoExpulsaNemConvida()
        {
            ReceberLista(2, Membro(2, "Bia"), Membro(Eu, "Ana"));

            var linhas = ApresentacaoGrupo.Linhas(_estado);

            Assert.IsFalse(linhas.Any(l => l.PodeExpulsar || l.PodePassarLideranca));
            Assert.IsFalse(_estado.PodeConvidar);
        }

        [Test]
        public void Lista_SemGrupoPodeConvidar()
        {
            Assert.IsTrue(_estado.PodeConvidar);
        }

        [Test]
        public void Lista_ComMembrosNulosEListaNula_NaoQuebra()
        {
            _conexao.Receber(
                SocialMessageIds.ListaDoGrupo,
                new ListaDoGrupo { GrupoId = 1, Membros = null }
            );
            Assert.IsFalse(_estado.EmGrupo);

            _conexao.Receber<ListaDoGrupo>(SocialMessageIds.ListaDoGrupo, null);
            Assert.IsFalse(_estado.EmGrupo);

            ReceberLista(Eu, null, Membro(Eu, "Ana"));
            Assert.AreEqual(1, ApresentacaoGrupo.Linhas(_estado).Count);
        }

        [Test]
        public void Lista_TrocarDePersonagemEsqueceOGrupoDoAnterior()
        {
            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia"));
            ReceberConvite(1, "Caio");

            _estado.DefinirPersonagem(9);

            Assert.IsFalse(_estado.EmGrupo);
            Assert.AreEqual(0, _estado.Convites.Count);
        }

        // ── Intenções enviadas ─────────────────────────────────────────────────────

        [Test]
        public void Convidar_EnviaONomeSemEspacosNasPontas()
        {
            Assert.IsTrue(_estado.Convidar("  Bia  "));

            Assert.AreEqual(
                "Bia",
                Ultimo<ConvidarParaGrupo>(SocialMessageIds.ConvidarParaGrupo).Nome
            );
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Convidar_NomeVazio_NaoEnviaEMostraAviso(string nome)
        {
            Assert.IsFalse(_estado.Convidar(nome));

            Assert.AreEqual(0, _conexao.Enviados.Count);
            Assert.AreEqual(TextosGrupo.NomeVazio, _estado.Mensagem);
            Assert.IsTrue(_estado.MensagemEhErro);
        }

        [Test]
        public void SemConexao_NaoEnviaEMostraAviso()
        {
            _conexao.Conectado = false;

            Assert.IsFalse(_estado.Convidar("Bia"));
            Assert.IsFalse(_estado.Sair());
            Assert.IsFalse(_estado.Expulsar(2));
            Assert.IsFalse(_estado.PassarLideranca(2));

            Assert.AreEqual(0, _conexao.Enviados.Count);
            Assert.AreEqual(TextosGrupo.SemConexao, _estado.Mensagem);
        }

        [Test]
        public void Sair_Expulsar_PassarLideranca_EnviamAIntencao()
        {
            Assert.IsTrue(_estado.Sair());
            Assert.IsTrue(_estado.Expulsar(2));
            Assert.IsTrue(_estado.PassarLideranca(3));

            Assert.AreEqual(1, _conexao.Enviados.Count(e => e.Id == SocialMessageIds.SairDoGrupo));
            Assert.AreEqual(
                2,
                Ultimo<ExpulsarDoGrupo>(SocialMessageIds.ExpulsarDoGrupo).PersonagemId
            );
            Assert.AreEqual(
                3,
                Ultimo<PassarLideranca>(SocialMessageIds.PassarLideranca).PersonagemId
            );
        }

        [Test]
        public void Enviar_NaoMudaOGrupoSozinho_SoOServidorMuda()
        {
            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia"));

            _estado.Sair();
            _estado.Expulsar(2);
            _estado.PassarLideranca(2);

            Assert.AreEqual(2, _estado.Membros.Count);
            Assert.AreEqual(Eu, _estado.LiderId);
        }

        // ── Convites ────────────────────────────────────────────────────────────────

        [Test]
        public void Convite_RecebidoApareceComONomeEComTempo()
        {
            ReceberConvite(5, "Bia", 60);

            Assert.AreEqual(1, _estado.Convites.Count);
            Assert.AreEqual(5, _estado.Convites[0].ConviteId);
            Assert.AreEqual("Bia", _estado.Convites[0].NomeDoConvidante);
            StringAssert.Contains("Bia", ApresentacaoGrupo.TextoDoConvite(_estado.Convites[0]));
            StringAssert.Contains("60", ApresentacaoGrupo.TextoDoConvite(_estado.Convites[0]));
        }

        [Test]
        public void Convite_MesmoIdSubstituiEValidadeZeroNaoAparece()
        {
            ReceberConvite(5, "Bia", 60);
            ReceberConvite(5, "Bia", 30);
            Assert.AreEqual(1, _estado.Convites.Count);
            Assert.AreEqual(30f, _estado.Convites[0].Restante);

            ReceberConvite(6, "Caio", 0);
            Assert.AreEqual(1, _estado.Convites.Count);
        }

        [Test]
        public void Convite_AceitarEnviaOPedidoETiraDaLista()
        {
            ReceberConvite(5, "Bia");

            Assert.IsTrue(_estado.Responder(5, true));

            var enviado = Ultimo<ResponderConvite>(SocialMessageIds.ResponderConvite);
            Assert.AreEqual(5, enviado.ConviteId);
            Assert.IsTrue(enviado.Aceitar);
            Assert.AreEqual(0, _estado.Convites.Count);
            Assert.IsFalse(_estado.EmGrupo, "só o servidor coloca a pessoa no grupo");
        }

        [Test]
        public void Convite_NoAvisoNaTela_AceitarERecusarEnviamResposta()
        {
            var objeto = new GameObject("AvisoDoGrupoTeste");
            var aviso = objeto.AddComponent<AvisoDoGrupoNaTela>();
            aviso.Configurar(_raiz, null, _estado);
            try
            {
                ReceberConvite(5, "Bia");
                aviso.Atualizar();
                Click(_raiz.Query<Button>().ToList().First(b => b.text == "Aceitar (Y)"));
                Assert.IsTrue(Ultimo<ResponderConvite>(SocialMessageIds.ResponderConvite).Aceitar);

                ReceberConvite(6, "Caio");
                aviso.Atualizar();
                Click(_raiz.Query<Button>().ToList().First(b => b.text == "Recusar (N)"));
                Assert.IsFalse(Ultimo<ResponderConvite>(SocialMessageIds.ResponderConvite).Aceitar);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objeto);
            }
        }

        [Test]
        public void Convite_RecusarEnviaOPedidoETiraDaLista()
        {
            ReceberConvite(5, "Bia");

            Assert.IsTrue(_estado.Responder(5, false));

            Assert.IsFalse(Ultimo<ResponderConvite>(SocialMessageIds.ResponderConvite).Aceitar);
            Assert.AreEqual(0, _estado.Convites.Count);
        }

        [Test]
        public void Convite_SemConexaoMantemOConviteNaTela()
        {
            ReceberConvite(5, "Bia");
            _conexao.Conectado = false;

            Assert.IsFalse(_estado.Responder(5, true));

            Assert.AreEqual(1, _estado.Convites.Count);
        }

        [Test]
        public void Convite_SaiDaTelaQuandoOTempoAcaba()
        {
            ReceberConvite(5, "Bia", 60);

            _estado.Atualizar(59f);
            Assert.AreEqual(1, _estado.Convites.Count);

            _estado.Atualizar(1f);
            Assert.AreEqual(0, _estado.Convites.Count);
        }

        [Test]
        public void Convite_AvisoDeExpiradoTiraOConviteDaqueleConvidante()
        {
            ReceberConvite(5, "Bia");
            ReceberConvite(6, "Caio");

            _conexao.Receber(
                SocialMessageIds.AvisoDoGrupo,
                new AvisoDoGrupo { Tipo = TipoDeAvisoDoGrupo.ConviteExpirou, Nome = "bia" }
            );

            Assert.AreEqual(1, _estado.Convites.Count);
            Assert.AreEqual("Caio", _estado.Convites[0].NomeDoConvidante);
        }

        [Test]
        public void Convite_AtualizarAvisaACadaSegundoParaAContagemAndar()
        {
            ReceberConvite(5, "Bia", 60);
            _estado.Atualizar(0.1f);
            var mudancas = 0;
            _estado.Mudou += () => mudancas++;

            _estado.Atualizar(0.1f);
            Assert.AreEqual(0, mudancas, "ainda no mesmo segundo");

            _estado.Atualizar(1f);
            Assert.AreEqual(1, mudancas);
        }

        // ── Avisos e recusas ────────────────────────────────────────────────────────

        [Test]
        public void Aviso_ViraMensagemEApagaDepoisDoTempo()
        {
            _conexao.Receber(
                SocialMessageIds.AvisoDoGrupo,
                new AvisoDoGrupo
                {
                    Tipo = TipoDeAvisoDoGrupo.EntrouNoGrupo,
                    PersonagemId = 2,
                    Nome = "Bia",
                }
            );

            Assert.AreEqual("Bia entrou no grupo.", _estado.Mensagem);
            Assert.IsFalse(_estado.MensagemEhErro);

            _estado.Atualizar(EstadoDoGrupo.DuracaoDaMensagemSegundos);

            Assert.AreEqual(0f, _estado.MensagemRestante);
        }

        [TestCase(TipoDeAvisoDoGrupo.ConviteRecusado, "Bia recusou o convite.")]
        [TestCase(TipoDeAvisoDoGrupo.ConviteExpirou, "O convite de Bia expirou.")]
        [TestCase(TipoDeAvisoDoGrupo.EntrouNoGrupo, "Bia entrou no grupo.")]
        [TestCase(TipoDeAvisoDoGrupo.SaiuDoGrupo, "Bia saiu do grupo.")]
        [TestCase(TipoDeAvisoDoGrupo.LiderMudou, "Bia é o novo líder.")]
        [TestCase(TipoDeAvisoDoGrupo.ExpulsoDoGrupo, "Bia foi expulso do grupo.")]
        [TestCase(TipoDeAvisoDoGrupo.GrupoDesfeito, "O grupo foi desfeito.")]
        [TestCase(TipoDeAvisoDoGrupo.MembroOffline, "Bia ficou offline.")]
        [TestCase(TipoDeAvisoDoGrupo.MembroVoltou, "Bia voltou.")]
        public void Aviso_TodosOsTiposTemTexto(TipoDeAvisoDoGrupo tipo, string esperado)
        {
            var aviso = new AvisoDoGrupo
            {
                Tipo = tipo,
                PersonagemId = 2,
                Nome = "Bia",
            };

            Assert.AreEqual(esperado, TextosGrupo.Aviso(aviso, euMesmo: false));
        }

        [Test]
        public void Aviso_SobreOProprioPersonagem_FalaComVoce()
        {
            _conexao.Receber(
                SocialMessageIds.AvisoDoGrupo,
                new AvisoDoGrupo { Tipo = TipoDeAvisoDoGrupo.ExpulsoDoGrupo, PersonagemId = Eu }
            );

            Assert.AreEqual("Você foi expulso do grupo.", _estado.Mensagem);
        }

        [Test]
        public void Aviso_TipoDesconhecido_NaoMostraMensagem()
        {
            _conexao.Receber(
                SocialMessageIds.AvisoDoGrupo,
                new AvisoDoGrupo { Tipo = (TipoDeAvisoDoGrupo)99, Nome = "Bia" }
            );

            Assert.AreEqual("", _estado.Mensagem);
        }

        [Test]
        public void Recusada_TodosOsCodigosTemTextoProprio()
        {
            // Os códigos do chat (MensagemVazia ... SussurroParaSiMesmo) têm textos em TextosDeChat.
            var doChat = new[]
            {
                CodigosErroSocial.MensagemVazia,
                CodigosErroSocial.MensagemLongaDemais,
                CodigosErroSocial.MensagensRapidasDemais,
                CodigosErroSocial.MensagemRepetida,
                CodigosErroSocial.Silenciado,
                CodigosErroSocial.CanalIndisponivel,
                CodigosErroSocial.SussurroParaSiMesmo,
            };
            var doAmigos = new[]
            {
                CodigosErroSocial.AmizadeConsigoMesmo,
                CodigosErroSocial.AlvoIgnorado,
                CodigosErroSocial.JaSaoAmigos,
                CodigosErroSocial.PedidoJaPendente,
                CodigosErroSocial.PedidosRapidosDemais,
                CodigosErroSocial.ListaDeAmigosCheia,
                CodigosErroSocial.AlvoComPedidosDemais,
                CodigosErroSocial.PedidoNaoEncontrado,
                CodigosErroSocial.AmigoNaoEncontrado,
                CodigosErroSocial.IgnorarASiMesmo,
                CodigosErroSocial.JaIgnorado,
                CodigosErroSocial.ListaDeIgnoradosCheia,
                CodigosErroSocial.IgnoradoNaoEncontrado,
            };
            var codigos = typeof(CodigosErroSocial)
                .GetFields()
                .Select(f => (string)f.GetRawConstantValue())
                .Where(c => !doChat.Contains(c) && !doAmigos.Contains(c))
                .ToArray();

            Assert.AreEqual(16, codigos.Length);
            foreach (var codigo in codigos)
            {
                Assert.AreNotEqual(
                    TextosGrupo.RecusaGenerica,
                    TextosGrupo.Recusa(codigo),
                    $"{codigo} sem texto"
                );
            }
        }

        [Test]
        public void Recusada_ViraMensagemDeErro_ECodigoDesconhecidoTemTextoGenerico()
        {
            _conexao.Receber(
                SocialMessageIds.OperacaoDoGrupoRecusada,
                new OperacaoDoGrupoRecusada { Codigo = CodigosErroSocial.GrupoCheio }
            );
            Assert.AreEqual("O grupo está cheio.", _estado.Mensagem);
            Assert.IsTrue(_estado.MensagemEhErro);

            _conexao.Receber(
                SocialMessageIds.OperacaoDoGrupoRecusada,
                new OperacaoDoGrupoRecusada { Codigo = "CodigoNovo" }
            );
            Assert.AreEqual(TextosGrupo.RecusaGenerica, _estado.Mensagem);

            _conexao.Receber<OperacaoDoGrupoRecusada>(
                SocialMessageIds.OperacaoDoGrupoRecusada,
                null
            );
            Assert.AreEqual(TextosGrupo.RecusaGenerica, _estado.Mensagem);
        }

        [Test]
        public void Dispose_CancelaAsAssinaturas()
        {
            Assert.AreEqual(4, _conexao.Assinantes);

            _estado.Dispose();
            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia"));

            Assert.AreEqual(0, _conexao.Assinantes);
            Assert.IsFalse(_estado.EmGrupo);
        }

        // ── Texto por cima do jogo ───────────────────────────────────────────────────

        [Test]
        public void NaTela_ConviteMostraATeclaDaJanela_EMensagemVenceOConvite()
        {
            ReceberConvite(5, "Bia");
            var texto = ApresentacaoGrupo.TextoNaTela(_estado, "O");
            StringAssert.Contains("Bia", texto);
            StringAssert.Contains("O: responder", texto);

            _conexao.Receber(
                SocialMessageIds.OperacaoDoGrupoRecusada,
                new OperacaoDoGrupoRecusada { Codigo = CodigosErroSocial.NaoELider }
            );
            Assert.AreEqual(
                TextosGrupo.Recusa(CodigosErroSocial.NaoELider),
                ApresentacaoGrupo.TextoNaTela(_estado, "O")
            );
        }

        [Test]
        public void NaTela_SemNadaAMostrarFicaVazio()
        {
            Assert.AreEqual("", ApresentacaoGrupo.TextoNaTela(_estado, "O"));
        }

        // ── Janela e painel ─────────────────────────────────────────────────────────

        [Test]
        public void Definicao_UsaATeclaO_ECriaUmaJanelaNovaACadaAbertura()
        {
            var definicao = JanelaGrupo.CriarDefinicao();

            Assert.AreEqual("social.grupo", definicao.Chave);
            Assert.AreEqual("O", definicao.Tecla);
            var primeira = definicao.Criar();
            Assert.IsInstanceOf<JanelaGrupo>(primeira);
            Assert.AreNotSame(primeira, definicao.Criar());
        }

        private PainelGrupo NovoPainel(out VisualElement conteudo, out List<string> titulos)
        {
            conteudo = new VisualElement();
            _raiz.Add(conteudo);
            var lista = new List<string>();
            titulos = lista;
            return new PainelGrupo(conteudo, _estado, lista.Add);
        }

        [Test]
        public void Painel_ListaOsMembrosNaOrdemDoServidor_ComTituloEMarcas()
        {
            ReceberLista(2, Membro(2, "Bia"), Membro(Eu, "Ana"), Membro(3, "Caio", online: false));
            var painel = NovoPainel(out var conteudo, out var titulos);

            var nomes = conteudo
                .Query<Label>(className: EstiloSocial.Nome)
                .ToList()
                .Select(l => l.text)
                .ToArray();

            CollectionAssert.AreEqual(
                new[]
                {
                    "Bia" + ApresentacaoGrupo.SufixoLider,
                    "Ana" + ApresentacaoGrupo.SufixoVoce,
                    "Caio" + ApresentacaoGrupo.SufixoOffline,
                },
                nomes
            );
            Assert.AreEqual("Grupo (3/5)", titulos.Last());
            var rotulos = conteudo.Query<Label>(className: EstiloSocial.Nome).ToList();
            Assert.IsTrue(rotulos[0].ClassListContains(EstiloSocial.Lider));
            Assert.IsFalse(rotulos[1].ClassListContains(EstiloSocial.Lider));
            Assert.IsTrue(rotulos[2].ClassListContains(EstiloSocial.Offline));
            Assert.IsFalse(rotulos[0].ClassListContains(EstiloSocial.Offline));
            painel.Dispose();
        }

        [Test]
        public void Painel_AcompanhaAsMudancasDoEstado_EParaDepoisDoDispose()
        {
            var painel = NovoPainel(out var conteudo, out var titulos);
            Assert.AreEqual(0, conteudo.Query<Label>(className: EstiloSocial.Nome).ToList().Count);

            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia"));
            Assert.AreEqual(2, conteudo.Query<Label>(className: EstiloSocial.Nome).ToList().Count);

            painel.Dispose();
            ReceberLista(0);
            Assert.AreEqual(2, conteudo.Query<Label>(className: EstiloSocial.Nome).ToList().Count);
        }

        [Test]
        public void Painel_ConviteTemAceitarERecusar_QueEnviamAResposta()
        {
            ReceberConvite(5, "Bia");
            var painel = NovoPainel(out var conteudo, out _);

            var botoes = conteudo.Query<Button>().ToList();
            var aceitar = botoes.First(b => b.text == PainelGrupo.TextoAceitar);
            var recusar = botoes.First(b => b.text == PainelGrupo.TextoRecusar);
            Assert.IsNotNull(recusar);

            Click(aceitar);

            Assert.IsTrue(Ultimo<ResponderConvite>(SocialMessageIds.ResponderConvite).Aceitar);
            Assert.IsFalse(
                conteudo.Query<Button>().ToList().Any(b => b.text == PainelGrupo.TextoAceitar)
            );
            painel.Dispose();
        }

        [Test]
        public void Painel_LiderVeExpulsarEPassarLideranca_ENaoClicaEmSi()
        {
            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia"));
            var painel = NovoPainel(out var conteudo, out _);

            var botoes = conteudo.Query<Button>().ToList();
            Assert.AreEqual(1, botoes.Count(b => b.text == PainelGrupo.TextoExpulsar));
            Assert.AreEqual(1, botoes.Count(b => b.text == PainelGrupo.TextoPassarLideranca));

            Click(botoes.First(b => b.text == PainelGrupo.TextoExpulsar));
            Click(botoes.First(b => b.text == PainelGrupo.TextoPassarLideranca));
            Click(botoes.First(b => b.text == PainelGrupo.TextoSair));

            Assert.AreEqual(
                2,
                Ultimo<ExpulsarDoGrupo>(SocialMessageIds.ExpulsarDoGrupo).PersonagemId
            );
            Assert.AreEqual(
                2,
                Ultimo<PassarLideranca>(SocialMessageIds.PassarLideranca).PersonagemId
            );
            Assert.AreEqual(1, _conexao.Enviados.Count(e => e.Id == SocialMessageIds.SairDoGrupo));
            painel.Dispose();
        }

        [Test]
        public void Painel_MembroComumNaoVeExpulsarNemConvidar()
        {
            ReceberLista(2, Membro(2, "Bia"), Membro(Eu, "Ana"));
            var painel = NovoPainel(out var conteudo, out _);

            var botoes = conteudo.Query<Button>().ToList();
            Assert.IsFalse(botoes.Any(b => b.text == PainelGrupo.TextoExpulsar));
            Assert.IsFalse(botoes.Any(b => b.text == PainelGrupo.TextoPassarLideranca));
            var convidar = botoes.First(b => b.text == PainelGrupo.TextoConvidar);
            Assert.AreEqual(DisplayStyle.None, convidar.parent.style.display.value);
            painel.Dispose();
        }

        [Test]
        public void Painel_ConvidarEnviaONomeDigitadoELimpaOCampo()
        {
            var painel = NovoPainel(out var conteudo, out _);
            var campo = conteudo.Q<TextField>();
            campo.value = " Bia ";

            Click(
                conteudo.Query<Button>().ToList().First(b => b.text == PainelGrupo.TextoConvidar)
            );

            Assert.AreEqual(
                "Bia",
                Ultimo<ConvidarParaGrupo>(SocialMessageIds.ConvidarParaGrupo).Nome
            );
            Assert.AreEqual("", campo.value);
            painel.Dispose();
        }

        [Test]
        public void Painel_ConvidarSemNomeMostraAvisoEMantemOCampo()
        {
            var painel = NovoPainel(out var conteudo, out _);

            Click(
                conteudo.Query<Button>().ToList().First(b => b.text == PainelGrupo.TextoConvidar)
            );

            Assert.AreEqual(0, _conexao.Enviados.Count);
            var aviso = conteudo.Query<Label>(className: EstiloCore.Mensagem).First();
            Assert.AreEqual(TextosGrupo.NomeVazio, aviso.text);
            painel.Dispose();
        }

        private static void Click(Button botao)
        {
            using (var evento = NavigationSubmitEvent.GetPooled())
            {
                evento.target = botao;
                botao.SendEvent(evento);
            }
        }

        [Test]
        public void Estado_EhAliado_IdentificaMembrosAliados()
        {
            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia"), Membro(3, "Caio"));

            Assert.IsTrue(_estado.EhAliado(2));
            Assert.IsTrue(_estado.EhAliado(3));
            Assert.IsFalse(_estado.EhAliado(Eu));
            Assert.IsFalse(_estado.EhAliado(4));
            Assert.IsFalse(_estado.EhAliado(0));
        }

        [Test]
        public void Estado_VidaDoMembroMudou_AtualizaVidaEMaxima()
        {
            ReceberLista(Eu, Membro(Eu, "Ana"), Membro(2, "Bia", true, 100, 100));
            var mudouDisparado = false;
            _estado.Mudou += () => mudouDisparado = true;

            _conexao.Receber(
                SocialMessageIds.VidaDoMembroMudou,
                new VidaDoMembroMudou
                {
                    PersonagemId = 2,
                    Vida = 45,
                    VidaMaxima = 150,
                }
            );

            Assert.IsTrue(mudouDisparado);
            var bia = _estado.Membros.First(m => m.PersonagemId == 2);
            Assert.AreEqual(45, bia.Vida);
            Assert.AreEqual(150, bia.VidaMaxima);
        }

        [Test]
        public void PainelHud_SemGrupoOuSozinho_EscondeOPainel()
        {
            var painel = new PainelListaGrupoHud(_raiz, _estado);
            Assert.AreEqual(DisplayStyle.None, painel.ElementoRaiz.style.display.value);

            // Grupo com apenas o jogador local
            ReceberLista(Eu, Membro(Eu, "Ana"));
            Assert.AreEqual(DisplayStyle.None, painel.ElementoRaiz.style.display.value);

            painel.Dispose();
        }

        [Test]
        public void PainelHud_ComAliados_MostraLinhasComVidaExataEPercentual()
        {
            var painel = new PainelListaGrupoHud(_raiz, _estado);
            ReceberLista(
                2,
                Membro(Eu, "Ana", true, 100, 100),
                Membro(2, "Bia", true, 80, 100),
                Membro(3, "Caio", true, 20, 100)
            );

            Assert.AreEqual(DisplayStyle.Flex, painel.ElementoRaiz.style.display.value);
            var linhaBia = painel.Linhas[0];
            Assert.AreEqual(DisplayStyle.Flex, linhaBia.Raiz.style.display.value);
            Assert.AreEqual("Bia", linhaBia.Nome.text);
            Assert.AreEqual(DisplayStyle.Flex, linhaBia.IndicadorLider.style.display.value);
            Assert.AreEqual("80/100 (80%)", linhaBia.TextoVida.text);
            Assert.IsFalse(linhaBia.Raiz.ClassListContains(PainelListaGrupoHud.ClasseCritica));

            var linhaCaio = painel.Linhas[1];
            Assert.AreEqual(DisplayStyle.Flex, linhaCaio.Raiz.style.display.value);
            Assert.AreEqual("Caio", linhaCaio.Nome.text);
            Assert.AreEqual(DisplayStyle.None, linhaCaio.IndicadorLider.style.display.value);
            Assert.AreEqual("20/100 (20%)", linhaCaio.TextoVida.text);
            Assert.IsTrue(linhaCaio.Raiz.ClassListContains(PainelListaGrupoHud.ClasseCritica));

            // Demais linhas vazias ficam escondidas
            Assert.AreEqual(DisplayStyle.None, painel.Linhas[2].Raiz.style.display.value);
            Assert.AreEqual(DisplayStyle.None, painel.Linhas[3].Raiz.style.display.value);

            painel.Dispose();
        }

        [Test]
        public void PainelHud_AliadoOffline_AplicaClasseOfflineENaoCritica()
        {
            var painel = new PainelListaGrupoHud(_raiz, _estado);
            ReceberLista(
                Eu,
                Membro(Eu, "Ana"),
                Membro(2, "Bia", false, 10, 100) // offline com vida baixa
            );

            var linhaBia = painel.Linhas[0];
            Assert.IsTrue(linhaBia.Raiz.ClassListContains(PainelListaGrupoHud.ClasseOffline));
            Assert.AreEqual(DisplayStyle.Flex, linhaBia.StatusOffline.style.display.value);
            Assert.IsFalse(linhaBia.Raiz.ClassListContains(PainelListaGrupoHud.ClasseCritica));

            painel.Dispose();
        }

        [Test]
        public void PainelHud_Descartar_RemoveDaHierarquia()
        {
            var painel = new PainelListaGrupoHud(_raiz, _estado);
            Assert.IsTrue(_raiz.Contains(painel.ElementoRaiz));

            painel.Descartar();
            Assert.IsFalse(_raiz.Contains(painel.ElementoRaiz));
        }
    }
}
