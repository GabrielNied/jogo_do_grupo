using System;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Client → servidor: pede os detalhes de equipamento e build de outro personagem.
    /// Id: <see cref="SocialMessageIds.PedirInspecao"/>.
    /// </summary>
    [Serializable]
    public class PedirInspecao
    {
        public long PersonagemId;
        public string Nome = "";
    }
}
