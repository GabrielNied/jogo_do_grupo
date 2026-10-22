using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>Um jogador que pode ser chamado para duelo: quem é e onde está no plano do chão.</summary>
    public readonly struct CandidatoDeDuelo
    {
        public CandidatoDeDuelo(long personagemId, string nome, float x, float z)
        {
            PersonagemId = personagemId;
            Nome = nome ?? "";
            X = x;
            Z = z;
        }

        public long PersonagemId { get; }
        public string Nome { get; }
        public float X { get; }
        public float Z { get; }
    }

    /// <summary>
    /// De onde vêm os jogadores próximos. PvP não referencia Mundo nem Social (regra 2): o
    /// <see cref="ObservadorDePvp"/> registra <see cref="DosAvataresNoJogo"/>, que lê os avatares
    /// que o Mundo registra no Core (<see cref="AvataresNoJogo"/>). Sem fonte, não há alvo.
    /// </summary>
    public static class FonteDeAlvosDeDuelo
    {
        private static readonly List<CandidatoDeDuelo> _doRegistro = new List<CandidatoDeDuelo>();

        public static Func<IReadOnlyList<CandidatoDeDuelo>> Candidatos { get; set; }

        public static IReadOnlyList<CandidatoDeDuelo> Listar() =>
            Candidatos?.Invoke() ?? Array.Empty<CandidatoDeDuelo>();

        /// <summary>Os jogadores do <see cref="AvataresNoJogo"/> (o local sai na escolha, pelo id).</summary>
        public static IReadOnlyList<CandidatoDeDuelo> DosAvataresNoJogo()
        {
            _doRegistro.Clear();
            foreach (var avatar in AvataresNoJogo.Todos)
            {
                if (avatar.Raiz == null)
                {
                    continue;
                }

                var posicao = avatar.Raiz.position;
                _doRegistro.Add(
                    new CandidatoDeDuelo(avatar.PersonagemId, avatar.Nome, posicao.x, posicao.z)
                );
            }

            return _doRegistro;
        }
    }

    /// <summary>Regra pura da escolha do alvo do duelo: o mais perto dentro do alcance.</summary>
    public static class EscolhaDoAlvoDeDuelo
    {
        /// <summary>
        /// O candidato mais perto de (<paramref name="x"/>, <paramref name="z"/>) a até
        /// <paramref name="alcanceMetros"/>, sem contar o próprio jogador. Empate fica com o
        /// de menor id, para a escolha não oscilar. Devolve false se não houver ninguém.
        /// </summary>
        public static bool TentarEscolher(
            float x,
            float z,
            long meuId,
            IReadOnlyList<CandidatoDeDuelo> candidatos,
            float alcanceMetros,
            out CandidatoDeDuelo escolhido
        )
        {
            escolhido = default;
            if (candidatos == null)
            {
                return false;
            }

            var achou = false;
            var melhor = float.MaxValue;
            var limite = alcanceMetros * alcanceMetros;
            foreach (var candidato in candidatos)
            {
                if (candidato.PersonagemId == meuId || candidato.PersonagemId <= 0)
                {
                    continue;
                }

                var dx = candidato.X - x;
                var dz = candidato.Z - z;
                var distancia = dx * dx + dz * dz;
                if (distancia > limite)
                {
                    continue;
                }

                if (
                    !achou
                    || distancia < melhor
                    || (distancia == melhor && candidato.PersonagemId < escolhido.PersonagemId)
                )
                {
                    achou = true;
                    melhor = distancia;
                    escolhido = candidato;
                }
            }

            return achou;
        }
    }
}
