using System;

namespace JogoDoGrupo.Shared.Features.Profissoes
{
    /// <summary>Um material de profissão (saldo por personagem, fora da bolsa).</summary>
    public sealed class MaterialDeProfissao
    {
        public readonly int Id;
        public readonly string Nome;

        public MaterialDeProfissao(int id, string nome)
        {
            Id = id;
            Nome = nome;
        }
    }

    /// <summary>
    /// Um ponto de coleta no mundo: onde fica, qual profissão o coleta e o que rende. Todos os
    /// jogadores da área enxergam o mesmo ponto; quem coleta primeiro leva.
    /// </summary>
    public sealed class PontoDeColeta
    {
        public readonly int Id;
        public readonly string Nome;
        public readonly Profissao Profissao;
        public readonly int MaterialId;
        public readonly int Quantidade;
        public readonly float X;
        public readonly float Z;

        public PontoDeColeta(
            int id,
            string nome,
            Profissao profissao,
            int materialId,
            int quantidade,
            float x,
            float z
        )
        {
            Id = id;
            Nome = nome;
            Profissao = profissao;
            MaterialId = materialId;
            Quantidade = quantidade;
            X = x;
            Z = z;
        }
    }

    /// <summary>Uma receita de produção: o que gasta e o que entrega (um equipamento do catálogo).</summary>
    public sealed class ReceitaDeProducao
    {
        public readonly int Id;
        public readonly Profissao Profissao;
        public readonly int ModeloId;
        public readonly int[] MateriaisIds;
        public readonly int[] MateriaisQuantidades;

        public ReceitaDeProducao(
            int id,
            Profissao profissao,
            int modeloId,
            int[] materiaisIds,
            int[] materiaisQuantidades
        )
        {
            Id = id;
            Profissao = profissao;
            ModeloId = modeloId;
            MateriaisIds = materiaisIds;
            MateriaisQuantidades = materiaisQuantidades;
        }
    }

    /// <summary>
    /// Conteúdo e valores iniciais de teste das profissões (issue #192): coleta de madeira,
    /// minério e couro, e as receitas do Ferreiro. Balanceamento final é da #335. Os números
    /// daqui são os do servidor; o client só os usa para desenhar e para a dica de proximidade.
    /// </summary>
    public static class ConfigProfissoes
    {
        /// <summary>A cidade e o campo em volta (qualquer canal) é onde tudo acontece.</summary>
        public const string PrefixoDaArea = "cidade";

        public const int MaterialMadeira = 1;
        public const int MaterialMinerioDeFerro = 2;
        public const int MaterialCouroCru = 3;

        public const int NoArvore1 = 1;
        public const int NoArvore2 = 2;
        public const int NoVeioDeFerro1 = 3;
        public const int NoVeioDeFerro2 = 4;
        public const int NoCarcaca1 = 5;
        public const int NoCarcaca2 = 6;

        public const int ReceitaEspada = 1;
        public const int ReceitaEscudo = 2;
        public const int ReceitaPeitoral = 3;

        /// <summary>Segundos até um ponto coletado voltar para todos.</summary>
        public const int RespawnSegundos = 60;

        /// <summary>Distância horizontal máxima aceita pelo servidor, em metros.</summary>
        public const float AlcanceMetros = 4f;

        /// <summary>Distância em que o client mostra a dica (menor que a do servidor).</summary>
        public const float AlcanceDaDicaMetros = 3f;

        /// <summary>A bancada da forja: rua oeste, a alguns passos do ferreiro da cidade.</summary>
        public const float ForjaX = -14f;

        public const float ForjaZ = -11f;

        /// <summary>
        /// Profissões que o personagem pode aprender agora. Alquimista e Cozinheiro chegam com a
        /// #335 (poção e comida com buff).
        /// </summary>
        public static readonly Profissao[] Disponiveis =
        {
            Profissao.Lenhador,
            Profissao.Minerador,
            Profissao.Curtidor,
            Profissao.Ferreiro,
        };

        public static readonly MaterialDeProfissao[] Materiais =
        {
            new MaterialDeProfissao(MaterialMadeira, "Madeira"),
            new MaterialDeProfissao(MaterialMinerioDeFerro, "Minério de ferro"),
            new MaterialDeProfissao(MaterialCouroCru, "Couro cru"),
        };

