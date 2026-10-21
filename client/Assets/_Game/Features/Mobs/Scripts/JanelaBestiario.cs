using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Janela do Bestiário de Asterra (tecla B) nas <see cref="JanelasDoJogo"/> (#922).
    /// </summary>
    public sealed class JanelaBestiario : JanelaDoJogo
    {
        public const string Chave = "mobs.bestiario";
        public const string Titulo = "Bestiário de Asterra";
        public const string Tecla = "H";
        public const int Ordem = 40;

        private enum FiltroCategoria
        {
            Todas,
            Comum,
            Chefe,
            ChefeDeMundo,
        }

        private FiltroCategoria _filtro = FiltroCategoria.Todas;
        private int _criaturaSelecionadaId = 1;

        private VisualElement _cabecalhoAbas;
        private Label _resumoStatus;
        private ScrollView _listaCriaturas;
        private VisualElement _painelDetalhes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaBestiario());
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.AddToClassList("bestiario-janela");
            conteudo.style.flexDirection = FlexDirection.Column;
            conteudo.style.flexGrow = 1f;

            // Cabeçalho com abas e estatísticas de progresso
            var barraSuperior = new VisualElement();
            barraSuperior.style.flexDirection = FlexDirection.Row;
            barraSuperior.style.justifyContent = Justify.SpaceBetween;
            barraSuperior.style.alignItems = Align.Center;
            barraSuperior.style.marginBottom = 10;
            conteudo.Add(barraSuperior);

            _cabecalhoAbas = new VisualElement();
            _cabecalhoAbas.style.flexDirection = FlexDirection.Row;
            barraSuperior.Add(_cabecalhoAbas);

            _resumoStatus = new Label();
            _resumoStatus.style.fontSize = 12;
            _resumoStatus.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            barraSuperior.Add(_resumoStatus);

            // Corpo dividido em duas colunas (lista à esquerda, ficha à direita)
            var corpo = new VisualElement();
            corpo.style.flexDirection = FlexDirection.Row;
            corpo.style.flexGrow = 1f;
            conteudo.Add(corpo);

            _listaCriaturas = new ScrollView();
            _listaCriaturas.style.width = 280;
            _listaCriaturas.style.marginRight = 12;
            _listaCriaturas.style.borderRightWidth = 1;
            _listaCriaturas.style.borderRightColor = new Color(0.25f, 0.25f, 0.3f, 0.8f);
            corpo.Add(_listaCriaturas);

            _painelDetalhes = new VisualElement();
            _painelDetalhes.style.flexGrow = 1f;
            _painelDetalhes.style.paddingLeft = 8;
            corpo.Add(_painelDetalhes);

            CriarBotoesDeFiltro();
            AtualizarResumo();
            PreencherLista();
            ExibirDetalhes(_criaturaSelecionadaId);

            ControleDeBestiario.EstadoMudou += AoEstadoMudou;
        }

        private void AoEstadoMudou()
        {
            AtualizarResumo();
            PreencherLista();
            ExibirDetalhes(_criaturaSelecionadaId);
        }

        private void CriarBotoesDeFiltro()
        {
            _cabecalhoAbas.Clear();

            AdicionarBotaoFiltro("Todas", FiltroCategoria.Todas);
            AdicionarBotaoFiltro("Comuns", FiltroCategoria.Comum);
            AdicionarBotaoFiltro("Chefes", FiltroCategoria.Chefe);
            AdicionarBotaoFiltro("Chefes de Mundo", FiltroCategoria.ChefeDeMundo);
        }

        private void AdicionarBotaoFiltro(string texto, FiltroCategoria filtro)
        {
            var btn = new Button(() =>
            {
                _filtro = filtro;
                CriarBotoesDeFiltro();
                PreencherLista();
            })
            {
                text = texto,
            };

            btn.style.marginRight = 6;
            btn.style.paddingTop = 4;
            btn.style.paddingBottom = 4;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 12;
            btn.style.borderTopLeftRadius = 4;
            btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4;
            btn.style.borderBottomRightRadius = 4;

            bool ativo = _filtro == filtro;
            btn.style.backgroundColor = ativo
                ? new Color(0.28f, 0.26f, 0.22f, 1f)
                : new Color(0.15f, 0.15f, 0.18f, 1f);
            btn.style.color = ativo
                ? new Color(0.95f, 0.82f, 0.45f, 1f)
                : new Color(0.65f, 0.65f, 0.7f, 1f);
            btn.style.borderBottomWidth = ativo ? 2 : 1;
            btn.style.borderBottomColor = ativo
                ? new Color(0.95f, 0.82f, 0.45f, 1f)
                : new Color(0.25f, 0.25f, 0.3f, 1f);

            _cabecalhoAbas.Add(btn);
        }

        private void AtualizarResumo()
        {
            int total = CatalogoCriaturas.Todas.Length;
            int descobertas = 0;
            int mestres = 0;

            for (int i = 0; i < CatalogoCriaturas.Todas.Length; i++)
            {
                var entrada = ControleDeBestiario.ObterEntrada(CatalogoCriaturas.Todas[i].Id);
                if (entrada.Nivel > NivelDeConhecimentoBestiario.Desconhecido)
                    descobertas++;
                if (entrada.Nivel == NivelDeConhecimentoBestiario.Mestre)
                    mestres++;
            }

            _resumoStatus.text = $"Descobertas: {descobertas}/{total}  ·  Mestres: {mestres}";
        }

        private void PreencherLista()
        {
            _listaCriaturas.Clear();

            var criaturas = CatalogoCriaturas.Todas;
            bool primeiraSelecionada = false;

            for (int i = 0; i < criaturas.Length; i++)
            {
                var criatura = criaturas[i];

                if (!CorrespondeAoFiltro(criatura.Categoria, _filtro))
                    continue;

                if (!primeiraSelecionada && _criaturaSelecionadaId == 0)
                {
                    _criaturaSelecionadaId = criatura.Id;
                    primeiraSelecionada = true;
                }

                var entrada = ControleDeBestiario.ObterEntrada(criatura.Id);
                var itemCard = CriarItemLista(criatura, entrada);
                _listaCriaturas.Add(itemCard);
            }
        }

        private bool CorrespondeAoFiltro(CategoriaDeCriatura categoria, FiltroCategoria filtro)
        {
            switch (filtro)
            {
                case FiltroCategoria.Comum:
                    return categoria == CategoriaDeCriatura.Comum;
                case FiltroCategoria.Chefe:
                    return categoria == CategoriaDeCriatura.Chefe
                        || categoria == CategoriaDeCriatura.ChefeRaro;
                case FiltroCategoria.ChefeDeMundo:
                    return categoria == CategoriaDeCriatura.ChefeDeMundo;
                default:
                    return true;
            }
        }

        private VisualElement CriarItemLista(
            DefinicaoDeCriatura criatura,
            EntradaBestiarioDto entrada
        )
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Column;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.marginBottom = 6;
            card.style.borderLeftWidth = 4;
            card.style.borderTopLeftRadius = 4;
            card.style.borderBottomLeftRadius = 4;
            card.style.borderTopRightRadius = 4;
            card.style.borderBottomRightRadius = 4;

            bool selecionada = criatura.Id == _criaturaSelecionadaId;
            card.style.backgroundColor = selecionada
                ? new Color(0.24f, 0.22f, 0.2f, 1f)
                : new Color(0.14f, 0.14f, 0.17f, 1f);

            Color corBorda = ObterCorDoNivel(entrada.Nivel);
            card.style.borderLeftColor = corBorda;

            card.RegisterCallback<ClickEvent>(_ =>
            {
                _criaturaSelecionadaId = criatura.Id;
                PreencherLista();
                ExibirDetalhes(criatura.Id);
            });

            // Linha superior: Nome e Nível de Conhecimento
            var linhaTopo = new VisualElement();
            linhaTopo.style.flexDirection = FlexDirection.Row;
            linhaTopo.style.justifyContent = Justify.SpaceBetween;
            linhaTopo.style.alignItems = Align.Center;
            card.Add(linhaTopo);

            var nomeLabel = new Label();
            nomeLabel.style.fontSize = 13;
            nomeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            if (entrada.Nivel == NivelDeConhecimentoBestiario.Desconhecido)
            {
                nomeLabel.text = "??? Desconhecido";
                nomeLabel.style.color = new Color(0.5f, 0.5f, 0.55f, 1f);
            }
            else
            {
                nomeLabel.text = criatura.Nome;
                nomeLabel.style.color = new Color(0.9f, 0.9f, 0.92f, 1f);
            }
            linhaTopo.Add(nomeLabel);

            var badgeNivel = new Label(entrada.Nivel.ToString().ToUpperInvariant());
            badgeNivel.style.fontSize = 10;
            badgeNivel.style.color = corBorda;
            badgeNivel.style.unityFontStyleAndWeight = FontStyle.Bold;
            linhaTopo.Add(badgeNivel);

            // Linha inferior: Categoria e Derrotas
            var linhaSub = new VisualElement();
            linhaSub.style.flexDirection = FlexDirection.Row;
            linhaSub.style.justifyContent = Justify.SpaceBetween;
            linhaSub.style.marginTop = 3;
            card.Add(linhaSub);

            var catLabel = new Label(FormatarCategoria(criatura.Categoria));
            catLabel.style.fontSize = 11;
            catLabel.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            linhaSub.Add(catLabel);

            var derrotasLabel = new Label($"{entrada.Derrotas} derrotas");
            derrotasLabel.style.fontSize = 11;
            derrotasLabel.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            linhaSub.Add(derrotasLabel);

            return card;
        }

        private void ExibirDetalhes(int criaturaId)
        {
            _painelDetalhes.Clear();

            var criatura = CatalogoCriaturas.Obter(criaturaId);
            if (criatura == null)
            {
                var vazio = new Label("Selecione uma espécie para ver sua ficha no bestiário.");
                vazio.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
                vazio.style.marginTop = 20;
                _painelDetalhes.Add(vazio);
                return;
            }

            var entrada = ControleDeBestiario.ObterEntrada(criaturaId);
            var lore = CatalogoBestiario.Obter(criaturaId);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            _painelDetalhes.Add(scroll);

            // Bloco de cabeçalho da ficha
            var header = new VisualElement();
            header.style.paddingBottom = 10;
            header.style.marginBottom = 10;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.25f, 0.25f, 0.3f, 0.6f);
            scroll.Add(header);

            var tituloLinha = new VisualElement();
            tituloLinha.style.flexDirection = FlexDirection.Row;
            tituloLinha.style.justifyContent = Justify.SpaceBetween;
            tituloLinha.style.alignItems = Align.Center;
            header.Add(tituloLinha);

            var tituloFicha = new Label(
                entrada.Nivel == NivelDeConhecimentoBestiario.Desconhecido
                    ? "??? Criatura Não Identificada"
                    : criatura.Nome
            );
            tituloFicha.style.fontSize = 20;
            tituloFicha.style.unityFontStyleAndWeight = FontStyle.Bold;
            tituloFicha.style.color = ObterCorDoNivel(entrada.Nivel);
            tituloLinha.Add(tituloFicha);

            var pillNivel = new Label($"NÍVEL {entrada.Nivel.ToString().ToUpperInvariant()}");
            pillNivel.style.fontSize = 11;
            pillNivel.style.paddingLeft = 8;
            pillNivel.style.paddingRight = 8;
            pillNivel.style.paddingTop = 2;
            pillNivel.style.paddingBottom = 2;
            pillNivel.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
            pillNivel.style.color = ObterCorDoNivel(entrada.Nivel);
            pillNivel.style.borderTopLeftRadius = 4;
            pillNivel.style.borderTopRightRadius = 4;
            pillNivel.style.borderBottomLeftRadius = 4;
            pillNivel.style.borderBottomRightRadius = 4;
            tituloLinha.Add(pillNivel);

            var subtitulo = new Label(
                $"{FormatarCategoria(criatura.Categoria)} · Nível {criatura.Nivel} de Força"
            );
            subtitulo.style.fontSize = 12;
            subtitulo.style.color = new Color(0.65f, 0.65f, 0.7f, 1f);
            subtitulo.style.marginTop = 2;
            header.Add(subtitulo);

            // Seção de barra de progresso para o próximo nível
            var progressoSecao = new VisualElement();
            progressoSecao.style.marginBottom = 14;
            scroll.Add(progressoSecao);

            RegrasBestiario.ObterProgressoNoNivel(
                criatura.Categoria,
                entrada.Derrotas,
                out int progresso,
                out int meta
            );

            var textoProgresso = new Label();
            textoProgresso.style.fontSize = 11;
            textoProgresso.style.marginBottom = 3;
            textoProgresso.style.color = new Color(0.75f, 0.75f, 0.8f, 1f);

            if (entrada.Nivel == NivelDeConhecimentoBestiario.Mestre)
            {
                textoProgresso.text =
                    $"Conhecimento Completo (Mestre) · {entrada.Derrotas} derrotas totais registradas";
            }
            else
            {
                int proximaMeta = RegrasBestiario.DerrotasParaProximoNivel(
                    criatura.Categoria,
                    entrada.Derrotas
                );
                textoProgresso.text =
                    $"Progresso: {entrada.Derrotas} / {proximaMeta} derrotas para o próximo nível";
            }
            progressoSecao.Add(textoProgresso);

            var barraFundo = new VisualElement();
            barraFundo.style.height = 8;
            barraFundo.style.backgroundColor = new Color(0.18f, 0.18f, 0.22f, 1f);
            barraFundo.style.borderTopLeftRadius = 4;
            barraFundo.style.borderTopRightRadius = 4;
            barraFundo.style.borderBottomLeftRadius = 4;
            barraFundo.style.borderBottomRightRadius = 4;
            progressoSecao.Add(barraFundo);

            var barraPreenchimento = new VisualElement();
            barraPreenchimento.style.height = 8;
            barraPreenchimento.style.backgroundColor = ObterCorDoNivel(entrada.Nivel);
            barraPreenchimento.style.borderTopLeftRadius = 4;
            barraPreenchimento.style.borderBottomLeftRadius = 4;

            float porcentagem = meta > 0 ? Mathf.Clamp01((float)progresso / meta) * 100f : 100f;
            barraPreenchimento.style.width = Length.Percent(porcentagem);
            barraFundo.Add(barraPreenchimento);

            // Nível 1 (Avistado): Habitat e Descrição Geral
            if (entrada.Nivel >= NivelDeConhecimentoBestiario.Avistado)
            {
                AdicionarCardInformativo(
                    scroll,
                    "Habitat & Região",
                    lore?.Regiao ?? "Terras selvagens",
                    lore?.DescricaoGeral ?? "Nenhuma descrição disponível."
                );
            }
            else
            {
                AdicionarCardBloqueado(
                    scroll,
                    "Habitat & Região",
                    "Derrote esta criatura 1 vez para registrar seu habitat e identidade."
                );
            }

            // Nível 2 (Estudado): Atributos e Sinais de Ataque
            if (entrada.Nivel >= NivelDeConhecimentoBestiario.Estudado)
            {
                string atributosTexto =
                    $"Vida Base: {criatura.VidaBase:N0}  ·  Dano Base: {criatura.DanoBase}  ·  XP: {criatura.ExperienciaBase}";
                AdicionarCardInformativo(
                    scroll,
                    "Comportamento & Sinais de Ataque",
                    atributosTexto,
                    lore?.SinaisDeAtaque ?? "Ataque padrão direto."
                );
            }
            else
            {
                int req = RegrasBestiario.DerrotasParaNivel(
                    criatura.Categoria,
                    NivelDeConhecimentoBestiario.Estudado
                );
                AdicionarCardBloqueado(
                    scroll,
                    "Comportamento & Atributos",
                    $"Requer nível Estudado ({req} derrotas) para revelar vida, dano e padrões de ataque."
                );
            }

            // Nível 3 (Mestre): Fraquezas, Drops e Frase de Lore
            if (entrada.Nivel >= NivelDeConhecimentoBestiario.Mestre)
            {
                AdicionarCardInformativo(
                    scroll,
                    "Fraquezas & Pontos Fracos",
                    "Estratégia comprovada",
                    lore?.Fraquezas ?? "Nenhum ponto fraco evidente."
                );
                AdicionarCardInformativo(
                    scroll,
                    "Espólios & Drops Obtidos",
                    "Itens documentados",
                    lore?.DropsConhecidos ?? "Materiais diversos."
                );

                if (!string.IsNullOrEmpty(lore?.FraseBestiario))
                {
                    var fraseCard = new VisualElement();
                    fraseCard.style.paddingTop = 10;
                    fraseCard.style.paddingBottom = 10;
                    fraseCard.style.paddingLeft = 14;
                    fraseCard.style.paddingRight = 14;
                    fraseCard.style.marginTop = 8;
                    fraseCard.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.8f);
                    fraseCard.style.borderLeftWidth = 3;
                    fraseCard.style.borderLeftColor = new Color(0.95f, 0.82f, 0.45f, 1f);

                    var fraseLabel = new Label($"\"{lore.FraseBestiario}\"");
                    fraseLabel.style.fontSize = 12;
                    fraseLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                    fraseLabel.style.color = new Color(0.9f, 0.85f, 0.7f, 1f);
                    fraseLabel.style.whiteSpace = WhiteSpace.Normal;
                    fraseCard.Add(fraseLabel);

                    scroll.Add(fraseCard);
                }
            }
            else
            {
                int req = RegrasBestiario.DerrotasParaNivel(
                    criatura.Categoria,
                    NivelDeConhecimentoBestiario.Mestre
                );
                AdicionarCardBloqueado(
                    scroll,
                    "Fraquezas, Drops e Lore Avançado",
                    $"Requer nível Mestre ({req} derrotas) para revelar fraquezas táticas e drops raros."
                );
            }
        }

        private void AdicionarCardInformativo(
            VisualElement container,
            string titulo,
            string subtitulo,
            string corpo
        )
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.13f, 0.13f, 0.16f, 1f);
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.marginBottom = 8;
            card.style.borderTopLeftRadius = 4;
            card.style.borderTopRightRadius = 4;
            card.style.borderBottomLeftRadius = 4;
            card.style.borderBottomRightRadius = 4;

            var t = new Label(titulo);
            t.style.fontSize = 12;
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            t.style.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            card.Add(t);

            if (!string.IsNullOrEmpty(subtitulo))
            {
                var s = new Label(subtitulo);
                s.style.fontSize = 11;
                s.style.color = new Color(0.9f, 0.78f, 0.45f, 1f);
                s.style.marginBottom = 4;
                card.Add(s);
            }

            var c = new Label(corpo);
            c.style.fontSize = 12;
            c.style.color = new Color(0.72f, 0.72f, 0.76f, 1f);
            c.style.whiteSpace = WhiteSpace.Normal;
            card.Add(c);

            container.Add(card);
        }

        private void AdicionarCardBloqueado(VisualElement container, string titulo, string dica)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.6f);
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.marginBottom = 8;
            card.style.borderLeftWidth = 2;
            card.style.borderLeftColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);
            card.style.borderTopLeftRadius = 4;
            card.style.borderTopRightRadius = 4;
            card.style.borderBottomLeftRadius = 4;
            card.style.borderBottomRightRadius = 4;

            var t = new Label($"🔒 {titulo} (Bloqueado)");
            t.style.fontSize = 12;
            t.style.color = new Color(0.55f, 0.55f, 0.6f, 1f);
            card.Add(t);

            var d = new Label(dica);
            d.style.fontSize = 11;
            d.style.color = new Color(0.45f, 0.45f, 0.5f, 1f);
            d.style.marginTop = 2;
            d.style.whiteSpace = WhiteSpace.Normal;
            card.Add(d);

            container.Add(card);
        }

        private static Color ObterCorDoNivel(NivelDeConhecimentoBestiario nivel)
        {
            switch (nivel)
            {
                case NivelDeConhecimentoBestiario.Mestre:
                    return new Color(0.95f, 0.82f, 0.25f, 1f); // Dourado
                case NivelDeConhecimentoBestiario.Estudado:
                    return new Color(0.45f, 0.75f, 0.95f, 1f); // Azul Claro
                case NivelDeConhecimentoBestiario.Avistado:
                    return new Color(0.85f, 0.55f, 0.25f, 1f); // Bronze/Âmbar
                default:
                    return new Color(0.45f, 0.45f, 0.5f, 1f); // Cinza
            }
        }

        private static string FormatarCategoria(CategoriaDeCriatura categoria)
        {
            switch (categoria)
            {
                case CategoriaDeCriatura.Chefe:
                    return "Chefe de Área";
                case CategoriaDeCriatura.ChefeRaro:
                    return "Chefe Raro";
                case CategoriaDeCriatura.ChefeDeMundo:
                    return "Chefe de Mundo";
                default:
                    return "Criatura Comum";
            }
        }
    }
}
