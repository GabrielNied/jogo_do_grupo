using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Lembra o último contato de combate com inimigos (acertar ou levar golpe) para a barra do
    /// topo. Não decide dano nem alvo de ataque.
    /// </summary>
    internal sealed class RastreadorDeAlvoDoHud
    {
        private readonly Dictionary<long, float> _contatos = new Dictionary<long, float>();

        public void RegistrarAcertoDoJogador(AtaqueAcertou mensagem, long personagemId)
        {
            if (mensagem?.Acertos == null || mensagem.AtacanteId != personagemId)
            {
                return;
            }

            var agora = Time.time;
            foreach (var acerto in mensagem.Acertos)
            {
                if (acerto != null)
                {
                    _contatos[acerto.AlvoId] = agora;
                }
            }
        }

        public void RegistrarGolpeDoInimigo(InimigoAcertou mensagem, long personagemId)
        {
            if (mensagem == null || mensagem.AlvoId != personagemId)
            {
                return;
            }

            _contatos[mensagem.InimigoId] = Time.time;
        }

        public void Limpar() => _contatos.Clear();

        public double SegundosDesdeContato(long inimigoId)
        {
            if (!_contatos.TryGetValue(inimigoId, out var instante))
            {
                return -1.0;
            }

            return Time.time - instante;
        }

        public long? UltimoContatoComum(IReadOnlyCollection<InimigoNoClient> inimigos)
        {
            return UltimoContato(inimigos, chefe: false);
        }

        public long? UltimoContatoComBoss(IReadOnlyCollection<InimigoNoClient> inimigos)
        {
            return UltimoContato(inimigos, chefe: true);
        }

        private long? UltimoContato(IReadOnlyCollection<InimigoNoClient> inimigos, bool chefe)
        {
            long? escolhido = null;
            var instante = float.MinValue;
            foreach (var inimigo in inimigos)
            {
                if (inimigo == null || inimigo.Vida <= 0)
                {
                    continue;
                }

                var ehChefe = RegrasAlvoDoHud.EhChefe(inimigo.CriaturaId);
                if (ehChefe != chefe)
                {
                    continue;
                }

                if (!_contatos.TryGetValue(inimigo.InimigoId, out var quando))
                {
                    continue;
                }

                if (quando > instante)
                {
                    instante = quando;
                    escolhido = inimigo.InimigoId;
                }
            }

            return escolhido;
        }
    }
}
