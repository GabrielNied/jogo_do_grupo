using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Pede a prévia ou a confirmação de uma fusão (rotas <c>fusao/previa</c> e
    /// <c>fusao/confirmar</c> sob <see cref="RegrasEquipamentos.Rota"/>). O principal é o
    /// resultado; o material é consumido; <see cref="Status"/> é o único atributo que muda.
    /// </summary>
    [Serializable]
    public class PedidoFusao
    {
        public long PrincipalId;
        public long MaterialId;
        public StatusItem Status;

        /// <summary>Só na confirmação: a <see cref="PreviaFusao.Confirmacao"/> da prévia.</summary>
        public string Confirmacao = "";
    }

    [Serializable]
    public class PedidoFavorito
    {
        public long InstanciaId;
        public bool Favorito;
        public long[] InstanciaIds = System.Array.Empty<long>();
    }

    /// <summary>
    /// O que a fusão vai fazer (FUSAO-010). Não altera nada: só a confirmação com a
    /// <see cref="Confirmacao"/> aplica.
    /// </summary>
    [Serializable]
    public class PreviaFusao
    {
        public long PrincipalId;
        public long MaterialId;
        public string Principal = "";
        public string Material = "";

        /// <summary>Categoria de peça dos dois (a compatibilidade, FUSAO-002).</summary>
        public TipoEquipamento Categoria;

        public int NivelAtual;
        public int NivelDepois;
        public StatusItem Status;
        public int ValorAtual;
        public int ValorMaterial;
        public int ValorDepois;
        public TierItem TierAntes;
        public TierItem TierDepois;
        public long Custo;
        public int MateriaisDungeon;
        public int MateriaisRaid;

        /// <summary>
        /// Falso quando a fusão exige materiais de dungeon ou raid que o jogador não tem (hoje
        /// nunca os tem: veja <see cref="ConfigFusao"/>). A confirmação é recusada.
        /// </summary>
        public bool MateriaisSuficientes = true;

        public string Confirmacao = "";
    }

    /// <summary>
    /// Regras puras da fusão (issue #616, FUSAO-001 a FUSAO-015). Genéricas sobre
    /// <see cref="StatusItem"/>: nenhuma regra cita um atributo; elegibilidade e teto vêm de
    /// <see cref="ConfigTier"/>, custos de <see cref="ConfigFusao"/>.
    /// </summary>
    public static class RegrasFusao
    {
        /// <summary>
        /// Confere o par principal e material. Devolve <c>null</c> se pode fundir, ou o código do
        /// motivo (<see cref="CodigosErroItens"/>). Quem chama confere antes que o material não
        /// está equipado e que não é o próprio principal.
        /// </summary>
        public static string Validar(Equipamento principal, Equipamento material)
        {
            if (principal == null || material == null)
                return CodigosErroItens.ItemNaoEncontrado;
            if (principal.Atributos == null || material.Atributos == null)
                return CodigosErroItens.ItemInvalido;
            if (principal.NivelFusao < 0 || principal.NivelFusao > ConfigFusao.NivelMaximo)
                return CodigosErroItens.ItemInvalido;
            if (principal.Tipo != material.Tipo)
                return CodigosErroItens.MaterialIncompativel;
            if (material.Favorito)
                return CodigosErroItens.ItemFavorito;
            if (principal.NivelFusao >= ConfigFusao.NivelMaximo)
                return CodigosErroItens.NivelMaximo;
            return null;
        }

        /// <summary>
        /// Os doadores compatíveis de <paramref name="principal"/> na bolsa (#561): as outras
        /// peças da bolsa que <see cref="Validar"/> aceitaria como material (mesmo
        /// <see cref="TipoEquipamento"/>, sem favorito, principal que ainda não está em +10).
        /// Peça equipada nunca é doadora (FUSAO-001), então só olha <paramref name="entradas"/>
        /// na bolsa. Devolve as <c>InstanciaId</c>.
        /// </summary>
        public static List<long> Doadores(
            long instanciaDoPrincipal,
            Equipamento principal,
            IEnumerable<EntradaInventario> entradas
        )
        {
            var ids = new List<long>();
            if (principal == null || entradas == null)
                return ids;
            foreach (var entrada in entradas)
            {
                if (
                    entrada == null
                    || entrada.InstanciaId == instanciaDoPrincipal
                    || entrada.Posicao != PosicaoEquipamento.Inventario
                )
                    continue;
                if (Validar(principal, RegrasEquipamentos.DadosDe(entrada)) == null)
                    ids.Add(entrada.InstanciaId);
            }
            return ids;
        }

        /// <summary>
        /// Os atributos que podem ser escolhidos: existem nos dois itens e são elegíveis na
        /// categoria (FUSAO-003). Sem repetição, na ordem do principal.
        /// </summary>
        public static StatusItem[] Elegiveis(Equipamento principal, Equipamento material) =>
            Elegiveis(principal, material, ConfigTier.Faixas);

        public static StatusItem[] Elegiveis(
            Equipamento principal,
            Equipamento material,
            TabelaDeFaixas faixas
        )
        {
            var lista = new List<StatusItem>();
            if (principal?.Atributos == null || material?.Atributos == null)
                return lista.ToArray();
            foreach (var atributo in principal.Atributos)
            {
                if (
                    !lista.Contains(atributo.Status)
                    && faixas.Faixa(principal.Tipo, atributo.Status) != null
                    && Procurar(material, atributo.Status) != null
                )
                    lista.Add(atributo.Status);
            }

            return lista.ToArray();
        }

        public static AtributoItem Procurar(Equipamento item, StatusItem status)
        {
            if (item?.Atributos == null)
                return null;
            foreach (var atributo in item.Atributos)
                if (atributo.Status == status)
                    return atributo;
            return null;
        }

        /// <summary>
        /// O valor do atributo depois da fusão (FUSAO-005, FUSAO-014 e FUSAO-015):
        /// <c>atual + sinal(d) x teto(|d| / 2)</c>, com <c>d = material - atual</c> sobre os
        /// valores arredondados para inteiro. Anda pelo menos 1 quando os valores diferem, nunca
        /// passa do material e é limitado à faixa (o máximo absoluto da categoria é o teto).
        /// Pode diminuir.
        /// </summary>
        public static int Resultado(double atual, double material, FaixaDeAtributo faixa)
        {
            var a = Arredondar(atual);
            var m = Arredondar(material);
            var diferenca = m - a;
            var passo = (Math.Abs(diferenca) + 1) / 2;
            var resultado = a + Math.Sign(diferenca) * passo;
            return resultado < faixa.Minimo ? faixa.Minimo
                : resultado > faixa.Maximo ? faixa.Maximo
                : resultado;
        }

        private static int Arredondar(double valor) =>
            (int)Math.Round(valor, MidpointRounding.AwayFromZero);

        /// <summary>
        /// O descarte pede segunda confirmação (ITEM-004): nível de fusão acima de +0 ou tier
        /// atual A ou acima.
        /// </summary>
        public static bool DescarteExigeSegundaConfirmacao(Equipamento item) =>
            item != null && (item.NivelFusao > 0 || RegrasTier.TierDe(item) >= TierItem.A);
    }
}
