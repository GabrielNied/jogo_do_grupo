using System;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Dados de um perfil de build de talentos.</summary>
    public sealed class PerfilTalentosDto
    {
        public long Id { get; set; }

        public string Nome { get; set; } = "";

        public int VersaoConfiguracao { get; set; }

        /// <summary>Data e hora da criação no formato ISO 8601 UTC.</summary>
        public string CriadoEm { get; set; } = "";

        public RankDoNo[] Ranks { get; set; } = Array.Empty<RankDoNo>();

        /// <summary>Total de pontos necessários para este perfil na versão em que foi salvo.</summary>
        public int PontosTotais { get; set; }
    }
}
