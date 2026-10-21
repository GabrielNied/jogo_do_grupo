using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Core.Visual;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Skills.UI
{
    /// <summary>
    /// Janela de consulta de habilidades por conjunto ou arma (tecla K), somente leitura.
    /// Exibe o catálogo de skills catalogadas por tipo de arma, destacando a arma e as
    /// 3 skills ativas da barra do jogador (definido no quiz 3). Trocar de arma com [R]
    /// reflete na barra e no destaque desta tela em tempo real.
    /// </summary>
    public sealed class JanelaSkills : JanelaDoJogo
    {
        public const string Chave = "skills.livro";
        public const string Titulo = "Skills";
        public const string Tecla = "K";
        public const int Ordem = 35;

        private TipoEquipamento _tipoSelecionado = TipoEquipamento.Espada;

        private VisualElement _conteudoRaiz;
        private VisualElement _bannerArma;
        private Label _bannerTitulo;
        private Label _bannerSub;
        private VisualElement _bannerChips;
        private VisualElement _seletorArmas;
        private ScrollView _scrollSkills;

        private readonly Dictionary<TipoEquipamento, Button> _botoesArma =
            new Dictionary<TipoEquipamento, Button>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaSkills());
        }

        protected override void Montar(VisualElement conteudo)
        {
            _conteudoRaiz = conteudo;
            conteudo.style.minWidth = 540f;
            conteudo.style.maxWidth = 660f;
            conteudo.style.paddingLeft = 14f;
            conteudo.style.paddingRight = 14f;
            conteudo.style.paddingTop = 10f;
            conteudo.style.paddingBottom = 12f;

            var estilo = Resources.Load<StyleSheet>(PainelSkills.CaminhoEstilo);
            if (estilo != null && !conteudo.styleSheets.Contains(estilo))
            {
                conteudo.styleSheets.Add(estilo);
            }

            // Cabeçalho
            var subtitulo = new Label("Habilidades de Combate de Asterra");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.style.fontSize = 14f;
            subtitulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            subtitulo.style.color = new Color(0.85f, 0.70f, 0.30f);
            subtitulo.style.marginBottom = 4f;
            conteudo.Add(subtitulo);

            var descricao = new Label(
                "As habilidades ativas pertencem à arma equipada no conjunto ativo (teclas 1, 2 e 3). Troque de conjunto com [R] para alternar suas habilidades."
            );
            descricao.AddToClassList(EstiloCore.Texto);
            descricao.style.fontSize = 11.5f;
            descricao.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoSecundario);
            descricao.style.marginBottom = 10f;
            conteudo.Add(descricao);

            // Banner do conjunto ativo
            _bannerArma = new VisualElement();
            _bannerArma.AddToClassList("skills-janela-banner");

            _bannerTitulo = new Label();
            _bannerTitulo.AddToClassList("skills-janela-banner-titulo");
            _bannerArma.Add(_bannerTitulo);

            _bannerSub = new Label();
            _bannerSub.AddToClassList("skills-janela-banner-sub");
            _bannerArma.Add(_bannerSub);

            _bannerChips = new VisualElement();
            _bannerChips.AddToClassList("skills-janela-banner-chips");
            _bannerArma.Add(_bannerChips);

            conteudo.Add(_bannerArma);

            // Barra seletora de armas
            _seletorArmas = new VisualElement();
            _seletorArmas.AddToClassList("skills-janela-seletor-armas");
            _botoesArma.Clear();

            foreach (var tipo in CatalogoDeSkills.TiposComSkills)
            {
                var tipoCapturado = tipo;
                var botao = new Button(() => SelecionarTipoArma(tipoCapturado))
                {
                    text = ApresentacaoSkills.NomeDaArma(tipoCapturado),
                };
                botao.AddToClassList("skills-janela-aba-arma");
                _botoesArma[tipoCapturado] = botao;
                _seletorArmas.Add(botao);
            }

            conteudo.Add(_seletorArmas);

            // Lista rolável de skills da arma selecionada
            _scrollSkills = new ScrollView(ScrollViewMode.Vertical);
            _scrollSkills.AddToClassList("skills-janela-scroll");
            conteudo.Add(_scrollSkills);

            // Rodapé com dicas
            var rodape = new Label(
                "Dica: Esta tela é somente leitura. O servidor sorteia 3 habilidades por exemplar de arma. Use [R] para alternar entre armas equipadas ou [K] para fechar."
            );
            rodape.AddToClassList(EstiloCore.Mensagem);
            rodape.style.fontSize = 11f;
            rodape.style.marginTop = 10f;
            rodape.style.color = EstiloVisual.Cor(ConfigVisual.Cores.TextoDesabilitado);
            conteudo.Add(rodape);
        }

        protected override void AoAbrir()
        {
            if (ControleSkills.Instancia != null)
            {
                ControleSkills.Instancia.BarraMudou += AoMudarBarra;
            }

            var estado = ControleSkills.Instancia?.Estado;
            var tipoAtivo = estado?.TipoDaArmaAtiva();
            if (tipoAtivo.HasValue)
            {
                _tipoSelecionado = tipoAtivo.Value;
            }

            AtualizarVisual();
        }

        protected override void AoFechar()
        {
            if (ControleSkills.Instancia != null)
            {
                ControleSkills.Instancia.BarraMudou -= AoMudarBarra;
            }
        }

        private void AoMudarBarra(BarraDeSkillsMudou _)
        {
            AtualizarVisual();
        }

        private void SelecionarTipoArma(TipoEquipamento tipo)
        {
            _tipoSelecionado = tipo;
            AtualizarVisual();
        }

        private void AtualizarVisual()
        {
            if (_conteudoRaiz == null)
            {
                return;
            }

            var estado = ControleSkills.Instancia?.Estado;
            var tipoAtivo = estado?.TipoDaArmaAtiva();

            // Atualiza Banner do conjunto ativo
            _bannerChips.Clear();
            if (tipoAtivo.HasValue && estado != null && estado.TemSkills)
            {
                var nomeArmaAtiva = ApresentacaoSkills.NomeDaArma(tipoAtivo.Value);
                _bannerTitulo.text = $"★ Conjunto Ativo: {nomeArmaAtiva}";
                _bannerSub.text = "Habilidades ativas prontas na barra de combate:";
                _bannerSub.style.display = DisplayStyle.Flex;
                _bannerChips.style.display = DisplayStyle.Flex;

                for (var i = 0; i < ConfigSkills.SkillsPorArma; i++)
                {
                    var skill = estado.Skill(i);
                    if (skill != null)
                    {
                        var chip = new Label(
                            $"[{i + 1}] {skill.Nome} ({ApresentacaoSkills.Custo(skill)})"
                        );
                        chip.AddToClassList("skills-janela-chip");
                        _bannerChips.Add(chip);
                    }
                }
            }
            else
            {
                _bannerTitulo.text = "Nenhuma arma ativa equipada";
                _bannerSub.text = "Equipe uma arma para habilitar habilidades ativas na barra.";
                _bannerSub.style.display = DisplayStyle.Flex;
                _bannerChips.style.display = DisplayStyle.None;
            }

            // Atualiza abas dos tipos de armas
            foreach (var par in _botoesArma)
            {
                var tipo = par.Key;
                var botao = par.Value;
                var ehAtiva = tipoAtivo.HasValue && tipoAtivo.Value == tipo;
                var ehSelecionada = tipo == _tipoSelecionado;

                var prefixo = ehAtiva ? "★ " : "";
                botao.text = prefixo + ApresentacaoSkills.NomeDaArma(tipo);

                if (ehSelecionada)
                {
                    botao.AddToClassList("skills-janela-aba-arma-selecionada");
                }
                else
                {
                    botao.RemoveFromClassList("skills-janela-aba-arma-selecionada");
                }

                if (ehAtiva)
                {
                    botao.AddToClassList("skills-janela-aba-arma-ativa");
                }
                else
                {
                    botao.RemoveFromClassList("skills-janela-aba-arma-ativa");
                }
            }

            // Atualiza lista de skills do tipo selecionado
            _scrollSkills.Clear();
            var pool = CatalogoDeSkills.Pool(_tipoSelecionado);
            if (pool.Count == 0)
            {
                var vazio = new Label("Nenhuma habilidade catalogada para esta arma.");
                vazio.AddToClassList(EstiloCore.Texto);
                vazio.style.color = new Color(0.6f, 0.65f, 0.75f);
                _scrollSkills.Add(vazio);
                return;
            }

            foreach (var definicao in pool)
            {
                var cartao = CriarCartaoSkill(definicao, estado);
                _scrollSkills.Add(cartao);
            }
        }

        private VisualElement CriarCartaoSkill(DefinicaoDeSkill skill, EstadoDaBarra estado)
        {
            var cartao = new VisualElement();
            cartao.AddToClassList("skills-janela-cartao");

            var ehAtivaNaBarra = estado != null && estado.ContemSkill(skill.Id);
            var espacoNaBarra = ehAtivaNaBarra ? estado.EspacoDaSkill(skill.Id) : -1;

            if (ehAtivaNaBarra)
            {
                cartao.AddToClassList("skills-janela-cartao-ativo");
            }

            // Linha superior: Nome e Custo (ou Badge Ativa)
            var cabecalho = new VisualElement();
            cabecalho.AddToClassList("skills-janela-cartao-cabecalho");

            var ladoEsquerdo = new VisualElement();
            ladoEsquerdo.style.flexDirection = FlexDirection.Row;
            ladoEsquerdo.style.alignItems = Align.Center;

            var nome = new Label(skill.Nome);
            nome.AddToClassList("skills-janela-cartao-nome");
            ladoEsquerdo.Add(nome);

            if (ehAtivaNaBarra && espacoNaBarra >= 0)
            {
                var badge = new Label($"[Ativa no Atalho {espacoNaBarra + 1}]");
                badge.AddToClassList("skills-janela-cartao-badge-ativa");
                badge.style.marginLeft = 8f;
                ladoEsquerdo.Add(badge);
            }

            cabecalho.Add(ladoEsquerdo);

            var custo = new Label(ApresentacaoSkills.Custo(skill));
            custo.AddToClassList("skills-janela-cartao-custo");
            if (skill.Recurso == RecursoDeSkill.Mana)
            {
                custo.AddToClassList("skills-janela-cartao-custo-mana");
            }
            else
            {
                custo.AddToClassList("skills-janela-cartao-custo-stamina");
            }
            cabecalho.Add(custo);

            cartao.Add(cabecalho);

            // Linha de atributos: Dano e Recarga
            var detalhes = new VisualElement();
            detalhes.AddToClassList("skills-janela-cartao-detalhes");

            var dano = new Label(
                $"Dano: {ApresentacaoSkills.DescricaoDano(skill)} ({ApresentacaoSkills.DescricaoCategoria(skill)})"
            );
            dano.AddToClassList("skills-janela-cartao-detalhe");
            detalhes.Add(dano);

            var recarga = new Label(
                $"Recarga: {ApresentacaoSkills.RecargaEmSegundos(skill.RecargaMs)}"
            );
            recarga.AddToClassList("skills-janela-cartao-detalhe");
            detalhes.Add(recarga);

            cartao.Add(detalhes);

            // Linha de etiquetas
            var etiquetas = new List<string>();
            if (skill.Propriedades != null)
            {
                if (skill.Propriedades.PodeCritar)
                {
                    etiquetas.Add("Crítica");
                }
                if (skill.Propriedades.RecebePenetracao)
                {
                    etiquetas.Add("Penetração");
                }
            }
            if (skill.Etiquetas != null)
            {
                foreach (var etiqueta in skill.Etiquetas)
                {
                    if (etiqueta == EtiquetasDeSkill.Investida)
                    {
                        etiquetas.Add("Investida");
                    }
                }
            }

            if (etiquetas.Count > 0)
            {
                var containerTags = new VisualElement();
                containerTags.AddToClassList("skills-janela-cartao-tags");
                foreach (var tag in etiquetas)
                {
                    var labelTag = new Label(tag);
                    labelTag.AddToClassList("skills-janela-cartao-tag");
                    containerTags.Add(labelTag);
                }
                cartao.Add(containerTags);
            }

            return cartao;
        }
    }
}
