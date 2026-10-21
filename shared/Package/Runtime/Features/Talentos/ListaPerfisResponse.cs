using System;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Resposta da listagem de perfis de build de um personagem.</summary>
    [System.Serializable]
    public sealed class ListaPerfisResponse
    {
        public PerfilTalentosDto[] Perfis = Array.Empty<PerfilTalentosDto>();
    }
}
