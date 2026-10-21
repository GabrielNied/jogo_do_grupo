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
        public readonly string Nome;
        public readonly int DuracaoBuffMinutos;

        public ReceitaDeProducao(
            int id,
            Profissao profissao,
            int modeloId,
            int[] materiaisIds,
            int[] materiaisQuantidades
        )
            : this(id, profissao, modeloId, materiaisIds, materiaisQuantidades, null, 0) { }

        public ReceitaDeProducao(
            int id,
            Profissao profissao,
            int modeloId,
            int[] materiaisIds,
            int[] materiaisQuantidades,
            string nome,
            int duracaoBuffMinutos
        )
        {
            Id = id;
            Profissao = profissao;
            ModeloId = modeloId;
            MateriaisIds = materiaisIds;
            MateriaisQuantidades = materiaisQuantidades;
            Nome = nome;
            DuracaoBuffMinutos = duracaoBuffMinutos;
        }
    }

    /// <summary>
    /// Conteúdo e valores das seis profissões (#192, #335): coleta (Lenhador, Minerador, Curtidor)
    /// e produção (Ferreiro, Curtidor, Alquimista, Cozinheiro). Os números daqui são os do servidor;
    /// o client só os usa para desenhar e para a dica de proximidade.
    /// </summary>
    public static class ConfigProfissoes
    {
        /// <summary>A cidade e o campo em volta (qualquer canal) é onde tudo acontece.</summary>
        public const string PrefixoDaArea = "cidade";

        public const int MaterialMadeira = 1;
        public const int MaterialMinerioDeFerro = 2;
        public const int MaterialCouroCru = 3;
        public const int MaterialErvaMedicinal = 4;
        public const int MaterialCristalArcano = 5;
        public const int MaterialGraos = 6;
        public const int MaterialCarneDeCaca = 7;

        public const int NoArvore1 = 1;
        public const int NoArvore2 = 2;
        public const int NoVeioDeFerro1 = 3;
        public const int NoVeioDeFerro2 = 4;
        public const int NoCarcaca1 = 5;
        public const int NoCarcaca2 = 6;
        public const int NoArbustoErvas1 = 7;
        public const int NoArbustoErvas2 = 8;
        public const int NoGeodoCristal1 = 9;
        public const int NoGeodoCristal2 = 10;
        public const int NoPlantacaoGraos1 = 11;
        public const int NoPlantacaoGraos2 = 12;
        public const int NoCaca1 = 13;
        public const int NoCaca2 = 14;

        // Ferreiro
        public const int ReceitaEspada = 1;
        public const int ReceitaEscudo = 2;
        public const int ReceitaPeitoral = 3;
        public const int ReceitaMartelo = 4;

        // Curtidor
        public const int ReceitaColete = 5;
        public const int ReceitaLuvas = 6;
        public const int ReceitaBotas = 7;
        public const int ReceitaCapuz = 8;

        // Alquimista
        public const int ReceitaFocoArcano = 9;
        public const int ReceitaAnelArcano = 10;
        public const int ReceitaBrincoArcano = 11;
        public const int ReceitaMantoArcano = 12;

        // Cozinheiro
        public const int ReceitaCaldoDosErrantes = 13;
        public const int ReceitaAssadoDeGalhada = 14;
        public const int ReceitaEnsopadoDaFornalha = 15;

        /// <summary>Segundos até um ponto coletado voltar para todos.</summary>
        public const int RespawnSegundos = 60;

        /// <summary>Distância horizontal máxima aceita pelo servidor, em metros.</summary>
        public const float AlcanceMetros = 4f;

        /// <summary>Distância em que o client mostra a dica (menor que a do servidor).</summary>
        public const float AlcanceDaDicaMetros = 3f;

        /// <summary>A bancada da forja: rua oeste, a alguns passos do ferreiro da cidade.</summary>
        public const float ForjaX = -14f;
        public const float ForjaZ = -11f;

        /// <summary>Bancada do curtume: rua oeste, próximo à forja.</summary>
        public const float CurtumeX = -14f;
        public const float CurtumeZ = -8f;

        /// <summary>O caldeirão de alquimia: rua oeste da cidade.</summary>
        public const float CaldeiraoX = -12f;
        public const float CaldeiraoZ = -8f;

        /// <summary>A cozinha da guilda/cidade: rua oeste, polo de produção.</summary>
        public const float CozinhaX = -10f;
        public const float CozinhaZ = -8f;

        /// <summary>
        /// Todas as seis profissões que o personagem pode aprender, sem limite por personagem (#335).
        /// </summary>
        public static readonly Profissao[] Disponiveis =
        {
            Profissao.Lenhador,
            Profissao.Minerador,
            Profissao.Curtidor,
            Profissao.Ferreiro,
            Profissao.Alquimista,
            Profissao.Cozinheiro,
        };

        public static readonly MaterialDeProfissao[] Materiais =
        {
            new MaterialDeProfissao(MaterialMadeira, "Madeira"),
            new MaterialDeProfissao(MaterialMinerioDeFerro, "Minério de ferro"),
            new MaterialDeProfissao(MaterialCouroCru, "Couro cru"),
            new MaterialDeProfissao(MaterialErvaMedicinal, "Erva medicinal"),
            new MaterialDeProfissao(MaterialCristalArcano, "Cristal arcano"),
            new MaterialDeProfissao(MaterialGraos, "Grãos"),
            new MaterialDeProfissao(MaterialCarneDeCaca, "Carne de caça"),
        };

        // Pontos de coleta no campo norte ao portão da cidade.
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
            new PontoDeColeta(
                NoArbustoErvas1,
                "Arbusto medicinal",
                Profissao.Lenhador,
                MaterialErvaMedicinal,
                2,
                -20f,
                62f
            ),
            new PontoDeColeta(
                NoArbustoErvas2,
                "Arbusto medicinal",
                Profissao.Lenhador,
                MaterialErvaMedicinal,
                2,
                -14f,
                70f
            ),
            new PontoDeColeta(
                NoGeodoCristal1,
                "Geodo arcano",
                Profissao.Minerador,
                MaterialCristalArcano,
                2,
                22f,
                62f
            ),
            new PontoDeColeta(
                NoGeodoCristal2,
                "Geodo arcano",
                Profissao.Minerador,
                MaterialCristalArcano,
                2,
                30f,
                72f
            ),
            new PontoDeColeta(
                NoPlantacaoGraos1,
                "Plantação de grãos",
                Profissao.Lenhador,
                MaterialGraos,
                2,
                -10f,
                54f
            ),
            new PontoDeColeta(
                NoPlantacaoGraos2,
                "Plantação de grãos",
                Profissao.Lenhador,
                MaterialGraos,
                2,
                -4f,
                64f
            ),
            new PontoDeColeta(
                NoCaca1,
                "Rastro de caça",
                Profissao.Curtidor,
                MaterialCarneDeCaca,
                2,
                12f,
                80f
            ),
            new PontoDeColeta(
                NoCaca2,
                "Rastro de caça",
                Profissao.Curtidor,
                MaterialCarneDeCaca,
                2,
                4f,
                82f
            ),
        };

        // Receitas de produção de equipamentos e preparos do catálogo (#335).
        public static readonly ReceitaDeProducao[] Receitas =
        {
            // Ferreiro (Armas e armaduras pesadas de ferro)
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
            new ReceitaDeProducao(
                ReceitaMartelo,
                Profissao.Ferreiro,
                100,
                new[] { MaterialMinerioDeFerro, MaterialMadeira },
                new[] { 5, 2 }
            ),
            // Curtidor (Armaduras de couro e agilidade)
            new ReceitaDeProducao(
                ReceitaColete,
                Profissao.Curtidor,
                111,
                new[] { MaterialCouroCru, MaterialMadeira },
                new[] { 4, 2 }
            ),
            new ReceitaDeProducao(
                ReceitaLuvas,
                Profissao.Curtidor,
                112,
                new[] { MaterialCouroCru, MaterialMinerioDeFerro },
                new[] { 3, 1 }
            ),
            new ReceitaDeProducao(
                ReceitaBotas,
                Profissao.Curtidor,
                114,
                new[] { MaterialCouroCru, MaterialMinerioDeFerro },
                new[] { 3, 2 }
            ),
            new ReceitaDeProducao(
                ReceitaCapuz,
                Profissao.Curtidor,
                110,
                new[] { MaterialCouroCru, MaterialCristalArcano },
                new[] { 3, 1 }
            ),
            // Alquimista (Focos arcanos, joalheria mágica e mantos)
            new ReceitaDeProducao(
                ReceitaFocoArcano,
                Profissao.Alquimista,
                177,
                new[] { MaterialCristalArcano, MaterialMadeira },
                new[] { 3, 2 }
            ),
            new ReceitaDeProducao(
                ReceitaAnelArcano,
                Profissao.Alquimista,
                125,
                new[] { MaterialCristalArcano, MaterialMinerioDeFerro },
                new[] { 2, 2 }
            ),
            new ReceitaDeProducao(
                ReceitaBrincoArcano,
                Profissao.Alquimista,
                126,
                new[] { MaterialCristalArcano, MaterialErvaMedicinal },
                new[] { 2, 1 }
            ),
            new ReceitaDeProducao(
                ReceitaMantoArcano,
                Profissao.Alquimista,
                120,
                new[] { MaterialErvaMedicinal, MaterialCristalArcano, MaterialCouroCru },
                new[] { 4, 2, 2 }
            ),
            // Cozinheiro (Refeições de expedição com buffs de 30 a 60 min, regra de uma comida ativa)
            new ReceitaDeProducao(
                ReceitaCaldoDosErrantes,
                Profissao.Cozinheiro,
                109,
                new[] { MaterialGraos, MaterialCarneDeCaca, MaterialErvaMedicinal },
                new[] { 2, 2, 1 },
                "Caldo dos Errantes",
                30
            ),
            new ReceitaDeProducao(
                ReceitaAssadoDeGalhada,
                Profissao.Cozinheiro,
                118,
                new[] { MaterialCarneDeCaca, MaterialErvaMedicinal },
                new[] { 3, 1 },
                "Assado de Galhada",
                45
            ),
            new ReceitaDeProducao(
                ReceitaEnsopadoDaFornalha,
                Profissao.Cozinheiro,
                136,
                new[] { MaterialGraos, MaterialCarneDeCaca, MaterialMadeira },
                new[] { 3, 2, 1 },
                "Ensopado da Fornalha",
                60
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

        public static bool PertoDaEstacao(Profissao profissao, float x, float z)
        {
            switch (profissao)
            {
                case Profissao.Ferreiro:
                    return PertoDaForja(x, z);
                case Profissao.Curtidor:
                    return PertoDoCurtume(x, z) || PertoDaForja(x, z);
                case Profissao.Alquimista:
                    return PertoDoCaldeirao(x, z);
                case Profissao.Cozinheiro:
                    return PertoDaCozinha(x, z);
                default:
                    return false;
            }
        }

        public static bool PertoDaForja(float x, float z) =>
            DistanciaAoQuadrado(ForjaX, ForjaZ, x, z) <= AlcanceMetros * AlcanceMetros;

        public static bool PertoDoCurtume(float x, float z) =>
            DistanciaAoQuadrado(CurtumeX, CurtumeZ, x, z) <= AlcanceMetros * AlcanceMetros;

        public static bool PertoDoCaldeirao(float x, float z) =>
            DistanciaAoQuadrado(CaldeiraoX, CaldeiraoZ, x, z) <= AlcanceMetros * AlcanceMetros;

        public static bool PertoDaCozinha(float x, float z) =>
            DistanciaAoQuadrado(CozinhaX, CozinhaZ, x, z) <= AlcanceMetros * AlcanceMetros;
    }
}
