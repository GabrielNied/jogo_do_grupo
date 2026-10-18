using JogoDoGrupo.Shared.Features.Desenvolvimento;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Admin
{
    /// <summary>
    /// Monta os pedidos da tela de admin (F8) a partir dos campos e escreve os textos de
    /// status. Lógica pura, sem UI, para os testes.
    /// </summary>
    internal static class PedidosDeAdmin
    {
        /// <summary>Segundos até a tela liberar os botões se o servidor não responder.</summary>
        public const float EsperaMaximaSegundos = 5f;

        public const string SemConexao = "Sem conexão com o servidor.";

        public const string SemResposta =
            "Sem resposta do servidor (comandos de teste desligados?).";

        public static PedirAcaoDeAdmin Item(
            int modeloId,
            GrauEquipamento grau,
            OrigemDoLoot origem,
            int quantidade
        ) =>
            new PedirAcaoDeAdmin
            {
                Acao = AcaoDeAdmin.GerarItem,
                ModeloId = modeloId,
                Grau = grau,
                Origem = origem,
                Quantidade = quantidade,
            };

        public static PedirAcaoDeAdmin ComValor(AcaoDeAdmin acao, long valor) =>
            new PedirAcaoDeAdmin { Acao = acao, Valor = valor };

        public static PedirAcaoDeAdmin Teleporte(float x, float y, float z) =>
            new PedirAcaoDeAdmin
            {
                Acao = AcaoDeAdmin.Teleportar,
                X = x,
                Y = y,
                Z = z,
            };

        public static PedirAcaoDeAdmin Inimigo(int especieId, int quantidade) =>
            new PedirAcaoDeAdmin
            {
                Acao = AcaoDeAdmin.SpawnarInimigo,
                EspecieId = especieId,
                Quantidade = quantidade,
            };

        public static PedirAcaoDeAdmin Simples(AcaoDeAdmin acao) =>
            new PedirAcaoDeAdmin { Acao = acao };

        /// <summary>Texto da linha de status para a resposta do servidor. Nunca nulo.</summary>
        public static string TextoDoResultado(ResultadoDeAcaoDeAdmin resultado)
        {
            if (resultado == null || string.IsNullOrEmpty(resultado.Texto))
            {
                return resultado != null && resultado.Aceito ? "Feito." : "Recusado.";
            }

            return resultado.Texto;
        }

        /// <summary>Nome do modelo para o seletor ("Nome (id)"); só o id se o catálogo não o conhece.</summary>
        public static string NomeDoModelo(int modeloId)
        {
            var modelo = CatalogoEquipamentos.Obter(modeloId);
            return modelo == null ? modeloId.ToString() : $"{modelo.Nome} ({modeloId})";
        }

        /// <summary>Limita a quantidade digitada à faixa aceita pelo servidor.</summary>
        public static int LimitarQuantidade(int quantidade, int maximo)
        {
            if (quantidade < 1)
            {
                return 1;
            }

            return quantidade > maximo ? maximo : quantidade;
        }
    }
}
