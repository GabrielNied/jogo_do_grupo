using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>
    /// Confere o pedido da tela de admin e lista os modelos que ela oferece. Regra pura: o
    /// servidor chama <see cref="Validar"/> antes de executar, e o client usa as listas para
    /// montar os seletores.
    /// </summary>
    public static class RegrasAdmin
    {
        /// <summary>Modelos que o gerador conhece, em ordem de id.</summary>
        public static readonly int[] TodosOsModelos = ListarModelos();

        /// <summary>Texto da recusa, ou <c>null</c> se o pedido é válido para a ação.</summary>
        public static string Validar(PedirAcaoDeAdmin pedido)
        {
            if (pedido == null || !Enum.IsDefined(typeof(AcaoDeAdmin), pedido.Acao))
            {
                return "Ação desconhecida.";
            }

            switch (pedido.Acao)
            {
                case AcaoDeAdmin.GerarItem:
                    return ValidarItem(pedido);
                case AcaoDeAdmin.AjustarExperiencia:
                    return Faixa(pedido.Valor, ConfigAdmin.ExperienciaMaxima, "Experiência", true);
                case AcaoDeAdmin.AjustarCoroas:
                    return Faixa(pedido.Valor, ConfigAdmin.CoroasMaximas, "Coroas", true);
                case AcaoDeAdmin.CreditarEssencias:
                    return Faixa(pedido.Valor, ConfigAdmin.EssenciasMaximas, "Essências", false);
                case AcaoDeAdmin.Teleportar:
                    return ValidarDestino(pedido);
                case AcaoDeAdmin.SpawnarInimigo:
                    return ValidarInimigo(pedido);
                default:
                    return null;
            }
        }

        /// <summary>Modelos do tipo, em ordem de id (o seletor de modelo da tela).</summary>
        public static List<int> ModelosDoTipo(TipoEquipamento tipo)
        {
            var lista = new List<int>();
            for (var i = 0; i < TodosOsModelos.Length; i++)
            {
                var modelo = CatalogoEquipamentos.Obter(TodosOsModelos[i]);
                if (modelo != null && modelo.Tipo == tipo)
                {
                    lista.Add(TodosOsModelos[i]);
                }
            }

            return lista;
        }

        private static string ValidarItem(PedirAcaoDeAdmin pedido)
        {
            if (CatalogoEquipamentos.Obter(pedido.ModeloId) == null)
            {
                return "Modelo de item desconhecido.";
            }

            if (!Enum.IsDefined(typeof(GrauEquipamento), pedido.Grau))
            {
                return "Grau inválido.";
            }

            if (!Enum.IsDefined(typeof(OrigemDoLoot), pedido.Origem))
            {
                return "Origem inválida.";
            }

            return Quantidade(pedido.Quantidade, ConfigAdmin.QuantidadeMaximaDeItens);
        }

        private static string ValidarInimigo(PedirAcaoDeAdmin pedido)
        {
            if (CatalogoCriaturas.Obter(pedido.EspecieId) == null)
            {
                return "Espécie desconhecida.";
            }

            return Quantidade(pedido.Quantidade, ConfigAdmin.QuantidadeMaximaDeInimigos);
        }

        private static string ValidarDestino(PedirAcaoDeAdmin pedido)
        {
            if (!float.IsFinite(pedido.X) || !float.IsFinite(pedido.Y) || !float.IsFinite(pedido.Z))
            {
                return "Destino inválido.";
            }

            return null;
        }

        private static string Quantidade(int quantidade, int maximo)
        {
            if (quantidade < 1 || quantidade > maximo)
            {
                return "Quantidade de 1 a " + maximo + ".";
            }

            return null;
        }

        private static string Faixa(long valor, long maximo, string nome, bool aceitaNegativo)
        {
            var minimo = aceitaNegativo ? -maximo : 1;
            if (valor == 0 || valor < minimo || valor > maximo)
            {
                return aceitaNegativo
                    ? nome + " de -" + maximo + " a " + maximo + ", sem ser zero."
                    : nome + " de 1 a " + maximo + ".";
            }

            return null;
        }

        private static int[] ListarModelos()
        {
            var ids = new List<int> { CatalogoItens.EspadaDeTeste, CatalogoItens.EscudoDeTeste };
            ids.AddRange(CatalogoEquipamentos.ModelosDeTeste);
            ids.Add(CatalogoEquipamentos.CajadoDeFogoDeTeste);
            ids.Add(CatalogoEquipamentos.CajadoDeGeloDeTeste);
            ids.Add(CatalogoEquipamentos.CajadoDeCuraDeTeste);
            ids.Add(CatalogoEquipamentos.AdornoPresaVelha);
            ids.Sort();
            return ids.ToArray();
        }
    }
}
