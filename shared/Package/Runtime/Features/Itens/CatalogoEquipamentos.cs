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

        /// <summary>Primeiro modelo de arma ou mão secundária de teste (depois dos 72 modelos).</summary>
        public const int PrimeiroModeloDeArma = PrimeiroModelo + QuantidadeModelos;

        /// <summary>Espada, Arco, Cajado, Escudo e Foco de teste, só para o botão de equipamento de teste.</summary>
        public const int QuantidadeModelosDeArma = 5;

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

        // Principal de cada família: Vigia/Bastião, Predador/Ruptor, Aurora/Tempestade e
        // Batedor/Olho Longo (itens.md, Atributos dos equipamentos).
        private static readonly StatusItem[] Principais =
        {
            StatusItem.VidaMaxima,
            StatusItem.DanoFisico,
            StatusItem.DanoMagico,
            StatusItem.RegeneracaoStamina,
        };

        private static readonly StatusItem[] Especializacoes =
        {
            StatusItem.DefesaMagica,
            StatusItem.RegeneracaoStamina,
            StatusItem.RegeneracaoMana,
            StatusItem.RegeneracaoStamina,
        };

        // O atributo próprio de cada categoria (capacete, peito, luvas, calça, botas, cinto,
        // anel, brinco, adorno): o que o modelo traz além do principal da família.
        private static readonly StatusItem[] Nucleos =
        {
            StatusItem.DefesaFisica,
            StatusItem.DefesaFisica,
            StatusItem.DefesaFisica,
            StatusItem.DefesaFisica,
            StatusItem.DefesaFisica,
            StatusItem.VidaMaxima,
            StatusItem.ChanceCritica,
            StatusItem.DanoCritico,
            StatusItem.DefesaMagica,
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

        /// <summary>
        /// O modelo-base: só diz <b>quais</b> atributos o item tem (todos elegíveis na categoria,
        /// TIER-007); os valores de 0 a 100 saem do sorteio do gerador. Atributo do modelo vem
        /// marcado como fixo (<see cref="AtributoItem.Fixo"/>).
        /// </summary>
        public static Equipamento Obter(int id)
        {
            if (id == CatalogoItens.EspadaDeTeste)
                return Arma(id, CatalogoItens.ObterNome(id), TipoEquipamento.Espada);
            if (id == CatalogoItens.EscudoDeTeste)
                return Arma(id, CatalogoItens.ObterNome(id), TipoEquipamento.Escudo);
            if (id == 100)
                return Arma(id, "Martelo Ressonante", TipoEquipamento.Martelo);
            if (id >= PrimeiroModeloDeArma && id < PrimeiroModeloDeArma + QuantidadeModelosDeArma)
            {
                var tipo = ArmasDeTeste[id - PrimeiroModeloDeArma];
                return Arma(id, TextoDoTipo(tipo) + " Ressonante", tipo);
            }
            if (id < PrimeiroModelo || id >= PrimeiroModelo + QuantidadeModelos)
                return null;
            var indice = id - PrimeiroModelo;
            var categoria = indice % Categorias;
            var variante = indice / Categorias;
            var familia = variante / 2;
            var especializada = variante % 2 == 1;
            var tipoDaPeca = (TipoEquipamento)categoria;
            var atributos = new List<AtributoItem>();
            Acrescentar(atributos, tipoDaPeca, Principais[familia]);
            Acrescentar(atributos, tipoDaPeca, Nucleos[categoria]);
            if (especializada)
                Acrescentar(atributos, tipoDaPeca, Especializacoes[familia]);
            return new Equipamento
            {
                ModeloId = id,
                Nome = Nomes[familia][categoria] + " " + Sufixos[variante],
                Familia = Familias[familia],
                Variacao = especializada ? "B" : "A",
                Regiao = "Vale",
                Tipo = tipoDaPeca,
                Atributos = atributos.ToArray(),
            };
        }

        private static readonly TipoEquipamento[] ArmasDeTeste =
        {
            TipoEquipamento.Espada,
            TipoEquipamento.Arco,
            TipoEquipamento.Cajado,
            TipoEquipamento.Escudo,
            TipoEquipamento.Foco,
        };

        // Arma ou mão secundária de teste: os atributos do modelo são os da matriz que dão o golpe
        // (dano da arma e o principal) ou a defesa.
        private static Equipamento Arma(int id, string nome, TipoEquipamento tipo)
        {
            var atributos = new List<AtributoItem>();
            foreach (var status in ConfigAtributosItem.Elegiveis(tipo))
            {
                if (EhDoModeloDeArma(status))
                    atributos.Add(Fixo(status, 0));
            }
            return new Equipamento
            {
                ModeloId = id,
                Nome = nome,
                Regiao = "Vale",
                Tipo = tipo,
                DuasMaos = tipo == TipoEquipamento.Martelo,
                Atributos = atributos.ToArray(),
            };
        }

        // Dano da arma, Força, Intelecto, Vigor e defesas; o resto da matriz vem do sorteio.
        private static bool EhDoModeloDeArma(StatusItem status) =>
            status == StatusItem.DanoDaArma
            || status == StatusItem.DanoFisico
            || status == StatusItem.DanoMagico
            || status == StatusItem.VidaMaxima
            || status == StatusItem.DefesaFisica;

        private static string TextoDoTipo(TipoEquipamento tipo) => tipo.ToString();

        // Só entra o atributo elegível na categoria e que o modelo ainda não tem.
        private static void Acrescentar(
            List<AtributoItem> atributos,
            TipoEquipamento tipo,
            StatusItem status
        )
        {
            if (
                ConfigAtributosItem.Elegivel(tipo, status)
                && !atributos.Exists(a => a.Status == status)
            )
                atributos.Add(Fixo(status, 0));
        }

        // A arma existente, os 72 modelos e as cinco armas de teste; IDs antigos permanecem estáveis.
        public static readonly int[] ModelosDeTeste = CriarIds();

        private static int[] CriarIds()
        {
            var total = QuantidadeModelos + 1 + QuantidadeModelosDeArma;
            var ids = new int[total];
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
