using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Janela do inventário (tecla I), registrada nas <see cref="JanelasDoJogo"/>: pede o
    /// inventário do personagem logado (<c>GET</c>) ou, quando aberta pelo baú, abre o baú
    /// (<c>POST</c>) e mostra os ganhos e o inventário resultante. Só envia a intenção e mostra
    /// a resposta: o sorteio e a gravação são do servidor. A moldura, o atalho, o Esc e o
    /// cursor são do Core.
    /// </summary>
    internal sealed class JanelaInventario : JanelaDoJogo
    {
        public const string Chave = "itens.inventario";
        public const string Titulo = "Inventário";
        public const string TituloBau = "Baú";
        public const string Tecla = "I";
        public const int Ordem = 20;

        // A janela não recebe parâmetros: o baú marca aqui que a próxima abertura é dele.
        private static bool _abrirBau;

        // Última lista que o servidor mandou, para manter na tela se a próxima chamada falhar.
        private static ResultadoItens _ultimo;
        private static long _personagemDoUltimo;

        private PainelItens _painel;
        private VisualElement _areaItens;
        private VisualElement _areaEquipamentos;
        private PainelEquipamentos _equipamentos;
        private EquipamentosResponse _estadoEquipamentos;
        private bool _ocupadoEquipamentos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            JanelasDoJogo.Registrar(
                new DefinicaoDeJanela(Chave, Titulo, Tecla, Ordem, () => new JanelaInventario())
            );
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, os campos estáticos
        // sobrevivem entre execuções.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _abrirBau = false;
            _ultimo = null;
            _personagemDoUltimo = 0;
        }

        /// <summary>
        /// Abre a janela pedindo ao servidor para abrir o baú. Sem efeito se outra janela ou
        /// o menu estiver aberto.
        /// </summary>
        internal static void AbrirComBau()
        {
            if (JanelasDoJogo.AlgoAberto)
            {
                return;
            }

            _abrirBau = true;
            JanelasDoJogo.Abrir(Chave);
            _abrirBau = false;
        }

        protected override void Montar(VisualElement conteudo)
        {
            var abas = new VisualElement();
            abas.AddToClassList(JogoDoGrupo.Client.Core.UI.EstiloCore.LinhaBotoes);
            var itens = new Button(() =>
            {
                _areaEquipamentos.style.display = DisplayStyle.None;
                _areaItens.style.display = DisplayStyle.Flex;
                Carregar(false);
            })
            {
                text = "Itens",
            };
            itens.AddToClassList(JogoDoGrupo.Client.Core.UI.EstiloCore.Botao);
            var equipamentos = new Button(() => CarregarEquipamentos(null, null))
            {
                text = "Equipamentos",
            };
            equipamentos.AddToClassList(JogoDoGrupo.Client.Core.UI.EstiloCore.Botao);
            abas.Add(itens);
            abas.Add(equipamentos);
            conteudo.Add(abas);
            _areaItens = new VisualElement();
            _areaItens.style.flexGrow = 1;
            _areaItens.style.minHeight = 0;
            _areaEquipamentos = new VisualElement();
            _areaEquipamentos.style.display = DisplayStyle.None;
            _areaEquipamentos.style.flexGrow = 1;
            _areaEquipamentos.style.minHeight = 0;
            conteudo.Add(_areaItens);
            conteudo.Add(_areaEquipamentos);
            _painel = new PainelItens(_areaItens, DefinirTitulo);
            _equipamentos = new PainelEquipamentos(_areaEquipamentos, CarregarEquipamentos);
        }

        private async void CarregarEquipamentos(string acao, object pedido)
        {
            if (_ocupadoEquipamentos)
                return;
            var personagemId = EntradaNoJogo.PersonagemId;
            _ocupadoEquipamentos = true;
            _areaItens.style.display = DisplayStyle.None;
            _areaEquipamentos.style.display = DisplayStyle.Flex;
            DefinirTitulo("Equipamentos");
            _equipamentos.Avisar("Carregando...");
            try
            {
                var rota = RegrasEquipamentos.RotaDe(personagemId);
                var resposta =
                    acao == null
                        ? await ClienteApi.Get<EquipamentosResponse>(rota)
                        : await ClienteApi.Post<EquipamentosResponse>(rota + "/" + acao, pedido);
                if (!AindaVale(personagemId))
                    return;
                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _estadoEquipamentos = resposta.Corpo;
                    _equipamentos.Mostrar(_estadoEquipamentos);
                }
                else
                {
                    var aviso = MensagemEquipamento(resposta.CodigoErro);
                    if (_estadoEquipamentos != null)
                        _equipamentos.Mostrar(_estadoEquipamentos, aviso);
                    else
                        _equipamentos.Avisar(aviso);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (AindaVale(personagemId))
                    _equipamentos.Avisar(
                        "Não foi possível atualizar os equipamentos. Abra a aba para tentar novamente."
                    );
            }
            finally
            {
                _ocupadoEquipamentos = false;
            }
        }

        private static string MensagemEquipamento(string codigo)
        {
            switch (codigo)
            {
                case "InventarioCheio":
                    return "Não há espaço no inventário para guardar as peças retiradas.";
                case "NivelInsuficiente":
                    return "Seu personagem ainda não tem o nível exigido pela peça.";
                case "ArmaDeDuasMaos":
                    return "Retire a arma de duas mãos antes de equipar a mão secundária.";
                case "ItemNaoEncontrado":
                    return "Esse item não está mais disponível. Reabra a aba para atualizar.";
                case "PosicaoInvalida":
                    return "Essa peça não pode ser equipada nessa posição.";
                default:
                    return "Não foi possível alterar os equipamentos. Tente novamente.";
            }
        }

        protected override void AoAbrir()
        {
            var abrindoBau = _abrirBau;
            _abrirBau = false;
            Carregar(abrindoBau);
        }

        private async void Carregar(bool abrindoBau)
        {
            var personagemId = EntradaNoJogo.PersonagemId;
            _painel.MostrarCarregando(abrindoBau ? TituloBau : Titulo);

            ResultadoItens resultado;
            try
            {
                if (abrindoBau)
                {
                    var resposta = await ClienteApi.Post<AbrirBauResponse>(
                        RotasItens.BauDe(personagemId),
                        null
                    );
                    if (!AindaVale(personagemId))
                    {
                        return;
                    }

                    resultado = ApresentacaoItens.InterpretarBau(
                        resposta.Sucesso,
                        resposta.CodigoErro,
                        resposta.Corpo
                    );
                }
                else
                {
                    var resposta = await ClienteApi.Get<InventarioResponse>(
                        RotasItens.InventarioDe(personagemId)
                    );
                    if (!AindaVale(personagemId))
                    {
                        return;
                    }

                    resultado = ApresentacaoItens.InterpretarInventario(
                        resposta.Sucesso,
                        resposta.CodigoErro,
                        resposta.Corpo
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (!AindaVale(personagemId))
                {
                    return;
                }

                resultado = ResultadoItens.Falha(
                    ApresentacaoItens.MensagemDeErro(null, abrindoBau),
                    null
                );
            }

            MostrarResultado(resultado, abrindoBau, personagemId);
        }

        // Depois do await a janela pode ter sido fechada, o jogador pode ter saído do mundo ou o
        // Core pode ter voltado ao login (401): nesses casos não mexe em nada.
        private bool AindaVale(long personagemId)
        {
            return Aberta && EntradaNoJogo.NoJogo && EntradaNoJogo.PersonagemId == personagemId;
        }

        private void MostrarResultado(ResultadoItens resultado, bool abrindoBau, long personagemId)
        {
            if (resultado.Deu)
            {
                _ultimo = resultado;
                _personagemDoUltimo = personagemId;
                var titulo = ApresentacaoItens.TituloDoInventario(resultado.Inventario.Count);
                var aviso =
                    abrindoBau && resultado.Ganhos.Count == 0 ? "O baú estava vazio." : null;
                _painel.Mostrar(titulo, aviso, false, resultado.Ganhos, resultado.Inventario);
                return;
            }

            if (resultado.CodigoErro == CodigosErroCliente.Cancelada)
            {
                JanelasDoJogo.Fechar();
                return;
            }

            // Falha: mostra o aviso e mantém a última lista que o servidor mandou (nunca inventa).
            var anterior =
                _ultimo != null && _personagemDoUltimo == personagemId ? _ultimo.Inventario : null;
            var tituloAnterior =
                anterior != null ? ApresentacaoItens.TituloDoInventario(anterior.Count)
                : abrindoBau ? TituloBau
                : Titulo;
            _painel.Mostrar(tituloAnterior, resultado.Aviso, true, null, anterior);
        }
    }
}
