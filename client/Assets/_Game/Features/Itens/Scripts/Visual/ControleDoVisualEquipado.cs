using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Visual
{
    /// <summary>
    /// Mantém as armas nos avatares: para cada personagem com visual recebido, acha o avatar (o
    /// jogador local, ou o que o Mundo registrou em <see cref="AvataresNoJogo"/>) e aplica o estado quando
    /// ele é novo ou quando o avatar é outro objeto (entrou de novo na área). Confere poucas vezes
    /// por segundo: não precisa de evento do Mundo, que não conheço.
    /// </summary>
    public sealed class ControleDoVisualEquipado : MonoBehaviour
    {
        /// <summary>Segundos entre uma conferência e outra.</summary>
        public const float Intervalo = 0.25f;

        /// <summary>Como achar o avatar de um personagem; os testes trocam por um falso.</summary>
        public static Func<long, GameObject> Buscar { get; set; } = BuscarNoRegistro;

        private readonly Dictionary<long, (GameObject Avatar, int Versao)> _aplicados =
            new Dictionary<long, (GameObject, int)>();

        private float _espera;

        private static GameObject BuscarNoRegistro(long personagemId)
        {
            if (personagemId == EntradaNoJogo.PersonagemId && EntradaNoJogo.Jogador != null)
            {
                return EntradaNoJogo.Jogador;
            }

            return AvataresNoJogo.Obter(personagemId);
        }

        private void Update()
        {
            _espera -= Time.unscaledDeltaTime;
            if (_espera > 0f)
            {
                return;
            }

            _espera = Intervalo;
            Conferir();
        }

        /// <summary>Aplica o que falta. Público para o teste não depender do tempo.</summary>
        public void Conferir()
        {
            foreach (var par in VisualEquipadoDoCliente.Estados)
            {
                var avatar = Buscar(par.Key);
                if (avatar == null)
                {
                    _aplicados.Remove(par.Key);
                    continue;
                }

                if (
                    _aplicados.TryGetValue(par.Key, out var feito)
                    && feito.Avatar == avatar
                    && feito.Versao == par.Value.Versao
                )
                {
                    continue;
                }

                ApresentadorDoVisual.Aplicar(avatar.transform, par.Value.Visual);
                _aplicados[par.Key] = (avatar, par.Value.Versao);
            }
        }
    }
}
