using System;

namespace JogoDoGrupo.Shared.Features.Mundo
{
    /// <summary>Rótulos de área para mapa e lista de amigos (cidade, campo, dungeon).</summary>
    public static class RotulosDeArea
    {
        public const string Cidade = "Cidade";
        public const string Campo = "Campo";
        public const string Dungeon = "Dungeon";

        /// <summary>Identificador da área da cidade no servidor.</summary>
        public const string IdCidade = "cidade";

        /// <summary>Prefixo de instância (<c>instancia:&lt;id&gt;</c>).</summary>
        public const string PrefixoInstancia = "instancia:";

        /// <summary>Texto curto para HUD e mapa.</summary>
        public static string Descricao(string area, float x, float z)
        {
            if (string.IsNullOrEmpty(area))
            {
                return "";
            }

            if (area.StartsWith(PrefixoInstancia, StringComparison.Ordinal))
            {
                return Dungeon;
            }

            if (area == IdCidade)
            {
                var dentro =
                    Math.Abs(x) <= MapaDoMundo.MetadeDaCidade
                    && Math.Abs(z) <= MapaDoMundo.MetadeDaCidade;
                return dentro ? Cidade : Campo;
            }

            return area;
        }
    }
}
