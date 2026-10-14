using System;

namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>Resposta da listagem de perfis de build de um personagem.</summary>
    public sealed class ListaPerfisResponse
    {
        public PerfilTalentosDto[] Perfis { get; set; } = Array.Empty<PerfilTalentosDto>();
    }
}
