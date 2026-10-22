using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>Um jogador na cena: o Id, a raiz do avatar e o nome (vazio se ainda não se sabe).</summary>
    public readonly struct AvatarNoJogo
    {
        public AvatarNoJogo(long personagemId, Transform raiz, string nome)
        {
            PersonagemId = personagemId;
            Raiz = raiz;
            Nome = nome ?? "";
        }

        public long PersonagemId { get; }
        public Transform Raiz { get; }
        public string Nome { get; }
    }

    /// <summary>
    /// Os avatares dos jogadores na cena, por Id, para as funcionalidades acharem um jogador sem
    /// varrer a cena por nome e sem referenciar o Mundo (regra 2). O Mundo registra os outros
    /// jogadores ao criar o avatar e remove ao destruir; o Core registra o jogador local. Limpa
    /// ao sair do jogo.
    /// </summary>
    public static class AvataresNoJogo
    {
        private static readonly Dictionary<long, AvatarNoJogo> _avatares =
            new Dictionary<long, AvatarNoJogo>();

        /// <summary>
        /// Os avatares registrados, inclusive o jogador local. Uma raiz já destruída aparece
        /// como nula até sair do registro.
        /// </summary>
        public static IReadOnlyCollection<AvatarNoJogo> Todos => _avatares.Values;

        /// <summary>Registra (ou troca) o avatar do personagem. Id não positivo ou raiz nula é ignorado.</summary>
        public static void Registrar(long personagemId, Transform raiz, string nome = "")
        {
            if (personagemId <= 0 || raiz == null)
            {
                return;
            }

            _avatares[personagemId] = new AvatarNoJogo(personagemId, raiz, nome);
        }

        /// <summary>Tira o avatar do personagem (no-op se não estava).</summary>
        public static void Remover(long personagemId) => _avatares.Remove(personagemId);

        /// <summary>
        /// A raiz do avatar do personagem, se registrada e ainda viva. Um avatar destruído sem
        /// <see cref="Remover"/> sai do registro aqui.
        /// </summary>
        public static bool TentarObter(long personagemId, out Transform raiz)
        {
            raiz = null;
            if (!_avatares.TryGetValue(personagemId, out var avatar))
            {
                return false;
            }

            if (avatar.Raiz == null)
            {
                _avatares.Remove(personagemId);
                return false;
            }

            raiz = avatar.Raiz;
            return true;
        }

        /// <summary>O GameObject do avatar do personagem, ou <c>null</c>.</summary>
        public static GameObject Obter(long personagemId) =>
            TentarObter(personagemId, out var raiz) ? raiz.gameObject : null;

        /// <summary>Esvazia o registro (o Core chama ao sair do jogo).</summary>
        public static void Limpar() => _avatares.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Limpar();
    }
}
