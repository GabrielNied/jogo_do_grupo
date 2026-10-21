using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Npcs.UI
{
    /// <summary>
    /// Prólogo opcional "O sol que nasceu na estrada" (#327). Abre sozinha uma vez por personagem
    /// neste aparelho, oferecendo Assistir ou Pular; Esc ou fechar também pula. Assistir toca a cena
    /// do <see cref="PalcoDoPrologo"/> em tela inteira, com o personagem parado. Quem assiste e
    /// quem pula voltam ao mesmo ponto: o jogador onde estava, na cidade, com a missão principal 1
    /// como já estava (o prólogo não entrega nem muda missão).
    /// </summary>
    public sealed class JanelaPrologo : JanelaDoJogo
    {
        public const string Chave = "npcs.prologo";
        public const int Ordem = 95;
        public const float EsperaParaOferecerSegundos = 2f;
        private const string PrefixoDaMarca = "npcs.prologo.visto.";

        public override bool TelaInteira => true;

        private PalcoDoPrologo _palco;
        private ControleTerceiraPessoa _controle;
        private bool _controleEstavaAtivo;
        private Label _nome;
        private Label _texto;
        private VisualElement _clarao;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(
                    Chave,
                    PrologoDoSol.Titulo,
                    null,
                    Ordem,
                    () => new JanelaPrologo()
                )
            );
        }

        /// <summary>Se o personagem já viu (ou pulou) a oferta neste aparelho.</summary>
        public static bool JaOferecido(long personagemId)
        {
            try
            {
                return PlayerPrefs.GetInt(PrefixoDaMarca + personagemId, 0) == 1;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>Abre a oferta uma vez por personagem; não faz nada se já foi oferecida.</summary>
        public static bool OferecerSeNovo(long personagemId)
        {
            if (personagemId <= 0 || JaOferecido(personagemId) || JanelasDoJogo.AlgoAberto)
            {
                return false;
            }

            return JanelasDoJogo.Abrir(Chave);
        }

        protected override void Montar(VisualElement conteudo)
        {
            MarcarComoOferecido();
            MontarOferta(conteudo);
        }

        private static void MarcarComoOferecido()
        {
            try
            {
                PlayerPrefs.SetInt(PrefixoDaMarca + EntradaNoJogo.PersonagemId, 1);
                PlayerPrefs.Save();
            }
            catch
            {
                // Sem armazenamento local a oferta só reaparece na próxima entrada.
            }
        }

        private void MontarOferta(VisualElement conteudo)
        {
            conteudo.Clear();
            conteudo.style.alignItems = Align.Center;
            conteudo.style.justifyContent = Justify.Center;
            conteudo.style.flexGrow = 1f;
            // Véu leve: a vista do mundo da tela cheia aparece por trás da oferta.
            conteudo.style.backgroundColor = new Color(0.04f, 0.05f, 0.1f, 0.35f);

            var caixa = new VisualElement();
            caixa.style.maxWidth = 560f;
            caixa.style.paddingLeft = 24f;
            caixa.style.paddingRight = 24f;
            caixa.style.paddingTop = 20f;
            caixa.style.paddingBottom = 22f;

            var titulo = new Label(PrologoDoSol.Titulo);
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.style.fontSize = 28f;
            titulo.style.unityTextAlign = TextAnchor.MiddleCenter;
            caixa.Add(titulo);

            var sinopse = new Label(
                "Uma cena curta, antes de entrar na cidade: quatro magos seguram um sol enquanto uma manada se aproxima. "
                    + "Você não perde nada se pular, e a sua missão continua a mesma."
            );
            sinopse.AddToClassList(EstiloCore.Texto);
            sinopse.style.whiteSpace = WhiteSpace.Normal;
            sinopse.style.marginTop = 12f;
            sinopse.style.marginBottom = 18f;
            sinopse.style.unityTextAlign = TextAnchor.MiddleCenter;
            caixa.Add(sinopse);

            var linha = new VisualElement();
            linha.style.flexDirection = FlexDirection.Row;
            linha.style.justifyContent = Justify.Center;

            var assistir = new Button(ComecarCena) { text = "Assistir" };
            assistir.AddToClassList(EstiloCore.Botao);
            assistir.AddToClassList(EstiloCore.BotaoPrincipal);
            assistir.style.height = 40f;
            assistir.style.minWidth = 160f;
            assistir.style.marginRight = 10f;
            linha.Add(assistir);

            var pular = new Button(Pular) { text = "Pular (Esc)" };
            pular.AddToClassList(EstiloCore.Botao);
            pular.style.height = 40f;
            pular.style.minWidth = 160f;
            linha.Add(pular);

            caixa.Add(linha);
            conteudo.Add(caixa);
        }

        private void Pular()
        {
            JanelasDoJogo.Fechar(Chave);
        }

        private void ComecarCena()
        {
            if (!Aberta || _palco != null)
            {
                return;
            }

            var conteudo = Conteudo;
            conteudo.Clear();
            conteudo.style.alignItems = Align.Stretch;
            conteudo.style.justifyContent = Justify.FlexStart;
            conteudo.style.backgroundColor = Color.black;

            TravarPersonagem();
            _palco = PalcoDoPrologo.Criar();

            var imagem = new Image { image = _palco.Imagem, scaleMode = ScaleMode.ScaleAndCrop };
            imagem.style.position = Position.Absolute;
            imagem.style.left = 0f;
            imagem.style.right = 0f;
            imagem.style.top = 0f;
            imagem.style.bottom = 0f;
            conteudo.Add(imagem);

            _clarao = new VisualElement();
            _clarao.style.position = Position.Absolute;
            _clarao.style.left = 0f;
            _clarao.style.right = 0f;
            _clarao.style.top = 0f;
            _clarao.style.bottom = 0f;
            _clarao.style.backgroundColor = new Color(1f, 0.97f, 0.85f);
            _clarao.style.opacity = 0f;
            _clarao.pickingMode = PickingMode.Ignore;
            conteudo.Add(_clarao);

            var legenda = new VisualElement();
            legenda.style.position = Position.Absolute;
            legenda.style.left = 0f;
            legenda.style.right = 0f;
            legenda.style.bottom = 0f;
            legenda.style.paddingLeft = 48f;
            legenda.style.paddingRight = 48f;
            legenda.style.paddingTop = 14f;
            legenda.style.paddingBottom = 20f;
            legenda.style.backgroundColor = new Color(0f, 0f, 0f, 0.62f);
            legenda.pickingMode = PickingMode.Ignore;

            _nome = new Label();
            _nome.AddToClassList(EstiloCore.Subtitulo);
            _nome.style.fontSize = 16f;
            legenda.Add(_nome);

            _texto = new Label();
            _texto.AddToClassList(EstiloCore.Texto);
            _texto.style.whiteSpace = WhiteSpace.Normal;
            _texto.style.fontSize = 20f;
            _texto.style.minHeight = 52f;
            legenda.Add(_texto);
            conteudo.Add(legenda);

            var pular = new Button(Pular) { text = "Pular (Esc)" };
            pular.AddToClassList(EstiloCore.Botao);
            pular.style.position = Position.Absolute;
            pular.style.right = 24f;
            pular.style.top = 24f;
            pular.style.height = 34f;
            conteudo.Add(pular);

            _palco.FalaMudou += AoMudarFala;
            _palco.ClaraoMudou += AoMudarClarao;
            _palco.Terminou += Pular;
        }

        private void AoMudarFala(FalaDoPrologo fala)
        {
            if (_texto == null)
            {
                return;
            }

            if (fala == null)
            {
                _nome.text = string.Empty;
                _texto.text = string.Empty;
                return;
            }

            _nome.text =
                fala.Falante >= 0 ? PrologoDoSol.NomesDosMagos[fala.Falante]
                : fala.Falante == FalaDoPrologo.Viajante ? "Viajante"
                : string.Empty;
            _texto.text = fala.Texto;
        }

        private void AoMudarClarao(float clarao)
        {
            if (_clarao != null)
            {
                _clarao.style.opacity = clarao * 0.85f;
            }
        }

        // O personagem fica parado durante a cena: o controle é desligado e devolvido ao fechar.
        private void TravarPersonagem()
        {
            var jogador = EntradaNoJogo.Jogador;
            _controle = jogador != null ? jogador.GetComponent<ControleTerceiraPessoa>() : null;
            if (_controle != null)
            {
                _controleEstavaAtivo = _controle.enabled;
                _controle.enabled = false;
            }
        }

        protected override void AoFechar()
        {
            base.AoFechar();
            if (_palco != null)
            {
                _palco.FalaMudou -= AoMudarFala;
                _palco.ClaraoMudou -= AoMudarClarao;
                _palco.Terminou -= Pular;
                Object.Destroy(_palco.gameObject);
                _palco = null;
            }

            if (_controle != null)
            {
                _controle.enabled = _controleEstavaAtivo;
                _controle = null;
            }
        }
    }
}
