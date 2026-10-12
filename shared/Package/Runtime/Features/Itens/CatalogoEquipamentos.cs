using System;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>Modelos iniciais do livro. O catálogo completo por família pertence à #330.</summary>
    public static class CatalogoEquipamentos
    {
        public static Equipamento Obter(int id)
        {
            if (id == CatalogoItens.EspadaDeTeste || id == CatalogoItens.EscudoDeTeste)
                return Modelo(
                    id,
                    CatalogoItens.ObterNome(id),
                    id == 1 ? TipoEquipamento.Espada : TipoEquipamento.Escudo,
                    StatusItem.Forca,
                    0,
                    0
                );
            if (id == 100)
                return Modelo(
                    id,
                    "Martelo Ressonante",
                    TipoEquipamento.Martelo,
                    StatusItem.Forca,
                    20,
                    0,
                    true
                );
            if (id < 101 || id > 109)
                return null;
            var tipos = new[]
            {
                TipoEquipamento.Capacete,
                TipoEquipamento.Peito,
                TipoEquipamento.Luvas,
                TipoEquipamento.Calca,
                TipoEquipamento.Botas,
                TipoEquipamento.Cinto,
                TipoEquipamento.Anel,
                TipoEquipamento.Brinco,
                TipoEquipamento.Adorno,
            };
            var nomes = new[]
            {
                "Elmo do Vigia",
                "Peitoral do Vigia",
                "Manoplas do Vigia",
                "Grevas do Vigia",
                "Botas do Vigia",
                "Cinturão do Vigia",
                "Anel do Vigia",
                "Brinco do Vigia",
                "Insígnia do Vigia",
            };
            var defesas = new[] { 16, 30, 10, 20, 10, 8, 0, 0, 0 };
            var i = id - 101;
            return Modelo(id, nomes[i], tipos[i], StatusItem.Vitalidade, 6, defesas[i]);
        }

        public static readonly int[] ModelosDeTeste =
        {
            100,
            101,
            102,
            103,
            104,
            105,
            106,
            107,
            108,
            109,
        };

        private static Equipamento Modelo(
            int id,
            string nome,
            TipoEquipamento tipo,
            StatusItem fixo,
            double valor,
            int defesa,
            bool duasMaos = false
        )
        {
            return new Equipamento
            {
                ModeloId = id,
                Nome = nome,
                Tipo = tipo,
                Regiao = "Vale",
                DuasMaos = duasMaos,
                Atributos =
                    defesa > 0
                        ? new[]
                        {
                            new AtributoItem
                            {
                                Status = fixo,
                                Valor = valor,
                                Fixo = true,
                            },
                            new AtributoItem
                            {
                                Status = StatusItem.Defesa,
                                Valor = defesa,
                                Fixo = true,
                            },
                        }
                        : new[]
                        {
                            new AtributoItem
                            {
                                Status = fixo,
                                Valor = valor,
                                Fixo = true,
                            },
                        },
            };
        }
    }
}
