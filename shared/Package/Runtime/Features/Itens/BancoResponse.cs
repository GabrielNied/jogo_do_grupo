#nullable enable annotations
using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Resposta do banco pessoal da conta. Rota: <see cref="RotasItens.Banco"/>.
    /// </summary>
    [Serializable]
    public class BancoResponse
    {
        public EntradaInventario[] Itens = Array.Empty<EntradaInventario>();
        public CapacidadeBolsa Capacidade = RegrasBanco.CapacidadeBancoInicial();
        public long Coroas;
        public string Aviso = "";
        public EquipamentosResponse? Bolsa;
    }
}
