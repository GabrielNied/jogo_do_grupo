using System.Collections.Generic;
using System.Text;
using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Client.Features.Talentos
{
    /// <summary>Textos da árvore de atributos, em português, e a conversão de estados em frases.</summary>
    internal static class TextosTalentos
    {
        public const string Titulo = "Árvore de atributos";
        public const string Carregando = "Carregando a árvore...";
        public const string ErroAoCarregar =
            "Não foi possível carregar a árvore. Feche e abra de novo.";
        public const string SemConexao = "Sem conexão com o servidor. Tente de novo.";
        public const string Comprando = "Comprando...";
        public const string Desatualizada =
            "A árvore mudou. Feche e abra a janela para ver a versão nova.";
        public const string Generica = "Não foi possível comprar agora.";
        public const string Compra = "Comprar rank";
        public const string RankMaximo = "Rank máximo";
        public const string SemPontos =
            "Você não tem pontos livres. Suba de nível para ganhar mais.";
        public const string Dica =
            "Escolha um nó para ver o custo e o efeito. Os pontos vêm de subir de nível.";
        public const string FonteDosAtributos = "Atributos da árvore (equipamentos ficam à parte)";
        public const string Redistribuir = "Redistribuir pontos";
        public const string Redistribuindo = "Redistribuindo...";
        public const string SemPontosParaRedistribuir =
            "Você ainda não gastou nenhum ponto nesta árvore.";
        public const string SaldoInsuficiente =
            "Você não tem coroas suficientes para pagar a redistribuição.";
        public const string EmCombate = "Não é permitido alterar talentos durante o combate.";
        public const string TituloPerfis = "Perfis de build";
        public const string SalvarPerfil = "Salvar perfil";
        public const string CarregarPerfil = "Carregar";
        public const string ExcluirPerfil = "Excluir";
        public const string RenomearPerfil = "Renomear";
        public const string SalvandoPerfil = "Salvando...";
        public const string CarregandoPerfil = "Carregando...";
        public const string PerfilCarregado = "Perfil aplicado com sucesso.";
        public const string PerfilSalvo = "Perfil salvo com sucesso.";
        public const string LimitePerfisAtingido = "Limite de perfis atingido (máximo 5).";
        public const string NomePerfilInvalido =
            "Nome de perfil inválido (deve ter entre 1 e 32 caracteres).";

        /// <summary>O botão do perfil leva o custo que o servidor calculou; igual ao atual não cobra.</summary>
        public static string CarregarPerfilComCusto(long custo) =>
            CarregarPerfil + " (" + custo + " coroas)";

        public static string RedistribuirComCusto(long custo) =>
            Redistribuir + " (" + custo + " coroas)";

        public static string ConfirmarRedistribuicao(long custo) =>
            "Deseja redistribuir todos os pontos por "
            + custo
            + " coroas? Seus pontos gastos voltarão como pontos livres.";

        public static string CustoRedistribuicao(long custo) => "Custo: " + custo + " coroas";

        // Códigos do Core comparados pelo valor, como as outras funcionalidades fazem.
        private const string CodigoSemConexao = "SemConexao";
        private const string CodigoHttpBloqueado = "HttpBloqueado";
        private const string CodigoCancelada = "Cancelada";

        public static bool FoiCancelada(string codigo) => codigo == CodigoCancelada;

        public static string Rotulo(AtributoPrincipal atributo)
        {
            switch (atributo)
            {
                case AtributoPrincipal.Forca:
                    return "Força";
                case AtributoPrincipal.Vigor:
                    return "Vigor";
                case AtributoPrincipal.Agilidade:
                    return "Agilidade";
                case AtributoPrincipal.Intelecto:
                    return "Intelecto";
                default:
                    return "Espírito";
            }
        }

        public static string NomeDoEstado(EstadoDoNo estado)
        {
            switch (estado)
            {
                case EstadoDoNo.Bloqueado:
                    return "Bloqueado";
                case EstadoDoNo.Disponivel:
                    return "Disponível";
                case EstadoDoNo.Comprado:
                    return "Comprado";
                case EstadoDoNo.NoMaximo:
                    return "No máximo";
                default:
                    return "Ativo conforme a skill";
            }
        }

        /// <summary>Marca curta dentro do nó, para o estado não depender só da cor.</summary>
        public static string MarcaDoEstado(EstadoDoNo estado)
        {
            switch (estado)
            {
                case EstadoDoNo.Bloqueado:
                    return "x";
                case EstadoDoNo.Disponivel:
                    return "+";
                case EstadoDoNo.NoMaximo:
                    return "MAX";
                case EstadoDoNo.CondicionalmenteAtivo:
                    return "~";
                default:
                    return "";
            }
        }

        public static string LegendaDoEstado(EstadoDoNo estado)
        {
            var marca = MarcaDoEstado(estado);
            return (marca.Length > 0 ? marca + " " : "") + NomeDoEstado(estado);
        }

        public static string TipoDoNo(NoDaArvore no)
        {
            var tipo = no.Tipo == TipoDeNo.Hibrido ? "Híbrido" : "Nó";
            var caminhos = new List<string>();
            foreach (var caminho in no.Caminhos)
            {
                caminhos.Add(Rotulo(caminho));
            }

            return tipo + " · " + string.Join(" + ", caminhos);
        }

        /// <summary>Linha do efeito por rank: "+1 Força por rank".</summary>
        public static string Efeito(EfeitoDeNo efeito)
        {
            var nome =
                efeito.Tipo == TipoDeEfeito.AtributoPrincipal
                    ? Rotulo(efeito.Atributo)
                    : efeito.Chave;
            return "+" + efeito.ValorPorRank + " " + nome + " por rank";
        }

        /// <summary>"Compre antes: Braço firme (rank 2)". Vazio se não falta nada.</summary>
        public static string RequisitosPendentes(
            ConfiguracaoDaArvore configuracao,
            IEnumerable<RequisitoDeNo> pendentes
        )
        {
            var texto = new StringBuilder();
            foreach (var requisito in pendentes)
            {
                var origem = RegrasDaArvore.Procurar(configuracao, requisito.NoId);
                texto.Append(texto.Length == 0 ? "Compre antes: " : ", ");
                texto.Append(origem == null ? requisito.NoId : origem.Nome);
                texto.Append(" (rank ").Append(requisito.RankMinimo).Append(')');
            }

            return texto.ToString();
        }

        /// <summary>
        /// Por que o próximo rank não pode ser comprado agora, ou vazio se pode. Usa o mesmo
        /// resultado de <see cref="RegrasDaArvore.AvaliarCompra"/> que o servidor.
        /// </summary>
        public static string Motivo(
            EstadoDaArvore estado,
            NoDaArvore no,
            AvaliacaoDeCompra avaliacao
        )
        {
            switch (avaliacao.Erro)
            {
                case null:
                    return "";
                case CodigosErroTalentos.RankMaximo:
                    return RankMaximo + ".";
                case CodigosErroTalentos.RequisitoNaoCumprido:
                    return RequisitosPendentes(
                            estado.Configuracao,
                            RegrasDaArvore.RequisitosPendentes(estado.Ranks, no)
                        ) + ".";
                case CodigosErroTalentos.PontosInsuficientes:
                    return estado.PontosLivres <= 0
                        ? SemPontos
                        : "Custa "
                            + no.CustoPorRank[estado.RankDe(no)]
                            + " pontos e você tem "
                            + estado.PontosLivres
                            + ".";
                default:
                    return Generica;
            }
        }

        public static string MensagemDaCompra(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroTalentos.PontosInsuficientes:
                    return SemPontos;
                case CodigosErroTalentos.RequisitoNaoCumprido:
                    return "Falta cumprir um requisito deste nó.";
                case CodigosErroTalentos.RankMaximo:
                    return RankMaximo + ".";
                case CodigosErroTalentos.ArvoreDesatualizada:
                    return Desatualizada;
                case CodigoSemConexao:
                case CodigoHttpBloqueado:
                    return SemConexao;
                default:
                    return Generica;
            }
        }
    }
}
