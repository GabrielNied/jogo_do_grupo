using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>IDs de mensagem de Mobs e bosses. Use somente valores entre 11000 e 11999.</summary>
    public static class MobsMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(11000, 11999);

        /// <summary>Servidor → quem entra na área: os inimigos vivos que estão nela.</summary>
        public const ushort InimigosDaArea = 11000;

        /// <summary>Servidor → quem está na área: posição, estado, alvo ou vida de um inimigo.</summary>
        public const ushort InimigoMudou = 11001;

        /// <summary>Servidor → quem está na área: um inimigo começou o aviso do golpe.</summary>
        public const ushort InimigoPreparaAtaque = 11002;

        /// <summary>Servidor → quem está na área: um inimigo morreu.</summary>
        public const ushort InimigoMorreu = 11003;

        /// <summary>Servidor → quem está na área: um inimigo apareceu (respawn).</summary>
        public const ushort InimigoApareceu = 11004;

        /// <summary>Servidor → quem está na área: um boss ficou exposto a mais dano.</summary>
        public const ushort InimigoExposto = 11005;

        /// <summary>Servidor → quem está na área: o golpe de um inimigo comum encostou em alguém.</summary>
        public const ushort InimigoAcertou = 11006;

        /// <summary>Servidor → jogador: inimigo saiu do raio de interesse (sumir no client).</summary>
        public const ushort InimigoSaiuDoInteresse = 11007;
    }
}
