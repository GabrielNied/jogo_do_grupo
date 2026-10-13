using System;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Toca golpe e esquiva dos outros jogadores no avatar deles (#481): vira o corpo para a
    /// <c>DirecaoY</c> do servidor e dispara o Trigger do Animator, com os mesmos animadores do
    /// próprio jogador. O próprio jogador nunca passa por aqui (já animou ao apertar, ADR 0024).
    /// O avatar vem de <see cref="Buscar"/>: a convenção de nome do Mundo (<c>Jogador_&lt;id&gt;</c>),
    /// sem referência à funcionalidade Mundo; trocável quando o Mundo publicar um contrato.
    /// </summary>
    internal static class AnimacaoDeOutros
    {
        /// <summary>Prefixo do nome do objeto que o Mundo cria para cada outro jogador.</summary>
        public const string NomeDoAvatar = "Jogador_";

        /// <summary>Volume do som do golpe e da esquiva de quem não é você (baixo, para não cobrir os seus).</summary>
        public const float VolumeDeOutros = 0.2f;

        /// <summary>Como achar o avatar de um personagem; os testes trocam por um falso.</summary>
        public static Func<long, GameObject> Buscar { get; set; } = BuscarPorNome;

        public static void RestaurarBusca() => Buscar = BuscarPorNome;

        private static GameObject BuscarPorNome(long personagemId) =>
            personagemId > 0 ? GameObject.Find(NomeDoAvatar + personagemId) : null;

        /// <summary>Se o golpe é de outro jogador (e portanto é aqui que ele se anima).</summary>
        public static bool EhDeOutro(long autorId, long meuPersonagemId) =>
            meuPersonagemId != 0 && autorId > 0 && autorId != meuPersonagemId;

        /// <summary><c>true</c> se o golpe tocou no Animator do avatar.</summary>
        public static bool TocarGolpe(AtaqueIniciado ataque, long meuPersonagemId)
        {
            if (!EhDeOutro(ataque.AtacanteId, meuPersonagemId))
            {
                return false;
            }

            var avatar = Buscar(ataque.AtacanteId);
            if (avatar == null)
            {
                return false;
            }

            Virar(avatar, ataque.DirecaoY);
            return AnimadorDeAtaque.Tocar(
                avatar.GetComponentInChildren<Animator>(),
                ataque.Tipo,
                ataque.Passo
            );
        }

        /// <summary><c>true</c> se a esquiva tocou no Animator do avatar.</summary>
        public static bool TocarEsquiva(EsquivaIniciada esquiva, long meuPersonagemId)
        {
            if (!EhDeOutro(esquiva.PersonagemId, meuPersonagemId))
            {
                return false;
            }

            var avatar = Buscar(esquiva.PersonagemId);
            if (avatar == null)
            {
                return false;
            }

            Virar(avatar, esquiva.DirecaoY);
            return AnimadorDeEsquiva.Tocar(avatar.GetComponentInChildren<Animator>());
        }

        private static void Virar(GameObject avatar, float direcaoY) =>
            avatar.transform.rotation = Quaternion.Euler(0f, direcaoY, 0f);
    }
}
