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
            EstiloMobs.Aplicar(conteudo);
            conteudo.AddToClassList("bestiario-janela");

            // Cabeçalho com abas e estatísticas de progresso
            var barraSuperior = new VisualElement();
            barraSuperior.AddToClassList("bestiario-barra-superior");
            conteudo.Add(barraSuperior);

            _cabecalhoAbas = new VisualElement();
            _cabecalhoAbas.AddToClassList("bestiario-abas");
            barraSuperior.Add(_cabecalhoAbas);

            _resumoStatus = new Label();
            _resumoStatus.AddToClassList("bestiario-resumo");
            _resumoStatus.style.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            barraSuperior.Add(_resumoStatus);

            // Corpo dividido em duas colunas (lista à esquerda, ficha à direita)
            var corpo = new VisualElement();
            corpo.AddToClassList("bestiario-corpo");
            conteudo.Add(corpo);

            _listaCriaturas = new ScrollView();
            _listaCriaturas.AddToClassList("bestiario-lista");
            _listaCriaturas.style.borderRightColor = new Color(0.25f, 0.25f, 0.3f, 0.8f);
            corpo.Add(_listaCriaturas);

            _painelDetalhes = new VisualElement();
            _painelDetalhes.AddToClassList("bestiario-detalhes");
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

            btn.AddToClassList("bestiario-aba");

            bool ativo = _filtro == filtro;
            btn.EnableInClassList("bestiario-aba-ativa", ativo);
            if (ativo)
                btn.style.color = new Color(0.95f, 0.82f, 0.45f, 1f);
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
            card.AddToClassList("bestiario-item");
            card.EnableInClassList("bestiario-item-selecionado", criatura.Id == _criaturaSelecionadaId);

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
            linhaTopo.AddToClassList("bestiario-item-topo");
            card.Add(linhaTopo);

            var nomeLabel = new Label();
            nomeLabel.AddToClassList("bestiario-item-nome");

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
            badgeNivel.AddToClassList("bestiario-item-nivel");
            badgeNivel.style.color = corBorda;
            linhaTopo.Add(badgeNivel);

            // Linha inferior: Categoria e Derrotas
            var linhaSub = new VisualElement();
            linhaSub.AddToClassList("bestiario-item-sub");
            card.Add(linhaSub);

            var catLabel = new Label(FormatarCategoria(criatura.Categoria));
            catLabel.AddToClassList("bestiario-item-texto");
            catLabel.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            linhaSub.Add(catLabel);

            var derrotasLabel = new Label($"{entrada.Derrotas} derrotas");
            derrotasLabel.AddToClassList("bestiario-item-texto");
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
                vazio.AddToClassList("bestiario-vazio");
                vazio.style.color = new Color(0.6f, 0.6f, 0.65f, 1f);
                _painelDetalhes.Add(vazio);
                return;
            }

            var entrada = ControleDeBestiario.ObterEntrada(criaturaId);
            var lore = CatalogoBestiario.Obter(criaturaId);

            var scroll = new ScrollView();
            scroll.AddToClassList("bestiario-ficha");
            _painelDetalhes.Add(scroll);

            // Bloco de cabeçalho da ficha
            var header = new VisualElement();
            header.AddToClassList("bestiario-ficha-topo");
            header.style.borderBottomColor = new Color(0.25f, 0.25f, 0.3f, 0.6f);
            scroll.Add(header);

            var tituloLinha = new VisualElement();
            tituloLinha.AddToClassList("bestiario-ficha-titulo-linha");
            header.Add(tituloLinha);

            var tituloFicha = new Label(
                entrada.Nivel == NivelDeConhecimentoBestiario.Desconhecido
                    ? "??? Criatura Não Identificada"
                    : criatura.Nome
            );
            tituloFicha.AddToClassList("bestiario-ficha-titulo");
            tituloFicha.style.color = ObterCorDoNivel(entrada.Nivel);
            tituloLinha.Add(tituloFicha);

            var pillNivel = new Label($"NÍVEL {entrada.Nivel.ToString().ToUpperInvariant()}");
            pillNivel.AddToClassList("bestiario-ficha-pill");
            pillNivel.style.color = ObterCorDoNivel(entrada.Nivel);
            tituloLinha.Add(pillNivel);

            var subtitulo = new Label(
                $"{FormatarCategoria(criatura.Categoria)} · Nível {criatura.Nivel} de Força"
            );
            subtitulo.AddToClassList("bestiario-ficha-subtitulo");
            header.Add(subtitulo);

            // Seção de barra de progresso para o próximo nível
            var progressoSecao = new VisualElement();
            progressoSecao.AddToClassList("bestiario-progresso");
            scroll.Add(progressoSecao);

            RegrasBestiario.ObterProgressoNoNivel(
                criatura.Categoria,
                entrada.Derrotas,
                out int progresso,
                out int meta
            );

            var textoProgresso = new Label();
            textoProgresso.AddToClassList("bestiario-progresso-texto");
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
            barraFundo.AddToClassList("bestiario-barra-fundo");
            progressoSecao.Add(barraFundo);

            var barraPreenchimento = new VisualElement();
            barraPreenchimento.AddToClassList("bestiario-barra-preenchimento");
            barraPreenchimento.style.backgroundColor = ObterCorDoNivel(entrada.Nivel);

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
                    fraseCard.AddToClassList("bestiario-frase");
                    fraseCard.style.borderLeftColor = new Color(0.95f, 0.82f, 0.45f, 1f);

                    var fraseLabel = new Label($"\"{lore.FraseBestiario}\"");
                    fraseLabel.AddToClassList("bestiario-frase-texto");
                    fraseLabel.style.color = new Color(0.9f, 0.85f, 0.7f, 1f);
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
            card.AddToClassList("bestiario-cartao");

            var t = new Label(titulo);
            t.AddToClassList("bestiario-cartao-titulo");
            t.style.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            card.Add(t);

            if (!string.IsNullOrEmpty(subtitulo))
            {
                var s = new Label(subtitulo);
                s.AddToClassList("bestiario-cartao-subtitulo");
                s.style.color = new Color(0.9f, 0.78f, 0.45f, 1f);
                card.Add(s);
            }

            var c = new Label(corpo);
            c.AddToClassList("bestiario-cartao-corpo");
            c.style.color = new Color(0.72f, 0.72f, 0.76f, 1f);
            card.Add(c);

            container.Add(card);
        }

        private void AdicionarCardBloqueado(VisualElement container, string titulo, string dica)
        {
            var card = new VisualElement();
            card.AddToClassList("bestiario-cartao");
            card.AddToClassList("bestiario-cartao-bloqueado");
            card.style.borderLeftColor = new Color(0.4f, 0.4f, 0.45f, 0.5f);

            var t = new Label($"🔒 {titulo} (Bloqueado)");
            t.AddToClassList("bestiario-cartao-titulo-bloqueado");
            t.style.color = new Color(0.55f, 0.55f, 0.6f, 1f);
            card.Add(t);

            var d = new Label(dica);
            d.AddToClassList("bestiario-cartao-dica");
            d.style.color = new Color(0.45f, 0.45f, 0.5f, 1f);
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
