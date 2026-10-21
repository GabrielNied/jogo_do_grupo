#nullable enable annotations
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Resultado de <see cref="RegrasDaArvore.AvaliarCompra"/>.</summary>
    public sealed class AvaliacaoDeCompra
    {
        /// <summary>Código de <see cref="CodigosErroTalentos"/>, ou <c>null</c> se o pedido vale.</summary>
        public string? Erro;

        /// <summary>O nó já está no rank pedido ou acima: sucesso sem cobrar (pedido repetido).</summary>
        public bool JaComprado;

        /// <summary>Pontos a cobrar. Zero quando <see cref="JaComprado"/>.</summary>
        public int Custo;
    }

    /// <summary>
    /// Regras puras da árvore. O servidor usa para decidir a compra; o client usa para pintar os
    /// estados, mostrar o motivo do bloqueio e a prévia, sempre com a mesma regra.
    /// </summary>
    public static class RegrasDaArvore
    {
        public static NoDaArvore? Procurar(ConfiguracaoDaArvore configuracao, string noId)
        {
            if (configuracao == null || configuracao.Nos == null || noId == null)
            {
                return null;
            }

            foreach (var no in configuracao.Nos)
            {
                if (no != null && no.Id == noId)
                {
                    return no;
                }
            }

            return null;
        }

        public static int RankDe(IReadOnlyDictionary<string, int> ranks, string noId) =>
            ranks != null && noId != null && ranks.TryGetValue(noId, out var rank) ? rank : 0;

        public static Dictionary<string, int> ParaDicionario(RankDoNo[] ranks)
        {
            var resultado = new Dictionary<string, int>();
            if (ranks == null)
            {
                return resultado;
            }

            foreach (var item in ranks)
            {
                if (item != null && item.Rank > 0)
                {
                    resultado[item.NoId] = item.Rank;
                }
            }

            return resultado;
        }

        /// <summary>Requisitos do nó que ainda não foram cumpridos (vazio se pode comprar).</summary>
        public static List<RequisitoDeNo> RequisitosPendentes(
            IReadOnlyDictionary<string, int> ranks,
            NoDaArvore no
        )
        {
            var pendentes = new List<RequisitoDeNo>();
            if (no.Requisitos == null)
            {
                return pendentes;
            }

            foreach (var requisito in no.Requisitos)
            {
                if (RankDe(ranks, requisito.NoId) < requisito.RankMinimo)
                {
                    pendentes.Add(requisito);
                }
            }

            return pendentes;
        }

        /// <summary>
        /// Decide um pedido de compra: o nó precisa existir, o rank alvo ser o próximo (ou um que
        /// o nó já tem, o que é só um pedido repetido), o rank máximo não ser passado, os requisitos
        /// estarem cumpridos e os pontos cobrirem o custo.
        /// </summary>
        public static AvaliacaoDeCompra AvaliarCompra(
            ConfiguracaoDaArvore configuracao,
            IReadOnlyDictionary<string, int> ranks,
            int pontosLivres,
            string noId,
            int rankAlvo
        )
        {
            var no = Procurar(configuracao, noId);
            if (no == null)
            {
                return new AvaliacaoDeCompra { Erro = CodigosErroTalentos.NoInvalido };
            }

            var atual = RankDe(ranks, noId);
            if (rankAlvo < 1 || rankAlvo > atual + 1)
            {
                return new AvaliacaoDeCompra { Erro = CodigosErroTalentos.RankAlvoInvalido };
            }

            if (rankAlvo <= atual)
            {
                return new AvaliacaoDeCompra { JaComprado = true };
            }

            if (atual >= no.RankMaximo)
            {
                return new AvaliacaoDeCompra { Erro = CodigosErroTalentos.RankMaximo };
            }

            if (RequisitosPendentes(ranks, no).Count > 0)
            {
                return new AvaliacaoDeCompra { Erro = CodigosErroTalentos.RequisitoNaoCumprido };
            }

            var custo = no.CustoPorRank[atual];
            if (pontosLivres < custo)
            {
                return new AvaliacaoDeCompra { Erro = CodigosErroTalentos.PontosInsuficientes };
            }

            return new AvaliacaoDeCompra { Custo = custo };
        }

        public static EstadoDoNo EstadoDe(IReadOnlyDictionary<string, int> ranks, NoDaArvore no)
        {
            var rank = RankDe(ranks, no.Id);
            if (rank >= no.RankMaximo)
            {
                return EstadoDoNo.NoMaximo;
            }

            if (rank > 0)
            {
                return no.Condicional ? EstadoDoNo.CondicionalmenteAtivo : EstadoDoNo.Comprado;
            }

            return RequisitosPendentes(ranks, no).Count > 0
                ? EstadoDoNo.Bloqueado
                : EstadoDoNo.Disponivel;
        }

        /// <summary>Soma os efeitos de atributo principal dos ranks comprados.</summary>
        public static AtributosDaArvore SomarAtributos(
            ConfiguracaoDaArvore configuracao,
            IReadOnlyDictionary<string, int> ranks
        )
        {
            var total = new AtributosDaArvore();
            if (configuracao == null || configuracao.Nos == null)
            {
                return total;
            }

            foreach (var no in configuracao.Nos)
            {
                var rank = RankDe(ranks, no.Id);
                if (rank <= 0 || no.Efeitos == null)
                {
                    continue;
                }

                foreach (var efeito in no.Efeitos)
                {
                    if (efeito.Tipo == TipoDeEfeito.AtributoPrincipal)
                    {
                        Somar(total, efeito.Atributo, checked((int)(efeito.ValorPorRank * rank)));
                    }
                }
            }

            return total;
        }

        /// <summary>Total de pontos pagos pelos ranks (para conferir <c>PontosGastos</c>).</summary>
        public static int CustoPago(NoDaArvore no, int rank)
        {
            var total = 0;
            for (var i = 0; i < rank && i < no.CustoPorRank.Length; i++)
            {
                total += no.CustoPorRank[i];
            }

            return total;
        }

        /// <summary>Calcula a soma total de pontos gastos por um dicionário de ranks na configuração.</summary>
        public static int CalcularPontosGastos(
            ConfiguracaoDaArvore configuracao,
            IReadOnlyDictionary<string, int> ranks
        )
        {
            if (configuracao == null || configuracao.Nos == null || ranks == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var no in configuracao.Nos)
            {
                if (no != null)
                {
                    var rank = RankDe(ranks, no.Id);
                    if (rank > 0)
                    {
                        total += CustoPago(no, rank);
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Valida os ranks de um perfil contra a configuração atual da árvore e os pontos disponíveis.
        /// Nós que não existem mais ou cujos requisitos foram quebrados são removidos com aviso.
        /// </summary>
        public static ValidacaoDePerfil ValidarECalcularPerfil(
            ConfiguracaoDaArvore configuracao,
            RankDoNo[] ranksDoPerfil,
            int pontosDisponiveis
        )
        {
            var resultado = new ValidacaoDePerfil();
            if (configuracao == null || configuracao.Nos == null)
            {
                resultado.Erro = CodigosErroTalentos.ArvoreDesatualizada;
                return resultado;
            }

            var candidatos = new Dictionary<string, int>();
            if (ranksDoPerfil != null)
            {
                foreach (var item in ranksDoPerfil)
                {
                    if (item == null || string.IsNullOrEmpty(item.NoId) || item.Rank <= 0)
                    {
                        continue;
                    }

                    var no = Procurar(configuracao, item.NoId);
                    if (no == null)
                    {
                        resultado.Avisos.Add(
                            "O nó '" + item.NoId + "' não existe mais na árvore e foi ignorado."
                        );
                        continue;
                    }

                    var rankAjustado = item.Rank;
                    if (rankAjustado > no.RankMaximo)
                    {
                        resultado.Avisos.Add(
                            "O nó '"
                                + no.Nome
                                + "' foi ajustado para o rank máximo ("
                                + no.RankMaximo
                                + ")."
                        );
                        rankAjustado = no.RankMaximo;
                    }

                    candidatos[item.NoId] = rankAjustado;
                }
            }

            // Remove em cascata nós cujos requisitos não estão cumpridos entre os candidatos
            bool removeu;
            do
            {
                removeu = false;
                var aRemover = new List<string>();
                foreach (var par in candidatos)
                {
                    var no = Procurar(configuracao, par.Key);
                    if (no == null || RequisitosPendentes(candidatos, no).Count > 0)
                    {
                        aRemover.Add(par.Key);
                    }
                }

                foreach (var noId in aRemover)
                {
                    var no = Procurar(configuracao, noId);
                    var nome = no != null ? no.Nome : noId;
                    resultado.Avisos.Add(
                        "O nó '"
                            + nome
                            + "' não teve todos os pré-requisitos cumpridos e foi removido do perfil."
                    );
                    candidatos.Remove(noId);
                    removeu = true;
                }
            } while (removeu);

            resultado.RanksAplicaveis = candidatos;
            resultado.CustoTotal = CalcularPontosGastos(configuracao, candidatos);

            if (resultado.CustoTotal > pontosDisponiveis)
            {
                resultado.Erro = CodigosErroTalentos.PontosInsuficientes;
            }

            return resultado;
        }

        private static void Somar(AtributosDaArvore total, AtributoPrincipal atributo, int valor)
        {
            switch (atributo)
            {
                case AtributoPrincipal.Forca:
                    total.Forca += valor;
                    break;
                case AtributoPrincipal.Vigor:
                    total.Vigor += valor;
                    break;
                case AtributoPrincipal.Agilidade:
                    total.Agilidade += valor;
                    break;
                case AtributoPrincipal.Intelecto:
                    total.Intelecto += valor;
                    break;
                case AtributoPrincipal.Espirito:
                    total.Espirito += valor;
                    break;
            }
        }
    }

    /// <summary>Resultado de <see cref="RegrasDaArvore.ValidarECalcularPerfil"/>.</summary>
    public sealed class ValidacaoDePerfil
    {
        /// <summary>Código de erro, ou <c>null</c> se o perfil pode ser aplicado.</summary>
        public string? Erro;

        /// <summary>Ranks finais válidos e que cumprem todos os requisitos.</summary>
        public Dictionary<string, int> RanksAplicaveis = new Dictionary<string, int>();

        /// <summary>Custo total em pontos da árvore para aplicar estes ranks.</summary>
        public int CustoTotal;

        /// <summary>Avisos explicativos de nós descartados ou ajustados.</summary>
        public List<string> Avisos = new List<string>();
    }
}
