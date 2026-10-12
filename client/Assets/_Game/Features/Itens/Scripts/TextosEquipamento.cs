using System.Globalization;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Textos do equipamento em português: nomes, descrição por extenso, siglas e mensagens de
    /// erro do servidor. Regra pura (sem Unity), compartilhada pela grade, pelo tooltip e pelo
    /// painel de infusão.
    /// </summary>
    public static class TextosEquipamento
    {
        private static readonly string[] Raridades =
        {
            "Comum",
            "Incomum",
            "Raro",
            "Épico",
            "Lendário",
        };

        public static string NomeDaRaridade(RaridadeItem raridade) => Raridades[(int)raridade];

        /// <summary>Número com até duas casas, sem zeros sobrando (ex.: 6, 2,5).</summary>
        public static string Numero(double valor) =>
            valor.ToString("0.##", CultureInfo.CurrentCulture);

        /// <summary>Descrição completa em texto corrido (usada no painel de infusão).</summary>
        public static string Descrever(EntradaInventario entrada)
        {
            var item = entrada.Equipamento;
            if (item == null)
                return entrada.Nome;
            var texto =
                item.Nome
                + " · "
                + NomeDaRaridade(item.Raridade)
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
                var atributo = item.Atributos[i];
                texto +=
                    "\n"
                    + NomeStatus(atributo.Status)
                    + " +"
                    + Numero(atributo.Valor)
                    + (atributo.Fixo ? " (fixo)" : " (sorteado)")
                    + (i == item.IndiceInfusivel ? " · infusível" : "");
            }
            return texto
                + "\nInfusões: "
                + item.InfusoesUsadas
                + "/"
                + RegrasEquipamentos.LimiteInfusoes(item.Raridade);
        }

        public static string NomeStatus(StatusItem status)
        {
            switch (status)
            {
                case StatusItem.Forca:
                    return "Força";
                case StatusItem.PotenciaFogo:
                    return "Potência de fogo (%)";
                case StatusItem.PotenciaGelo:
                    return "Potência de gelo (%)";
                case StatusItem.Alcance:
                    return "Alcance (m)";
                case StatusItem.RecuperacaoVigor:
                    return "Recuperação de vigor (%)";
                default:
                    return status.ToString();
            }
        }

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
                case "MesmoItem":
                    return "Escolha uma peça diferente como doador.";
                case "DoadorIncompativel":
                    return "O doador precisa ter o mesmo tipo e raridade do receptor.";
                case "DoadorEquipado":
                    return "Desequipe o doador antes de consumi-lo.";
                case "ItemFavorito":
                    return "Desmarque o favorito do doador para permitir seu consumo.";
                case "LimiteInfusoes":
                    return "Esta peça atingiu o limite de infusões da raridade.";
                case "CoroasInsuficientes":
                    return "Você não tem coroas suficientes. Consulte uma nova prévia após obter mais coroas.";
                case "PreviaDesatualizada":
                    return "As peças ou o custo mudaram. Consulte uma nova prévia antes de confirmar.";
                case "ItemInvalido":
                    return "Esta peça não possui um atributo válido para infusão.";
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
