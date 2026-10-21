using System;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Servidor → Membros do grupo: lista completa e atual dos alvos marcados taticamente pelo grupo.
    /// </summary>
    [System.Serializable]
    public sealed class MarcadoresDeAlvoMudaram
    {
        public MarcadorAlvoDto[] Marcadores = Array.Empty<MarcadorAlvoDto>();
    }
}
