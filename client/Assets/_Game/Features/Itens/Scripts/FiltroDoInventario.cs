using System.Text;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>Recorte por marca do item (a lista "Mostrar" da barra de filtros).</summary>
    public enum EscopoDoFiltro
    {
        Todos,
        Favoritos,
        Lixo,
        Novos,
    }

    /// <summary>
    /// Busca e filtros da bolsa (#334). Só decide quais peças aparecem em destaque: não reordena
    /// nem muda nada no servidor, então arrastar, equipar e favoritar seguem como antes. Estado
    /// da sessão (não persiste), regra pura para teste. Favoritos continuam sendo o campo
    /// <c>Favorito</c> do exemplar, que o servidor guarda (#563).
    /// </summary>
    public sealed class FiltroDoInventario
    {
        /// <summary>Rótulos do seletor de tier, na ordem de <see cref="TierItem"/>.</summary>
        public static readonly string[] RotulosDeTier =
        {
            "Qualquer tier",
            "Tier C ou melhor",
            "Tier B ou melhor",
            "Tier A ou melhor",
            "Tier S ou melhor",
            "Só tier S+",
        };

        public static readonly string[] RotulosDeEscopo =
        {
            "Todos",
            "★ Favoritos",
            "Lixo",
            "Novos",
        };

        public const string RotuloQualquerTipo = "Qualquer tipo";

        private string _texto = "";
        private string[] _termos = new string[0];

        /// <summary>O que o jogador digitou; cada palavra precisa aparecer (sem acento nem caixa).</summary>
        public string Texto
        {
            get => _texto;
            set
            {
                _texto = value ?? "";
                _termos = Termos(_texto);
            }
        }

        /// <summary>Tier mínimo; <see cref="TierItem.D"/> não filtra por tier.</summary>
        public TierItem TierMinimo { get; set; } = TierItem.D;

        /// <summary>Tipo da peça; <c>null</c> aceita qualquer um.</summary>
        public TipoEquipamento? Tipo { get; set; }

        public EscopoDoFiltro Escopo { get; set; } = EscopoDoFiltro.Todos;

        /// <summary>Se algum critério está ligado (a barra destaca e mostra "Limpar").</summary>
        public bool Ativo =>
            _termos.Length > 0
            || TierMinimo != TierItem.D
            || Tipo != null
            || Escopo != EscopoDoFiltro.Todos;

        public void Limpar()
        {
            Texto = "";
            TierMinimo = TierItem.D;
            Tipo = null;
            Escopo = EscopoDoFiltro.Todos;
        }

        /// <summary>
        /// Se <paramref name="entrada"/> passa em todos os critérios. Item que não é equipamento
        /// (poção, material) não tem tier nem tipo: some quando o filtro pede um dos dois.
        /// </summary>
        public bool Passa(EntradaInventario entrada, bool novo)
        {
            if (entrada == null)
                return false;
            if (!Ativo)
                return true;

            var dados = RegrasEquipamentos.DadosDe(entrada);
            var tipo = LogicaInventario.TipoDe(entrada);

            if (Tipo != null && tipo != Tipo)
                return false;
            if (
                TierMinimo != TierItem.D
                && (dados == null || RegrasTier.TierDe(dados) < TierMinimo)
            )
                return false;

            switch (Escopo)
            {
                case EscopoDoFiltro.Favoritos:
                    if (dados == null || !dados.Favorito)
                        return false;
                    break;
                case EscopoDoFiltro.Lixo:
                    if (dados == null || !dados.Lixo)
                        return false;
                    break;
                case EscopoDoFiltro.Novos:
                    if (!novo)
                        return false;
                    break;
            }

            if (_termos.Length == 0)
                return true;
            var palheiro = Palheiro(entrada, dados, tipo);
            foreach (var termo in _termos)
            {
                if (!palheiro.Contains(termo))
                    return false;
            }
            return true;
        }

        // Nome, tipo e tier da peça, no mesmo formato dos termos.
        private static string Palheiro(
            EntradaInventario entrada,
            Equipamento dados,
            TipoEquipamento? tipo
        )
        {
            var nome = entrada.Nome;
            if (string.IsNullOrEmpty(nome))
                nome = CatalogoItens.ObterNome(entrada.ItemId) ?? "";
            var texto = new StringBuilder(nome);
            if (tipo != null)
                texto.Append(' ').Append(TextosEquipamento.NomeDoTipo(tipo.Value));
            if (dados != null)
                texto.Append(" tier ").Append(RegrasTier.Letra(RegrasTier.TierDe(dados)));
            return Simplificar(texto.ToString());
        }

        private static string[] Termos(string texto) =>
            Simplificar(texto).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        /// <summary>Minúsculas e sem acento do português ("Calça" casa com "calca").</summary>
        public static string Simplificar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return "";
            var resultado = new StringBuilder(texto.Length);
            foreach (var letra in texto.ToLowerInvariant())
                resultado.Append(SemAcento(letra));
            return resultado.ToString();
        }

        private static char SemAcento(char letra)
        {
            const string comAcento = "áàâãäéèêëíìîïóòôõöúùûüç";
            const string semAcento = "aaaaaeeeeiiiiooooouuuuc";
            var indice = comAcento.IndexOf(letra);
            return indice >= 0 ? semAcento[indice] : letra;
        }
    }
}
