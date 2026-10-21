using System;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Dados de um perfil de build de talentos.</summary>
    [System.Serializable]
    public sealed class PerfilTalentosDto
    {
        public long Id;

        public string Nome = "";

        public int VersaoConfiguracao;

        /// <summary>Data e hora da criação no formato ISO 8601 UTC.</summary>
        public string CriadoEm = "";

        public RankDoNo[] Ranks = Array.Empty<RankDoNo>();

        /// <summary>Total de pontos necessários para este perfil na versão em que foi salvo.</summary>
        public int PontosTotais;
    }
}
