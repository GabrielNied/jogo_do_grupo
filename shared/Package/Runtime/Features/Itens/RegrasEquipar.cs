namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Por que uma peça não pode ir para um encaixe (#496). Uma regra só para os dois lados: o
    /// servidor recusa com estes códigos e o client os usa antes do clique (tooltip e encaixe
    /// vermelho no arraste), então a tela nunca promete o que o servidor vai recusar.
    /// </summary>
    public static class RegrasEquipar
    {
        /// <summary>
        /// O código de <see cref="CodigosErroItens"/> que recusa equipar <paramref name="item"/>
        /// em <paramref name="posicao"/>, ou <c>null</c> se pode. Ordem: encaixe errado, nível,
        /// mão secundária com arma de duas mãos.
        /// </summary>
        /// <param name="nivelPersonagem">
        /// Nível do personagem; 0 ou menos é "não sei" (o client ainda sem o dado) e não recusa
        /// por nível. O servidor sempre passa o nível real.
        /// </param>
        /// <param name="principalDoConjunto">
        /// A arma vestida na mão principal do conjunto da <paramref name="posicao"/>, ou
        /// <c>null</c> (sem arma, ou encaixe que não é mão secundária).
        /// </param>
        public static string? Recusa(
            Equipamento item,
            PosicaoEquipamento posicao,
            int nivelPersonagem,
            Equipamento? principalDoConjunto
        )
        {
            if (item == null)
                return CodigosErroItens.ItemNaoEquipavel;
            if (!RegrasEquipamentos.Aceita(item.Tipo, posicao))
                return CodigosErroItens.PosicaoInvalida;
            if (nivelPersonagem > 0 && item.NivelPersonagemMinimo > nivelPersonagem)
                return CodigosErroItens.NivelInsuficiente;
            var conjunto = RegrasEquipamentos.Conjunto(posicao);
            if (
                conjunto != 0
                && posicao == RegrasEquipamentos.Secundaria(conjunto)
                && principalDoConjunto != null
                && principalDoConjunto.DuasMaos
            )
                return CodigosErroItens.ArmaDeDuasMaos;
            return null;
        }
    }
}
