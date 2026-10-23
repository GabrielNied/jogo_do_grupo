using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Montarias;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Montarias.UI
{
    /// <summary>
    /// Janela de coleção de montarias (tecla P), registrada nas <see cref="JanelasDoJogo"/>:
    /// exibe os companheiros disponíveis, detalhes visuais e permite equipar a montaria
    /// ativa a ser invocada com [T].
    /// </summary>
    public sealed class JanelaMontarias : JanelaDoJogo
    {
        public const string Chave = "montarias.colecao";
        public const string Titulo = "Montarias";
        public const string Tecla = "P";
        public const int Ordem = 50;

        private VisualElement _cartoesContainer;
        private readonly List<CartaoMontaria> _cartoes = new List<CartaoMontaria>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaMontarias());
        }

        public static string TextoDoSelo(int tipo)
        {
            return RegrasMontaria.ObterNome(tipo).ToUpperInvariant();
        }

        public static string ClasseDeRaridadeDoSelo(int tipo)
        {
            return RegrasMontaria.ObterRaridade(tipo) == "Raro"
                ? "raridade-rara"
                : "raridade-comum";
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloMontarias.Aplicar(conteudo);
            conteudo.AddToClassList("montarias-conteudo");

            // Cabeçalho descritivo
            var subtitulo = new Label("Coleção de Montarias de Asterra");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.AddToClassList("montarias-subtitulo");
            subtitulo.style.color = new Color(0.85f, 0.70f, 0.30f);
            conteudo.Add(subtitulo);

            var descricao = new Label(
                "Selecione e equipe sua montaria. Ao cavalgar, sua velocidade autoritativa aumenta para 9,0 m/s (+63% de corrida)."
            );
            descricao.AddToClassList(EstiloCore.Texto);
            descricao.AddToClassList("montarias-descricao");
            conteudo.Add(descricao);

            // Contêiner de cartões
            _cartoesContainer = new VisualElement();
            _cartoesContainer.AddToClassList("montarias-cartoes");
            conteudo.Add(_cartoesContainer);

            foreach (var tipo in RegrasMontaria.TiposDoCatalogo)
            {
                CriarCartao(tipo);
            }

            // Rodapé informativo
            var rodape = new Label(
                "Dica: Pressione [T] para subir na montaria equipada (1,5s parado) ou descer na hora."
            );
            rodape.AddToClassList(EstiloCore.Mensagem);
            rodape.AddToClassList("montarias-rodape");
            conteudo.Add(rodape);
        }

        protected override void AoAbrir()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.MontariaEquipadaMudou += AtualizarCartoesPorEquipada;
                ControleDeMontaria.Instancia.ColecaoMudou += AtualizarTodosOsCartoes;
            }

            AtualizarTodosOsCartoes();
        }

        protected override void AoFechar()
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.MontariaEquipadaMudou -= AtualizarCartoesPorEquipada;
                ControleDeMontaria.Instancia.ColecaoMudou -= AtualizarTodosOsCartoes;
            }
        }

        private void CriarCartao(int tipo)
        {
            var cartao = new CartaoMontaria(tipo, AoSolicitarEquipar);
            _cartoes.Add(cartao);
            _cartoesContainer.Add(cartao.Elemento);
        }

        private void AoSolicitarEquipar(int tipo)
        {
            if (ControleDeMontaria.Instancia != null)
            {
                ControleDeMontaria.Instancia.EquiparMontaria(tipo);
            }
            AtualizarTodosOsCartoes();
        }

        private void AtualizarCartoesPorEquipada(int _)
        {
            AtualizarTodosOsCartoes();
        }

        private void AtualizarTodosOsCartoes()
        {
            var controle = ControleDeMontaria.Instancia;
            var equipada = controle != null ? controle.MontariaEquipada : RegrasMontaria.TipoPadrao;

            for (var i = 0; i < _cartoes.Count; i++)
            {
                var tipo = _cartoes[i].Tipo;
                var desbloqueada = controle == null || controle.PossuiMontaria(tipo);
                var ehEquipada = desbloqueada && tipo == equipada;
                _cartoes[i].AtualizarEstado(desbloqueada, ehEquipada);
            }
        }

        /// <summary>
        /// Representa o cartão visual de uma montaria na coleção.
        /// </summary>
        private sealed class CartaoMontaria
        {
            public int Tipo { get; }
            public VisualElement Elemento { get; }

            private readonly Button _botaoEquipar;
            private readonly VisualElement _seloEquipado;
            private readonly VisualElement _seloBloqueado;
            private readonly Label _rotuloOrigem;
            private readonly Action<int> _aoEquipar;

            public CartaoMontaria(int tipo, Action<int> aoEquipar)
            {
                Tipo = tipo;
                _aoEquipar = aoEquipar;

                Elemento = new VisualElement();
                Elemento.AddToClassList("montarias-cartao");

                // Faixa de destaque visual no topo do cartão
                var faixaVisual = new VisualElement();
                faixaVisual.AddToClassList("montarias-faixa");

                var eRaro = RegrasMontaria.ObterRaridade(tipo) == "Raro";
                var corBorda = eRaro
                    ? new Color(0.28f, 0.70f, 0.95f, 0.6f)
                    : new Color(0.75f, 0.55f, 0.25f, 0.6f);
                faixaVisual.style.backgroundColor = eRaro
                    ? new Color(0.12f, 0.16f, 0.26f, 0.95f)
                    : new Color(0.26f, 0.18f, 0.12f, 0.95f);
                faixaVisual.style.borderLeftColor = corBorda;
                faixaVisual.style.borderRightColor = corBorda;
                faixaVisual.style.borderTopColor = corBorda;
                faixaVisual.style.borderBottomColor = corBorda;

                var insignia = new Label(TextoDoSelo(tipo));
                insignia.AddToClassList("montarias-insignia");
                insignia.AddToClassList(ClasseDeRaridadeDoSelo(tipo));
                faixaVisual.Add(insignia);
                Elemento.Add(faixaVisual);

                // Nome da montaria
                var nome = new Label(RegrasMontaria.ObterNome(tipo));
                nome.AddToClassList(EstiloCore.Titulo);
                nome.AddToClassList("montarias-nome");
                nome.style.color = new Color(0.95f, 0.96f, 0.98f);
                Elemento.Add(nome);

                // Categoria e raridade
                var raridade = new Label($"{RegrasMontaria.ObterRaridade(tipo)} • Terrestre");
                raridade.AddToClassList("montarias-raridade");
                raridade.AddToClassList(ClasseDeRaridadeDoSelo(tipo));
                Elemento.Add(raridade);

                // Descrição
                var desc = new Label(RegrasMontaria.ObterDescricao(tipo));
                desc.AddToClassList(EstiloCore.Texto);
                desc.AddToClassList("montarias-desc");
                desc.style.color = new Color(0.74f, 0.78f, 0.85f);
                Elemento.Add(desc);

                // Atributos / Estatísticas
                var atributos = new VisualElement();
                atributos.AddToClassList("montarias-atributos");

                var linhaVelocidade = new Label("⚡ Velocidade: 9,0 m/s (+63%)");
                linhaVelocidade.AddToClassList("montarias-linha-velocidade");
                linhaVelocidade.style.color = new Color(0.40f, 0.90f, 0.60f);

                var linhaInvocacao = new Label("⏳ Invocação: 1,5 segundos");
                linhaInvocacao.AddToClassList("montarias-linha-invocacao");
                linhaInvocacao.style.color = new Color(0.85f, 0.85f, 0.90f);

                atributos.Add(linhaVelocidade);
                atributos.Add(linhaInvocacao);
                Elemento.Add(atributos);

                // Botão de equipar
                _botaoEquipar = new Button(() => _aoEquipar(Tipo));
                _botaoEquipar.text = "Equipar Montaria";
                _botaoEquipar.AddToClassList(EstiloCore.Botao);
                _botaoEquipar.AddToClassList("montarias-equipar");
                _botaoEquipar.style.color = new Color(0.90f, 0.94f, 1.0f);
                Elemento.Add(_botaoEquipar);

                // Selo de montaria já equipada
                _seloEquipado = new VisualElement();
                _seloEquipado.AddToClassList("montarias-selo");
                var corSelo = new Color(0.35f, 0.80f, 0.35f, 0.9f);
                _seloEquipado.style.backgroundColor = new Color(0.18f, 0.28f, 0.16f, 0.9f);
                _seloEquipado.style.borderLeftColor = corSelo;
                _seloEquipado.style.borderRightColor = corSelo;
                _seloEquipado.style.borderTopColor = corSelo;
                _seloEquipado.style.borderBottomColor = corSelo;

                var rotuloEquipado = new Label("★ EQUIPADA");
                rotuloEquipado.AddToClassList("montarias-selo-rotulo");
                rotuloEquipado.style.color = new Color(0.50f, 0.95f, 0.50f);
                _seloEquipado.Add(rotuloEquipado);
                Elemento.Add(_seloEquipado);

                // Selo de montaria bloqueada
                _seloBloqueado = new VisualElement();
                _seloBloqueado.AddToClassList("montarias-selo");
                _seloBloqueado.AddToClassList("montarias-selo-bloqueado");

                var rotuloBloqueado = new Label("🔒 BLOQUEADA");
                rotuloBloqueado.AddToClassList("montarias-selo-rotulo");
                rotuloBloqueado.style.color = new Color(0.60f, 0.63f, 0.70f);
                _seloBloqueado.Add(rotuloBloqueado);
                Elemento.Add(_seloBloqueado);

                // Origem da montaria
                var origem = RegrasMontaria.ObterOrigem(tipo);
                _rotuloOrigem = new Label(string.IsNullOrEmpty(origem) ? "" : $"Origem: {origem}");
                _rotuloOrigem.AddToClassList("montarias-origem");
                _rotuloOrigem.style.color = new Color(0.68f, 0.72f, 0.80f);
                Elemento.Add(_rotuloOrigem);

                AtualizarEstado(true, false);
            }

            public void AtualizarEstado(bool desbloqueada, bool equipado)
            {
                Elemento.EnableInClassList("montarias-cartao-bloqueado", !desbloqueada);
                Elemento.EnableInClassList("montarias-cartao-equipado", desbloqueada && equipado);

                // Só a montaria equipada tem borda dourada (cor sem token); nos demais estados
                // vale a borda da classe.
                if (desbloqueada && equipado)
                {
                    var ouro = new Color(0.85f, 0.70f, 0.28f, 0.9f);
                    Elemento.style.borderLeftColor = ouro;
                    Elemento.style.borderRightColor = ouro;
                    Elemento.style.borderTopColor = ouro;
                    Elemento.style.borderBottomColor = ouro;
                }
                else
                {
                    Elemento.style.borderLeftColor = StyleKeyword.Null;
                    Elemento.style.borderRightColor = StyleKeyword.Null;
                    Elemento.style.borderTopColor = StyleKeyword.Null;
                    Elemento.style.borderBottomColor = StyleKeyword.Null;
                }

                _botaoEquipar.style.display =
                    desbloqueada && !equipado ? DisplayStyle.Flex : DisplayStyle.None;
                _seloEquipado.style.display =
                    desbloqueada && equipado ? DisplayStyle.Flex : DisplayStyle.None;
                _seloBloqueado.style.display = desbloqueada ? DisplayStyle.None : DisplayStyle.Flex;
                if (!desbloqueada)
                    _rotuloOrigem.style.display = DisplayStyle.Flex;
            }

            public void DefinirEquipado(bool equipado)
            {
                AtualizarEstado(true, equipado);
            }
        }
    }
}
