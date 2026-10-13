namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Códigos de erro de Itens que o servidor coloca em <c>ErroApi.Codigo</c>. Os códigos de
    /// conta e os genéricos (inclusive <c>NaoAutenticado</c>, 401) ficam em <c>CodigosErro</c>
    /// (Core). O status HTTP de cada um está indicado ao lado.
    /// </summary>
    public static class CodigosErroItens
    {
        /// <summary>
        /// 409: os itens sorteados não cabem nos <see cref="RegrasItens.EspacosInventario"/>
        /// espaços. O servidor não grava nada nesse caso.
        /// </summary>
        public const string InventarioCheio = "InventarioCheio";

        /// <summary>
        /// 404: não existe personagem com esse id, ou ele é de outra conta (os dois casos
        /// respondem igual). Nem Core nem Personagem tinham um código para isso em 2026-10-11.
        /// </summary>
        public const string PersonagemNaoEncontrado = "PersonagemNaoEncontrado";

        /// <summary>
        /// 404: o personagem não tem um exemplar com essa identidade. Vale para identidade que
        /// nunca existiu, de outra conta ou já descartada/consumida (repetir um descarte devolve
        /// este código e não altera nada).
        /// </summary>
        public const string ItemNaoEncontrado = "ItemNaoEncontrado";

        /// <summary>
        /// 409: o exemplar está equipado e não pode ser descartado. Desequipe antes.
        /// </summary>
        public const string ItemEquipado = "ItemEquipado";

        /// <summary>
        /// 400: aba ou casa fora da capacidade da bolsa, ou contêiner sem suporte ainda.
        /// </summary>
        public const string CasaInvalida = "CasaInvalida";

        /// <summary>409, fusão: o material não é da mesma categoria de peça do principal.</summary>
        public const string MaterialIncompativel = "MaterialIncompativel";

        /// <summary>409, fusão: o material está equipado; desequipe antes de consumi-lo.</summary>
        public const string MaterialEquipado = "MaterialEquipado";

        /// <summary>409, fusão: o material está marcado como favorito.</summary>
        public const string ItemFavorito = "ItemFavorito";

        /// <summary>409, fusão: o principal já está em +10.</summary>
        public const string NivelMaximo = "NivelMaximo";

        /// <summary>
        /// 409, fusão: o atributo escolhido não existe nos dois itens ou não é elegível na
        /// categoria.
        /// </summary>
        public const string AtributoInelegivel = "AtributoInelegivel";

        /// <summary>
        /// 409, fusão: faltam os materiais de dungeon ou raid que o nível exige (+5 em diante).
        /// </summary>
        public const string MateriaisInsuficientes = "MateriaisInsuficientes";

        /// <summary>409, fusão: o saldo de coroas não cobre o custo.</summary>
        public const string CoroasInsuficientes = "CoroasInsuficientes";

        /// <summary>
        /// 409, fusão: as peças, o atributo ou o custo mudaram desde a prévia. Peça outra prévia.
        /// </summary>
        public const string PreviaDesatualizada = "PreviaDesatualizada";

        /// <summary>400, fusão: principal e material são o mesmo item.</summary>
        public const string MesmoItem = "MesmoItem";

        /// <summary>409: o exemplar tem dados que a regra não consegue usar.</summary>
        public const string ItemInvalido = "ItemInvalido";

        /// <summary>
        /// Tempo real, em <see cref="ColetaRecusada.Codigo"/>: não há item no chão com esse id
        /// para este personagem (nunca existiu, já foi coletado, expirou ou é de outro), ou o
        /// pedido de "tudo por perto" não achou nada ao alcance.
        /// </summary>
        public const string LootNaoEncontrado = "LootNaoEncontrado";

        /// <summary>
        /// Tempo real, em <see cref="ColetaRecusada.Codigo"/>: o item está a mais de
        /// <see cref="RegrasLoot.RaioDeColetaMetros"/> do personagem.
        /// </summary>
        public const string LongeDemais = "LongeDemais";

        /// <summary>
        /// 409, baú: o personagem não está na área do baú ou está a mais de
        /// <see cref="ConfigBau.RaioParaAbrirMetros"/> dele (pela posição que o servidor aceitou).
        /// Nada é sorteado nem gravado.
        /// </summary>
        public const string BauLongeDemais = "BauLongeDemais";

        /// <summary>
        /// 429, baú: o personagem abriu há menos de <see cref="ConfigBau.RecargaSegundos"/>
        /// segundos. Nada é sorteado nem gravado.
        /// </summary>
        public const string BauEmRecarga = "BauEmRecarga";
    }
}
