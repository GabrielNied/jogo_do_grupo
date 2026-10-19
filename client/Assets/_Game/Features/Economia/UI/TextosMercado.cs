using System;
using JogoDoGrupo.Shared.Features.Economia;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Economia.UI
{
    /// <summary>Os textos do mercado (#176) num lugar só: recusas, avisos, rótulos.</summary>
    public static class TextosMercado
    {
        public const string Titulo = "Leilão";
        public const string AbaComprar = "Comprar";
        public const string AbaVender = "Vender";
        public const string AbaMinhas = "Minhas ordens";
        public const string AVenda = "À venda (mais baratas primeiro)";
        public const string PedidosAbertos = "Pedidos de compra abertos";
        public const string FazerPedido = "Fazer um pedido de compra";
        public const string SuaBolsa = "Sua bolsa (clique para escolher o item)";
        public const string NadaAVenda = "Nada à venda por aqui.";
        public const string NadaPedido = "Nenhum pedido de compra aberto.";
        public const string BolsaVazia = "Nada na bolsa que possa ser vendido.";
        public const string SemOrdens = "Você não tem ordens abertas.";
        public const string Buscar = "Buscar";
        public const string Comprar = "Comprar";
        public const string Cancelar = "Cancelar";
        public const string Anunciar = "Anunciar";
        public const string Pedir = "Fazer pedido";
        public const string ReceberDevolucoes = "Receber itens devolvidos";
        public const string Filtro = "Nome do item";
        public const string Quantidade = "Quantidade";
        public const string PrecoMaximo = "Preço máximo cada";
        public const string Preco = "Preço (coroas)";
        public const string AguardandoDevolucao = "aguardando devolução (bolsa cheia)";

        public static string Taxa(int percentual) =>
            $"Anunciar é grátis. A taxa de {percentual}% só é cobrada quando o item vende.";

        public static string Recebe(long preco) =>
            preco < ConfigMercado.PrecoMinimo
                ? ""
                : $"Você recebe {ConfigMercado.LiquidoDaVenda(preco)} coroas (taxa {ConfigMercado.TaxaDaVenda(preco)}).";

        public static string NomeDoItem(EntradaInventario item)
        {
            if (item == null)
            {
                return "";
            }

            var nome = string.IsNullOrEmpty(item.Nome) ? $"Item {item.ItemId}" : item.Nome;
            return item.Equipamento == null
                ? nome
                : $"{nome} (tier {RegrasTier.Letra(RegrasTier.TierDe(item.Equipamento))})";
        }

        public static string Linha(OrdemDeMercado ordem)
        {
            var nome = ordem.Item != null ? NomeDoItem(ordem.Item) : ordem.Nome;
            var quem = string.IsNullOrEmpty(ordem.Anunciante) ? "" : $" — {ordem.Anunciante}";
            var selo = string.IsNullOrEmpty(ordem.BarracaDe)
                ? ""
                : $" [na barraca de {ordem.BarracaDe}]";
            if (ordem.Venda)
            {
                return $"{nome}: {ordem.PrecoUnitario} coroas{selo}{quem}";
            }

            return $"{ordem.Quantidade}x {nome}: até {ordem.PrecoUnitario} coroas cada{quem}";
        }

        public static string Validade(long expiraEmSegundosUnix)
        {
            var restante = expiraEmSegundosUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (restante <= 0)
            {
                return "vencida";
            }

            var dias = restante / 86400;
            var horas = restante % 86400 / 3600;
            return dias > 0 ? $"{dias} d {horas} h" : $"{Math.Max(1, horas)} h";
        }

        public static string Motivo(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroEconomia.SaldoInsuficiente:
                    return "Coroas insuficientes.";
                case CodigosErroEconomia.InventarioCheio:
                    return "Sua bolsa está cheia.";
                case CodigosErroEconomia.ItemNaoEncontrado:
                    return "O item não está mais na bolsa (ou está equipado ou favorito).";
                case CodigosErroEconomia.PrecoInvalido:
                    return $"Preço entre {ConfigMercado.PrecoMinimo} e {ConfigMercado.PrecoMaximo} coroas.";
                case CodigosErroEconomia.QuantidadeInvalida:
                    return $"Quantidade entre 1 e {ConfigMercado.QuantidadeMaximaDaCompra}.";
                case CodigosErroEconomia.LimiteDeOrdens:
                    return $"No máximo {ConfigMercado.OrdensAbertasMaximas} ordens abertas.";
                case CodigosErroEconomia.OrdemNaoEncontrada:
                    return "Essa ordem não existe.";
                case CodigosErroEconomia.OrdemIndisponivel:
                    return "Essa ordem não está mais aberta.";
                case CodigosErroEconomia.OrdemPropria:
                    return "Você não pode comprar a própria ordem.";
                case CodigosErroEconomia.ItemNaoNegociavel:
                    return "Esse item não pode ser negociado.";
                case CodigosErroEconomia.LongeDoLeiloeiro:
                    return "Chegue perto do leiloeiro.";
                case CodigosErroEconomia.ForaDaPraca:
                    return "Você precisa estar na praça da cidade para armar a barraca.";
                case CodigosErroEconomia.BarracaNaoEncontrada:
                    return "Esta barraca não está aberta.";
                case CodigosErroEconomia.VendedorOfflineNaoAtivo:
                    return "Você não possui um vendedor offline ativo.";
                case CodigosErroEconomia.DiariaInvalida:
                    return "Quantidade de diárias inválida.";
                case CodigosErroEconomia.LongeDaBarraca:
                    return "Chegue mais perto da barraca.";
                default:
                    return "Operação recusada.";
            }
        }

        public static string Aviso(AvisoDoMercado aviso)
        {
            switch (aviso.Tipo)
            {
                case TiposAvisoDoMercado.Vendeu:
                    return $"Você vendeu {aviso.Nome} por {aviso.PrecoUnitario} coroas (taxa {aviso.Taxa}).";
                case TiposAvisoDoMercado.Comprou:
                    return $"Você comprou {aviso.Nome} por {aviso.PrecoUnitario} coroas.";
                case TiposAvisoDoMercado.Expirou:
                    return $"Sua ordem de {aviso.Nome} venceu e foi devolvida.";
                case TiposAvisoDoMercado.Devolvido:
                    return $"{aviso.Nome} voltou para a sua bolsa.";
                default:
                    return "";
            }
        }
    }
}
