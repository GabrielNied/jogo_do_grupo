using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Profissoes;

namespace JogoDoGrupo.Client.Features.Profissoes
{
    /// <summary>Os textos de Profissões na interface (o servidor manda só códigos).</summary>
    internal static class TextosProfissoes
    {
        public const string Titulo = "Profissões";
        public const string SemReceitas =
            "Este ofício só coleta: leve o material do campo para quem produz.";

        public static string TituloDa(Profissao profissao) =>
            "Ofício de " + ConfigProfissoes.NomeDaProfissao(profissao);

        public static string RotuloDaBancada(Profissao profissao) =>
            "usar a bancada de " + ConfigProfissoes.NomeDaProfissao(profissao).ToLowerInvariant();

        public static string AjudaDa(Profissao profissao) =>
            "Aprenda o ofício de "
            + ConfigProfissoes.NomeDaProfissao(profissao).ToLowerInvariant()
            + " aqui, de graça. "
            + (
                ConfigProfissoes.ReceitasDaProfissao(profissao).Length == 0
                    ? "Colete no campo, ao norte do portão."
                    : "Produza na bancada com o que coletar no campo, ao norte do portão."
            );

        public static string Recusa(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroProfissoes.ProfissaoInvalida:
                    return "Essa profissão ainda não pode ser aprendida.";
                case CodigosErroProfissoes.JaAprendida:
                    return "Você já aprendeu essa profissão.";
                case CodigosErroProfissoes.ProfissaoNaoAprendida:
                    return "Aprenda a profissão na oficina da cidade.";
                case CodigosErroProfissoes.NoInexistente:
                    return "Não há nada para coletar aqui.";
                case CodigosErroProfissoes.NoEsgotado:
                    return "Outro jogador já coletou. Volta em instantes.";
                case CodigosErroProfissoes.Longe:
                    return "Chegue mais perto.";
                case CodigosErroProfissoes.ReceitaInexistente:
                    return "Essa receita não existe.";
                case CodigosErroProfissoes.MateriaisInsuficientes:
                    return "Faltam materiais para essa receita.";
                case CodigosErroProfissoes.InventarioCheio:
                    return "Bolsa cheia. Libere espaço e tente de novo.";
                default:
                    return "Não foi possível agora. Tente de novo.";
            }
        }

        public static string DicaDoPonto(PontoDeColeta ponto, bool aprendeu)
        {
            return aprendeu
                ? "coletar " + ponto.Nome.ToLowerInvariant()
                : ponto.Nome + ": requer " + ConfigProfissoes.NomeDaProfissao(ponto.Profissao);
        }

        public static string Coleta(ColetaConcluida coleta)
        {
            return "+"
                + coleta.Quantidade
                + " "
                + ConfigProfissoes.NomeDoMaterial(coleta.MaterialId);
        }

        public static string NomeDoProduto(ReceitaDeProducao receita)
        {
            if (receita == null)
                return "Item";
            if (!string.IsNullOrEmpty(receita.Nome))
                return receita.Nome;
            var modelo = CatalogoEquipamentos.Obter(receita.ModeloId);
            return modelo != null ? modelo.Nome : "Item";
        }

        public static string Producao(ProducaoConcluida producao)
        {
            var receita = ConfigProfissoes.ObterReceita(producao.ReceitaId);
            var nome =
                receita != null && !string.IsNullOrEmpty(receita.Nome)
                    ? receita.Nome
                    : (CatalogoEquipamentos.Obter(producao.ModeloId)?.Nome ?? "Item");
            return nome + " na bolsa. Equipe pelo inventário.";
        }
    }
}
