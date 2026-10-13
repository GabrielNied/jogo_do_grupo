using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// A pipeline única dos atributos secundários: junta os modificadores de um atributo num valor
    /// e guarda as parcelas. Pura e determinística: a ordem em que a lista chega não importa.
    /// <para>
    /// Regra: o valor começa em 0 e os passos entram por <see cref="Modificador.Ordem"/> crescente.
    /// Em cada passo, primeiro todas as somas; depois, cada grupo multiplica o valor por
    /// <c>1 + soma dos bônus do grupo</c> (grupos em ordem alfabética). Assim +10% e +10% no mesmo
    /// grupo dão x1,20, e em grupos diferentes, x1,21. Não limita o resultado
    /// (<see cref="RegrasSecundarios"/> limita).
    /// </para>
    /// </summary>
    public static class PipelineDeModificadores
    {
        /// <summary>
        /// Calcula um atributo. Modificador de outro atributo, sem grupo de acúmulo ou com valor
        /// inválido não entra (<see cref="EhValido"/>).
        /// </summary>
        public static SecundarioCalculado Calcular(
            AtributoSecundario atributo,
            IEnumerable<Modificador> modificadores
        )
        {
            var validos = new List<Modificador>();
            foreach (var m in modificadores)
            {
                if (m != null && m.Atributo == atributo && EhValido(m))
                {
                    validos.Add(m);
                }
            }

            validos.Sort(Comparar);

            double valor = 0;
            int i = 0;
            while (i < validos.Count)
            {
                int ordem = validos[i].Ordem;
                int fim = i;
                while (fim < validos.Count && validos[fim].Ordem == ordem)
                {
                    fim++;
                }

                // Ordenado: dentro do passo, as somas vêm antes das multiplicações, e as
                // multiplicações do mesmo grupo ficam juntas.
                int k = i;
                for (; k < fim && validos[k].Operacao == OperacaoDeModificador.Soma; k++)
                {
                    valor += validos[k].Valor;
                }

                while (k < fim)
                {
                    string grupo = validos[k].Grupo;
                    double bonus = 0;
                    for (; k < fim && validos[k].Grupo == grupo; k++)
                    {
                        bonus += validos[k].Valor;
                    }
                    valor *= Math.Max(0, 1 + bonus);
                }

                i = fim;
            }

            return new SecundarioCalculado
            {
                Atributo = atributo,
                Valor = valor,
                Parcelas = validos.ToArray(),
            };
        }

        /// <summary>Todo modificador declara o grupo de acúmulo, e o valor precisa ser um número.</summary>
        public static bool EhValido(Modificador m) =>
            !string.IsNullOrEmpty(m.Grupo) && !double.IsNaN(m.Valor) && !double.IsInfinity(m.Valor);

        // Desempate fixo: a mesma lista, em qualquer ordem, dá o mesmo resultado.
        private static int Comparar(Modificador a, Modificador b)
        {
            int c = a.Ordem.CompareTo(b.Ordem);
            if (c != 0)
            {
                return c;
            }
            c = a.Operacao.CompareTo(b.Operacao);
            if (c != 0)
            {
                return c;
            }
            c = string.CompareOrdinal(a.Grupo, b.Grupo);
            if (c != 0)
            {
                return c;
            }
            c = a.Fonte.CompareTo(b.Fonte);
            if (c != 0)
            {
                return c;
            }
            c = string.CompareOrdinal(a.Origem ?? "", b.Origem ?? "");
            return c != 0 ? c : a.Valor.CompareTo(b.Valor);
        }
    }
}
