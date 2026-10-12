using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>Valores iniciais do livro. Grau é aplicado uma única vez pelo gerador.</summary>
    public static class CatalogoEquipamentos
    {
        public const int PrimeiroModelo = 101;
        public const int QuantidadeModelos = 72;
        private const int Categorias = 9;
        private const double AtributoPrincipalA = 6;
        private const double AtributoPrincipalB = 4;
        private const double DefesaBastiao = 8;
        private const double VigorEspecializado = 10;
        private const double EspiritoTempestade = 3;
        private static readonly string[] Familias =
        {
            "Vigia/Bastião",
            "Predador/Ruptor",
            "Aurora/Tempestade",
            "Batedor/Olho Longo",
        };
        private static readonly string[] Sufixos =
        {
            "do Vigia",
            "do Bastião",
            "do Predador",
            "do Ruptor",
            "da Aurora",
            "da Tempestade",
            "do Batedor",
            "do Olho Longo",
        };
        private static readonly StatusItem[] Principais =
        {
            StatusItem.Vitalidade,
            StatusItem.Forca,
            StatusItem.Intelecto,
            StatusItem.Destreza,
        };

        // Capacete, peito, luvas, calça, botas, cinto, anel, brinco, adorno.
        private static readonly string[][] Nomes =
        {
            new[]
            {
                "Elmo",
                "Peitoral",
                "Manoplas",
                "Grevas",
                "Botas",
                "Cinturão",
                "Anel",
                "Brinco",
                "Insígnia",
            },
            new[]
            {
                "Elmo",
                "Colete",
                "Luvas",
                "Calças",
                "Botas",
                "Cinto",
                "Anel",
                "Brinco",
                "Medalhão",
            },
            new[]
            {
                "Capuz",
                "Manto",
                "Luvas",
                "Calças",
                "Botas",
                "Faixa",
                "Anel",
                "Brinco",
                "Foco",
            },
            new[]
            {
                "Visor",
                "Gibão",
                "Braçadeiras",
                "Calças",
                "Botas",
                "Cinto",
                "Anel",
                "Brinco",
                "Talismã",
            },
        };
        private static readonly int[][] Defesas =
        {
            new[] { 16, 30, 10, 20, 10, 8, 0, 0, 0 },
            new[] { 10, 20, 7, 13, 7, 5, 0, 0, 0 },
            new[] { 5, 9, 3, 6, 3, 2, 0, 0, 0 },
            new[] { 6, 12, 4, 8, 4, 3, 0, 0, 0 },
        };

        public static Equipamento Obter(int id)
        {
            if (id == CatalogoItens.EspadaDeTeste || id == CatalogoItens.EscudoDeTeste || id == 100)
                return new Equipamento
                {
                    ModeloId = id,
                    Nome = id == 100 ? "Martelo Ressonante" : CatalogoItens.ObterNome(id),
                    Regiao = "Vale",
                    Tipo =
                        id == 100 ? TipoEquipamento.Martelo
                        : id == CatalogoItens.EspadaDeTeste ? TipoEquipamento.Espada
                        : TipoEquipamento.Escudo,
                    DuasMaos = id == 100,
                    Atributos = new[] { Fixo(StatusItem.Forca, id == 100 ? 20 : 0) },
                };
            if (id < PrimeiroModelo || id >= PrimeiroModelo + QuantidadeModelos)
                return null;
            var indice = id - PrimeiroModelo;
            var categoria = indice % Categorias;
            var variante = indice / Categorias;
            var familia = variante / 2;
            var especializada = variante % 2 == 1;
            var atributos = new List<AtributoItem>
            {
                Fixo(Principais[familia], especializada ? AtributoPrincipalB : AtributoPrincipalA),
            };
            var defesa =
                Defesas[familia][categoria] + (especializada && familia == 0 ? DefesaBastiao : 0);
            if (defesa > 0)
                atributos.Add(Fixo(StatusItem.Defesa, defesa));
            if (especializada && familia != 0)
                atributos.Add(
                    Fixo(
                        familia == 2 ? StatusItem.Espirito : StatusItem.Vigor,
                        familia == 2 ? EspiritoTempestade : VigorEspecializado
                    )
                );
            return new Equipamento
            {
                ModeloId = id,
                Nome = Nomes[familia][categoria] + " " + Sufixos[variante],
                Familia = Familias[familia],
                Variacao = especializada ? "B" : "A",
                Regiao = "Vale",
                Tipo = (TipoEquipamento)categoria,
                Atributos = atributos.ToArray(),
            };
        }

        // A arma existente e os 72 modelos; IDs antigos permanecem estáveis.
        public static readonly int[] ModelosDeTeste = CriarIds();

        private static int[] CriarIds()
        {
            var ids = new int[QuantidadeModelos + 1];
            for (var i = 0; i < ids.Length; i++)
                ids[i] = 100 + i;
            return ids;
        }

        private static AtributoItem Fixo(StatusItem status, double valor) =>
            new AtributoItem
            {
                Status = status,
                Valor = valor,
                Fixo = true,
            };
    }
}
