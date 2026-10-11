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
            _painel = new PainelItens(conteudo, DefinirTitulo);
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
