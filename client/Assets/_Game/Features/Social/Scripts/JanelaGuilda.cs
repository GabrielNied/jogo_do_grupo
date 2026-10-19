using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Janela de Guilda (tecla G). Permite criar guilda, convidar jogadores, visualizar membros,
    /// alterar cargos e sair da guilda.
    /// </summary>
    internal sealed class JanelaGuilda : JanelaDoJogo
    {
        public const string Chave = "social.guilda";
        public const string TituloPadrao = "Guilda";
        public const string Tecla = "G";
        public const int Ordem = 55;

        private readonly EstadoDaGuilda _estado;

        private VisualElement _corpo;
        private Label _avisoErro;

        public override bool MolduraSocial => true;

        public JanelaGuilda()
            : this(EstadoDaGuilda.Instancia) { }

        internal JanelaGuilda(EstadoDaGuilda estado)
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
                () => new JanelaGuilda()
            );
        }

        protected override void Montar(VisualElement conteudo)
        {
            EstiloSocial.Aplicar(conteudo);

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

            if (!_estado.TemGuilda)
            {
                MontarSemGuilda(_corpo);
            }
            else
            {
                MontarComGuilda(_corpo);
            }
        }

        private void MontarSemGuilda(VisualElement container)
        {
            if (_estado.ConviteRecebido != null)
            {
                var blocoConvite = new VisualElement();
                blocoConvite.AddToClassList(EstiloSocial.Secao);

                var tituloConvite = new Label(
                    $"Convite de Guilda: {_estado.ConviteRecebido.NomeDaGuilda} (por {_estado.ConviteRecebido.ConvidanteNome})"
                );
                tituloConvite.AddToClassList(EstiloCore.Titulo);
                blocoConvite.Add(tituloConvite);

                var botoesConvite = new VisualElement();
                botoesConvite.AddToClassList(EstiloCore.LinhaBotoes);

                var aceitar = new Button(() =>
                    _estado.ResponderConvite(_estado.ConviteRecebido.GuildaId, true)
                )
                {
                    text = "Aceitar",
                };
                aceitar.AddToClassList(EstiloCore.Botao);

                var recusar = new Button(() =>
                    _estado.ResponderConvite(_estado.ConviteRecebido.GuildaId, false)
                )
                {
                    text = "Recusar",
                };
                recusar.AddToClassList(EstiloCore.Botao);

                botoesConvite.Add(aceitar);
                botoesConvite.Add(recusar);
                blocoConvite.Add(botoesConvite);
                container.Add(blocoConvite);
            }

            var secaoCriar = new VisualElement();
            secaoCriar.AddToClassList(EstiloSocial.Secao);

            var tituloCriar = new Label("Fundar Nova Guilda");
            tituloCriar.AddToClassList(EstiloCore.Titulo);
            secaoCriar.Add(tituloCriar);

            var infoCusto = new Label(
                $"Custo de fundação: {RegrasGuilda.CustoCriacaoCoroas:N0} coroas"
            );
            infoCusto.AddToClassList(EstiloCore.Texto);
            secaoCriar.Add(infoCusto);

            var linhaNome = new VisualElement();
            linhaNome.AddToClassList(EstiloSocial.Convidar);

            var campoNome = new TextField { label = "Nome da Guilda:" };
            campoNome.maxLength = RegrasGuilda.NomeMaximoCaracteres;

            var botaoCriar = new Button(() => _estado.Criar(campoNome.value)) { text = "Fundar" };
            botaoCriar.AddToClassList(EstiloCore.Botao);

            linhaNome.Add(campoNome);
            linhaNome.Add(botaoCriar);
            secaoCriar.Add(linhaNome);

            container.Add(secaoCriar);
        }

        private void MontarComGuilda(VisualElement container)
        {
            var dados = _estado.Dados;

            var cabecalho = new VisualElement();
            cabecalho.AddToClassList(EstiloSocial.Secao);

            var nomeGuilda = new Label(dados.Nome);
            nomeGuilda.AddToClassList(EstiloCore.Titulo);
            cabecalho.Add(nomeGuilda);

            var infoGeral = new Label(
                $"Membros: {dados.Membros.Length}/{RegrasGuilda.MaximoDeMembros} | Seu Cargo: {dados.MeuCargo}"
            );
            infoGeral.AddToClassList(EstiloCore.Texto);
            cabecalho.Add(infoGeral);

            container.Add(cabecalho);

            if (RegrasGuilda.PodeConvidar(dados.MeuCargo))
            {
                var linhaConvidar = new VisualElement();
                linhaConvidar.AddToClassList(EstiloSocial.Convidar);

                var campoAlvo = new TextField { label = "Convidar jogador:" };
                var botaoConvidar = new Button(() =>
                {
                    if (!string.IsNullOrWhiteSpace(campoAlvo.value))
                    {
                        _estado.Convidar(campoAlvo.value);
                        campoAlvo.value = "";
                    }
                })
                {
                    text = "Convidar",
                };
                botaoConvidar.AddToClassList(EstiloCore.Botao);

                linhaConvidar.Add(campoAlvo);
                linhaConvidar.Add(botaoConvidar);
                container.Add(linhaConvidar);
            }

            var secaoMembros = new ScrollView();
            secaoMembros.AddToClassList(EstiloSocial.Secao);

            foreach (var membro in dados.Membros)
            {
                var linha = new VisualElement();
                linha.AddToClassList(EstiloSocial.Linha);

                var status = membro.Online ? "●" : "○";
                var rotuloMembro = new Label($"{status} {membro.Nome} ({membro.Cargo})");
                rotuloMembro.AddToClassList(EstiloSocial.Nome);
                if (membro.Cargo == CargoDaGuilda.Lider)
                {
                    rotuloMembro.AddToClassList(EstiloSocial.Lider);
                }
                if (!membro.Online)
                {
                    rotuloMembro.AddToClassList(EstiloSocial.Offline);
                }
                linha.Add(rotuloMembro);

                var acoes = new VisualElement();
                acoes.AddToClassList(EstiloSocial.Acoes);

                if (dados.MeuCargo == CargoDaGuilda.Lider && membro.Cargo != CargoDaGuilda.Lider)
                {
                    var botaoLider = new Button(() => _estado.PassarLideranca(membro.PersonagemId))
                    {
                        text = "Liderar",
                    };
                    botaoLider.AddToClassList(EstiloSocial.Botao);
                    acoes.Add(botaoLider);
                }

                if (
                    RegrasGuilda.PodeAlterarCargo(
                        dados.MeuCargo,
                        membro.Cargo,
                        CargoDaGuilda.Oficial
                    )
                    && membro.Cargo != CargoDaGuilda.Oficial
                )
                {
                    var botaoOficial = new Button(() =>
                        _estado.AlterarCargo(membro.PersonagemId, CargoDaGuilda.Oficial)
                    )
                    {
                        text = "Promover",
                    };
                    botaoOficial.AddToClassList(EstiloSocial.Botao);
                    acoes.Add(botaoOficial);
                }
                else if (
                    RegrasGuilda.PodeAlterarCargo(
                        dados.MeuCargo,
                        membro.Cargo,
                        CargoDaGuilda.Membro
                    )
                    && membro.Cargo != CargoDaGuilda.Membro
                )
                {
                    var botaoMembro = new Button(() =>
                        _estado.AlterarCargo(membro.PersonagemId, CargoDaGuilda.Membro)
                    )
                    {
                        text = "Rebaixar",
                    };
                    botaoMembro.AddToClassList(EstiloSocial.Botao);
                    acoes.Add(botaoMembro);
                }

                if (RegrasGuilda.PodeExpulsar(dados.MeuCargo, membro.Cargo))
                {
                    var botaoExpulsar = new Button(() => _estado.Expulsar(membro.PersonagemId))
                    {
                        text = "Expulsar",
                    };
                    botaoExpulsar.AddToClassList(EstiloSocial.Botao);
                    acoes.Add(botaoExpulsar);
                }

                linha.Add(acoes);
                secaoMembros.Add(linha);
            }

            container.Add(secaoMembros);

            var rodape = new VisualElement();
            rodape.AddToClassList(EstiloSocial.Rodape);

            var botaoSair = new Button(() => _estado.Sair()) { text = "Sair da Guilda" };
            botaoSair.AddToClassList(EstiloCore.Botao);
            rodape.Add(botaoSair);

            container.Add(rodape);
        }
    }
}
