using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// Quais atributos são elegíveis em cada categoria de peça e em que faixa (TIER-006 e
    /// TIER-007). É só dados, por <see cref="StatusItem"/>: o tier e a fusão consultam esta tabela
    /// e não citam nenhum atributo. Trocar a lista de atributos de item (#623) é trocar as
    /// linhas de <see cref="ConfigTier"/>, sem mexer em <see cref="RegrasTier"/>.
    /// <para>
    /// Elegível quer dizer ter faixa para a categoria. Vale a faixa da categoria, se houver;
    /// senão a faixa padrão do atributo, se houver; senão o atributo não é elegível ali.
    /// </para>
    /// </summary>
    public sealed class TabelaDeFaixas
    {
        private readonly Dictionary<StatusItem, FaixaDeAtributo> _padrao =
            new Dictionary<StatusItem, FaixaDeAtributo>();
        private readonly Dictionary<(TipoEquipamento, StatusItem), FaixaDeAtributo?> _porCategoria =
            new Dictionary<(TipoEquipamento, StatusItem), FaixaDeAtributo?>();

        /// <summary>Faixa de <paramref name="status"/> em toda categoria sem ajuste próprio.</summary>
        public TabelaDeFaixas Padrao(StatusItem status, int minimo, int maximo)
        {
            _padrao[status] = new FaixaDeAtributo(minimo, maximo);
            return this;
        }

        /// <summary>Faixa de <paramref name="status"/> só em <paramref name="tipo"/>.</summary>
        public TabelaDeFaixas Categoria(
            TipoEquipamento tipo,
            StatusItem status,
            int minimo,
            int maximo
        )
        {
            _porCategoria[(tipo, status)] = new FaixaDeAtributo(minimo, maximo);
            return this;
        }

        /// <summary>O atributo não é elegível em <paramref name="tipo"/>, mesmo com faixa padrão.</summary>
        public TabelaDeFaixas Inelegivel(TipoEquipamento tipo, StatusItem status)
        {
            _porCategoria[(tipo, status)] = null;
            return this;
        }

        /// <summary>A faixa do atributo na categoria, ou <c>null</c> se não é elegível.</summary>
        public FaixaDeAtributo? Faixa(TipoEquipamento tipo, StatusItem status)
        {
            if (_porCategoria.TryGetValue((tipo, status), out var propria))
            {
                return propria;
            }

            return _padrao.TryGetValue(status, out var padrao) ? padrao : (FaixaDeAtributo?)null;
        }

        public bool Elegivel(TipoEquipamento tipo, StatusItem status) =>
            Faixa(tipo, status) != null;

        /// <summary>Os atributos elegíveis da categoria, na ordem de <see cref="StatusItem"/>.</summary>
        public StatusItem[] Elegiveis(TipoEquipamento tipo)
        {
            var lista = new List<StatusItem>();
            foreach (StatusItem status in System.Enum.GetValues(typeof(StatusItem)))
            {
                if (Elegivel(tipo, status))
                {
                    lista.Add(status);
                }
            }

            return lista.ToArray();
        }
    }
}
