using System;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Economia
{
    [Serializable]
    public sealed class ItemBancoGuildaDto
    {
        public long Id;
        public int Aba;
        public int Casa;
        public int ItemId;
        public string EquipamentoJson = "";
        public string DepositadoPorNome = "";
        public long DataDepositoUnix;
        public EntradaInventario Detalhes = new EntradaInventario();
    }

    /// <summary>Client → servidor: consulta o estado atual do banco da guilda.</summary>
    [Serializable]
    public sealed class ConsultarBancoGuilda { }

    /// <summary>Servidor → client: estado atualizado do banco da guilda.</summary>
    [Serializable]
    public sealed class EstadoDoBancoGuilda
    {
        public long GuildaId;
        public string NomeGuilda = "";
        public long SaldoCoroas;
        public bool PodeRetirar;
        public ItemBancoGuildaDto[] Itens = new ItemBancoGuildaDto[0];
    }

    /// <summary>Client → servidor: deposita um item da bolsa no banco da guilda.</summary>
    [Serializable]
    public sealed class DepositarItemBancoGuilda
    {
        public long InstanciaId;
        public int Aba;
        public int Casa;
    }

    /// <summary>Client → servidor: retira um item do banco da guilda para a bolsa.</summary>
    [Serializable]
    public sealed class RetirarItemBancoGuilda
    {
        public long ItemBancoId;
    }

    /// <summary>Client → servidor: deposita coroas da carteira no saldo da guilda.</summary>
    [Serializable]
    public sealed class DepositarCoroasBancoGuilda
    {
        public long Quantidade;
    }

    /// <summary>Client → servidor: retira coroas do saldo da guilda para a carteira.</summary>
    [Serializable]
    public sealed class RetirarCoroasBancoGuilda
    {
        public long Quantidade;
    }

    /// <summary>Servidor → client: notificação ou evento do banco da guilda.</summary>
    [Serializable]
    public sealed class AvisoBancoGuilda
    {
        public string Mensagem = "";
    }
}