        // Clareira ao norte do portão (o muro norte da cidade tem o portão; o campo é aberto).
        public static readonly PontoDeColeta[] Pontos =
        {
            new PontoDeColeta(
                NoArvore1,
                "Árvore",
                Profissao.Lenhador,
                MaterialMadeira,
                2,
                -18f,
                58f
            ),
            new PontoDeColeta(
                NoArvore2,
                "Árvore",
                Profissao.Lenhador,
                MaterialMadeira,
                2,
                -26f,
                68f
            ),
            new PontoDeColeta(
                NoVeioDeFerro1,
                "Veio de ferro",
                Profissao.Minerador,
                MaterialMinerioDeFerro,
                2,
                18f,
                58f
            ),
            new PontoDeColeta(
                NoVeioDeFerro2,
                "Veio de ferro",
                Profissao.Minerador,
                MaterialMinerioDeFerro,
                2,
                26f,
                68f
            ),
            new PontoDeColeta(
                NoCarcaca1,
                "Carcaça",
                Profissao.Curtidor,
                MaterialCouroCru,
                2,
                -8f,
                76f
            ),
            new PontoDeColeta(
                NoCarcaca2,
                "Carcaça",
                Profissao.Curtidor,
                MaterialCouroCru,
                2,
                8f,
                76f
            ),
        };

        // Modelos do catálogo de equipamentos (CatalogoEquipamentos): Espada Ressonante (173),
        // Escudo Ressonante (176) e Peitoral do Vigia (102). O nome exibido vem do catálogo.
        public static readonly ReceitaDeProducao[] Receitas =
        {
            new ReceitaDeProducao(
                ReceitaEspada,
                Profissao.Ferreiro,
                173,
                new[] { MaterialMinerioDeFerro, MaterialMadeira },
                new[] { 4, 2 }
            ),
            new ReceitaDeProducao(
                ReceitaEscudo,
                Profissao.Ferreiro,
                176,
                new[] { MaterialMinerioDeFerro, MaterialMadeira, MaterialCouroCru },
                new[] { 3, 1, 2 }
            ),
            new ReceitaDeProducao(
                ReceitaPeitoral,
                Profissao.Ferreiro,
                102,
                new[] { MaterialMinerioDeFerro, MaterialCouroCru },
                new[] { 3, 3 }
            ),
        };

        public static bool EhDaArea(string area) =>
            area != null
            && (
                area == PrefixoDaArea
                || area.StartsWith(PrefixoDaArea + ":", StringComparison.Ordinal)
            );

        public static bool PodeAprender(Profissao profissao) =>
            Array.IndexOf(Disponiveis, profissao) >= 0;

        public static PontoDeColeta ObterPonto(int id)
        {
            for (var i = 0; i < Pontos.Length; i++)
            {
                if (Pontos[i].Id == id)
                    return Pontos[i];
            }
            return null;
        }

        public static ReceitaDeProducao ObterReceita(int id)
        {
            for (var i = 0; i < Receitas.Length; i++)
            {
                if (Receitas[i].Id == id)
                    return Receitas[i];
            }
            return null;
        }

        public static string NomeDoMaterial(int materialId)
        {
            for (var i = 0; i < Materiais.Length; i++)
            {
                if (Materiais[i].Id == materialId)
                    return Materiais[i].Nome;
            }
            return null;
        }

        public static string NomeDaProfissao(Profissao profissao)
        {
            switch (profissao)
            {
                case Profissao.Lenhador:
                    return "Lenhador";
                case Profissao.Minerador:
                    return "Minerador";
                case Profissao.Curtidor:
                    return "Curtidor";
                case Profissao.Ferreiro:
                    return "Ferreiro";
                case Profissao.Alquimista:
                    return "Alquimista";
                case Profissao.Cozinheiro:
                    return "Cozinheiro";
                default:
                    return null;
            }
        }

        /// <summary>Distância horizontal ao quadrado entre dois pontos.</summary>
        public static float DistanciaAoQuadrado(float ax, float az, float bx, float bz)
        {
            var dx = ax - bx;
            var dz = az - bz;
            return dx * dx + dz * dz;
        }

        public static bool PertoDoPonto(PontoDeColeta ponto, float x, float z) =>
            DistanciaAoQuadrado(ponto.X, ponto.Z, x, z) <= AlcanceMetros * AlcanceMetros;

        public static bool PertoDaForja(float x, float z) =>
            DistanciaAoQuadrado(ForjaX, ForjaZ, x, z) <= AlcanceMetros * AlcanceMetros;
    }
}
