using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Janela do inventário (tecla I), registrada nas <see cref="JanelasDoJogo"/>: paperdoll,
    /// bolsa em grade e resumo de bônus numa janela só. Pede o estado ao servidor, desenha o que
    /// ele devolve (<see cref="PainelInventario"/>) e transforma o que a pessoa faz (clique
    /// direito, arrastar e soltar) em intenções. O servidor decide e responde com o estado novo.
    /// A moldura, o atalho, o Esc e o cursor são do Core.
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

        /// <summary>
        /// Quando o baú volta a abrir (<c>Time.unscaledTime</c>), pela recarga do servidor
        /// (#492). A dica "E: abrir o baú" mostra a espera até lá.
        /// </summary>
        internal static float BauLivreEm;

        // Itens que a pessoa já viu, para marcar como "novo" o que chegou desde a última vez
        // (baú, loot). Só vale para o mesmo personagem.
        private static HashSet<long> _conhecidos;
        private static long _personagemDosConhecidos;

        // Último estado que o servidor mandou, para manter na tela se uma chamada falhar.
        private static EquipamentosResponse _ultimo;
        private static long _personagemDoUltimo;

        private PainelInventario _painel;
        private VisualElement _areaPrincipal;
        private VisualElement _areaFusao;
        private PainelFusao _fusao;
        private EquipamentosResponse _estado;
        private HashSet<long> _novos = new HashSet<long>();
        private HashSet<long> _pendentes = new HashSet<long>();
        private bool _ocupado;

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
            BauLivreEm = 0f;
            _conhecidos = null;
            _personagemDosConhecidos = 0;
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
            _areaPrincipal = new VisualElement();
            _areaFusao = new VisualElement();
            _areaFusao.style.display = DisplayStyle.None;
            conteudo.Add(_areaPrincipal);
            conteudo.Add(_areaFusao);

            _painel = new PainelInventario(_areaPrincipal);
            _painel.AcaoPedida += AoPedirAcao;
            _painel.ConjuntoPedido += numero =>
                _ = Postar(
                    RegrasEquipamentos.RotaDe(EntradaNoJogo.PersonagemId) + "/conjunto",
                    new PedidoConjunto { Conjunto = numero },
                    null
                );
            _painel.BancadaPedida += () => EnviarFusao("fusao/estado", null);
            _painel.ItemDeTestePedido += grau =>
                _ = Postar(
                    RegrasEquipamentos.RotaDe(EntradaNoJogo.PersonagemId)
                        + "/bau-teste?grau="
                        + grau,
                    null,
                    null
                );
            _painel.LimpezaDeTestePedida += () =>
                _ = Postar(
                    RegrasEquipamentos.RotaDe(EntradaNoJogo.PersonagemId) + "/limpar-teste",
                    null,
                    null
                );
            _painel.ItemVisto += id => _novos.Remove(id);
            _fusao = new PainelFusao(_areaFusao, EnviarFusao);
        }

        protected override void AoAbrir()
        {
            var abrindoBau = _abrirBau;
            _abrirBau = false;
            DefinirTitulo(abrindoBau ? TituloBau : Titulo);
            _ = Carregar(abrindoBau);
        }

        protected override void AoFechar()
        {
            _painel?.Encerrar();
        }

        // Depois do await a janela pode ter sido fechada, o jogador pode ter saído do mundo ou o
        // Core pode ter voltado ao login (401): nesses casos não mexe em nada.
        private bool AindaVale(long personagemId)
        {
            return Aberta && EntradaNoJogo.NoJogo && EntradaNoJogo.PersonagemId == personagemId;
        }

        // ---- carregar ----

        private async System.Threading.Tasks.Task Carregar(bool abrindoBau)
        {
            var personagemId = EntradaNoJogo.PersonagemId;
            _painel.MostrarAviso(null, false);
            _painel.MostrarCarregando();
            var ganhos = new HashSet<long>();
            string aviso = null;
            try
            {
                if (abrindoBau)
                {
                    var bau = await ClienteApi.Post<AbrirBauResponse>(
                        RotasItens.BauDe(personagemId),
                        null
                    );
                    if (!AindaVale(personagemId))
                    {
                        return;
                    }

                    if (bau.Sucesso || bau.CodigoErro == CodigosErroItens.BauEmRecarga)
                    {
                        // O servidor é quem confere; isto só faz a dica mostrar a espera.
                        BauLivreEm = Time.unscaledTime + ConfigBau.RecargaSegundos;
                    }

                    if (bau.Sucesso && bau.Corpo != null)
                    {
                        foreach (var ganho in bau.Corpo.Ganhos)
                        {
                            ganhos.Add(ganho.InstanciaId);
                        }

                        aviso = bau.Corpo.Ganhos.Length == 0 ? "O baú estava vazio." : null;
                    }
                    else if (bau.CodigoErro == CodigosErroCliente.Cancelada)
                    {
                        JanelasDoJogo.Fechar();
                        return;
                    }
                    else
                    {
                        aviso = ApresentacaoItens.MensagemDeErro(bau.CodigoErro, true);
                    }
                }

                var resposta = await ClienteApi.Get<EquipamentosResponse>(
                    RegrasEquipamentos.RotaDe(personagemId)
                );
                if (!AindaVale(personagemId))
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    Aplicar(
                        resposta.Corpo,
                        personagemId,
                        ganhos,
                        aviso,
                        aviso != null && ganhos.Count == 0
                    );
                    return;
                }

                if (resposta.CodigoErro == CodigosErroCliente.Cancelada)
                {
                    JanelasDoJogo.Fechar();
                    return;
                }

                MostrarFalha(
                    personagemId,
                    ApresentacaoItens.MensagemDeErro(resposta.CodigoErro, abrindoBau)
                );
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (AindaVale(personagemId))
                {
                    MostrarFalha(personagemId, ApresentacaoItens.MensagemDeErro(null, abrindoBau));
                }
            }
        }

        // Falha: mostra o aviso e mantém o último estado que o servidor mandou (nunca inventa).
        private void MostrarFalha(long personagemId, string aviso)
        {
            if (_ultimo != null && _personagemDoUltimo == personagemId)
            {
                _estado = _ultimo;
                _painel.Mostrar(_estado, _novos, _pendentes, false);
            }
            else
            {
                _painel.MostrarCarregando();
            }

            _painel.MostrarAviso(aviso, true);
        }

        private void Aplicar(
            EquipamentosResponse estado,
            long personagemId,
            HashSet<long> ganhos,
            string aviso,
            bool avisoEhErro
        )
        {
            MarcarNovos(estado, personagemId, ganhos);
            _ultimo = estado;
            _personagemDoUltimo = personagemId;
            Mostrar(estado, aviso, avisoEhErro);
        }

        private void Mostrar(EquipamentosResponse estado, string aviso, bool erro)
        {
            _estado = estado;
            if (estado.ModoFusao)
            {
                _areaPrincipal.style.display = DisplayStyle.None;
                _areaFusao.style.display = DisplayStyle.Flex;
                _fusao.Mostrar(estado, aviso);
                return;
            }

            _areaFusao.style.display = DisplayStyle.None;
            _areaPrincipal.style.display = DisplayStyle.Flex;
            _painel.Mostrar(estado, _novos, _pendentes, _ocupado);
            _painel.MostrarAviso(aviso, erro);
        }

        private void MarcarNovos(
            EquipamentosResponse estado,
            long personagemId,
            HashSet<long> ganhos
        )
        {
            var todos = new HashSet<long>();
            foreach (var item in estado.Inventario)
            {
                todos.Add(item.InstanciaId);
            }

            foreach (var item in estado.Equipados)
            {
                todos.Add(item.InstanciaId);
            }

            if (_conhecidos != null && _personagemDosConhecidos == personagemId)
            {
                foreach (var item in estado.Inventario)
                {
                    if (!_conhecidos.Contains(item.InstanciaId))
                    {
                        _novos.Add(item.InstanciaId);
                    }
                }
            }

            _novos.UnionWith(ganhos);
            _novos.IntersectWith(todos);
            _conhecidos = todos;
            _personagemDosConhecidos = personagemId;
        }

        // ---- intenções ----

        private void AoPedirAcao(AcaoDoInventario acao)
        {
            if (_ocupado || _estado == null)
            {
                return;
            }

            switch (acao.Tipo)
            {
                case TipoAcao.Nada:
                    return;
                case TipoAcao.Recusada:
                    _painel.MostrarAviso(acao.Aviso, true);
                    return;
                case TipoAcao.Descartar:
                    var item = ProcurarNaBolsa(acao.InstanciaId);
                    if (item == null)
                    {
                        return;
                    }

                    _painel.MostrarAviso(null, false);
                    _painel.PedirConfirmacaoDescarte(item, () => Executar(acao), null);
                    return;
                default:
                    Executar(acao);
                    return;
            }
        }

        private EntradaInventario ProcurarNaBolsa(long instanciaId)
        {
            foreach (var item in _estado.Inventario)
            {
                if (item.InstanciaId == instanciaId)
                {
                    return item;
                }
            }

            return null;
        }

        private void Executar(AcaoDoInventario acao)
        {
            if (_ocupado || _estado == null)
            {
                return;
            }

            var personagemId = EntradaNoJogo.PersonagemId;
            var rota = RegrasEquipamentos.RotaDe(personagemId);
            var pendentes = new HashSet<long>(LogicaInventario.InstanciasEnvolvidas(acao, _estado));
            switch (acao.Tipo)
            {
                case TipoAcao.Mover:
                    _ = Postar(
                        RotasItens.MoverDe(personagemId),
                        new PedidoMover
                        {
                            InstanciaId = acao.InstanciaId,
                            Aba = acao.Aba,
                            Casa = acao.Casa,
                        },
                        pendentes
                    );
                    break;
                case TipoAcao.Equipar:
                    _ = Postar(
                        rota + "/equipar",
                        new PedidoEquipar
                        {
                            InstanciaId = acao.InstanciaId,
                            Posicao = acao.Posicao,
                        },
                        pendentes
                    );
                    break;
                case TipoAcao.Desequipar:
                    _ = Postar(
                        rota + "/desequipar",
                        new PedidoDesequipar
                        {
                            Posicao = acao.Posicao,
                            TemDestino = acao.TemDestino,
                            Aba = acao.Aba,
                            Casa = acao.Casa,
                        },
                        pendentes
                    );
                    break;
                case TipoAcao.Descartar:
                    _ = Postar(
                        RotasItens.DescartarDe(personagemId),
                        new PedidoDescartar { InstanciaId = acao.InstanciaId },
                        pendentes
                    );
                    break;
            }
        }

        // A bancada de fusão usa as ações do servidor por nome; null volta ao inventário.
        private void EnviarFusao(string acao, object pedido)
        {
            var personagemId = EntradaNoJogo.PersonagemId;
            if (acao == null)
            {
                _ = Recarregar(personagemId);
                return;
            }

            _ = Postar(RegrasEquipamentos.RotaDe(personagemId) + "/" + acao, pedido, null);
        }

        private async System.Threading.Tasks.Task Recarregar(long personagemId)
        {
            if (_ocupado)
            {
                return;
            }

            _ocupado = true;
            try
            {
                var resposta = await ClienteApi.Get<EquipamentosResponse>(
                    RegrasEquipamentos.RotaDe(personagemId)
                );
                if (!AindaVale(personagemId))
                {
                    return;
                }

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    _ocupado = false;
                    Aplicar(resposta.Corpo, personagemId, new HashSet<long>(), null, false);
                }
                else if (resposta.CodigoErro != CodigosErroCliente.Cancelada)
                {
                    _ocupado = false;
                    MostrarFalha(
                        personagemId,
                        TextosEquipamento.MensagemDeErro(resposta.CodigoErro)
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _ocupado = false;
            }
        }

        // Um pedido por vez. Os exemplares envolvidos ficam semitransparentes e travados até o
        // servidor responder; recusa devolve o estado de antes com a mensagem.
        private async System.Threading.Tasks.Task Postar(
            string caminho,
            object pedido,
            HashSet<long> pendentes
        )
        {
            if (_ocupado)
            {
                return;
            }

            var personagemId = EntradaNoJogo.PersonagemId;
            _ocupado = true;
            _pendentes = pendentes ?? new HashSet<long>();
            if (_estado != null && !_estado.ModoFusao)
            {
                Mostrar(_estado, null, false);
            }

            try
            {
                var resposta = await ClienteApi.Post<EquipamentosResponse>(caminho, pedido);
                if (!AindaVale(personagemId))
                {
                    return;
                }

                _pendentes = new HashSet<long>();
                _ocupado = false;
                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    Aplicar(resposta.Corpo, personagemId, new HashSet<long>(), null, false);
                }
                else if (resposta.CodigoErro != CodigosErroCliente.Cancelada && _estado != null)
                {
                    Mostrar(_estado, TextosEquipamento.MensagemDeErro(resposta.CodigoErro), true);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _pendentes = new HashSet<long>();
                _ocupado = false;
                if (AindaVale(personagemId) && _estado != null)
                {
                    Mostrar(_estado, "Não foi possível concluir. Tente novamente.", true);
                }
            }
            finally
            {
                _ocupado = false;
            }
        }
    }
}
