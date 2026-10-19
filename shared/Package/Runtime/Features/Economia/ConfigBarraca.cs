namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Constantes e contas de balanceamento para Barracas de Jogador e Vendedor Offline (#905).
    /// As ofertas entram no mesmo livro de ordens do leilão; compras presenciais na praça pagam taxa menor.
    /// </summary>
    public static class ConfigBarraca
    {
        /// <summary>
        /// Taxa reduzida cobrada do vendedor na compra presencial na barraca (3%), contra 5% no leilão geral.
        /// </summary>
        public const int TaxaVendaBarracaPercentual = 3;

        /// <summary>
        /// Diária em coroas cobrada para manter um vendedor offline (manequim) ativo.
        /// </summary>
        public const long DiariaVendedorOfflineCoroas = 50;

        /// <summary>
        /// Duração de uma diária em segundos (24 horas).
        /// </summary>
        public const long DuracaoDiariaSegundos = 24L * 60L * 60L;

        /// <summary>
        /// Limite de dias que se pode contratar adiantado para um vendedor offline.
        /// </summary>
        public const int DiasMaximosContratacao = 30;

        /// <summary>
        /// Área onde as barracas são permitidas (a cidade).
        /// </summary>
        public const string Area = Npcs.CatalogoNpcs.AreaCidade;

        /// <summary>
        /// Coordenada central da praça da cidade onde as barracas se concentram.
        /// </summary>
        public const float PracaCidadeX = 0f;
        public const float PracaCidadeZ = -10f;

        /// <summary>
        /// Raio em metros ao redor da praça onde é permitido armar barraca.
        /// </summary>
        public const float RaioPracaMetros = 20f;

        /// <summary>
        /// Distância máxima em metros para interagir ou comprar diretamente na barraca de outro jogador.
        /// </summary>
        public const float RaioInteracaoBarracaMetros = 4f;

        /// <summary>
        /// Verifica se as coordenadas informadas estão dentro do raio permitido da praça da cidade.
        /// </summary>
        public static bool DentroDaPraca(float x, float z)
        {
            var dx = x - PracaCidadeX;
            var dz = z - PracaCidadeZ;
            return (dx * dx + dz * dz) <= (RaioPracaMetros * RaioPracaMetros);
        }

        /// <summary>
        /// Calcula a taxa de uma venda presencial na barraca (3%), arredondada para cima.
        /// </summary>
        public static long TaxaDaVendaPresencial(long preco)
        {
            if (preco <= 0)
            {
                return 0;
            }

            return (preco * TaxaVendaBarracaPercentual + 99) / 100;
        }

        /// <summary>
        /// O que o vendedor recebe de uma venda presencial na barraca de <paramref name="preco"/> coroas.
        /// </summary>
        public static long LiquidoDaVendaPresencial(long preco) =>
            preco - TaxaDaVendaPresencial(preco);
    }
}
