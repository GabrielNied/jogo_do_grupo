using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Calcula os nove secundários: junta a base (<see cref="ConfigAtributosSecundarios"/>) aos
    /// modificadores das outras fontes pela <see cref="PipelineDeModificadores"/> e limita o
    /// resultado. O servidor é quem decide; o client só mostra.
    /// </summary>
    public static class RegrasSecundarios
    {
        /// <summary>Os nove, na ordem do enum.</summary>
        public static readonly AtributoSecundario[] Todos =
        {
            AtributoSecundario.RegeneracaoVida,
            AtributoSecundario.RegeneracaoStamina,
            AtributoSecundario.RegeneracaoMana,
            AtributoSecundario.ChanceCritica,
            AtributoSecundario.DanoCritico,
            AtributoSecundario.VelocidadeAtaqueFisico,
            AtributoSecundario.VelocidadeAtaqueMagico,
            AtributoSecundario.PenetracaoFisica,
            AtributoSecundario.PenetracaoMagica,
        };

        /// <summary>O valor de base de um secundário (fonte Base).</summary>
        public static double Base(AtributoSecundario atributo)
        {
            switch (atributo)
            {
                case AtributoSecundario.RegeneracaoVida:
                    return ConfigAtributosSecundarios.BaseRegeneracaoVida;
                case AtributoSecundario.RegeneracaoStamina:
                    return ConfigAtributosSecundarios.BaseRegeneracaoStamina;
                case AtributoSecundario.RegeneracaoMana:
                    return ConfigAtributosSecundarios.BaseRegeneracaoMana;
                case AtributoSecundario.ChanceCritica:
                    return ConfigAtributosSecundarios.BaseChanceCritica;
                case AtributoSecundario.DanoCritico:
                    return ConfigAtributosSecundarios.BaseDanoCritico;
                case AtributoSecundario.VelocidadeAtaqueFisico:
                case AtributoSecundario.VelocidadeAtaqueMagico:
                    return ConfigAtributosSecundarios.BaseVelocidadeAtaque;
                default:
                    return ConfigAtributosSecundarios.BasePenetracao;
            }
        }

        /// <summary>A parcela de base de cada secundário (soma, primeiro passo).</summary>
        public static Modificador ParcelaBase(AtributoSecundario atributo) =>
            new Modificador
            {
                Atributo = atributo,
                Fonte = FonteDeModificador.Base,
                Origem = "Base",
                Operacao = OperacaoDeModificador.Soma,
                Ordem = ConfigAtributosSecundarios.OrdemBase,
                Grupo = ConfigAtributosSecundarios.GrupoBase,
                Valor = Base(atributo),
            };

        /// <summary>
        /// Calcula os nove com a base mais <paramref name="modificadores"/> (árvore, equipamento,
        /// efeito, alvo...). A base nunca vem de fora: um modificador da fonte Base na lista é
        /// ignorado, para ninguém duplicar a base.
        /// </summary>
        public static SecundarioCalculado[] Calcular(IEnumerable<Modificador> modificadores)
        {
            var todos = new List<Modificador>();
            foreach (var atributo in Todos)
            {
                todos.Add(ParcelaBase(atributo));
            }
            if (modificadores != null)
            {
                foreach (var m in modificadores)
                {
                    if (m != null && m.Fonte != FonteDeModificador.Base)
                    {
                        todos.Add(m);
                    }
                }
            }

            var resultado = new SecundarioCalculado[Todos.Length];
            for (int i = 0; i < Todos.Length; i++)
            {
                var calculado = PipelineDeModificadores.Calcular(Todos[i], todos);
                calculado.Valor = Limitar(Todos[i], calculado.Valor);
                resultado[i] = calculado;
            }
            return resultado;
        }

        /// <summary>Os valores finais dos nove, para as contas do servidor.</summary>
        public static AtributosSecundarios Valores(SecundarioCalculado[] calculados)
        {
            var valores = new AtributosSecundarios();
            foreach (var c in calculados)
            {
                valores.Definir(c.Atributo, c.Valor);
            }
            return valores;
        }

        /// <summary>Limites de cada secundário (valores de teste em <see cref="ConfigAtributosSecundarios"/>).</summary>
        public static double Limitar(AtributoSecundario atributo, double valor)
        {
            switch (atributo)
            {
                case AtributoSecundario.ChanceCritica:
                    return Math.Min(
                        ConfigAtributosSecundarios.TetoChanceCritica,
                        Math.Max(0, valor)
                    );
                case AtributoSecundario.DanoCritico:
                    return Math.Max(ConfigAtributosSecundarios.DanoCriticoMinimo, valor);
                case AtributoSecundario.VelocidadeAtaqueFisico:
                case AtributoSecundario.VelocidadeAtaqueMagico:
                    return Math.Min(
                        ConfigAtributosSecundarios.VelocidadeMaxima,
                        Math.Max(ConfigAtributosSecundarios.VelocidadeMinima, valor)
                    );
                case AtributoSecundario.PenetracaoFisica:
                case AtributoSecundario.PenetracaoMagica:
                    return Math.Min(ConfigAtributosSecundarios.TetoPenetracao, Math.Max(0, valor));
                default:
                    return Math.Max(0, valor);
            }
        }
    }
}
