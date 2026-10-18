using System;

namespace JogoDoGrupo.Shared.Features.Economia
{
    /// <summary>
    /// Servidor → client: confirmação do reparo do item e débito de coroas.
    /// Id: <see cref="EconomiaMessageIds.ItemReparado"/>.
    /// </summary>
    [Serializable]
    public class ItemReparado
    {
        public long InstanciaId;
        public int DurabilidadeRestaurada;
        public long CustoCoroas;
        public long SaldoAtualizado;

        public ItemReparado() { }

        public ItemReparado(
            long instanciaId,
            int durabilidadeRestaurada,
            long custoCoroas,
            long saldoAtualizado
        )
        {
            InstanciaId = instanciaId;
            DurabilidadeRestaurada = durabilidadeRestaurada;
            CustoCoroas = custoCoroas;
            SaldoAtualizado = saldoAtualizado;
        }
    }
}
