using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Missoes;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Missoes
{
    /// <summary>
    /// Registro e diário de missões (tecla L) nas <see cref="JanelasDoJogo"/> (#179, #1061).
    /// </summary>
    public sealed class JanelaMissoes : JanelaDoJogo
    {
        public const string Chave = "missoes.registro";
        public const string Titulo = "Diário de Missões";
        public const string Tecla = "L";
        public const int Ordem = 30;

        private enum AbaAtual
        {
            Ativas,
            Concluidas,
        }

        private AbaAtual _aba = AbaAtual.Ativas;
        private int _missaoSelecionadaId = 0;

        // Item escolhido por missão para a entrega (missões com recompensa à escolha).
        private readonly Dictionary<int, int> _escolhas = new Dictionary<int, int>();

        private VisualElement _cabecalhoAbas;
        private ScrollView _listaMissoes;
        private VisualElement _painelDetalhes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaMissoes());
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.AddToClassList("missoes-janela");
            conteudo.style.flexDirection = FlexDirection.Column;
            conteudo.style.flexGrow = 1f;

            // Abas
            _cabecalhoAbas = new VisualElement();
            _cabecalhoAbas.style.flexDirection = FlexDirection.Row;
            _cabecalhoAbas.style.marginBottom = 10;
            conteudo.Add(_cabecalhoAbas);

            // Área principal dividida em duas colunas
            var corpo = new VisualElement();
            corpo.style.flexDirection = FlexDirection.Row;
            corpo.style.flexGrow = 1f;
            conteudo.Add(corpo);

            // Coluna esquerda: Lista de missões
            _listaMissoes = ComponentesVisuais.PainelRolavel();
            _listaMissoes.style.width = 240;
            _listaMissoes.style.marginRight = 12;
            corpo.Add(_listaMissoes);

            // Coluna direita: Detalhes da missão selecionada
            _painelDetalhes = ComponentesVisuais.PainelRolavel();
            _painelDetalhes.style.flexGrow = 1f;
            corpo.Add(_painelDetalhes);

            Redesenhar();
        }

        protected override void AoAbrir()
        {
            ControleDeMissoes.EstadoMudou += Redesenhar;
            ControleDeMissoes.Consultar();
            Redesenhar();
        }

        protected override void AoFechar()
        {
            ControleDeMissoes.EstadoMudou -= Redesenhar;
        }

        private void Redesenhar()
        {
            if (_cabecalhoAbas == null || !Aberta)
                return;

            DesenharAbas();
            DesenharLista();
            DesenharDetalhes();
        }

        private void DesenharAbas()
        {
            _cabecalhoAbas.Clear();

            var btnAtivas = ComponentesVisuais.Botao(
                $"Em Andamento ({ControleDeMissoes.Ativas.Count})",
                () =>
                {
                    _aba = AbaAtual.Ativas;
                    Redesenhar();
                },
                principal: _aba == AbaAtual.Ativas
            );
            btnAtivas.style.marginRight = 8;
            _cabecalhoAbas.Add(btnAtivas);

            var btnConcluidas = ComponentesVisuais.Botao(
                $"Concluídas ({ControleDeMissoes.Concluidas.Count})",
                () =>
                {
                    _aba = AbaAtual.Concluidas;
                    Redesenhar();
                },
                principal: _aba == AbaAtual.Concluidas
            );
            _cabecalhoAbas.Add(btnConcluidas);
        }

        private void DesenharLista()
        {
            _listaMissoes.Clear();

            if (_aba == AbaAtual.Ativas)
            {
                var ativas = ControleDeMissoes.Ativas;
                if (ativas.Count == 0)
                {
                    _listaMissoes.Add(ComponentesVisuais.Texto("Nenhuma missão ativa no momento."));
                    _missaoSelecionadaId = 0;
                    return;
                }

                if (
                    _missaoSelecionadaId == 0
                    || ControleDeMissoes.ObterAtiva(_missaoSelecionadaId) == null
                )
                {
                    _missaoSelecionadaId = ativas[0].MissaoId;
                }

                for (int i = 0; i < ativas.Count; i++)
                {
                    var prog = ativas[i];
                    var def = CatalogoDeMissoes.Obter(prog.MissaoId);
                    var titulo = def?.Titulo ?? $"Missão #{prog.MissaoId}";

                    var cartao = new Button(() =>
                    {
                        _missaoSelecionadaId = prog.MissaoId;
                        DesenharLista();
                        DesenharDetalhes();
                    });
                    cartao.AddToClassList("ui-botao");
                    cartao.style.flexDirection = FlexDirection.Column;
                    cartao.style.alignItems = Align.FlexStart;
                    cartao.style.marginBottom = 6;
                    cartao.style.paddingLeft = 8;
                    cartao.style.paddingRight = 8;
                    cartao.style.paddingTop = 6;
                    cartao.style.paddingBottom = 6;

                    if (prog.MissaoId == _missaoSelecionadaId)
                    {
                        cartao.style.borderLeftColor = new StyleColor(new Color(0.9f, 0.75f, 0.2f));
                        cartao.style.borderLeftWidth = 4;
                    }

                    var lblTitulo = ComponentesVisuais.Texto(titulo, subtitulo: true);
                    lblTitulo.style.whiteSpace = WhiteSpace.Normal;
                    cartao.Add(lblTitulo);

                    var subInfo = new VisualElement();
                    subInfo.style.flexDirection = FlexDirection.Row;
                    subInfo.style.marginTop = 2;

                    var tipoTexto = def != null ? ObterNomeTipo(def.Tipo) : "";
                    var lblTipo = ComponentesVisuais.Texto(tipoTexto);
                    lblTipo.style.fontSize = 11;
                    lblTipo.style.opacity = 0.75f;
                    subInfo.Add(lblTipo);

                    if (prog.Estado == EstadoDaMissao.ProntaParaEntregar)
                    {
                        var lblPronta = ComponentesVisuais.Texto(" • Pronta!");
                        lblPronta.style.fontSize = 11;
                        lblPronta.style.color = new StyleColor(new Color(0.3f, 0.9f, 0.3f));
                        subInfo.Add(lblPronta);
                    }
                    else if (prog.Acompanhada)
                    {
                        var lblRastreada = ComponentesVisuais.Texto(" • Rastreada");
                        lblRastreada.style.fontSize = 11;
                        lblRastreada.style.color = new StyleColor(new Color(0.4f, 0.7f, 1f));
                        subInfo.Add(lblRastreada);
                    }

                    cartao.Add(subInfo);
                    _listaMissoes.Add(cartao);
                }
            }
            else
            {
                var concluidas = ControleDeMissoes.Concluidas;
                if (concluidas.Count == 0)
                {
                    _listaMissoes.Add(ComponentesVisuais.Texto("Nenhuma missão concluída ainda."));
                    _missaoSelecionadaId = 0;
                    return;
                }

                foreach (var id in concluidas)
                {
                    var def = CatalogoDeMissoes.Obter(id);
                    var titulo = def?.Titulo ?? $"Missão #{id}";

                    var cartao = new Button(() =>
                    {
                        _missaoSelecionadaId = id;
                        DesenharLista();
                        DesenharDetalhes();
                    });
                    cartao.AddToClassList("ui-botao");
                    cartao.style.flexDirection = FlexDirection.Column;
                    cartao.style.alignItems = Align.FlexStart;
                    cartao.style.marginBottom = 6;

                    if (id == _missaoSelecionadaId)
                    {
                        cartao.style.borderLeftColor = new StyleColor(new Color(0.4f, 0.8f, 0.4f));
                        cartao.style.borderLeftWidth = 4;
                    }

                    var lblTitulo = ComponentesVisuais.Texto(titulo, subtitulo: true);
                    cartao.Add(lblTitulo);

                    var lblConcluida = ComponentesVisuais.Texto("Concluída ✔");
                    lblConcluida.style.fontSize = 11;
                    lblConcluida.style.color = new StyleColor(new Color(0.4f, 0.8f, 0.4f));
                    cartao.Add(lblConcluida);

                    _listaMissoes.Add(cartao);
                }
            }
        }

        private void DesenharDetalhes()
        {
            _painelDetalhes.Clear();

            if (_missaoSelecionadaId == 0)
            {
                _painelDetalhes.Add(
                    ComponentesVisuais.Texto("Selecione uma missão para ver os detalhes.")
                );
                return;
            }

            var def = CatalogoDeMissoes.Obter(_missaoSelecionadaId);
            if (def == null)
            {
                _painelDetalhes.Add(ComponentesVisuais.Texto("Detalhes da missão indisponíveis."));
                return;
            }

            var prog = ControleDeMissoes.ObterAtiva(_missaoSelecionadaId);
            var concluida = ControleDeMissoes.EstaConcluida(_missaoSelecionadaId);

            // Cabeçalho da missão
            var titulo = ComponentesVisuais.Texto(def.Titulo, titulo: true);
            titulo.style.marginBottom = 4;
            _painelDetalhes.Add(titulo);

            var meta = ComponentesVisuais.Texto(
                $"{ObterNomeTipo(def.Tipo)} • Nível {def.NivelRecomendado} • Região: {def.Regiao}"
            );
            meta.style.fontSize = 12;
            meta.style.opacity = 0.8f;
            meta.style.marginBottom = 12;
            _painelDetalhes.Add(meta);

            // Descrição da história
            var desc = ComponentesVisuais.Texto(def.Descricao);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = 16;
            _painelDetalhes.Add(desc);

            // Objetivos
            var tituloObjs = ComponentesVisuais.Texto("Objetivos", subtitulo: true);
            tituloObjs.style.marginBottom = 8;
            _painelDetalhes.Add(tituloObjs);

            for (int i = 0; i < def.Objetivos.Length; i++)
            {
                var objDef = def.Objetivos[i];
                var progObj =
                    prog?.Objetivos != null
                        ? Array.Find(prog.Objetivos, o => o.ObjetivoId == objDef.Id)
                        : null;

                var feito = concluida || (progObj != null && progObj.Concluido);
                var qtd = concluida
                    ? objDef.QuantidadeTotal
                    : (progObj != null ? progObj.QuantidadeAtual : 0);

                var linha = new VisualElement();
                linha.style.flexDirection = FlexDirection.Row;
                linha.style.alignItems = Align.Center;
                linha.style.marginBottom = 4;

                var icone = ComponentesVisuais.Texto(feito ? "[✔] " : "[ ] ");
                icone.style.color = feito
                    ? new StyleColor(new Color(0.3f, 0.9f, 0.3f))
                    : new StyleColor(Color.white);
                linha.Add(icone);

                var textoObj = ComponentesVisuais.Texto(
                    $"{objDef.Descricao} ({qtd}/{objDef.QuantidadeTotal})"
                );
                textoObj.style.color = feito
                    ? new StyleColor(new Color(0.7f, 0.9f, 0.7f))
                    : new StyleColor(Color.white);
                linha.Add(textoObj);

                _painelDetalhes.Add(linha);
            }

            // Recompensas
            var tituloRec = ComponentesVisuais.Texto("Recompensas", subtitulo: true);
            tituloRec.style.marginTop = 16;
            tituloRec.style.marginBottom = 8;
            _painelDetalhes.Add(tituloRec);

            var recs = new VisualElement();
            recs.style.flexDirection = FlexDirection.Row;
            recs.style.marginBottom = 16;

            if (def.RecompensaXp > 0)
            {
                var xp = ComponentesVisuais.Texto($"{def.RecompensaXp} de experiência");
                xp.style.marginRight = 12;
                recs.Add(xp);
            }

            if (def.RecompensaCoroas > 0)
            {
                var coroas = ComponentesVisuais.Texto($"{def.RecompensaCoroas} coroas");
                coroas.style.marginRight = 12;
                recs.Add(coroas);
            }

            if (!string.IsNullOrEmpty(def.ReputacaoFaccao))
            {
                var rep = ComponentesVisuais.Texto(
                    $"+{def.ReputacaoValor} de reputação ({def.ReputacaoFaccao})"
                );
                recs.Add(rep);
            }

            _painelDetalhes.Add(recs);

            // Itens da recompensa: na entrega com mais de um, o jogador escolhe aqui.
            var itens = def.ItensParaEscolha ?? Array.Empty<int>();
            var escolherAgora =
                prog != null
                && !concluida
                && prog.Estado == EstadoDaMissao.ProntaParaEntregar
                && itens.Length > 1;
            if (itens.Length > 0)
            {
                var rotuloItens = ComponentesVisuais.Texto(
                    itens.Length > 1 ? "Escolha uma recompensa ao entregar:" : "Você também recebe:"
                );
                rotuloItens.style.marginBottom = 6;
                _painelDetalhes.Add(rotuloItens);

                var linhaItens = new VisualElement();
                linhaItens.style.flexDirection = FlexDirection.Row;
                linhaItens.style.flexWrap = Wrap.Wrap;
                linhaItens.style.marginBottom = 16;
                foreach (var itemId in itens)
                {
                    var id = itemId;
                    var nome = JanelaConversaDeMissao.NomeDoItem(id);
                    if (escolherAgora)
                    {
                        var marcado = _escolhas.TryGetValue(def.Id, out var atual) && atual == id;
                        var opcao = ComponentesVisuais.Botao(
                            nome,
                            () =>
                            {
                                _escolhas[def.Id] = id;
                                Redesenhar();
                            },
                            principal: marcado
                        );
                        opcao.style.marginRight = 8;
                        opcao.style.marginBottom = 6;
                        linhaItens.Add(opcao);
                    }
                    else
                    {
                        var rotulo = ComponentesVisuais.Texto(nome);
                        rotulo.style.marginRight = 12;
                        linhaItens.Add(rotulo);
                    }
                }

                _painelDetalhes.Add(linhaItens);
            }

            // Ações (somente se ativa)
            if (prog != null && !concluida)
            {
                var acoes = new VisualElement();
                acoes.style.flexDirection = FlexDirection.Row;
                acoes.style.flexWrap = Wrap.Wrap;
                acoes.style.marginTop = 12;

                // Entregar (se pronta)
                if (prog.Estado == EstadoDaMissao.ProntaParaEntregar)
                {
                    var escolhido = 0;
                    if (itens.Length == 1)
                    {
                        escolhido = itens[0];
                    }
                    else if (itens.Length > 1)
                    {
                        _escolhas.TryGetValue(def.Id, out escolhido);
                    }

                    var btnEntregar = ComponentesVisuais.Botao(
                        "Entregar Missão",
                        () => ControleDeMissoes.Entregar(def.Id, escolhido),
                        principal: true
                    );
                    btnEntregar.SetEnabled(itens.Length == 0 || escolhido != 0);
                    btnEntregar.style.marginRight = 8;
                    acoes.Add(btnEntregar);
                }

                // Acompanhar / Desacompanhar
                var textoRastreio = prog.Acompanhada ? "Parar de Acompanhar" : "Acompanhar no HUD";
                var btnRastreio = ComponentesVisuais.Botao(
                    textoRastreio,
                    () => ControleDeMissoes.AlternarAcompanhamento(def.Id, !prog.Acompanhada)
                );
                btnRastreio.style.marginRight = 8;
                acoes.Add(btnRastreio);

                // Compartilhar com Grupo
                var btnCompartilhar = ComponentesVisuais.Botao(
                    "Compartilhar com Grupo",
                    () => ControleDeMissoes.Compartilhar(def.Id)
                );
                btnCompartilhar.style.marginRight = 8;
                acoes.Add(btnCompartilhar);

                // Abandonar
                if (def.Tipo != TipoDeMissao.Principal)
                {
                    var btnAbandonar = ComponentesVisuais.Botao(
                        "Abandonar",
                        () => ControleDeMissoes.Abandonar(def.Id)
                    );
                    btnAbandonar.style.backgroundColor = new StyleColor(
                        new Color(0.6f, 0.2f, 0.2f, 0.8f)
                    );
                    acoes.Add(btnAbandonar);
                }

                _painelDetalhes.Add(acoes);
            }
        }

        private static string ObterNomeTipo(TipoDeMissao tipo)
        {
            switch (tipo)
            {
                case TipoDeMissao.Principal:
                    return "Missão Principal";
                case TipoDeMissao.Secundaria:
                    return "Missão Secundária";
                case TipoDeMissao.Diaria:
                    return "Missão Diária";
                case TipoDeMissao.Semanal:
                    return "Missão Semanal";
                default:
                    return "Missão";
            }
        }
    }
}
