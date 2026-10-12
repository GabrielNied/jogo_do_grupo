using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    public enum RaridadeItem
    {
        Comum,
        Incomum,
        Raro,
        Epico,
        Lendario,
    }

    public enum GrauEquipamento
    {
        Inicial,
        Medio,
        Forte,
    }

    public enum TipoEquipamento
    {
        Capacete,
        Peito,
        Luvas,
        Calca,
        Botas,
        Cinto,
        Anel,
        Brinco,
        Adorno,
        Espada,
        Martelo,
        Arco,
        Cajado,
        Escudo,
        Foco,
    }

    public enum PosicaoEquipamento
    {
        Inventario,
        Capacete,
        Peito,
        Luvas,
        Calca,
        Botas,
        Cinto,
        Anel1,
        Anel2,
        Brinco1,
        Brinco2,
        Adorno,
        Arma1,
        Secundaria1,
        Arma2,
        Secundaria2,
    }

    public enum StatusItem
    {
        Forca,
        Destreza,
        Intelecto,
        Vitalidade,
        Espirito,
        Vida,
        Vigor,
        Energia,
        Defesa,
        PotenciaFogo,
        PotenciaGelo,
        Alcance,
        RecuperacaoVigor,
    }

    [Serializable]
    public class AtributoItem
    {
        public StatusItem Status;
        public double Valor;
        public bool Fixo;
    }

    /// <summary>Exemplar gerado uma vez pelo servidor. Nível da peça não é nível do personagem.</summary>
    [Serializable]
    public class Equipamento
    {
        public int ModeloId;
        public string Nome = "";
        public string Regiao = "";
        public string Familia = "";
        public string Variacao = "";
        public TipoEquipamento Tipo;
        public RaridadeItem Raridade;
        public GrauEquipamento Grau;
        public int Nivel = 1;
        public int NivelPersonagemMinimo = 1;
        public bool DuasMaos;
        public AtributoItem[] Atributos = Array.Empty<AtributoItem>();
        public int IndiceInfusivel;
        public int InfusoesUsadas;
        public double ValorBaseInfusivel;
        public bool Favorito;
    }

    [Serializable]
    public class PedidoEquipar
    {
        public long InstanciaId;
        public PosicaoEquipamento Posicao;
    }

    [Serializable]
    public class PedidoDesequipar
    {
        public PosicaoEquipamento Posicao;
    }

    [Serializable]
    public class PedidoConjunto
    {
        public int Conjunto;
    }

    [Serializable]
    public class EquipamentosResponse
    {
        public EntradaInventario[] Inventario = Array.Empty<EntradaInventario>();
        public EntradaInventario[] Equipados = Array.Empty<EntradaInventario>();
        public int ConjuntoAtivo = 1;
        public AtributoItem[] Bonus = Array.Empty<AtributoItem>();
        public long Coroas;
        public bool ModoInfusao;
        public PreviaInfusao Previa;
        public string Aviso = "";
    }

    public static class RegrasEquipamentos
    {
        public const string Rota = "/api/personagens/{personagemId}/equipamentos";

        public static string RotaDe(long id) => "/api/personagens/" + id + "/equipamentos";

        public static int NivelMaximo(RaridadeItem raridade) => 10 + 5 * (int)raridade;

        public static int LimiteInfusoes(RaridadeItem raridade) => NivelMaximo(raridade);

        public static int SlotsSkills(RaridadeItem raridade) => 1 + (int)raridade;

        public static int NivelSkill(int slot) => slot == 0 ? 1 : slot * 5;

        public static int Fator(GrauEquipamento grau) => 1 << (int)grau;

        public static string NomeDaRaridade(RaridadeItem raridade)
        {
            switch (raridade)
            {
                case RaridadeItem.Incomum:
                    return "Incomum";
                case RaridadeItem.Raro:
                    return "Raro";
                case RaridadeItem.Epico:
                    return "Épico";
                case RaridadeItem.Lendario:
                    return "Lendário";
                default:
                    return "Comum";
            }
        }

        public static int Conjunto(PosicaoEquipamento posicao) =>
            posicao == PosicaoEquipamento.Arma1 || posicao == PosicaoEquipamento.Secundaria1 ? 1
            : posicao == PosicaoEquipamento.Arma2 || posicao == PosicaoEquipamento.Secundaria2 ? 2
            : 0;

        public static PosicaoEquipamento Principal(int conjunto) =>
            conjunto == 1 ? PosicaoEquipamento.Arma1 : PosicaoEquipamento.Arma2;

        public static PosicaoEquipamento Secundaria(int conjunto) =>
            conjunto == 1 ? PosicaoEquipamento.Secundaria1 : PosicaoEquipamento.Secundaria2;

        public static bool Aceita(TipoEquipamento tipo, PosicaoEquipamento posicao)
        {
            if (posicao >= PosicaoEquipamento.Capacete && posicao <= PosicaoEquipamento.Cinto)
                return (int)tipo == (int)posicao - 1;
            if (posicao == PosicaoEquipamento.Anel1 || posicao == PosicaoEquipamento.Anel2)
                return tipo == TipoEquipamento.Anel;
            if (posicao == PosicaoEquipamento.Brinco1 || posicao == PosicaoEquipamento.Brinco2)
                return tipo == TipoEquipamento.Brinco;
            if (posicao == PosicaoEquipamento.Adorno)
                return tipo == TipoEquipamento.Adorno;
            if (posicao == PosicaoEquipamento.Arma1 || posicao == PosicaoEquipamento.Arma2)
                return tipo >= TipoEquipamento.Espada && tipo <= TipoEquipamento.Cajado;
            if (
                posicao == PosicaoEquipamento.Secundaria1
                || posicao == PosicaoEquipamento.Secundaria2
            )
                return tipo == TipoEquipamento.Escudo || tipo == TipoEquipamento.Foco;
            return false;
        }

        public static AtributoItem[] Somar(IEnumerable<EntradaInventario> entradas, int conjunto)
        {
            var totais = new SortedDictionary<StatusItem, double>();
            foreach (var entrada in entradas)
            {
                if (entrada.Equipamento == null || entrada.Posicao == PosicaoEquipamento.Inventario)
                    continue;
                var origem = Conjunto(entrada.Posicao);
                if (origem != 0 && origem != conjunto)
                    continue;
                foreach (var atributo in entrada.Equipamento.Atributos)
                {
                    totais.TryGetValue(atributo.Status, out var atual);
                    totais[atributo.Status] = atual + atributo.Valor;
                }
            }
            var resultado = new List<AtributoItem>();
            foreach (var total in totais)
                resultado.Add(new AtributoItem { Status = total.Key, Valor = total.Value });
            return resultado.ToArray();
        }
    }
}
