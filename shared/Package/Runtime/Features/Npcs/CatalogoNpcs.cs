#nullable enable annotations
using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>
    /// Catálogo autoritativo de NPCs do jogo: posições, papéis e nomes canônicos (#1063).
    /// Fonte única de localização de NPCs no mundo.
    /// </summary>
    public static class CatalogoNpcs
    {
        public const string AreaCidade = "cidade";

        public const int IdVendedor = 1;
        public const int IdFerreiro = 2;
        public const int IdBanqueiro = 3;
        public const int IdArmeiro = 4;
        public const int IdArmadureiro = 5;
        public const int IdTaverneiro = 6;
        public const int IdMerceeiro = 7;
        public const int IdCavalarico = 8;
        public const int IdCuidadorDePets = 9;
        public const int IdLeiloeiro = 10;
        public const int IdValeria = 11;
        public const int IdGuardaNorte = 12;
        public const int IdQuadroDeDiarias = 13;
        public const int IdMoradorElric = 14;
        public const int IdMoradoraLyra = 15;

        public static readonly DefinicaoNpc Vendedor = new DefinicaoNpc(
            id: IdVendedor,
            nome: "Mercador Alden",
            papel: PapelNpc.Vendedor,
            area: AreaCidade,
            x: 12f,
            y: 0f,
            z: -8f,
            rotacaoY: 180f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Ferreiro = new DefinicaoNpc(
            id: IdFerreiro,
            nome: "Ferreiro Brom",
            papel: PapelNpc.Ferreiro,
            area: AreaCidade,
            x: -12f,
            y: 0f,
            z: -6f,
            rotacaoY: 90f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Banqueiro = new DefinicaoNpc(
            id: IdBanqueiro,
            nome: "Banqueiro Cassian",
            papel: PapelNpc.Banqueiro,
            area: AreaCidade,
            x: 0f,
            y: 0f,
            z: -14f,
            rotacaoY: 0f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Armeiro = new DefinicaoNpc(
            id: IdArmeiro,
            nome: "Armeiro Gareth",
            papel: PapelNpc.Armeiro,
            area: AreaCidade,
            x: 10f,
            y: 0f,
            z: -4f,
            rotacaoY: 180f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Armadureiro = new DefinicaoNpc(
            id: IdArmadureiro,
            nome: "Armadureira Bruna",
            papel: PapelNpc.Armadureiro,
            area: AreaCidade,
            x: 14f,
            y: 0f,
            z: -4f,
            rotacaoY: 180f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Taverneiro = new DefinicaoNpc(
            id: IdTaverneiro,
            nome: "Taverneiro Barnabé",
            papel: PapelNpc.Taverneiro,
            area: AreaCidade,
            x: -6f,
            y: 0f,
            z: -12f,
            rotacaoY: 90f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Merceeiro = new DefinicaoNpc(
            id: IdMerceeiro,
            nome: "Merceeiro Tomas",
            papel: PapelNpc.Merceeiro,
            area: AreaCidade,
            x: 16f,
            y: 0f,
            z: -10f,
            rotacaoY: 180f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Cavalarico = new DefinicaoNpc(
            id: IdCavalarico,
            nome: "Cavalariço Roderic",
            papel: PapelNpc.Cavalarico,
            area: AreaCidade,
            x: -14f,
            y: 0f,
            z: 8f,
            rotacaoY: 90f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc CuidadorDePets = new DefinicaoNpc(
            id: IdCuidadorDePets,
            nome: "Cuidadora Mirna",
            papel: PapelNpc.CuidadorDePets,
            area: AreaCidade,
            x: -14f,
            y: 0f,
            z: 14f,
            rotacaoY: 90f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Leiloeiro = new DefinicaoNpc(
            id: IdLeiloeiro,
            nome: "Leiloeiro Dumas",
            papel: PapelNpc.Leiloeiro,
            area: AreaCidade,
            x: 4f,
            y: 0f,
            z: -14f,
            rotacaoY: 0f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc Valeria = new DefinicaoNpc(
            id: IdValeria,
            nome: "Capitã Valéria",
            papel: PapelNpc.Guarda,
            area: AreaCidade,
            x: 0f,
            y: 0f,
            z: -32f,
            rotacaoY: 0f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc GuardaNorte = new DefinicaoNpc(
            id: IdGuardaNorte,
            nome: "Guarda Kael",
            papel: PapelNpc.Guarda,
            area: AreaCidade,
            x: -2f,
            y: 0f,
            z: 32f,
            rotacaoY: 180f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc QuadroDeDiarias = new DefinicaoNpc(
            id: IdQuadroDeDiarias,
            nome: "Quadro de Diárias",
            papel: PapelNpc.DadorDeMissao,
            area: AreaCidade,
            x: 4f,
            y: 0f,
            z: 4f,
            rotacaoY: 225f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc MoradorElric = new DefinicaoNpc(
            id: IdMoradorElric,
            nome: "Morador Elric",
            papel: PapelNpc.Morador,
            area: AreaCidade,
            x: 16f,
            y: 0f,
            z: 12f,
            rotacaoY: 270f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc MoradoraLyra = new DefinicaoNpc(
            id: IdMoradoraLyra,
            nome: "Moradora Lyra",
            papel: PapelNpc.Morador,
            area: AreaCidade,
            x: -10f,
            y: 0f,
            z: -14f,
            rotacaoY: 90f,
            raioInteracao: 4f
        );

        public static readonly DefinicaoNpc[] Todos =
        {
            Vendedor,
            Ferreiro,
            Banqueiro,
            Armeiro,
            Armadureiro,
            Taverneiro,
            Merceeiro,
            Cavalarico,
            CuidadorDePets,
            Leiloeiro,
            Valeria,
            GuardaNorte,
            QuadroDeDiarias,
            MoradorElric,
            MoradoraLyra,
        };

        private static readonly Dictionary<int, DefinicaoNpc> _porId = new Dictionary<
            int,
            DefinicaoNpc
        >
        {
            { IdVendedor, Vendedor },
            { IdFerreiro, Ferreiro },
            { IdBanqueiro, Banqueiro },
            { IdArmeiro, Armeiro },
            { IdArmadureiro, Armadureiro },
            { IdTaverneiro, Taverneiro },
            { IdMerceeiro, Merceeiro },
            { IdCavalarico, Cavalarico },
            { IdCuidadorDePets, CuidadorDePets },
            { IdLeiloeiro, Leiloeiro },
            { IdValeria, Valeria },
            { IdGuardaNorte, GuardaNorte },
            { IdQuadroDeDiarias, QuadroDeDiarias },
            { IdMoradorElric, MoradorElric },
            { IdMoradoraLyra, MoradoraLyra },
        };

        public static DefinicaoNpc Obter(int id)
        {
            _porId.TryGetValue(id, out var def);
            return def;
        }

        public static DefinicaoNpc Buscar(int id) => Obter(id);

        public static DefinicaoNpc[] PorArea(string area)
        {
            if (string.IsNullOrEmpty(area))
            {
                return Array.Empty<DefinicaoNpc>();
            }

            var resultado = new List<DefinicaoNpc>();
            for (var i = 0; i < Todos.Length; i++)
            {
                if (string.Equals(Todos[i].Area, area, StringComparison.OrdinalIgnoreCase))
                {
                    resultado.Add(Todos[i]);
                }
            }

            return resultado.ToArray();
        }

        public static DefinicaoNpc? PorPapel(PapelNpc papel, string area = AreaCidade)
        {
            for (var i = 0; i < Todos.Length; i++)
            {
                if (
                    Todos[i].Papel == papel
                    && string.Equals(Todos[i].Area, area, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return Todos[i];
                }
            }

            return null;
        }

        public static bool DentroDoAlcance(int id, float x, float z)
        {
            var npc = Obter(id);
            return npc != null && npc.Alcanca(x, z);
        }
    }
}
