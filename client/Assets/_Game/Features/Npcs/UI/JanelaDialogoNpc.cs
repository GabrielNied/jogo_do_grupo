using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Npcs.UI
{
    /// <summary>
    /// Janela modal de diálogo com NPCs na cidade (#178).
    /// Aberta pelo servidor ao interagir com um NPC próximo; apresenta nome, papel,
    /// a fala do interlocutor e botões de ação para fechar ou despachar para serviços da cidade.
    /// </summary>
    public sealed class JanelaDialogoNpc : JanelaDoJogo
    {
        public const string Chave = "npcs.dialogo";
        public const string Titulo = "Diálogo";
        public const int Ordem = 90;

        private static DialogoDoNpc? s_ultimoDialogo;
        private static IDisposable? s_assinaturaDialogo;
        private static IDisposable? s_assinaturaRecusa;

        private Label _rotuloNome;
        private Label _rotuloPapel;
        private Label _rotuloFala;
        private VisualElement _containerOpcoes;
        private Label _rotuloAviso;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(CriarDefinicao());
            ConfigurarEscutaDeRede();
        }

        public static DefinicaoDeJanela CriarDefinicao()
        {
            return new DefinicaoDeJanela(Chave, Titulo, null, Ordem, () => new JanelaDialogoNpc());
        }

        private static void ConfigurarEscutaDeRede()
        {
            s_assinaturaDialogo?.Dispose();
            s_assinaturaRecusa?.Dispose();

            s_assinaturaDialogo = ConexaoTempoReal.Assinar<DialogoDoNpc>(
                NpcsMessageIds.DialogoDoNpc,
                dialogo =>
                {
                    s_ultimoDialogo = dialogo;
                    JanelasDoJogo.Abrir(Chave);
                    if (InstanciaAtual is JanelaDialogoNpc janela && janela.Aberta)
                    {
                        janela.Atualizar(dialogo);
                    }
                }
            );

            s_assinaturaRecusa = ConexaoTempoReal.Assinar<DialogoRecusado>(
                NpcsMessageIds.DialogoRecusado,
                recusa =>
                {
                    if (recusa.Motivo != MotivoRecusaDialogo.Nenhum)
                    {
                        if (InstanciaAtual is JanelaDialogoNpc janela && janela.Aberta)
                        {
                            janela.ExibirAviso(ObterTextoRecusa(recusa.Motivo));
                        }
                    }
                }
            );
        }

        private static string ObterTextoRecusa(MotivoRecusaDialogo motivo) =>
            motivo switch
            {
                MotivoRecusaDialogo.LongeDemais => "Você está longe demais do NPC.",
                MotivoRecusaDialogo.NpcNaoEncontrado => "NPC não encontrado.",
                MotivoRecusaDialogo.EmCombate => "Você não pode conversar durante o combate.",
                MotivoRecusaDialogo.OpcaoInvalida => "Opção de diálogo inválida.",
                _ => "Não foi possível conversar.",
            };

        public static JanelaDialogoNpc? InstanciaAtual { get; private set; }

        protected override void Montar(VisualElement conteudo)
        {
            InstanciaAtual = this;

            conteudo.style.minWidth = 460f;
            conteudo.style.maxWidth = 520f;
            conteudo.style.paddingLeft = 16f;
            conteudo.style.paddingRight = 16f;
            conteudo.style.paddingTop = 14f;
            conteudo.style.paddingBottom = 16f;

            // Cabeçalho do NPC
            var cabecalho = new VisualElement();
            cabecalho.style.marginBottom = 12f;

            _rotuloNome = new Label("Nome do NPC");
            _rotuloNome.AddToClassList(EstiloCore.Titulo);
            _rotuloNome.style.fontSize = 20f;
            cabecalho.Add(_rotuloNome);

            _rotuloPapel = new Label("Papel");
            _rotuloPapel.AddToClassList(EstiloCore.Subtitulo);
            _rotuloPapel.style.fontSize = 13f;
            cabecalho.Add(_rotuloPapel);

            conteudo.Add(cabecalho);

            // Balão de fala
            var painelFala = new VisualElement();
            painelFala.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.9f);
            painelFala.style.borderTopLeftRadius = 8f;
            painelFala.style.borderTopRightRadius = 8f;
            painelFala.style.borderBottomLeftRadius = 8f;
            painelFala.style.borderBottomRightRadius = 8f;
            painelFala.style.paddingLeft = 14f;
            painelFala.style.paddingRight = 14f;
            painelFala.style.paddingTop = 12f;
            painelFala.style.paddingBottom = 12f;
            painelFala.style.marginBottom = 14f;

            _rotuloFala = new Label("Fala...");
            _rotuloFala.AddToClassList(EstiloCore.Texto);
            _rotuloFala.style.whiteSpace = WhiteSpace.Normal;
            _rotuloFala.style.fontSize = 15f;
            painelFala.Add(_rotuloFala);

            conteudo.Add(painelFala);

            // Container de opções
            _containerOpcoes = new VisualElement();
            _containerOpcoes.style.flexDirection = FlexDirection.Column;
            conteudo.Add(_containerOpcoes);

            // Aviso de erro
            _rotuloAviso = new Label();
            _rotuloAviso.AddToClassList(EstiloCore.Mensagem);
            _rotuloAviso.style.color = new Color(0.95f, 0.35f, 0.35f);
            _rotuloAviso.style.marginTop = 8f;
            conteudo.Add(_rotuloAviso);

            if (s_ultimoDialogo != null)
            {
                Atualizar(s_ultimoDialogo);
            }
        }

        public void Atualizar(DialogoDoNpc dialogo)
        {
            if (dialogo == null)
            {
                return;
            }

            _rotuloAviso.text = string.Empty;
            _rotuloNome.text = dialogo.Nome;
            _rotuloPapel.text = NpcNoMundo.ObterTituloDoPapel(dialogo.Papel);
            _rotuloFala.text = $"\"{dialogo.Fala}\"";

            _containerOpcoes.Clear();

            if (dialogo.Opcoes != null)
            {
                foreach (var opcao in dialogo.Opcoes)
                {
                    if (opcao == null)
                    {
                        continue;
                    }

                    var btn = new Button(() => AoEscolherOpcao(dialogo.NpcId, opcao));
                    btn.text = opcao.Texto;
                    btn.AddToClassList(EstiloCore.Botao);
                    if (opcao.Acao == AcaoDeDialogo.AbrirServico)
                    {
                        btn.AddToClassList(EstiloCore.BotaoPrincipal);
                    }
                    btn.style.marginBottom = 6f;
                    btn.style.height = 36f;
                    btn.style.fontSize = 14f;
                    _containerOpcoes.Add(btn);
                }
            }
        }

        private void AoEscolherOpcao(int npcId, OpcaoDeDialogo opcao)
        {
            ConexaoTempoReal.Enviar(
                NpcsMessageIds.ExecutarOpcaoDeDialogo,
                new ExecutarOpcaoDeDialogo(npcId, opcao.Id)
            );

            JanelasDoJogo.Fechar(Chave);

            if (opcao.Acao == AcaoDeDialogo.AbrirServico && !string.IsNullOrEmpty(opcao.Parametro))
            {
                JanelasDoJogo.Abrir(opcao.Parametro);
            }
        }

        public void ExibirAviso(string aviso)
        {
            _rotuloAviso.text = aviso;
        }

        protected override void AoFechar()
        {
            base.AoFechar();
            if (InstanciaAtual == this)
            {
                InstanciaAtual = null;
            }
        }
    }
}
