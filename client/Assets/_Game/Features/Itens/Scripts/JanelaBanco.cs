using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Janela do banco pessoal da conta (tecla B; issue #177), registrada nas <see cref="JanelasDoJogo"/>.
    /// Armazenamento compartilhado entre todos os personagens da mesma conta.
    /// Abre o inventário lado a lado para permitir movimentação por arrastar e soltar e Ctrl+clique (#566).
    /// </summary>
    public sealed class JanelaBanco : JanelaDoJogo
    {
        public const string Chave = "itens.banco";
        public const string Titulo = "Banco Pessoal";
        public const string Tecla = "B";
        public const int Ordem = 25;

        public static JanelaBanco Instancia { get; private set; }

        public static bool EstaAberta => JanelasDoJogo.EstaAberta(Chave);

        public static void Alternar()
        {
            if (EstaAberta)
            {
                Fechar();
            }
            else
            {
                Abrir();
            }
        }

        public static void Abrir() => JanelasDoJogo.Abrir(Chave);

        public static void Fechar() => JanelasDoJogo.Fechar(Chave);

        private BancoResponse _estado;
        private int _abaAtiva;
        private bool _ocupado;

        private Label _rotuloSaldo;
        private Label _rotuloCapacidade;
        private Label _rotuloAviso;
        private VisualElement _barraAbas;
        private Button _botaoExpandir;
        private VisualElement _grade;
        private List<VisualElement> _slots = new List<VisualElement>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaBanco())
            );
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Instancia = null;
        }

        public static void DepositarItem(long instanciaId)
        {
            if (Instancia != null && Instancia.Aberta)
            {
                _ = Instancia.ExecutarDepositar(instanciaId, false, 0, 0);
            }
        }

        public static void RetirarItem(long instanciaId)
        {
            if (Instancia != null && Instancia.Aberta)
            {
                _ = Instancia.ExecutarRetirar(instanciaId, false, 0, 0);
            }
        }

        protected override void Montar(VisualElement conteudo)
        {
            conteudo.style.minWidth = 520f;
            conteudo.style.maxWidth = 600f;
            conteudo.style.paddingLeft = 14f;
            conteudo.style.paddingRight = 14f;
            conteudo.style.paddingTop = 10f;
            conteudo.style.paddingBottom = 12f;

            EstiloItens.Aplicar(conteudo);

            var topo = new VisualElement();
            topo.style.flexDirection = FlexDirection.Row;
            topo.style.justifyContent = Justify.SpaceBetween;
            topo.style.alignItems = Align.Center;
            topo.style.marginBottom = 6f;
            topo.style.paddingBottom = 6f;
            topo.style.borderBottomWidth = 1f;
            topo.style.borderBottomColor = new Color(0.25f, 0.28f, 0.35f, 0.8f);

            var info = new VisualElement();
            var titulo = new Label("Banco da Conta");
            titulo.AddToClassList(EstiloCore.Titulo);
            titulo.style.fontSize = 15f;
            titulo.style.unityFontStyleAndWeight = FontStyle.Bold;
            titulo.style.color = new Color(0.92f, 0.94f, 0.98f);
            info.Add(titulo);

            var subtitulo = new Label("Itens compartilhados entre todos os personagens");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);
            subtitulo.style.fontSize = 10.5f;
            subtitulo.style.color = new Color(0.65f, 0.70f, 0.80f);
            info.Add(subtitulo);
            topo.Add(info);

            var direita = new VisualElement();
            direita.style.alignItems = Align.FlexEnd;

            _rotuloSaldo = new Label("Coroas: 0");
            _rotuloSaldo.AddToClassList(EstiloCore.Subtitulo);
            _rotuloSaldo.style.fontSize = 13f;
            _rotuloSaldo.style.unityFontStyleAndWeight = FontStyle.Bold;
            _rotuloSaldo.style.color = new Color(0.95f, 0.80f, 0.30f);
            direita.Add(_rotuloSaldo);

            _rotuloCapacidade = new Label("0 / 40");
            _rotuloCapacidade.AddToClassList(EstiloCore.Texto);
            _rotuloCapacidade.style.fontSize = 11f;
            _rotuloCapacidade.style.color = new Color(0.70f, 0.75f, 0.85f);
            direita.Add(_rotuloCapacidade);

            topo.Add(direita);
            conteudo.Add(topo);

            _rotuloAviso = new Label("");
            _rotuloAviso.AddToClassList(EstiloCore.Mensagem);
            _rotuloAviso.style.fontSize = 11f;
            _rotuloAviso.style.marginBottom = 6f;
            _rotuloAviso.style.display = DisplayStyle.None;
            conteudo.Add(_rotuloAviso);

            var barraTopo = new VisualElement();
            barraTopo.style.flexDirection = FlexDirection.Row;
            barraTopo.style.justifyContent = Justify.SpaceBetween;
            barraTopo.style.alignItems = Align.Center;
            barraTopo.style.marginBottom = 8f;

            _barraAbas = new VisualElement();
            _barraAbas.style.flexDirection = FlexDirection.Row;
            barraTopo.Add(_barraAbas);

            _botaoExpandir = new Button(AoClicarExpandir)
            {
                text = $"+ Expandir ({RegrasBanco.CustoExpansaoBancoCoroas} coroas)",
            };
            _botaoExpandir.AddToClassList(EstiloCore.Botao);
            _botaoExpandir.style.fontSize = 11f;
            _botaoExpandir.style.paddingLeft = 8f;
            _botaoExpandir.style.paddingRight = 8f;
            _botaoExpandir.style.paddingTop = 3f;
            _botaoExpandir.style.paddingBottom = 3f;
            barraTopo.Add(_botaoExpandir);

            conteudo.Add(barraTopo);

            _grade = new VisualElement();
            _grade.AddToClassList(EstiloItens.Grade);
            _grade.style.flexDirection = FlexDirection.Row;
            _grade.style.flexWrap = Wrap.Wrap;
            _grade.style.justifyContent = Justify.FlexStart;
            _grade.style.alignItems = Align.FlexStart;
            conteudo.Add(_grade);

            var rodape = new VisualElement();
            rodape.style.marginTop = 8f;
            rodape.style.paddingTop = 6f;
            rodape.style.borderTopWidth = 1f;
            rodape.style.borderTopColor = new Color(0.20f, 0.22f, 0.28f, 0.8f);

            var dicaCtrl = new Label(
                "Dica: Ctrl+Clique move instantaneamente entre a bolsa e o banco."
            );
            dicaCtrl.style.fontSize = 10.5f;
            dicaCtrl.style.color = new Color(0.60f, 0.65f, 0.75f);
            rodape.Add(dicaCtrl);
            conteudo.Add(rodape);
        }

        protected override void AoAbrir()
        {
            Instancia = this;

            // Abre o inventário junto para permitir ver bolsa e banco simultaneamente
            if (!JanelasDoJogo.EstaAberta(JanelaInventario.Chave))
            {
                JanelasDoJogo.Abrir(JanelaInventario.Chave);
            }

            _ = Carregar();
        }

        protected override void AoFechar()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }
        }

        private async System.Threading.Tasks.Task Carregar()
        {
            if (!EntradaNoJogo.NoJogo)
            {
                return;
            }

            _ocupado = true;
            MostrarAviso(null, false);
            try
            {
                var personagemId = EntradaNoJogo.PersonagemId;
                var resposta = await ClienteApi.Get<BancoResponse>(
                    RotasItens.BancoDe(personagemId)
                );
                if (!Aberta || !EntradaNoJogo.NoJogo || EntradaNoJogo.PersonagemId != personagemId)
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _estado = resposta.Corpo;
                    AtualizarInterface();
                    if (_estado.Bolsa != null)
                    {
                        JanelaInventario.AtualizarCom(_estado.Bolsa);
                    }
                }
                else
                {
                    MostrarAviso(resposta.Erro?.Mensagem ?? "Erro ao carregar banco.", true);
                }
            }
            finally
            {
                _ocupado = false;
            }
        }

        public async System.Threading.Tasks.Task ExecutarDepositar(
            long instanciaId,
            bool temDestino,
            int aba,
            int casa
        )
        {
            if (_ocupado || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            _ocupado = true;
            MostrarAviso(null, false);
            try
            {
                var personagemId = EntradaNoJogo.PersonagemId;
                var pedido = new PedidoDepositarBanco
                {
                    InstanciaId = instanciaId,
                    TemDestino = temDestino,
                    Aba = aba,
                    Casa = casa,
                };
                var resposta = await ClienteApi.Post<BancoResponse>(
                    RotasItens.BancoDepositarDe(personagemId),
                    pedido
                );

                if (!Aberta || !EntradaNoJogo.NoJogo || EntradaNoJogo.PersonagemId != personagemId)
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _estado = resposta.Corpo;
                    AtualizarInterface();
                    if (_estado.Bolsa != null)
                    {
                        JanelaInventario.AtualizarCom(_estado.Bolsa);
                    }
                    MostrarAviso("Item guardado no banco.", false);
                }
                else
                {
                    MostrarAviso(resposta.Erro?.Mensagem ?? "Erro ao depositar item.", true);
                }
            }
            finally
            {
                _ocupado = false;
            }
        }

        public async System.Threading.Tasks.Task ExecutarRetirar(
            long instanciaId,
            bool temDestino,
            int aba,
            int casa
        )
        {
            if (_ocupado || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            _ocupado = true;
            MostrarAviso(null, false);
            try
            {
                var personagemId = EntradaNoJogo.PersonagemId;
                var pedido = new PedidoRetirarBanco
                {
                    InstanciaId = instanciaId,
                    TemDestino = temDestino,
                    Aba = aba,
                    Casa = casa,
                };
                var resposta = await ClienteApi.Post<BancoResponse>(
                    RotasItens.BancoRetirarDe(personagemId),
                    pedido
                );

                if (!Aberta || !EntradaNoJogo.NoJogo || EntradaNoJogo.PersonagemId != personagemId)
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _estado = resposta.Corpo;
                    AtualizarInterface();
                    if (_estado.Bolsa != null)
                    {
                        JanelaInventario.AtualizarCom(_estado.Bolsa);
                    }
                    MostrarAviso("Item retirado do banco.", false);
                }
                else
                {
                    MostrarAviso(resposta.Erro?.Mensagem ?? "Erro ao retirar item.", true);
                }
            }
            finally
            {
                _ocupado = false;
            }
        }

        private async void AoClicarExpandir()
        {
            if (_ocupado || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            _ocupado = true;
            MostrarAviso(null, false);
            try
            {
                var personagemId = EntradaNoJogo.PersonagemId;
                var resposta = await ClienteApi.Post<BancoResponse>(
                    RotasItens.BancoExpandirDe(personagemId),
                    null
                );

                if (!Aberta || !EntradaNoJogo.NoJogo || EntradaNoJogo.PersonagemId != personagemId)
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _estado = resposta.Corpo;
                    _abaAtiva = _estado.Capacidade.Abas - 1;
                    AtualizarInterface();
                    if (_estado.Bolsa != null)
                    {
                        JanelaInventario.AtualizarCom(_estado.Bolsa);
                    }
                    MostrarAviso(_estado.Aviso ?? "Aba do banco desbloqueada!", false);
                }
                else
                {
                    MostrarAviso(resposta.Erro?.Mensagem ?? "Erro ao expandir banco.", true);
                }
            }
            finally
            {
                _ocupado = false;
            }
        }

        private void AtualizarInterface()
        {
            if (_estado == null)
            {
                return;
            }

            _rotuloSaldo.text = $"Coroas: {_estado.Coroas}";
            var totalOcupados = _estado.Itens.Length;
            _rotuloCapacidade.text = $"Total: {totalOcupados} / {_estado.Capacidade.Total}";

            // Abas
            _barraAbas.Clear();
            for (var i = 0; i < _estado.Capacidade.Abas; i++)
            {
                var indice = i;
                var botaoAba = new Button(() => SelecionarAba(indice))
                {
                    text = $"Aba {indice + 1}",
                };
                botaoAba.AddToClassList(EstiloItens.Aba);
                if (indice == _abaAtiva)
                {
                    botaoAba.AddToClassList(EstiloItens.AbaAtiva);
                }
                _barraAbas.Add(botaoAba);
            }

            _botaoExpandir.style.display =
                _estado.Capacidade.Abas < RegrasBanco.AbasBancoMaximas
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            // Grade da aba ativa
            _grade.Clear();
            _slots.Clear();

            var casas = _estado.Capacidade.CasasPorAba;
            for (var c = 0; c < casas; c++)
            {
                var casa = c;
                var entrada = ObterItemNaCasa(_abaAtiva, casa);
                var slot = CriarSlot(entrada, _abaAtiva, casa);
                _grade.Add(slot);
                _slots.Add(slot);
            }
        }

        private void SelecionarAba(int indice)
        {
            _abaAtiva = indice;
            AtualizarInterface();
        }

        private EntradaInventario ObterItemNaCasa(int aba, int casa)
        {
            if (_estado?.Itens == null)
            {
                return null;
            }

            foreach (var item in _estado.Itens)
            {
                if (item.Aba == aba && item.Casa == casa)
                {
                    return item;
                }
            }

            return null;
        }

        private VisualElement CriarSlot(EntradaInventario entrada, int aba, int casa)
        {
            var slot = new VisualElement();
            slot.AddToClassList(EstiloItens.Slot);
            slot.style.width = 44f;
            slot.style.height = 44f;
            slot.style.marginRight = 4f;
            slot.style.marginBottom = 4f;

            var icone = new VisualElement { pickingMode = PickingMode.Ignore };
            icone.AddToClassList(EstiloItens.Icone);
            var sigla = new Label { pickingMode = PickingMode.Ignore };
            sigla.AddToClassList(EstiloCore.Texto);
            sigla.AddToClassList(EstiloItens.Sigla);
            slot.Add(icone);
            slot.Add(sigla);

            if (entrada == null)
            {
                slot.AddToClassList(EstiloItens.SlotVazio);
                return slot;
            }

            var dados = entrada.Equipamento;
            if (dados != null)
            {
                var tier = RegrasTier.TierDe(dados);
                slot.AddToClassList(EstiloItens.Tier(tier));
                var letra = new Label(RegrasTier.Letra(tier)) { pickingMode = PickingMode.Ignore };
                letra.AddToClassList(EstiloItens.LetraTier);
                slot.Add(letra);
            }

            sigla.text = Abreviar(entrada.Nome);
            slot.tooltip = entrada.Nome;

            slot.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0 && e.ctrlKey)
                {
                    e.StopImmediatePropagation();
                    _ = ExecutarRetirar(entrada.InstanciaId, false, 0, 0);
                }
                else if (e.button == 1)
                {
                    e.StopImmediatePropagation();
                    _ = ExecutarRetirar(entrada.InstanciaId, false, 0, 0);
                }
            });

            return slot;
        }

        private static string Abreviar(string nome)
        {
            if (string.IsNullOrEmpty(nome))
            {
                return "";
            }

            var partes = nome.Split(' ');
            if (partes.Length == 1)
            {
                return nome.Length <= 3 ? nome : nome.Substring(0, 3);
            }

            var sigla = "";
            foreach (var p in partes)
            {
                if (p.Length > 0 && char.IsLetter(p[0]))
                {
                    sigla += char.ToUpperInvariant(p[0]);
                    if (sigla.Length >= 3)
                    {
                        break;
                    }
                }
            }

            return sigla;
        }

        private void MostrarAviso(string mensagem, bool erro)
        {
            if (_rotuloAviso == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(mensagem))
            {
                _rotuloAviso.style.display = DisplayStyle.None;
                return;
            }

            _rotuloAviso.text = mensagem;
            _rotuloAviso.style.color = erro
                ? new Color(0.95f, 0.40f, 0.40f)
                : new Color(0.50f, 0.90f, 0.50f);
            _rotuloAviso.style.display = DisplayStyle.Flex;
        }
    }
}
