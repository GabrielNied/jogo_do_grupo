using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Pvp;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>
    /// Janela de PvP (tecla V): bandeira, fila da arena 3v3 com o ranking e o duelo (desafiar o
    /// jogador próximo, cancelar). Só envia intenções e mostra o que o servidor mandou.
    /// </summary>
    internal sealed class JanelaPvp : JanelaDoJogo
    {
        public const string Chave = "pvp.janela";
        public const string Tecla = "V";
        public const int Ordem = 60;
        private const long IntervaloDeAtualizacaoMs = 500;

        private readonly EstadoDePvp _estado;

        private Label _aviso;
        private Label _estadoBandeira;
        private Button _bandeira;
        private Label _estadoArena;
        private Button _fila;
        private Label _ranking;
        private Label _estadoDuelo;
        private Label _alvo;
        private Button _desafiar;
        private Button _cancelar;
        private CandidatoDeDuelo _alvoAtual;
        private bool _temAlvo;

        public JanelaPvp()
            : this(EstadoDePvp.Atual) { }

        internal JanelaPvp(EstadoDePvp estado)
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
                TextosPvp.TituloDaJanela,
                Tecla,
                Ordem,
                () => new JanelaPvp()
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloPvp.Aplicar(conteudo);

            _aviso = new Label();
            _aviso.AddToClassList(EstiloCore.Mensagem);
            EstiloPvp.Mostrar(_aviso, false);
            conteudo.Add(_aviso);

            if (_estado == null)
            {
                _aviso.text = TextosPvp.SemConexao;
                EstiloPvp.Mostrar(_aviso, true);
                return;
            }

            var bandeira = Secao(conteudo, TextosPvp.SecaoBandeira);
            _estadoBandeira = Texto(bandeira);
            _bandeira = Botao(bandeira, "", AlternarBandeira, true);

            var arena = Secao(conteudo, TextosPvp.SecaoArena);
            _estadoArena = Texto(arena);
            _fila = Botao(arena, "", AlternarFila, true);
            _ranking = Texto(arena);
            Botao(arena, TextosPvp.AtualizarRanking, _estado.ConsultarRankingArena, false);

            var duelo = Secao(conteudo, TextosPvp.SecaoDuelo);
            _estadoDuelo = Texto(duelo);
            _alvo = Texto(duelo);
            _desafiar = Botao(duelo, TextosPvp.DesafiarProximo, Desafiar, true);
            _cancelar = Botao(duelo, TextosPvp.CancelarDuelo, _estado.CancelarDuelo, false);

            Redesenhar();
        }

        protected override void AoAbrir()
        {
            if (_estado == null)
            {
                return;
            }

            _estado.AoReceberRankingArena += AoReceberRanking;
            _estado.AoMudarStatusFilaArena += AoMudarFila;
            _estado.AoMudarBandeiraPvp += AoMudarBandeira;
            _estado.AoIniciarDuelo += AoIniciarDuelo;
            _estado.AoEncerrarDuelo += AoEncerrarDuelo;
            _estado.AoRecusarDuelo += AoRecusar;
            _estado.ConsultarRankingArena();
            // O alvo próximo muda com o movimento e o convite some com o tempo.
            Conteudo.schedule.Execute(Redesenhar).Every(IntervaloDeAtualizacaoMs);
        }

        protected override void AoFechar()
        {
            if (_estado == null)
            {
                return;
            }

            _estado.AoReceberRankingArena -= AoReceberRanking;
            _estado.AoMudarStatusFilaArena -= AoMudarFila;
            _estado.AoMudarBandeiraPvp -= AoMudarBandeira;
            _estado.AoIniciarDuelo -= AoIniciarDuelo;
            _estado.AoEncerrarDuelo -= AoEncerrarDuelo;
            _estado.AoRecusarDuelo -= AoRecusar;
        }

        private void AlternarBandeira() => _estado.AlternarBandeiraPvp(!_estado.BandeiraPvpAtiva);

        private void AlternarFila()
        {
            if (_estado.NaFilaArena)
                _estado.SairDaFilaArena();
            else
                _estado.EntrarNaFilaArena();
        }

        private void Desafiar()
        {
            if (!_temAlvo)
            {
                return;
            }

            _estado.SolicitarDuelo(_alvoAtual.PersonagemId);
            Avisar(TextosPvp.DesafioEnviado(_alvoAtual.Nome), false);
        }

        private void AoReceberRanking(RespostaRankingArena _) => Redesenhar();

        private void AoMudarFila(bool _, int __) => Redesenhar();

        private void AoMudarBandeira(long _, bool __) => Redesenhar();

        private void AoIniciarDuelo(DueloIniciado _) => Redesenhar();

        private void AoEncerrarDuelo(DueloEncerrado _) => Redesenhar();

        private void AoRecusar(string motivo) => Avisar(TextosPvp.TraduzirRecusa(motivo), true);

        private void Avisar(string texto, bool erro)
        {
            if (_aviso == null)
            {
                return;
            }

            _aviso.text = texto;
            _aviso.EnableInClassList(EstiloCore.MensagemErro, erro);
            EstiloPvp.Mostrar(_aviso, !string.IsNullOrEmpty(texto));
        }

        private void Redesenhar()
        {
            if (!Aberta || _estado == null || _estadoBandeira == null)
            {
                return;
            }

            _estadoBandeira.text = TextosPvp.EstadoDaBandeira(_estado.BandeiraPvpAtiva);
            _bandeira.text = _estado.BandeiraPvpAtiva
                ? TextosPvp.DesligarBandeira
                : TextosPvp.LigarBandeira;

            _estadoArena.text = TextosPvp.EstadoDaArena(
                _estado.NaFilaArena,
                _estado.EmArena,
                _estado.SegundosNaFilaArena
            );
            _fila.text = _estado.NaFilaArena ? TextosPvp.SairDaFila : TextosPvp.EntrarNaFila;
            _fila.SetEnabled(!_estado.EmArena && !_estado.EmDuelo);
            _ranking.text =
                _estado.RankingAtual == null
                    ? TextosPvp.SemRanking
                    : TextosPvp.Ranking(_estado.RankingAtual);

            _estadoDuelo.text = _estado.EmDuelo ? TextosPvp.EmDueloAgora : TextosPvp.SemDuelo;
            _cancelar.SetEnabled(_estado.EmDuelo);
            AtualizarAlvo();
        }

        private void AtualizarAlvo()
        {
            _temAlvo = false;
            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                var posicao = jogador.transform.position;
                _temAlvo = EscolhaDoAlvoDeDuelo.TentarEscolher(
                    posicao.x,
                    posicao.z,
                    EntradaNoJogo.PersonagemId,
                    FonteDeAlvosDeDuelo.Listar(),
                    RegrasPvp.DistanciaMaximaConviteDueloMetros,
                    out _alvoAtual
                );
            }

            _alvo.text = _temAlvo
                ? TextosPvp.AlvoProximo(_alvoAtual.Nome)
                : TextosPvp.SemAlvoProximo;
            _desafiar.SetEnabled(_temAlvo && !_estado.EmDuelo && !_estado.EmArena);
        }

        private static VisualElement Secao(VisualElement pai, string titulo)
        {
            var secao = new VisualElement();
            secao.AddToClassList(EstiloPvp.Secao);
            var rotulo = new Label(titulo);
            rotulo.AddToClassList(EstiloCore.Subtitulo);
            secao.Add(rotulo);
            pai.Add(secao);
            return secao;
        }

        private static Label Texto(VisualElement pai)
        {
            var texto = new Label();
            texto.AddToClassList(EstiloCore.Texto);
            pai.Add(texto);
            return texto;
        }

        private static Button Botao(
            VisualElement pai,
            string texto,
            System.Action acao,
            bool principal
        )
        {
            var botao = new Button(acao) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            if (principal)
            {
                botao.AddToClassList(EstiloCore.BotaoPrincipal);
            }
            pai.Add(botao);
            return botao;
        }
    }
}
