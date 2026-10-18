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

        public static readonly DefinicaoNpc[] Todos = { Vendedor, Ferreiro, Banqueiro };

        private static readonly Dictionary<int, DefinicaoNpc> _porId = new Dictionary<
            int,
            DefinicaoNpc
        >
        {
            { IdVendedor, Vendedor },
            { IdFerreiro, Ferreiro },
            { IdBanqueiro, Banqueiro },
        };

        public static DefinicaoNpc Obter(int id)
        {
            _porId.TryGetValue(id, out var def);
            return def;
        }

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

        public static DefinicaoNpc PorPapel(PapelNpc papel, string area = AreaCidade)
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
