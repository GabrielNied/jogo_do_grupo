using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Confere a configuração da árvore. O servidor chama ao subir e recusa subir com problema;
    /// os testes chamam para a árvore de teste. Devolve o motivo de cada problema em português.
    /// </summary>
    public static class ValidadorDaArvore
    {
        /// <summary>Lista vazia quando a configuração é válida.</summary>
        public static List<string> Validar(ConfiguracaoDaArvore configuracao)
        {
            var problemas = new List<string>();
            if (configuracao == null)
            {
                problemas.Add("A configuração da árvore está ausente.");
                return problemas;
            }

            if (configuracao.Versao < 1)
            {
                problemas.Add("A versão da árvore precisa ser 1 ou mais.");
            }

            if (configuracao.Nos == null || configuracao.Nos.Length == 0)
            {
                problemas.Add("A árvore não tem nós.");
                return problemas;
            }

            var porId = new Dictionary<string, NoDaArvore>();
            foreach (var no in configuracao.Nos)
            {
                if (no == null || string.IsNullOrEmpty(no.Id))
                {
                    problemas.Add("Há um nó sem id.");
                    continue;
                }

                if (porId.ContainsKey(no.Id))
                {
                    problemas.Add("Id de nó repetido: '" + no.Id + "'.");
                    continue;
                }

                porId.Add(no.Id, no);
            }

            foreach (var no in porId.Values)
            {
                ValidarNo(no, porId, problemas);
            }

            ValidarCiclos(porId, problemas);
            return problemas;
        }

        private static void ValidarNo(
            NoDaArvore no,
            Dictionary<string, NoDaArvore> porId,
            List<string> problemas
        )
        {
            var rotulo = "Nó '" + no.Id + "'";
            if (string.IsNullOrEmpty(no.Nome))
            {
                problemas.Add(rotulo + " não tem nome.");
            }

            if (no.RankMaximo < 1)
            {
                problemas.Add(rotulo + " tem rank máximo menor que 1.");
            }

            if (no.CustoPorRank == null || no.CustoPorRank.Length != Math.Max(no.RankMaximo, 0))
            {
                problemas.Add(rotulo + " precisa de um custo para cada rank (custo por rank).");
            }
            else
            {
                foreach (var custo in no.CustoPorRank)
                {
                    if (custo < 1)
                    {
                        problemas.Add(rotulo + " tem custo de rank menor que 1.");
                        break;
                    }
                }
            }

            ValidarCaminhos(no, rotulo, problemas);

            if (no.Efeitos == null || no.Efeitos.Length == 0)
            {
                if (no.Tipo != TipoDeNo.Hibrido)
                {
                    problemas.Add(rotulo + " não tem efeito.");
                }
            }
            else
            {
                foreach (var efeito in no.Efeitos)
                {
                    ValidarEfeito(efeito, rotulo, problemas);
                }
            }

            if (no.Requisitos == null)
            {
                return;
            }

            foreach (var requisito in no.Requisitos)
            {
                if (requisito == null || !porId.TryGetValue(requisito.NoId ?? "", out var origem))
                {
                    problemas.Add(
                        rotulo
                            + " pede um nó que não existe: '"
                            + (requisito == null ? "" : requisito.NoId)
                            + "'."
                    );
                    continue;
                }

                if (requisito.RankMinimo < 1 || requisito.RankMinimo > origem.RankMaximo)
                {
                    problemas.Add(
                        rotulo
                            + " pede rank "
                            + requisito.RankMinimo
                            + " de '"
                            + origem.Id
                            + "', que vai de 1 a "
                            + origem.RankMaximo
                            + "."
                    );
                }
            }
        }

        private static void ValidarCaminhos(NoDaArvore no, string rotulo, List<string> problemas)
        {
            var caminhos = no.Caminhos ?? new AtributoPrincipal[0];
            foreach (var caminho in caminhos)
            {
                if (!Enum.IsDefined(typeof(AtributoPrincipal), caminho))
                {
                    problemas.Add(rotulo + " tem um caminho que não existe.");
                    return;
                }
            }

            if (no.Tipo == TipoDeNo.Hibrido)
            {
                if (caminhos.Length != 2 || caminhos[0] == caminhos[1])
                {
                    problemas.Add(rotulo + " é híbrido e precisa de dois caminhos diferentes.");
                }
            }
            else if (caminhos.Length < 1)
            {
                problemas.Add(rotulo + " precisa de pelo menos um caminho.");
            }
        }

        private static void ValidarEfeito(EfeitoDeNo efeito, string rotulo, List<string> problemas)
        {
            if (efeito == null)
            {
                problemas.Add(rotulo + " tem um efeito vazio.");
                return;
            }

            if (efeito.ValorPorRank < 1)
            {
                problemas.Add(rotulo + " tem efeito com valor por rank menor que 1.");
            }

            if (efeito.Tipo == TipoDeEfeito.AtributoPrincipal)
            {
                if (!Enum.IsDefined(typeof(AtributoPrincipal), efeito.Atributo))
                {
                    problemas.Add(rotulo + " tem efeito de um atributo que não existe.");
                }
            }
            else if (efeito.Tipo == TipoDeEfeito.AtributoSecundario)
            {
                if (string.IsNullOrEmpty(efeito.Chave))
                {
                    problemas.Add(rotulo + " tem efeito secundário sem chave.");
                }
            }
            else
            {
                problemas.Add(rotulo + " tem um tipo de efeito que não existe.");
            }
        }

        // Busca em profundidade nos requisitos (do nó para o que ele pede). Achar um nó que ainda
        // está no caminho atual é um ciclo.
        private static void ValidarCiclos(
            Dictionary<string, NoDaArvore> porId,
            List<string> problemas
        )
        {
            var estado = new Dictionary<string, int>();
            foreach (var id in porId.Keys)
            {
                if (!estado.ContainsKey(id) && TemCiclo(id, porId, estado))
                {
                    problemas.Add("Ciclo de pré-requisitos passando pelo nó '" + id + "'.");
                    return;
                }
            }
        }

        private static bool TemCiclo(
            string id,
            Dictionary<string, NoDaArvore> porId,
            Dictionary<string, int> estado
        )
        {
            estado[id] = 1;
            var requisitos = porId[id].Requisitos ?? new RequisitoDeNo[0];
            foreach (var requisito in requisitos)
            {
                if (requisito == null || !porId.ContainsKey(requisito.NoId ?? ""))
                {
                    continue;
                }

                if (estado.TryGetValue(requisito.NoId, out var visita))
                {
                    if (visita == 1)
                    {
                        return true;
                    }

                    continue;
                }

                if (TemCiclo(requisito.NoId, porId, estado))
                {
                    return true;
                }
            }

            estado[id] = 2;
            return false;
        }
    }
}
