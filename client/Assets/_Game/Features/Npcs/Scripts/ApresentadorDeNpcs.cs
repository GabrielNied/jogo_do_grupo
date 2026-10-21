using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Features.Npcs.UI;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Gerencia o ciclo de vida e a presença física dos NPCs recebidos pela rede na área atual (#1063).
    /// </summary>
    public sealed class ApresentadorDeNpcs : MonoBehaviour
    {
        private readonly Dictionary<int, NpcNoMundo> _npcsInstanciados =
            new Dictionary<int, NpcNoMundo>();
        private readonly List<HabitanteNoMundo> _habitantes = new List<HabitanteNoMundo>();
        private IDisposable _assinaturaNpcs;
        private string _areaAtual = "";
        private bool _prologoAgendado;

        public IReadOnlyDictionary<int, NpcNoMundo> NpcsInstanciados => _npcsInstanciados;
        public string AreaAtual => _areaAtual;
        public IReadOnlyList<HabitanteNoMundo> Habitantes => _habitantes;

        private void OnEnable()
        {
            _assinaturaNpcs = ConexaoTempoReal.Assinar<NpcsDaArea>(
                NpcsMessageIds.NpcsDaArea,
                AoReceberNpcsDaArea
            );

            ConexaoTempoReal.EstadoMudou += AoMudarEstadoConexao;

            if (ConexaoTempoReal.Conectado)
            {
                SolicitarNpcs();
            }
        }

        private void OnDisable()
        {
            _assinaturaNpcs?.Dispose();
            _assinaturaNpcs = null;

            ConexaoTempoReal.EstadoMudou -= AoMudarEstadoConexao;
            LimparNpcs();
        }

        private void AoMudarEstadoConexao(EstadoTempoReal estado)
        {
            if (estado == EstadoTempoReal.Conectado)
            {
                SolicitarNpcs();
            }
        }

        public void SolicitarNpcs()
        {
            ConexaoTempoReal.Enviar(NpcsMessageIds.SolicitarNpcs, new SolicitarNpcs());
        }

        public void AoReceberNpcsDaArea(NpcsDaArea mensagem)
        {
            if (mensagem == null)
            {
                return;
            }

            _areaAtual = mensagem.Area ?? "";
            var idsRecebidos = new HashSet<int>();

            if (mensagem.Npcs != null)
            {
                for (var i = 0; i < mensagem.Npcs.Length; i++)
                {
                    var def = mensagem.Npcs[i];
                    if (def == null)
                    {
                        continue;
                    }

                    idsRecebidos.Add(def.Id);
                    if (
                        _npcsInstanciados.TryGetValue(def.Id, out var npcExistente)
                        && npcExistente != null
                    )
                    {
                        npcExistente.Configurar(def);
                    }
                    else
                    {
                        var obj = new GameObject($"Npc_{def.Id}");
                        obj.transform.SetParent(transform, true);
                        var npc = obj.AddComponent<NpcNoMundo>();
                        npc.Configurar(def);
                        _npcsInstanciados[def.Id] = npc;
                    }
                }
            }

            var idsParaRemover = new List<int>();
            foreach (var kvp in _npcsInstanciados)
            {
                if (!idsRecebidos.Contains(kvp.Key))
                {
                    idsParaRemover.Add(kvp.Key);
                }
            }

            for (var i = 0; i < idsParaRemover.Count; i++)
            {
                var id = idsParaRemover[i];
                if (_npcsInstanciados.TryGetValue(id, out var npc))
                {
                    if (npc != null && npc.gameObject != null)
                    {
                        Destroy(npc.gameObject);
                    }
                    _npcsInstanciados.Remove(id);
                }
            }

            SincronizarHabitantes();
        }

        // Guardas e moradores ambiente só existem na cidade; vêm dos dados e não passam pela rede (#1069).
        private void SincronizarHabitantes()
        {
            var naCidade = string.Equals(
                _areaAtual,
                CatalogoNpcs.AreaCidade,
                StringComparison.OrdinalIgnoreCase
            );
            if (!naCidade)
            {
                LimparHabitantes();
                return;
            }

            if (!_prologoAgendado)
            {
                _prologoAgendado = true;
                StartCoroutine(OferecerPrologo());
            }

            if (_habitantes.Count > 0)
            {
                return;
            }

            for (var i = 0; i < PopulacaoDaCidade.Todos.Length; i++)
            {
                var obj = new GameObject("Habitante");
                obj.transform.SetParent(transform, true);
                var habitante = obj.AddComponent<HabitanteNoMundo>();
                habitante.Configurar(PopulacaoDaCidade.Todos[i]);
                _habitantes.Add(habitante);
            }
        }

        // Prólogo opcional (#327): oferecido uma vez por personagem, logo depois de entrar na cidade.
        private System.Collections.IEnumerator OferecerPrologo()
        {
            yield return new WaitForSecondsRealtime(JanelaPrologo.EsperaParaOferecerSegundos);
            JanelaPrologo.OferecerSeNovo(EntradaNoJogo.PersonagemId);
        }

        private void LimparHabitantes()
        {
            for (var i = 0; i < _habitantes.Count; i++)
            {
                if (_habitantes[i] != null)
                {
                    Destroy(_habitantes[i].gameObject);
                }
            }

            _habitantes.Clear();
        }

        private void LimparNpcs()
        {
            foreach (var npc in _npcsInstanciados.Values)
            {
                if (npc != null && npc.gameObject != null)
                {
                    Destroy(npc.gameObject);
                }
            }
            _npcsInstanciados.Clear();
            LimparHabitantes();
        }
    }
}
