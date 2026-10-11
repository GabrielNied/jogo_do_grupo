using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Social;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social.Tests
{
    internal sealed class ArmazenamentoEmMemoria : IArmazenamentoDeTexto
    {
        public readonly Dictionary<string, string> Valores = new Dictionary<string, string>();

        public string Ler(string chave) => Valores.TryGetValue(chave, out var v) ? v : "";

        public void Gravar(string chave, string valor) => Valores[chave] = valor;
    }

    /// <summary>Comandos, ignorados, histórico e o serviço do chat (sem cena nem servidor).</summary>
    public class ChatTestes
    {
        private ArmazenamentoEmMemoria _armazenamento;
        private ListaDeIgnorados _ignorados;
        private ServicoDeChat _servico;
        private List<EnviarMensagemDeChat> _enviadas;
        private bool _conectado;
        private long _meuId;
        private long _agora;

        [SetUp]
        public void Preparar()
        {
            _armazenamento = new ArmazenamentoEmMemoria();
            _ignorados = new ListaDeIgnorados(_armazenamento);
            _enviadas = new List<EnviarMensagemDeChat>();
            _conectado = true;
            _meuId = 1;
            _agora = 1_000;
            _servico = new ServicoDeChat(
                m =>
                {
                    if (_conectado)
                    {
                        _enviadas.Add(m);
                    }

                    return _conectado;
                },
                () => _meuId,
                () => _agora,
                _ignorados
            );
        }

        private static MensagemDeChat Mensagem(
            CanalDeChat canal,
            long remetenteId,
            string nome,
            string texto,
            long hora = 0,
            string alvo = ""
        ) =>
            new MensagemDeChat
            {
                Canal = canal,
                RemetenteId = remetenteId,
                NomeDoRemetente = nome,
                NomeDoAlvo = alvo,
                Texto = texto,
                HoraUnixMs = hora,
            };

        // ── Interpretador ───────────────────────────────────────────────────────

        [Test]
        public void SemBarra_VaiParaOUltimoCanal_OPrimeiroECidade()
        {
            var estado = new EstadoDoEnvio();

            var resultado = InterpretadorDeComandos.Interpretar("oi gente", estado);

            Assert.AreEqual(AcaoDoComando.Enviar, resultado.Acao);
            Assert.AreEqual(CanalDeChat.Cidade, resultado.Canal);
            Assert.AreEqual("oi gente", resultado.Texto);
        }

        [Test]
        public void Grupo_ComTexto_EnviaENoProximoSemBarraContinuaNoGrupo()
        {
            var estado = new EstadoDoEnvio();

            var primeiro = InterpretadorDeComandos.Interpretar("/g bora", estado);
            estado.Registrar(primeiro);
            var segundo = InterpretadorDeComandos.Interpretar("vamos", estado);

            Assert.AreEqual(CanalDeChat.Grupo, primeiro.Canal);
            Assert.AreEqual("bora", primeiro.Texto);
            Assert.AreEqual(CanalDeChat.Grupo, segundo.Canal);
        }

        [Test]
        public void Cidade_DepoisDeGrupo_VoltaOUltimoCanal()
        {
            var estado = new EstadoDoEnvio();
            estado.Registrar(InterpretadorDeComandos.Interpretar("/g a", estado));

            var resultado = InterpretadorDeComandos.Interpretar("/c b", estado);
            estado.Registrar(resultado);

            Assert.AreEqual(CanalDeChat.Cidade, resultado.Canal);
            Assert.AreEqual(CanalDeChat.Cidade, estado.UltimoCanal);
        }

        [TestCase("/c")]
        [TestCase("/g")]
        public void ComandoDeCanalSozinho_SoTrocaOCanal(string entrada)
        {
            var resultado = InterpretadorDeComandos.Interpretar(entrada, new EstadoDoEnvio());

            Assert.AreEqual(AcaoDoComando.MudarCanal, resultado.Acao);
        }

        [TestCase("/s Ana oi tudo bem", "Ana", "oi tudo bem")]
        [TestCase("/S   Ana    oi", "Ana", "oi")]
        [TestCase("/w Ana oi", "Ana", "oi")]
        public void Sussurro_SeparaONomeDoTexto(string entrada, string nome, string texto)
        {
            var resultado = InterpretadorDeComandos.Interpretar(entrada, new EstadoDoEnvio());

            Assert.AreEqual(AcaoDoComando.Enviar, resultado.Acao);
            Assert.AreEqual(CanalDeChat.Sussurro, resultado.Canal);
            Assert.AreEqual(nome, resultado.Nome);
            Assert.AreEqual(texto, resultado.Texto);
        }

        [Test]
        public void Sussurro_SoComONome_TrocaDeCanalEOProximoTextoVaiParaEle()
        {
            var estado = new EstadoDoEnvio();
            var troca = InterpretadorDeComandos.Interpretar("/s Ana", estado);
            estado.Registrar(troca);

            var seguinte = InterpretadorDeComandos.Interpretar("e aí", estado);

            Assert.AreEqual(AcaoDoComando.MudarCanal, troca.Acao);
            Assert.AreEqual(CanalDeChat.Sussurro, seguinte.Canal);
            Assert.AreEqual("Ana", seguinte.Nome);
        }

        [Test]
        public void Sussurro_SemNome_EErro()
        {
            var resultado = InterpretadorDeComandos.Interpretar("/s", new EstadoDoEnvio());

            Assert.AreEqual(AcaoDoComando.Erro, resultado.Acao);
        }

        [Test]
        public void Responder_UsaQuemSussurrouPorUltimo()
        {
            var estado = new EstadoDoEnvio { UltimoSussurroRecebido = "Bia" };

            var resultado = InterpretadorDeComandos.Interpretar("/r valeu", estado);

            Assert.AreEqual(AcaoDoComando.Enviar, resultado.Acao);
            Assert.AreEqual(CanalDeChat.Sussurro, resultado.Canal);
            Assert.AreEqual("Bia", resultado.Nome);
            Assert.AreEqual("valeu", resultado.Texto);
        }

        [Test]
        public void Responder_SemNinguemParaResponder_EErro()
        {
            var resultado = InterpretadorDeComandos.Interpretar("/r oi", new EstadoDoEnvio());

            Assert.AreEqual(AcaoDoComando.Erro, resultado.Acao);
        }

        [Test]
        public void IgnorarEDesignorar_LeemONome()
        {
            var estado = new EstadoDoEnvio();

            var ignorar = InterpretadorDeComandos.Interpretar("/ignorar Chato", estado);
            var designorar = InterpretadorDeComandos.Interpretar("/designorar Chato", estado);

            Assert.AreEqual(AcaoDoComando.Ignorar, ignorar.Acao);
            Assert.AreEqual("Chato", ignorar.Nome);
            Assert.AreEqual(AcaoDoComando.Designorar, designorar.Acao);
            Assert.AreEqual("Chato", designorar.Nome);
        }

        [Test]
        public void ComandoDesconhecido_NaoEEnviado()
        {
            var resultado = InterpretadorDeComandos.Interpretar("/xyz oi", new EstadoDoEnvio());

            Assert.AreEqual(AcaoDoComando.Erro, resultado.Acao);
            StringAssert.Contains("/xyz", resultado.Mensagem);
        }

        [TestCase("")]
        [TestCase("   ")]
        public void TextoVazio_NaoFazNada(string entrada)
        {
            var resultado = InterpretadorDeComandos.Interpretar(entrada, new EstadoDoEnvio());

            Assert.AreEqual(AcaoDoComando.Nada, resultado.Acao);
        }

        [Test]
        public void Limite200Caracteres_ValeEMais1NaoEnvia()
        {
            var estado = new EstadoDoEnvio();

            var noLimite = InterpretadorDeComandos.Interpretar(new string('a', 200), estado);
            var acima = InterpretadorDeComandos.Interpretar(new string('a', 201), estado);

            Assert.AreEqual(AcaoDoComando.Enviar, noLimite.Acao);
            Assert.AreEqual(AcaoDoComando.Erro, acima.Acao);
            Assert.IsTrue(acima.ManterAberto);
        }

        [Test]
        public void Texto_SemMarcasDeFormatacaoInterpretadas_QuebraDeLinhaViraEspaco()
        {
            var resultado = InterpretadorDeComandos.Interpretar(
                "<b>oi</b>\nvocê",
                new EstadoDoEnvio()
            );

            Assert.AreEqual("<b>oi</b> você", resultado.Texto);
        }

        // ── Ignorados ───────────────────────────────────────────────────────────

        [Test]
        public void Ignorados_NaoDiferenciamMaiusculasENaoDuplicam()
        {
            Assert.AreEqual(ResultadoDeIgnorar.Adicionado, _ignorados.Adicionar("Chato"));
            Assert.AreEqual(ResultadoDeIgnorar.JaIgnorado, _ignorados.Adicionar("chato"));

            Assert.IsTrue(_ignorados.Contem("CHATO"));
            Assert.AreEqual(1, _ignorados.Nomes.Count);
        }

        [Test]
        public void Ignorados_LimiteDe50()
        {
            for (var i = 0; i < ListaDeIgnorados.Maximo; i++)
            {
                Assert.AreEqual(ResultadoDeIgnorar.Adicionado, _ignorados.Adicionar("Nome" + i));
            }

            Assert.AreEqual(ResultadoDeIgnorar.ListaCheia, _ignorados.Adicionar("Outro"));
            Assert.AreEqual(ListaDeIgnorados.Maximo, _ignorados.Nomes.Count);
            Assert.IsTrue(_ignorados.Remover("Nome0"));
            Assert.AreEqual(ResultadoDeIgnorar.Adicionado, _ignorados.Adicionar("Outro"));
        }

        [TestCase("")]
        [TestCase("  ")]
        [TestCase("dois nomes")]
        public void Ignorados_NomeInvalido(string nome)
        {
            Assert.AreEqual(ResultadoDeIgnorar.NomeInvalido, _ignorados.Adicionar(nome));
        }

        [Test]
        public void Ignorados_FicamGravadosEVoltamNaProximaVez()
        {
            _ignorados.Adicionar("Ana");
            _ignorados.Adicionar("Bia");
            _ignorados.Remover("Ana");

            var outra = new ListaDeIgnorados(_armazenamento);

            CollectionAssert.AreEqual(new[] { "Bia" }, outra.Nomes);
        }

        [Test]
        public void Ignorados_RemoverQuemNaoEstavaDevolveFalso()
        {
            Assert.IsFalse(_ignorados.Remover("Ninguem"));
        }

        // ── Histórico ───────────────────────────────────────────────────────────

        [Test]
        public void Historico_GuardaAs100UltimasLinhas()
        {
            for (var i = 0; i < HistoricoDeChat.Maximo + 5; i++)
            {
                _servico.Historico.Adicionar(LinhaDeChat.Sistema("linha " + i, i));
            }

            Assert.AreEqual(HistoricoDeChat.Maximo, _servico.Historico.Linhas.Count);
            Assert.AreEqual("linha 5", _servico.Historico.Linhas[0].Texto);
            Assert.AreEqual("linha 104", _servico.Historico.Linhas[99].Texto);
        }

        [Test]
        public void Historico_NomeIgnoradoNaoAparece_EVoltaAoDesignorar()
        {
            _servico.AoReceberMensagem(Mensagem(CanalDeChat.Cidade, 2, "Chato", "spam", 10));
            _servico.AoReceberMensagem(Mensagem(CanalDeChat.Cidade, 3, "Ana", "oi", 20));
            _ignorados.Adicionar("chato");

            var visiveis = _servico.Historico.Ultimas(8, _ignorados);

            Assert.AreEqual(1, visiveis.Count);
            Assert.AreEqual("Ana", visiveis[0].Nome);

            _ignorados.Remover("Chato");
            Assert.AreEqual(2, _servico.Historico.Ultimas(8, _ignorados).Count);
        }

        [Test]
        public void Historico_UltimasDevolveSoAsMaisNovas()
        {
            for (var i = 0; i < 12; i++)
            {
                _servico.Historico.Adicionar(LinhaDeChat.Sistema("l" + i, i));
            }

            var ultimas = _servico.Historico.Ultimas(8, _ignorados);

            Assert.AreEqual(8, ultimas.Count);
            Assert.AreEqual("l4", ultimas[0].Texto);
            Assert.AreEqual("l11", ultimas[7].Texto);
        }

        [Test]
        public void HistoricoDaCidade_EntraEmOrdemENaoDuplicaAoReentrar()
        {
            var historico = new HistoricoDaCidade
            {
                Mensagens = new[]
                {
                    Mensagem(CanalDeChat.Cidade, 2, "Ana", "primeira", 100),
                    Mensagem(CanalDeChat.Cidade, 3, "Bia", "segunda", 200),
                },
            };
            _servico.AoReceberMensagem(Mensagem(CanalDeChat.Cidade, 4, "Caio", "agora", 300));

            _servico.AoReceberHistorico(historico);
            _servico.AoReceberHistorico(historico);

            var textos = new List<string>();
            foreach (var linha in _servico.Historico.Linhas)
            {
                textos.Add(linha.Texto);
            }

            CollectionAssert.AreEqual(new[] { "primeira", "segunda", "agora" }, textos);
        }

        [Test]
        public void HistoricoDaCidade_NuloOuSemMensagensNaoQuebra()
        {
            _servico.AoReceberHistorico(null);
            _servico.AoReceberHistorico(new HistoricoDaCidade { Mensagens = null });

            Assert.AreEqual(0, _servico.Historico.Linhas.Count);
        }

        // ── Serviço: envio ──────────────────────────────────────────────────────

        [Test]
        public void Enviar_MensagemSemComando_VaiNaCidadeComTextoLimpo()
        {
            var fechar = _servico.Enviar("  oi  ");

            Assert.IsTrue(fechar);
            Assert.AreEqual(1, _enviadas.Count);
            Assert.AreEqual(CanalDeChat.Cidade, _enviadas[0].Canal);
            Assert.AreEqual("oi", _enviadas[0].Texto);
            Assert.AreEqual("", _enviadas[0].NomeDoAlvo);
        }

        [Test]
        public void Enviar_Sussurro_LevaONomeDoAlvoSoNesseCanal()
        {
            _servico.Enviar("/s Ana oi");
            _servico.Enviar("/g bora");

            Assert.AreEqual(CanalDeChat.Sussurro, _enviadas[0].Canal);
            Assert.AreEqual("Ana", _enviadas[0].NomeDoAlvo);
            Assert.AreEqual(CanalDeChat.Grupo, _enviadas[1].Canal);
            Assert.AreEqual("", _enviadas[1].NomeDoAlvo);
        }

        [Test]
        public void Enviar_TextoLongo_NaoEnviaEMantemACaixaAberta()
        {
            var fechar = _servico.Enviar(new string('a', 201));

            Assert.IsFalse(fechar);
            Assert.AreEqual(0, _enviadas.Count);
            Assert.AreEqual(CanalDaLinha.Sistema, _servico.Historico.Linhas[0].Canal);
        }

        [Test]
        public void Enviar_SemConexao_AvisaEMantemOTexto()
        {
            _conectado = false;

            var fechar = _servico.Enviar("oi");

            Assert.IsFalse(fechar);
            Assert.AreEqual(TextosDeChat.ChatSemConexao, _servico.Historico.Linhas[0].Texto);
        }

        [Test]
        public void Enviar_ComandoDesconhecido_NaoVaiAoServidorEMostraLinhaDeSistema()
        {
            var fechar = _servico.Enviar("/dancar");

            Assert.IsTrue(fechar);
            Assert.AreEqual(0, _enviadas.Count);
            Assert.AreEqual(CanalDaLinha.Sistema, _servico.Historico.Linhas[0].Canal);
            StringAssert.Contains("/dancar", _servico.Historico.Linhas[0].Texto);
        }

        [Test]
        public void Enviar_IgnorarEDesignorar_EnviamIntencaoAoServidor()
        {
            string nomeIgnorado = null;
            long idDesignorado = -1;
            _servico = new ServicoDeChat(
                m =>
                {
                    _enviadas.Add(m);
                    return true;
                },
                () => _meuId,
                () => _agora,
                _ignorados,
                _servico.Amigos,
                nome =>
                {
                    nomeIgnorado = nome;
                    return true;
                },
                id =>
                {
                    idDesignorado = id;
                    return true;
                }
            );
            _servico.Amigos.Aplicar(
                new JogoDoGrupo.Shared.Features.Social.ListaDeIgnorados
                {
                    Ignorados = new[]
                    {
                        new JogadorIgnorado { IgnoradoId = 9, Nome = "Chato" },
                    },
                }
            );

            _servico.Enviar("/ignorar Chato");
            _servico.Enviar("/designorar chato");

            Assert.AreEqual("Chato", nomeIgnorado);
            Assert.AreEqual(9, idDesignorado);
            Assert.AreEqual(0, _enviadas.Count);
            Assert.IsFalse(_ignorados.Contem("Chato"));
        }

        [Test]
        public void Enviar_TextoVazio_NaoEnviaEFechaACaixa()
        {
            Assert.IsTrue(_servico.Enviar("   "));
            Assert.AreEqual(0, _enviadas.Count);
            Assert.AreEqual(0, _servico.Historico.Linhas.Count);
        }

        // ── Serviço: recebimento ────────────────────────────────────────────────

        [Test]
        public void Receber_MensagemPropria_FicaMarcadaComoPropria()
        {
            _servico.AoReceberMensagem(Mensagem(CanalDeChat.Cidade, 1, "Eu", "oi", 10));

            Assert.IsTrue(_servico.Historico.Linhas[0].Propria);
        }

        [Test]
        public void Receber_Sussurro_GuardaQuemSussurrouParaOResponder()
        {
            _servico.AoReceberMensagem(
                Mensagem(CanalDeChat.Sussurro, 2, "Bia", "psiu", 10, alvo: "Eu")
            );

            var resultado = InterpretadorDeComandos.Interpretar("/r oi", _servico.Estado);

            Assert.AreEqual("Bia", resultado.Nome);
        }

        [Test]
        public void Receber_SussurroDeIgnorado_NaoApareceENaoViraAlvoDoResponder()
        {
            _ignorados.Adicionar("Chato");

            _servico.AoReceberMensagem(
                Mensagem(CanalDeChat.Sussurro, 2, "Chato", "psiu", 10, alvo: "Eu")
            );

            Assert.AreEqual("", _servico.Estado.UltimoSussurroRecebido);
            Assert.AreEqual(0, _servico.Historico.Ultimas(8, _ignorados).Count);
        }

        [Test]
        public void Receber_CanalInexistenteOuNulo_EIgnorado()
        {
            _servico.AoReceberMensagem(null);
            _servico.AoReceberMensagem(Mensagem((CanalDeChat)99, 2, "Ana", "x"));

            Assert.AreEqual(0, _servico.Historico.Linhas.Count);
        }

        [Test]
        public void Recusa_ViraLinhaDeSistemaComOsSegundosDoSilencio()
        {
            _servico.AoReceberRecusa(
                new MensagemDeChatRecusada
                {
                    Codigo = CodigosErroSocial.Silenciado,
                    SegundosRestantes = 42,
                }
            );

            var linha = _servico.Historico.Linhas[0];
            Assert.AreEqual(CanalDaLinha.Sistema, linha.Canal);
            StringAssert.Contains("42", linha.Texto);
        }

        [Test]
        public void Recusa_SemGrupo_VoltaOUltimoCanalParaACidade()
        {
            _servico.Enviar("/g oi");
            Assert.AreEqual(CanalDeChat.Grupo, _servico.Estado.UltimoCanal);

            _servico.AoReceberRecusa(
                new MensagemDeChatRecusada { Codigo = CodigosErroSocial.NaoEstaEmGrupo }
            );

            Assert.AreEqual(CanalDeChat.Cidade, _servico.Estado.UltimoCanal);
        }

        [Test]
        public void Recusa_CodigoDesconhecido_TemTextoGenerico()
        {
            Assert.AreEqual("A mensagem não foi enviada.", TextosDeChat.Recusa("Inventado", 0));
        }

        // ── Texto das linhas e painel ───────────────────────────────────────────

        [Test]
        public void Linha_MostraHoraCanalENome()
        {
            // 2026-10-11 14:05 UTC.
            var hora = new DateTimeOffset(
                2026,
                10,
                11,
                14,
                5,
                0,
                TimeSpan.Zero
            ).ToUnixTimeMilliseconds();
            var cidade = new LinhaDeChat(CanalDaLinha.Cidade, 2, "Ana", "", "oi", hora, false);
            var grupo = new LinhaDeChat(CanalDaLinha.Grupo, 2, "Ana", "", "bora", hora, false);
            var recebido = new LinhaDeChat(
                CanalDaLinha.Sussurro,
                2,
                "Ana",
                "Eu",
                "psiu",
                hora,
                false
            );
            var enviado = new LinhaDeChat(
                CanalDaLinha.Sussurro,
                1,
                "Eu",
                "Ana",
                "e aí",
                hora,
                true
            );

            Assert.AreEqual("[14:05] [Cidade] Ana: oi", TextosDeChat.Linha(cidade, TimeSpan.Zero));
            Assert.AreEqual("[14:05] [Grupo] Ana: bora", TextosDeChat.Linha(grupo, TimeSpan.Zero));
            Assert.AreEqual(
                "[14:05] [Sussurro] Ana sussurra: psiu",
                TextosDeChat.Linha(recebido, TimeSpan.Zero)
            );
            Assert.AreEqual(
                "[14:05] [Sussurro] Para Ana: e aí",
                TextosDeChat.Linha(enviado, TimeSpan.Zero)
            );
            Assert.AreEqual(
                "[11:05] [Cidade] Ana: oi",
                TextosDeChat.Linha(cidade, TimeSpan.FromHours(-3))
            );
        }

        [Test]
        public void Hora_ForaDoIntervalo_NaoQuebra()
        {
            Assert.AreEqual("--:--", TextosDeChat.Hora(long.MaxValue, TimeSpan.Zero));
        }

        [Test]
        public void Painel_MostraTextoComMarcasComoTextoPuro_ESoAsUltimas8Linhas()
        {
            var regiao = new VisualElement();
            var painel = new PainelDeChat(regiao);
            var linhas = new List<LinhaDeChat>();
            for (var i = 0; i < 10; i++)
            {
                linhas.Add(LinhaDeChat.Sistema(i == 9 ? "<color=red>oi</color>" : "l" + i, 0));
            }

            painel.Mostrar(linhas, TimeSpan.Zero);

            var rotulos = painel.Raiz.Query<Label>(className: "chat-linha").ToList();
            Assert.AreEqual(PainelDeChat.LinhasVisiveis, rotulos.Count);
            foreach (var rotulo in rotulos)
            {
                Assert.IsFalse(rotulo.enableRichText);
            }

            StringAssert.Contains("<color=red>oi</color>", rotulos[7].text);
            StringAssert.Contains("l2", rotulos[0].text);
        }

        [Test]
        public void Painel_ComecaFechadoEAbreEFecha()
        {
            var painel = new PainelDeChat(new VisualElement());

            Assert.IsFalse(painel.Aberta);
            painel.Abrir();
            Assert.IsTrue(painel.Aberta);
            painel.Texto = "algo";
            painel.Fechar();
            Assert.IsFalse(painel.Aberta);
            Assert.AreEqual("", painel.Texto);
        }

        [Test]
        public void Atividade_EsmaeceAposOsSegundosSemAtividade_EAbertaNaoEsmaece()
        {
            var atividade = new AtividadeDoPainel(100.0);

            Assert.IsFalse(atividade.Esmaecido(109.9, false));
            Assert.IsTrue(atividade.Esmaecido(110.0, false));
            Assert.IsFalse(atividade.Esmaecido(500.0, true));

            atividade.Registrar(200.0);
            Assert.IsFalse(atividade.Esmaecido(205.0, false));
        }
    }
}
