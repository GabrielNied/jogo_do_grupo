using System;
using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Missoes;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Missoes
{
    /// <summary>
    /// Missões na conversa com o NPC, como o quadro de missão do WoW: abre junto do diálogo
    /// quando o NPC tem missão para entregar ou para aceitar, com o texto, os objetivos, as
    /// recompensas, a escolha do item e o botão. O client só mostra e pede; o servidor confere
    /// o alcance, os pré-requisitos e a escolha.
    /// </summary>
    public sealed class JanelaConversaDeMissao : JanelaDoJogo
    {
        public const string Chave = "missoes.conversa";
        public const string Titulo = "Missões";
        public const int Ordem = 92;

        private const string CaminhoEstilo = "Missoes/EstiloMissoes";

        // O servidor conta a conversa em paralelo ao diálogo: se a missão fica pronta logo
        // depois, a janela ainda abre sozinha dentro deste tempo.
        private const float JanelaDeAberturaSegundos = 5f;

        private enum Situacao
        {
            Entregar,
            Aceitar,
            EmAndamento,
        }

        private readonly struct Entrada
        {
            public readonly DefinicaoDeMissao Definicao;
            public readonly ProgressoDeMissaoDto Progresso;
            public readonly Situacao Situacao;

            public Entrada(
                DefinicaoDeMissao definicao,
                ProgressoDeMissaoDto progresso,
                Situacao situacao
            )
            {
                Definicao = definicao;
                Progresso = progresso;
                Situacao = situacao;
            }
        }

        private static IDisposable _assinatura;
        private static int _npcId;
        private static string _nomeDoNpc = string.Empty;
        private static float _conversaEm = float.NegativeInfinity;

        private readonly Dictionary<int, int> _escolhas = new Dictionary<int, int>();
        private ScrollView _lista;
        private bool _teveAcao;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _assinatura?.Dispose();
            _assinatura = null;
            _npcId = 0;
            _nomeDoNpc = string.Empty;
            _conversaEm = float.NegativeInfinity;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(
                    Chave,
                    Titulo,
                    null,
                    Ordem,
                    () => new JanelaConversaDeMissao()
                )
            );
            _assinatura = ConexaoTempoReal.Assinar<DialogoDoNpc>(
                NpcsMessageIds.DialogoDoNpc,
                AoAbrirDialogo
            );
            ControleDeMissoes.EstadoMudou += AoMudarEstado;
        }

        private static void AoAbrirDialogo(DialogoDoNpc dialogo)
        {
            if (dialogo == null)
            {
                return;
            }

            _npcId = dialogo.NpcId;
            _nomeDoNpc = dialogo.Nome ?? string.Empty;
            _conversaEm = Time.realtimeSinceStartup;
            if (TemAlgoParaFazer(_npcId))
            {
                if (JanelasDoJogo.EstaAberta(Chave))
                {
                    JanelasDoJogo.Fechar(Chave);
                }

                JanelasDoJogo.Abrir(Chave);
            }
            else
            {
                // Falar com outro NPC encerra as missões da conversa anterior.
                JanelasDoJogo.Fechar(Chave);
            }
        }

        private static void AoMudarEstado()
        {
            if (_npcId <= 0 || JanelasDoJogo.EstaAberta(Chave))
            {
                return;
            }

            if (Time.realtimeSinceStartup - _conversaEm > JanelaDeAberturaSegundos)
            {
                return;
            }

            if (TemAlgoParaFazer(_npcId))
            {
                JanelasDoJogo.Abrir(Chave);
            }
        }

        protected override void Montar(VisualElement conteudo)
        {
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
            {
                conteudo.styleSheets.Add(estilo);
            }

            conteudo.AddToClassList("missao-conversa");
            _lista = new ScrollView(ScrollViewMode.Vertical);
            _lista.AddToClassList("missao-conversa-lista");
            conteudo.Add(_lista);
        }

        protected override void AoAbrir()
        {
            ControleDeMissoes.EstadoMudou += Redesenhar;
            Redesenhar();
        }

        protected override void AoFechar()
        {
            ControleDeMissoes.EstadoMudou -= Redesenhar;
        }

        private void Redesenhar()
        {
            if (!Aberta || _lista == null)
            {
                return;
            }

            var entradas = Listar(_npcId);
            var temAcao = entradas.Exists(e => e.Situacao != Situacao.EmAndamento);
            // Entregou ou aceitou tudo: fecha como o quadro de missão do WoW. Aberta sem nada (o
            // quadro vazio, pela opção do diálogo), mostra o aviso em vez de sumir.
            if (!temAcao && _teveAcao)
            {
                JanelasDoJogo.Fechar(Chave);
                return;
            }

            _teveAcao |= temAcao;
            DefinirTitulo(string.IsNullOrEmpty(_nomeDoNpc) ? Titulo : _nomeDoNpc);
            _lista.Clear();
            if (entradas.Count == 0)
            {
                var vazio = new Label("Nenhuma missão por aqui agora.");
                vazio.AddToClassList(EstiloCore.Texto);
                vazio.AddToClassList("missao-conversa-texto");
                _lista.Add(vazio);
            }

            foreach (var entrada in entradas)
            {
                _lista.Add(Desenhar(entrada));
            }
        }

        private static bool TemAlgoParaFazer(int npcId)
        {
            foreach (var entrada in Listar(npcId))
            {
                if (entrada.Situacao != Situacao.EmAndamento)
                {
                    return true;
                }
            }

            return false;
        }

        // Para entregar primeiro, depois as que ele oferece e por fim as em andamento com ele.
        private static List<Entrada> Listar(int npcId)
        {
            var entradas = new List<Entrada>();
            if (npcId <= 0)
            {
                return entradas;
            }

            var ativas = ControleDeMissoes.Ativas;
            var ativasIds = new HashSet<int>();
            foreach (var progresso in ativas)
            {
                ativasIds.Add(progresso.MissaoId);
                var def = CatalogoDeMissoes.Obter(progresso.MissaoId);
                if (def != null && def.NpcEntregadorId == npcId)
                {
                    entradas.Add(
                        new Entrada(
                            def,
                            progresso,
                            progresso.Estado == EstadoDaMissao.ProntaParaEntregar
                                ? Situacao.Entregar
                                : Situacao.EmAndamento
                        )
                    );
                }
            }

            var nivel = PerfilDoJogador.Nivel > 0 ? PerfilDoJogador.Nivel : 1;
            foreach (var def in CatalogoDeMissoes.Todas)
            {
                if (!RegrasMissoes.OfereceNoNpc(def, npcId))
                {
                    continue;
                }

                if (
                    def.Tipo == TipoDeMissao.Diaria
                    && !ControleDeMissoes.RotacaoDiaria.Contains(def.Id)
                )
                {
                    continue;
                }

                if (
                    def.Tipo == TipoDeMissao.Semanal
                    && !ControleDeMissoes.RotacaoSemanal.Contains(def.Id)
                )
                {
                    continue;
                }

                if (
                    RegrasMissoes.PodeAceitar(
                        def,
                        nivel,
                        ControleDeMissoes.Concluidas,
                        ativasIds,
                        out _
                    )
                )
                {
                    entradas.Add(new Entrada(def, null, Situacao.Aceitar));
                }
            }

            entradas.Sort((a, b) => a.Situacao.CompareTo(b.Situacao));
            return entradas;
        }

        private VisualElement Desenhar(Entrada entrada)
        {
            var def = entrada.Definicao;
            var caixa = new VisualElement();
            caixa.AddToClassList("missao-conversa-item");

            var topo = new VisualElement();
            topo.AddToClassList("missao-conversa-topo");
            var simbolo = new Label(entrada.Situacao == Situacao.Aceitar ? "!" : "?");
            simbolo.AddToClassList("missao-conversa-simbolo");
            if (entrada.Situacao == Situacao.EmAndamento)
            {
                simbolo.AddToClassList("missao-conversa-simbolo-pendente");
            }

            topo.Add(simbolo);
            var titulo = new Label(def.Titulo);
            titulo.AddToClassList("missao-conversa-titulo");
            topo.Add(titulo);
            caixa.Add(topo);

            var situacao = new Label(TextoDaSituacao(entrada.Situacao, def));
            situacao.AddToClassList("missao-conversa-situacao");
            caixa.Add(situacao);

            var texto = new Label(TextoDaFala(entrada.Situacao, def));
            texto.AddToClassList(EstiloCore.Texto);
            texto.AddToClassList("missao-conversa-texto");
            caixa.Add(texto);

            if (entrada.Situacao != Situacao.Entregar && def.Objetivos.Length > 0)
            {
                caixa.Add(Secao("Objetivos"));
                foreach (var objetivo in def.Objetivos)
                {
                    caixa.Add(DesenharObjetivo(objetivo, entrada.Progresso));
                }
            }

            DesenharRecompensas(caixa, entrada);

            var acao = CriarAcao(entrada);
            if (acao != null)
            {
                caixa.Add(acao);
            }

            return caixa;
        }

        private static Label DesenharObjetivo(
            ObjetivoMissao objetivo,
            ProgressoDeMissaoDto progresso
        )
        {
            var feito = false;
            var quantidade = 0;
            if (progresso?.Objetivos != null)
            {
                var atual = Array.Find(progresso.Objetivos, o => o.ObjetivoId == objetivo.Id);
                if (atual != null)
                {
                    feito = atual.Concluido;
                    quantidade = atual.QuantidadeAtual;
                }
            }

            var texto =
                objetivo.QuantidadeTotal > 1
                    ? $"• {objetivo.Descricao} ({quantidade}/{objetivo.QuantidadeTotal})"
                    : $"• {objetivo.Descricao}";
            var linha = new Label(texto);
            linha.AddToClassList("missao-conversa-objetivo");
            if (feito)
            {
                linha.AddToClassList("missao-conversa-objetivo-feito");
            }

            return linha;
        }

        private void DesenharRecompensas(VisualElement caixa, Entrada entrada)
        {
            var def = entrada.Definicao;
            var partes = new List<string>();
            if (def.RecompensaXp > 0)
            {
                partes.Add($"{def.RecompensaXp} de experiência");
            }

            if (def.RecompensaCoroas > 0)
            {
                partes.Add($"{def.RecompensaCoroas} coroas");
            }

            if (!string.IsNullOrEmpty(def.ReputacaoFaccao) && def.ReputacaoValor != 0)
            {
                partes.Add($"+{def.ReputacaoValor} de reputação ({def.ReputacaoFaccao})");
            }

            var itens = def.ItensParaEscolha ?? Array.Empty<int>();
            if (partes.Count == 0 && itens.Length == 0)
            {
                return;
            }

            caixa.Add(Secao("Recompensas"));
            if (partes.Count > 0)
            {
                var valores = new Label(string.Join(" · ", partes));
                valores.AddToClassList("missao-conversa-recompensa");
                caixa.Add(valores);
            }

            if (itens.Length == 0)
            {
                return;
            }

            var escolher = entrada.Situacao == Situacao.Entregar && itens.Length > 1;
            if (itens.Length == 1)
            {
                // Um item só: vai junto, sem escolha (o servidor ainda pede o id na entrega).
                _escolhas[def.Id] = itens[0];
            }

            var rotulo = new Label(
                itens.Length > 1 ? "Escolha uma recompensa:" : "Você também recebe:"
            );
            rotulo.AddToClassList("missao-conversa-rotulo");
            caixa.Add(rotulo);

            var linha = new VisualElement();
            linha.AddToClassList("missao-conversa-escolhas");
            foreach (var itemId in itens)
            {
                var id = itemId;
                if (escolher)
                {
                    var opcao = new Button(() =>
                    {
                        _escolhas[def.Id] = id;
                        Redesenhar();
                    })
                    {
                        text = NomeDoItem(id),
                    };
                    opcao.AddToClassList(EstiloCore.Botao);
                    opcao.AddToClassList("missao-conversa-escolha");
                    if (_escolhas.TryGetValue(def.Id, out var marcado) && marcado == id)
                    {
                        opcao.AddToClassList("missao-conversa-escolha-marcada");
                    }

                    linha.Add(opcao);
                }
                else
                {
                    var nome = new Label(NomeDoItem(id));
                    nome.AddToClassList("missao-conversa-premio");
                    linha.Add(nome);
                }
            }

            caixa.Add(linha);
        }

        private Button CriarAcao(Entrada entrada)
        {
            var def = entrada.Definicao;
            Button botao;
            if (entrada.Situacao == Situacao.Aceitar)
            {
                botao = new Button(() => ControleDeMissoes.Aceitar(def.Id))
                {
                    text = "Aceitar missão",
                };
            }
            else if (entrada.Situacao == Situacao.Entregar)
            {
                var itens = def.ItensParaEscolha ?? Array.Empty<int>();
                botao = new Button(() =>
                {
                    _escolhas.TryGetValue(def.Id, out var escolhido);
                    ControleDeMissoes.Entregar(def.Id, escolhido);
                })
                {
                    text = "Entregar missão",
                };
                botao.SetEnabled(itens.Length == 0 || _escolhas.ContainsKey(def.Id));
            }
            else
            {
                return null;
            }

            botao.AddToClassList(EstiloCore.Botao);
            botao.AddToClassList(EstiloCore.BotaoPrincipal);
            botao.AddToClassList("missao-conversa-acao");
            return botao;
        }

        private static Label Secao(string texto)
        {
            var secao = new Label(texto);
            secao.AddToClassList("missao-conversa-secao");
            return secao;
        }

        private static string TextoDaFala(Situacao situacao, DefinicaoDeMissao def)
        {
            var fala = situacao switch
            {
                Situacao.Aceitar => def.TextoAoAceitar,
                Situacao.Entregar => def.TextoAoEntregar,
                _ => "",
            };
            return string.IsNullOrWhiteSpace(fala) ? def.Descricao : fala;
        }

        private static string TextoDaSituacao(Situacao situacao, DefinicaoDeMissao def)
        {
            switch (situacao)
            {
                case Situacao.Entregar:
                    return "Pronta para entregar";
                case Situacao.EmAndamento:
                    return "Em andamento";
                default:
                    var tipo =
                        def.Tipo == TipoDeMissao.Principal ? "Missão principal"
                        : def.Tipo == TipoDeMissao.Diaria ? "Missão diária"
                        : def.Tipo == TipoDeMissao.Semanal ? "Missão semanal"
                        : "Missão disponível";
                    return $"{tipo} · nível {Math.Max(1, def.NivelRecomendado)}";
            }
        }

        /// <summary>Nome do item da recompensa: equipamento, item do catálogo ou o id.</summary>
        internal static string NomeDoItem(int itemId)
        {
            var equipamento = CatalogoEquipamentos.Obter(itemId);
            if (equipamento != null && !string.IsNullOrEmpty(equipamento.Nome))
            {
                return equipamento.Nome;
            }

            return CatalogoItens.ObterNome(itemId) ?? $"Item {itemId}";
        }
    }
}
