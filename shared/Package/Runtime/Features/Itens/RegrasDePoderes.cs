using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Quais efeitos estão ligados para o que o personagem veste: os poderes das peças ativas e
    /// os marcos de set atingidos (SET-003: recalculado a cada mudança). Pura; o servidor chama
    /// ao equipar, desequipar e trocar de conjunto.
    /// </summary>
    public static class RegrasDePoderes
    {
        /// <summary>
        /// Os efeitos ativos, na ordem das peças e dos marcos. Só conta a peça vestida (arma só
        /// do conjunto ativo). O mesmo poder em duas peças vale uma vez (acumulação), poder em
        /// encaixe incompatível ou regra malformada é ignorado e cada marco de set liga uma vez.
        /// Nada aqui olha o tier: poder e set não entram nele (DEC-004).
        /// </summary>
        public static List<EfeitoAtivo> Ativos(
            IEnumerable<EntradaInventario> entradas,
            int conjunto
        )
        {
            var efeitos = new List<EfeitoAtivo>();
            if (entradas == null)
                return efeitos;

            var lista = new List<EntradaInventario>(entradas);
            var poderesVistos = new HashSet<string>();
            foreach (var entrada in lista)
            {
                if (!RegrasEquipamentos.EstaAtiva(entrada, conjunto))
                    continue;
                var item = RegrasEquipamentos.DadosDe(entrada);
                var poder = item == null ? null : CatalogoDePoderes.Obter(item.PoderId);
                if (poder == null || !poder.Aceita(item.Tipo) || !poderesVistos.Add(poder.Id))
                    continue;
                foreach (var regra in poder.Regras)
                {
                    if (regra.Valida)
                    {
                        efeitos.Add(
                            new EfeitoAtivo
                            {
                                Origem = "poder:" + poder.Id,
                                Nome = poder.Nome,
                                Regra = regra,
                            }
                        );
                    }
                }
            }

            foreach (var set in CatalogoDeSets.Sets)
            {
                var pecas = RegrasDeSets.Contar(set, lista, conjunto);
                foreach (var marco in RegrasDeSets.MarcosAtivos(set, pecas))
                {
                    foreach (var regra in marco.Regras)
                    {
                        if (regra.Valida)
                        {
                            efeitos.Add(
                                new EfeitoAtivo
                                {
                                    Origem = "set:" + set.Id + ":" + marco.Pecas,
                                    Nome = set.Nome + " (" + marco.Pecas + ")",
                                    Regra = regra,
                                }
                            );
                        }
                    }
                }
            }

            return efeitos;
        }
    }
}
