using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Os outros jogadores presentes na cidade, por Id de personagem. Só guarda estado; quem
    /// desenha reage a <see cref="Criado"/> e <see cref="Removido"/>.
    /// </summary>
    public sealed class RegistroDeAvatares
    {
        private readonly Dictionary<long, AvatarRemoto> _avatares =
            new Dictionary<long, AvatarRemoto>();

        public RegistroDeAvatares(long meuPersonagemId)
        {
            MeuPersonagemId = meuPersonagemId;
        }

        public long MeuPersonagemId { get; }
        public int Quantidade => _avatares.Count;
        public IEnumerable<AvatarRemoto> Todos => _avatares.Values;

        public event Action<AvatarRemoto> Criado;
        public event Action<AvatarRemoto> Removido;

        public bool TentarObter(long personagemId, out AvatarRemoto avatar) =>
            _avatares.TryGetValue(personagemId, out avatar);

        /// <summary>Troca todos os avatares pelos de um novo snapshot (entrada ou reentrada).</summary>
        public void Aplicar(EstadoDaCidade estado)
        {
            var presentes = new HashSet<long>();
            if (estado?.Outros != null)
            {
                foreach (var jogador in estado.Outros)
                {
                    if (jogador != null && jogador.PersonagemId != MeuPersonagemId)
                    {
                        presentes.Add(jogador.PersonagemId);
                    }
                }
            }

            foreach (var id in new List<long>(_avatares.Keys))
            {
                if (!presentes.Contains(id))
                {
                    Remover(id);
                }
            }

            if (estado?.Outros == null)
            {
                return;
            }

            foreach (var jogador in estado.Outros)
            {
                if (jogador != null && jogador.PersonagemId != MeuPersonagemId)
                {
                    Entrou(jogador);
                }
            }
        }

        /// <summary>Cria o avatar (ou reposiciona, se já existir). Ignora o próprio jogador.</summary>
        public void Entrou(JogadorVisivel jogador)
        {
            if (jogador == null || jogador.PersonagemId == MeuPersonagemId)
            {
                return;
            }

            var posicao = new Vector3(jogador.X, jogador.Y, jogador.Z);
            if (_avatares.TryGetValue(jogador.PersonagemId, out var existente))
            {
                existente.Teleportar(posicao, jogador.RotacaoY);
                return;
            }

            var avatar = new AvatarRemoto(jogador.PersonagemId, posicao, jogador.RotacaoY);
            _avatares.Add(jogador.PersonagemId, avatar);
            Criado?.Invoke(avatar);
        }

        public void Saiu(long personagemId) => Remover(personagemId);

        /// <summary>Nova posição do servidor para um jogador; cria o avatar se ainda não existir.</summary>
        public void Mover(PosicaoJogador posicao)
        {
            if (posicao == null || posicao.PersonagemId == MeuPersonagemId)
            {
                return;
            }

            var alvo = new Vector3(posicao.X, posicao.Y, posicao.Z);
            if (_avatares.TryGetValue(posicao.PersonagemId, out var avatar))
            {
                avatar.DefinirAlvo(alvo, posicao.RotacaoY);
                return;
            }

            var novo = new AvatarRemoto(posicao.PersonagemId, alvo, posicao.RotacaoY);
            _avatares.Add(posicao.PersonagemId, novo);
            Criado?.Invoke(novo);
        }

        /// <summary>Remove todos (queda da conexão ou saída do mundo).</summary>
        public void Limpar()
        {
            foreach (var id in new List<long>(_avatares.Keys))
            {
                Remover(id);
            }
        }

        public void Atualizar(float delta)
        {
            foreach (var avatar in _avatares.Values)
            {
                avatar.Atualizar(delta);
            }
        }

        private void Remover(long personagemId)
        {
            if (_avatares.TryGetValue(personagemId, out var avatar))
            {
                _avatares.Remove(personagemId);
                Removido?.Invoke(avatar);
            }
        }
    }
}
