using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>Onde uma peça fica no corpo do personagem (o encaixe do esqueleto).</summary>
    public enum EncaixeDoVisual
    {
        MaoDireita,
        Costas,
        Cabeca,
        Tronco,
    }

    /// <summary>Regras puras do visual equipado, iguais no servidor e no client.</summary>
    public static class RegrasVisualEquipado
    {
        /// <summary>Nome do objeto de encaixe no visual do personagem (criado pelo Personagem).</summary>
        public static string NomeDoEncaixe(EncaixeDoVisual encaixe)
        {
            switch (encaixe)
            {
                case EncaixeDoVisual.MaoDireita:
                    return "EncaixeMaoDireita";
                case EncaixeDoVisual.Costas:
                    return "EncaixeCostas";
                case EncaixeDoVisual.Cabeca:
                    return "EncaixeCabeca";
                case EncaixeDoVisual.Tronco:
                    return "EncaixeTronco";
                default:
                    return "EncaixeDesconhecido";
            }
        }

        /// <summary>
        /// Onde a arma da posição fica: a do conjunto ativo na mão direita, a do inativo nas
        /// costas. <c>null</c> para o que não é arma principal (a mão secundária, escudo e foco,
        /// e as armaduras, não são armas).
        /// </summary>
        public static EncaixeDoVisual? EncaixeDe(PosicaoEquipamento posicao, int conjuntoAtivo)
        {
            if (posicao != PosicaoEquipamento.Arma1 && posicao != PosicaoEquipamento.Arma2)
                return null;
            return RegrasEquipamentos.Conjunto(posicao) == conjuntoAtivo
                ? EncaixeDoVisual.MaoDireita
                : EncaixeDoVisual.Costas;
        }

        /// <summary>
        /// Onde uma peça de armadura ou acessório fica: capacete na cabeça, peito no tronco.
        /// <c>null</c> para peças que ainda não têm modelo 3D.
        /// </summary>
        public static EncaixeDoVisual? EncaixeArmaduraDe(PosicaoEquipamento posicao)
        {
            switch (posicao)
            {
                case PosicaoEquipamento.Capacete:
                    return EncaixeDoVisual.Cabeca;
                case PosicaoEquipamento.Peito:
                    return EncaixeDoVisual.Tronco;
                default:
                    return null;
            }
        }

        /// <summary>Monta o estado inteiro a partir das peças vestidas do personagem.</summary>
        public static VisualEquipado Montar(
            long personagemId,
            int conjuntoAtivo,
            IEnumerable<EntradaInventario> equipados
        )
        {
            var armas = new List<ArmaVisual>();
            var armaduras = new List<ArmaduraVisual>();
            foreach (var entrada in equipados)
            {
                var dados = RegrasEquipamentos.DadosDe(entrada);
                if (dados == null)
                    continue;

                if (EncaixeDe(entrada.Posicao, conjuntoAtivo) != null)
                {
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
                else if (EncaixeArmaduraDe(entrada.Posicao) != null)
                {
                    armaduras.Add(
                        new ArmaduraVisual
                        {
                            Posicao = entrada.Posicao,
                            Tipo = dados.Tipo,
                            ModeloId = dados.ModeloId,
                            Tier = RegrasTier.TierDe(dados),
                        }
                    );
                }
            }
            armas.Sort((a, b) => a.Posicao.CompareTo(b.Posicao));
            armaduras.Sort((a, b) => a.Posicao.CompareTo(b.Posicao));
            return new VisualEquipado
            {
                PersonagemId = personagemId,
                ConjuntoAtivo = conjuntoAtivo,
                Armas = armas.ToArray(),
                Armaduras = armaduras.ToArray(),
            };
        }
    }
}
