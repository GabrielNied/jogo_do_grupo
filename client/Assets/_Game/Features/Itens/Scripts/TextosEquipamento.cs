using System.Globalization;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Textos do equipamento em português: nomes, descrição por extenso, siglas e mensagens de
    /// erro do servidor. Regra pura (sem Unity), compartilhada pela grade, pelo tooltip e pelo
    /// painel da fusão.
    /// </summary>
    public static class TextosEquipamento
    {
        /// <summary>
        /// O tier como o jogador lê: "Tier A". Quando o tier atual já não é o de origem (os
        /// valores mudaram), acrescenta a origem: "Tier B (origem D)". Exemplar sem tier de
        /// origem gravado (escala antiga) não mostra origem.
        /// </summary>
        public static string NomeDoTier(Equipamento item)
        {
            var atual = RegrasTier.TierDe(item);
            var texto = "Tier " + RegrasTier.Letra(atual);
            if (item.VersaoDosAtributos >= RegrasTier.VersaoAtual && item.TierOrigem != atual)
                texto += " (origem " + RegrasTier.Letra(item.TierOrigem) + ")";
            return texto;
        }

        /// <summary>
        /// Linha de um atributo do item: valor e máximo da faixa e o que ele faz no combate
        /// ("Força 72/100 · +14,4% de dano físico"). É o atributo <b>do item</b>: não soma na
        /// Força do personagem. Atributo sem faixa (legado) mostra só o valor.
        /// </summary>
        public static string LinhaDoAtributo(Equipamento item, int indice)
        {
            var atributo = item.Atributos[indice];
            var faixa = ConfigTier.Faixa(item.Tipo, atributo.Status);
            var texto = NomeStatus(atributo.Status);
            if (faixa == null)
                return texto + " +" + Numero(atributo.Valor);
            var convertido = ConfigAtributosItem.Converter(
                item.Tipo,
                atributo.Status,
                atributo.Valor
            );
            return texto
                + " "
                + Numero(atributo.Valor)
                + "/"
                + faixa.Value.Maximo
                + " · "
                + RegrasAtributosDeItem.Efeito(
                    atributo.Status,
                    convertido,
                    ConfigAtributosItem.ArmaMagica(item.Tipo)
                );
        }

        /// <summary>Número com até duas casas, sem zeros sobrando (ex.: 6, 2,5).</summary>
        public static string Numero(double valor) =>
            valor.ToString("0.##", CultureInfo.CurrentCulture);

        /// <summary>Descrição completa em texto corrido (usada na bancada de fusão).</summary>
        public static string Descrever(EntradaInventario entrada)
        {
            var item = RegrasEquipamentos.DadosDe(entrada);
            if (item == null)
                return entrada.Nome;
            var texto =
                item.Nome
                + " · "
                + NomeDoTier(item)
                + " · "
                + item.Regiao
                + " · nível "
                + item.Nivel
                + "/"
                + RegrasEquipamentos.NivelMaximo(item.Raridade)
                + " · grau "
                + item.Grau;
            texto +=
                "\nRequer personagem nível "
                + item.NivelPersonagemMinimo
                + (item.DuasMaos ? " · duas mãos" : "");
            if (!string.IsNullOrEmpty(item.Familia))
                texto += "\nFamília " + item.Familia + " · variante " + item.Variacao;
            for (var i = 0; i < item.Atributos.Length; i++)
            {
                texto += "\n" + LinhaDoAtributo(item, i);
            }
            return texto + "\nFusão +" + item.NivelFusao + "/" + ConfigFusao.NivelMaximo;
        }

        public static string NomeStatus(StatusItem status) => RegrasAtributosDeItem.Nome(status);

        public static string NomePosicao(PosicaoEquipamento posicao)
        {
            var nomes = new[]
            {
                "Inventário",
                "Capacete",
                "Peito",
                "Luvas",
                "Calça",
                "Botas",
                "Cinto",
                "Anel 1",
                "Anel 2",
                "Brinco 1",
                "Brinco 2",
                "Adorno",
                "Arma · conjunto 1",
                "Mão secundária · conjunto 1",
                "Arma · conjunto 2",
                "Mão secundária · conjunto 2",
            };
            return nomes[(int)posicao];
        }

        public static string NomeDoTipo(TipoEquipamento tipo)
        {
            switch (tipo)
            {
                case TipoEquipamento.Calca:
                    return "Calça";
                default:
                    return tipo.ToString();
            }
        }

        /// <summary>
        /// Sigla de três letras do tipo, mostrada quando ainda não há ícone (a silhueta definitiva
        /// é a #567).
        /// </summary>
        public static string SiglaDoTipo(TipoEquipamento tipo)
        {
            switch (tipo)
            {
                case TipoEquipamento.Capacete:
                    return "Cap";
                case TipoEquipamento.Peito:
                    return "Pei";
                case TipoEquipamento.Luvas:
                    return "Luv";
                case TipoEquipamento.Calca:
                    return "Cal";
                case TipoEquipamento.Botas:
                    return "Bot";
                case TipoEquipamento.Cinto:
                    return "Cin";
                case TipoEquipamento.Anel:
                    return "Ane";
                case TipoEquipamento.Brinco:
                    return "Bri";
                case TipoEquipamento.Adorno:
                    return "Ado";
                case TipoEquipamento.Espada:
                    return "Esp";
                case TipoEquipamento.Martelo:
                    return "Mar";
                case TipoEquipamento.Arco:
                    return "Arc";
                case TipoEquipamento.Cajado:
                    return "Caj";
                case TipoEquipamento.Escudo:
                    return "Esc";
                default:
                    return "Foc";
            }
        }

        /// <summary>
        /// Tipo da silhueta de um encaixe vazio (armas mostram a espada e a mão secundária, o
        /// escudo).
        /// </summary>
        public static TipoEquipamento TipoDoEncaixe(PosicaoEquipamento posicao)
        {
            switch (posicao)
            {
                case PosicaoEquipamento.Capacete:
                    return TipoEquipamento.Capacete;
                case PosicaoEquipamento.Peito:
                    return TipoEquipamento.Peito;
                case PosicaoEquipamento.Luvas:
                    return TipoEquipamento.Luvas;
                case PosicaoEquipamento.Calca:
                    return TipoEquipamento.Calca;
                case PosicaoEquipamento.Botas:
                    return TipoEquipamento.Botas;
                case PosicaoEquipamento.Cinto:
                    return TipoEquipamento.Cinto;
                case PosicaoEquipamento.Anel1:
                case PosicaoEquipamento.Anel2:
                    return TipoEquipamento.Anel;
                case PosicaoEquipamento.Brinco1:
                case PosicaoEquipamento.Brinco2:
                    return TipoEquipamento.Brinco;
                case PosicaoEquipamento.Adorno:
                    return TipoEquipamento.Adorno;
                case PosicaoEquipamento.Arma1:
                case PosicaoEquipamento.Arma2:
                    return TipoEquipamento.Espada;
                default:
                    return TipoEquipamento.Escudo;
            }
        }

        /// <summary>Mensagem para a pessoa a partir do código de erro do servidor.</summary>
        public static string MensagemDeErro(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroItens.InventarioCheio:
                    return "Não há espaço na bolsa para guardar as peças retiradas.";
                case "NivelInsuficiente":
                    return "Seu personagem ainda não tem o nível exigido pela peça.";
                case "ArmaDeDuasMaos":
                    return "Retire a arma de duas mãos antes de equipar a mão secundária.";
                case CodigosErroItens.ItemEquipado:
                    return "Desequipe a peça antes de descartá-la.";
                case CodigosErroItens.ItemNaoEncontrado:
                    return "Esse item não está mais disponível. Feche e abra o inventário para atualizar.";
                case CodigosErroItens.CasaInvalida:
                    return "Essa casa não existe na bolsa.";
                case CodigosErroItens.MesmoItem:
                    return "Escolha uma peça diferente como material.";
                case CodigosErroItens.MaterialIncompativel:
                    return "O material precisa ser da mesma categoria de peça do item principal.";
                case CodigosErroItens.MaterialEquipado:
                    return "Desequipe o material antes de consumi-lo.";
                case CodigosErroItens.ItemFavorito:
                    return "Desmarque o favorito do material para permitir seu consumo.";
                case CodigosErroItens.NivelMaximo:
                    return "Esta peça já está em +10 e não pode ser fundida de novo.";
                case CodigosErroItens.AtributoInelegivel:
                    return "Esse atributo não existe nos dois itens. Escolha outro.";
                case CodigosErroItens.MateriaisInsuficientes:
                    return "Faltam os materiais de dungeon ou raid que esta fusão exige.";
                case CodigosErroItens.CoroasInsuficientes:
                    return "Você não tem coroas suficientes para esta fusão.";
                case CodigosErroItens.PreviaDesatualizada:
                    return "As peças, o atributo ou o custo mudaram. Consulte uma nova prévia antes de confirmar.";
                case CodigosErroItens.ItemInvalido:
                    return "Esta peça não pode ser fundida: os dados dela são inválidos.";
                case "PosicaoInvalida":
                    return "Essa peça não pode ser equipada nesse encaixe.";
                case "ItemNaoEquipavel":
                    return "Esse item não pode ser equipado.";
                default:
                    return "Não foi possível concluir. Tente novamente.";
            }
        }
    }
}
