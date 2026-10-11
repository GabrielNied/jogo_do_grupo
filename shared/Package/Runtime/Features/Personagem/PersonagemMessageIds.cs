using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>IDs de mensagem de Personagem. Use somente valores entre 3000 e 3999.</summary>
    public static class PersonagemMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(3000, 3999);

        /// <summary>Servidor → dono: o personagem ganhou experiência (e talvez subiu de nível).</summary>
        public const ushort ExperienciaGanha = 3000;

        /// <summary>Servidor → quem está na área: um personagem subiu de nível.</summary>
        public const ushort SubiuDeNivel = 3001;
    }
}
