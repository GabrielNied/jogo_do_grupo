using System;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>Estado de uma ordem do mercado.</summary>
    public enum EstadoDaOrdemDeMercado
    {
        Aberta = 0,
        Cumprida = 1,
        Cancelada = 2,
        Expirada = 3,
    }

    /// <summary>Um modelo que se pode pedir numa ordem de compra.</summary>
    [Serializable]
    public class ModeloDoMercado
    {
        public int ItemId;
        public string Nome = "";
    }

    /// <summary>Uma ordem do livro do mercado, como o client a vê.</summary>
    [Serializable]
    public class OrdemDeMercado
    {
        public long Id;

        /// <summary>Verdadeiro para venda (um exemplar), falso para compra (por modelo).</summary>
        public bool Venda;

        public int ItemId;
        public string Nome = "";

        /// <summary>Unidades que faltam: sempre 1 na venda; na compra, as que ainda faltam.</summary>
        public int Quantidade;

        public long PrecoUnitario;

        /// <summary>Instante em que a ordem vence, em segundos Unix (UTC).</summary>
        public long ExpiraEm;

        public EstadoDaOrdemDeMercado Estado;

        /// <summary>Ordem do próprio jogador que pediu.</summary>
        public bool Minha;

        /// <summary>
        /// Venda cancelada ou vencida cujo item ainda não voltou à bolsa (bolsa cheia na hora).
        /// </summary>
        public bool AguardaDevolucao;

        public string Anunciante = "";

        /// <summary>
        /// Se o vendedor possui barraca montada na praça, indica o nome para o selo 'na barraca de <nome>'.
        /// </summary>
        public string BarracaDe = "";

        /// <summary>O exemplar à venda (atributos, tier); nulo nas ordens de compra.</summary>
        public EntradaInventario Item;
    }

    /// <summary>
    /// Client → servidor: abre o mercado (perto do leiloeiro) ou o atualiza. O servidor responde
    /// com <see cref="EstadoDoMercado"/>. Id: <see cref="EconomiaMessageIds.ConsultarMercado"/>.
    /// </summary>
    [Serializable]
    public class ConsultarMercado
    {
        /// <summary>Só as ordens desse modelo (0 = todos).</summary>
        public int ItemId;

        /// <summary>Só as ordens cujo nome contém o texto (vazio = todas).</summary>
        public string Filtro = "";
    }

    /// <summary>
    /// Client → servidor: anuncia um exemplar da bolsa por um preço. O item fica guardado na
    /// ordem. Id: <see cref="EconomiaMessageIds.OferecerVenda"/>.
    /// </summary>
    [Serializable]
    public class OferecerVenda
    {
        public long InstanciaId;
        public long PrecoCoroas;
    }

    /// <summary>
    /// Client → servidor: pede <see cref="Quantidade"/> unidades de um modelo por até
    /// <see cref="PrecoUnitario"/> coroas cada. As coroas ficam guardadas na ordem.
    /// Id: <see cref="EconomiaMessageIds.OferecerCompra"/>.
    /// </summary>
    [Serializable]
    public class OferecerCompra
    {
        public int ItemId;
        public int Quantidade = 1;
        public long PrecoUnitario;
    }

    /// <summary>
    /// Client → servidor: compra já a ordem de venda indicada, pelo preço dela.
    /// Id: <see cref="EconomiaMessageIds.ComprarOrdem"/>.
    /// </summary>
    [Serializable]
    public class ComprarOrdem
    {
        public long OrdemId;
    }

    /// <summary>
    /// Client → servidor: cancela uma ordem aberta própria e recebe de volta o item ou as
    /// coroas. Id: <see cref="EconomiaMessageIds.CancelarOrdem"/>.
    /// </summary>
    [Serializable]
    public class CancelarOrdem
    {
        public long OrdemId;
    }

    /// <summary>
    /// Client → servidor: pede os itens de vendas vencidas ou canceladas que não couberam na
    /// bolsa. Id: <see cref="EconomiaMessageIds.ReceberDevolucoes"/>.
    /// </summary>
    [Serializable]
    public class ReceberDevolucoes { }

    /// <summary>
    /// Servidor → client: o mercado inteiro como o jogador o vê, depois de cada pedido.
    /// Id: <see cref="EconomiaMessageIds.EstadoDoMercado"/>.
    /// </summary>
    [Serializable]
    public class EstadoDoMercado
    {
        public long Saldo;
        public int TaxaVendaPercentual = ConfigMercado.TaxaVendaPercentual;

        /// <summary>Vendas abertas, da mais barata para a mais cara.</summary>
        public OrdemDeMercado[] Vendas = Array.Empty<OrdemDeMercado>();

        /// <summary>Compras abertas, da que paga mais para a que paga menos.</summary>
        public OrdemDeMercado[] Compras = Array.Empty<OrdemDeMercado>();

        /// <summary>As ordens do jogador: abertas e vendas que aguardam devolução.</summary>
        public OrdemDeMercado[] Minhas = Array.Empty<OrdemDeMercado>();

        /// <summary>A bolsa negociável, para escolher o que vender.</summary>
        public EntradaInventario[] Bolsa = Array.Empty<EntradaInventario>();

        /// <summary>Id e nome dos modelos que se pode pedir (ordens de compra).</summary>
        public ModeloDoMercado[] Modelos = Array.Empty<ModeloDoMercado>();
    }

    /// <summary>Tipos de <see cref="AvisoDoMercado.Tipo"/>.</summary>
    public static class TiposAvisoDoMercado
    {
        public const string Comprou = "Comprou";
        public const string Vendeu = "Vendeu";
        public const string Expirou = "Expirou";
        public const string Devolvido = "Devolvido";
    }

    /// <summary>
    /// Servidor → client: algo aconteceu com uma ordem do jogador fora do pedido dele (casou,
    /// venceu, o item voltou). Id: <see cref="EconomiaMessageIds.AvisoDoMercado"/>.
    /// </summary>
    [Serializable]
    public class AvisoDoMercado
    {
        public string Tipo = "";
        public int ItemId;
        public string Nome = "";
        public long PrecoUnitario;

        /// <summary>Taxa paga (só em <see cref="TiposAvisoDoMercado.Vendeu"/>).</summary>
        public long Taxa;
    }
}
