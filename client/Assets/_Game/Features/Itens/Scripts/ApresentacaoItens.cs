using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// O que o inventário e o baú mostram: monta os textos a partir das respostas do servidor.
    /// Regra pura (sem Unity, sem rede), para testar sem servidor. O client não sorteia nem
    /// decide nada: só formata o que o servidor devolveu.
    /// </summary>
    public static class ApresentacaoItens
    {
        /// <summary>Avisa que o baú não abriu porque os itens sorteados não couberam.</summary>
        public static readonly string AvisoInventarioCheio =
            "O baú não abriu: os itens não cabem no inventário ("
            + RegrasItens.EspacosInventario
            + " espaços). Nada foi guardado.";

        /// <summary>Título do inventário, com os espaços usados (ex.: "Inventário (3/40)").</summary>
        public static string TituloDoInventario(int ocupados)
        {
            return "Inventário (" + ocupados + "/" + RegrasItens.EspacosInventario + ")";
        }

        /// <summary>
        /// Uma linha da lista: o <c>ItemId</c> e o nome (ex.: "[1] Espada de teste"). Sem nome
        /// vindo do servidor, usa o do catálogo do shared.
        /// </summary>
        public static string FormatarEntrada(EntradaInventario entrada)
        {
            var nome = entrada.Nome;
            if (string.IsNullOrEmpty(nome))
            {
                nome = CatalogoItens.ObterNome(entrada.ItemId) ?? "Item desconhecido";
            }

            return "[" + entrada.ItemId + "] " + nome;
        }

        /// <summary>
        /// Uma linha por entrada, na ordem recebida (itens repetidos ficam em linhas separadas,
        /// sem pilha). <c>null</c> vira lista vazia.
        /// </summary>
        public static List<string> MontarLinhas(EntradaInventario[] entradas)
        {
            var linhas = new List<string>();
            if (entradas == null)
            {
                return linhas;
            }

            foreach (var entrada in entradas)
            {
                if (entrada != null)
                {
                    linhas.Add(FormatarEntrada(entrada));
                }
            }

            return linhas;
        }

        /// <summary>
        /// Interpreta a resposta do <c>GET</c> do inventário. Falha vira só o aviso, sem lista.
        /// </summary>
        public static ResultadoItens InterpretarInventario(
            bool sucesso,
            string codigoErro,
            InventarioResponse corpo
        )
        {
            if (!sucesso || corpo == null)
            {
                return ResultadoItens.Falha(MensagemDeErro(codigoErro, false), codigoErro);
            }

            return ResultadoItens.Sucesso(null, MontarLinhas(corpo.Entradas));
        }

        /// <summary>
        /// Interpreta a resposta do <c>POST</c> do baú: ganhos e inventário resultante no
        /// sucesso; só o aviso na falha (<c>InventarioCheio</c> inclusive), sem inventar item.
        /// </summary>
        public static ResultadoItens InterpretarBau(
            bool sucesso,
            string codigoErro,
            AbrirBauResponse corpo
        )
        {
            if (!sucesso || corpo == null)
            {
                return ResultadoItens.Falha(MensagemDeErro(codigoErro, true), codigoErro);
            }

            return ResultadoItens.Sucesso(
                MontarLinhas(corpo.Ganhos),
                MontarLinhas(corpo.Inventario)
            );
        }

        /// <summary>
        /// Texto para a pessoa a partir do código de erro (do servidor ou do Core). Devolve
        /// <c>null</c> quando não precisa mostrar nada (chamada cancelada). O 401 (
        /// <c>NaoAutenticado</c>) o Core já trata voltando ao login; o texto aqui quase não
        /// chega a aparecer.
        /// </summary>
        /// <param name="abrindoBau"><c>true</c> para o baú, <c>false</c> para o inventário.</param>
        public static string MensagemDeErro(string codigo, bool abrindoBau)
        {
            switch (codigo)
            {
                case CodigosErroCliente.Cancelada:
                    return null;
                case CodigosErroItens.InventarioCheio:
                    return AvisoInventarioCheio;
                case CodigosErroItens.BauLongeDemais:
                    return "Chegue mais perto do baú.";
                case CodigosErroItens.BauEmRecarga:
                    return "O baú ainda está recarregando.";
                case CodigosErroItens.PersonagemNaoEncontrado:
                    return "Personagem não encontrado nesta conta.";
                case CodigosErro.NaoAutenticado:
                    return "Sua sessão expirou. Entre de novo.";
                case CodigosErroCliente.SemConexao:
                case CodigosErroCliente.HttpBloqueado:
                    return "Sem conexão com o servidor.";
                default:
                    return abrindoBau
                        ? "Não foi possível abrir o baú. Tente de novo."
                        : "Não foi possível carregar o inventário. Tente de novo.";
            }
        }
    }

    /// <summary>Resultado já interpretado de uma chamada de inventário ou de baú.</summary>
    public sealed class ResultadoItens
    {
        private ResultadoItens() { }

        /// <summary>Se o servidor aceitou a chamada.</summary>
        public bool Deu { get; private set; }

        /// <summary>Texto de erro a mostrar, ou <c>null</c> (sucesso ou nada a mostrar).</summary>
        public string Aviso { get; private set; }

        /// <summary>Código do erro (<c>null</c> no sucesso).</summary>
        public string CodigoErro { get; private set; }

        /// <summary>Ganhos desta abertura de baú (vazia no inventário e nas falhas).</summary>
        public List<string> Ganhos { get; private set; } = new List<string>();

        /// <summary>Inventário completo (vazia nas falhas: mantenha a lista anterior).</summary>
        public List<string> Inventario { get; private set; } = new List<string>();

        internal static ResultadoItens Sucesso(List<string> ganhos, List<string> inventario)
        {
            return new ResultadoItens
            {
                Deu = true,
                Ganhos = ganhos ?? new List<string>(),
                Inventario = inventario,
            };
        }

        internal static ResultadoItens Falha(string aviso, string codigoErro)
        {
            return new ResultadoItens
            {
                Deu = false,
                Aviso = aviso,
                CodigoErro = codigoErro,
            };
        }
    }
}
