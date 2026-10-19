using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Conquistas;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Conquistas
{
    internal sealed class JanelaConquistas : JanelaDoJogo
    {
        public const string Chave = "conquistas.registro";
        public const string TituloPadrao = "Conquistas";
        public const string Tecla = "Y";
        public const int Ordem = 57;

        private readonly EstadoDeConquistas _estado;

        private VisualElement _corpo;
        private Label _avisoErro;

        public override bool MolduraSocial => false;

        public JanelaConquistas()
            : this(EstadoDeConquistas.Instancia) { }

        internal JanelaConquistas(EstadoDeConquistas estado)
        {
            _estado = estado;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
        }

        internal static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(
                Chave,
                TituloPadrao,
                Tecla,
                Ordem,
                () => new JanelaConquistas()
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.AddToClassList(EstiloCore.Janela);

            _avisoErro = new Label();
            _avisoErro.AddToClassList(EstiloCore.Mensagem);
            conteudo.Add(_avisoErro);

            _corpo = new VisualElement();
            conteudo.Add(_corpo);

            _estado.Mudou += AtualizarVisual;
            _estado.Consultar();
            AtualizarVisual();
        }

        protected override void AoFechar()
        {
            _estado.Mudou -= AtualizarVisual;
            base.AoFechar();
        }

        private void AtualizarVisual()
        {
            _corpo.Clear();

            if (!string.IsNullOrEmpty(_estado.UltimoErro))
            {
                _avisoErro.text = $"Aviso: {_estado.UltimoErro}";
                _avisoErro.style.display = DisplayStyle.Flex;
            }
            else
            {
                _avisoErro.style.display = DisplayStyle.None;
            }

            var dados = _estado.Dados;
            if (dados == null)
            {
                var carregando = new Label("Carregando conquistas...");
                carregando.AddToClassList(EstiloCore.Texto);
                _corpo.Add(carregando);
                return;
            }

            var cabecalho = new VisualElement();
            cabecalho.AddToClassList(EstiloCore.LinhaLista);

            var pontos = new Label($"Pontos de Conquista: {dados.PontosTotais}");
            pontos.AddToClassList(EstiloCore.Titulo);
            cabecalho.Add(pontos);

            var tituloTexto = string.IsNullOrEmpty(dados.TituloAtivo)
                ? "Nenhum"
                : dados.TituloAtivo;
            var rotuloTitulo = new Label($"Título Ativo: {tituloTexto}");
            rotuloTitulo.AddToClassList(EstiloCore.Texto);
            cabecalho.Add(rotuloTitulo);

            _corpo.Add(cabecalho);

            if (dados.TitulosDesbloqueados != null && dados.TitulosDesbloqueados.Length > 0)
            {
                var secaoTitulos = new VisualElement();
                secaoTitulos.AddToClassList(EstiloCore.LinhaLista);

                var tituloSecao = new Label("Títulos Disponíveis:");
                tituloSecao.AddToClassList(EstiloCore.Subtitulo);
                secaoTitulos.Add(tituloSecao);

                var linhaBotoes = new VisualElement();
                linhaBotoes.AddToClassList(EstiloCore.LinhaBotoes);

                var botaoDesequipar = new Button(() => _estado.EquiparTitulo(""))
                {
                    text = "Nenhum",
                };
                botaoDesequipar.AddToClassList(EstiloCore.Botao);
                linhaBotoes.Add(botaoDesequipar);

                foreach (var t in dados.TitulosDesbloqueados)
                {
                    var tituloItem = t;
                    var botao = new Button(() => _estado.EquiparTitulo(tituloItem))
                    {
                        text = tituloItem,
                    };
                    botao.AddToClassList(EstiloCore.Botao);
                    linhaBotoes.Add(botao);
                }

                secaoTitulos.Add(linhaBotoes);
                _corpo.Add(secaoTitulos);
            }

            var scroll = new ScrollView();
            scroll.AddToClassList(EstiloCore.Lista);

            var progressosPorId = new System.Collections.Generic.Dictionary<
                int,
                ProgressoConquista
            >();
            if (dados.Conquistas != null)
            {
                foreach (var prog in dados.Conquistas)
                {
                    progressosPorId[prog.ConquistaId] = prog;
                }
            }

            foreach (var def in CatalogoDeConquistas.Todas)
            {
                var card = new VisualElement();
                card.AddToClassList(EstiloCore.LinhaLista);

                var concluida = progressosPorId.TryGetValue(def.Id, out var p) && p.Concluida;
                var progressoAtual = p != null ? p.ProgressoAtual : 0;

                var icone = concluida ? "✔" : "○";
                var nome = new Label($"{icone} {def.Nome} (+{def.Pontos} pts)");
                nome.AddToClassList(EstiloCore.Subtitulo);
                card.Add(nome);

                var desc = new Label(def.Descricao);
                desc.AddToClassList(EstiloCore.Texto);
                card.Add(desc);

                var status = new Label($"Progresso: {progressoAtual}/{def.Meta}");
                status.AddToClassList(EstiloCore.Texto);
                card.Add(status);

                if (!string.IsNullOrEmpty(def.TituloRecompensa))
                {
                    var recompensa = new Label($"Recompensa: título \"{def.TituloRecompensa}\"");
                    recompensa.AddToClassList(EstiloCore.TextoGanho);
                    card.Add(recompensa);
                }

                scroll.Add(card);
            }

            _corpo.Add(scroll);
        }
    }
}
