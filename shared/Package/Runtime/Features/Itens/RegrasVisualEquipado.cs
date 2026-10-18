using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>Onde uma peça fica no corpo do personagem (o encaixe do esqueleto).</summary>
    public enum EncaixeDoVisual
    {
        MaoDireita,
        Costas,
    }

    /// <summary>Regras puras do visual equipado, iguais no servidor e no client.</summary>
    public static class RegrasVisualEquipado
    {
        /// <summary>Nome do objeto de encaixe no visual do personagem (criado pelo Personagem).</summary>
        public static string NomeDoEncaixe(EncaixeDoVisual encaixe) =>
            encaixe == EncaixeDoVisual.MaoDireita ? "EncaixeMaoDireita" : "EncaixeCostas";

        /// <summary>
        /// Onde a arma da posição fica: a do conjunto ativo na mão direita, a do inativo nas
        /// costas. <c>null</c> para o que não é arma principal (a mão secundária, escudo e foco,
        /// e as armaduras, não têm modelo ainda).
        /// </summary>
        public static EncaixeDoVisual? EncaixeDe(PosicaoEquipamento posicao, int conjuntoAtivo)
        {
            if (posicao != PosicaoEquipamento.Arma1 && posicao != PosicaoEquipamento.Arma2)
                return null;
            return RegrasEquipamentos.Conjunto(posicao) == conjuntoAtivo
                ? EncaixeDoVisual.MaoDireita
                : EncaixeDoVisual.Costas;
        }

        /// <summary>Monta o estado inteiro a partir das peças vestidas do personagem.</summary>
        public static VisualEquipado Montar(
            long personagemId,
            int conjuntoAtivo,
            IEnumerable<EntradaInventario> equipados
        )
        {
            var armas = new List<ArmaVisual>();
            foreach (var entrada in equipados)
            {
                var dados = RegrasEquipamentos.DadosDe(entrada);
                if (dados == null || EncaixeDe(entrada.Posicao, conjuntoAtivo) == null)
                    continue;
                armas.Add(
                    new ArmaVisual
                    {
                        Posicao = entrada.Posicao,
                        Tipo = dados.Tipo,
                        ModeloId = dados.ModeloId,
                        Tier = RegrasTier.TierDe(dados),
                    }
                );
            }
            armas.Sort((a, b) => a.Posicao.CompareTo(b.Posicao));
            return new VisualEquipado
            {
                PersonagemId = personagemId,
                ConjuntoAtivo = conjuntoAtivo,
                Armas = armas.ToArray(),
            };
        }
    }
}
