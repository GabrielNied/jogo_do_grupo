using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    [Serializable]
    public class PedidoInfusao
    {
        public long ReceptorId;
        public long DoadorId;
        public string Confirmacao = "";
    }

    [Serializable]
    public class PedidoFavorito
    {
        public long InstanciaId;
        public bool Favorito;
    }

    [Serializable]
    public class PreviaInfusao
    {
        public long ReceptorId;
        public long DoadorId;
        public string Receptor = "";
        public string Doador = "";
        public StatusItem Status;
        public double Antes;
        public double Depois;
        public int SlotsDepois;
        public int Limite;
        public long Custo;
        public string Confirmacao = "";
    }

    public static class RegrasInfusao
    {
        public const decimal GanhoPorDoador = 0.02m;

        public static string Validar(Equipamento receptor, Equipamento doador)
        {
            if (receptor == null || doador == null)
                return "ItemNaoEncontrado";
            if (receptor.Tipo != doador.Tipo || receptor.Raridade != doador.Raridade)
                return "DoadorIncompativel";
            if (doador.Favorito)
                return "ItemFavorito";
            if (
                !Enum.IsDefined(typeof(RaridadeItem), receptor.Raridade)
                || receptor.InfusoesUsadas < 0
                || receptor.IndiceInfusivel < 0
                || receptor.IndiceInfusivel >= receptor.Atributos.Length
            )
                return "ItemInvalido";
            if (receptor.InfusoesUsadas >= RegrasEquipamentos.LimiteInfusoes(receptor.Raridade))
                return "LimiteInfusoes";
            return null;
        }

        public static double Depois(double antes, double valorBase) =>
            (double)((decimal)antes + (decimal)valorBase * GanhoPorDoador);
    }
}
