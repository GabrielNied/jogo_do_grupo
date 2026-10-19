using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Gerenciador de estado local do bestiário no client (#922).
    /// </summary>
    internal sealed class ControleDeBestiario : MonoBehaviour
    {
        private static ControleDeBestiario _instancia;
        private static readonly Dictionary<int, EntradaBestiarioDto> _entradas =
            new Dictionary<int, EntradaBestiarioDto>();

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public static event Action EstadoMudou;

        public static IReadOnlyDictionary<int, EntradaBestiarioDto> Entradas => _entradas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
            _entradas.Clear();
            EstadoMudou = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
                return;

            var objeto = new GameObject("Mobs.Bestiario.Controle");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ControleDeBestiario>();
        }

        public static EntradaBestiarioDto ObterEntrada(int criaturaId)
        {
            if (_entradas.TryGetValue(criaturaId, out var entrada))
                return entrada;

            return new EntradaBestiarioDto(
                criaturaId,
                0,
                NivelDeConhecimentoBestiario.Desconhecido
            );
        }

        public static int ContagemPorNivel(NivelDeConhecimentoBestiario nivel)
        {
            int total = 0;
            foreach (var entrada in _entradas.Values)
            {
                if (entrada.Nivel == nivel)
                    total++;
            }
            return total;
        }

        public static void Consultar()
        {
            ConexaoTempoReal.Enviar(MobsMessageIds.ConsultarBestiario, new ConsultarBestiario());
        }

        private void Awake()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDoBestiario>(
                    MobsMessageIds.EstadoDoBestiario,
                    AoEstadoDoBestiario
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ProgressoDoBestiarioAtualizado>(
                    MobsMessageIds.ProgressoDoBestiarioAtualizado,
                    AoProgressoAtualizado
                )
            );

            Consultar();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _assinaturas.Count; i++)
            {
                _assinaturas[i]?.Dispose();
            }
            _assinaturas.Clear();
        }

        private void AoEstadoDoBestiario(EstadoDoBestiario estado)
        {
            _entradas.Clear();
            if (estado?.Entradas != null)
            {
                for (int i = 0; i < estado.Entradas.Length; i++)
                {
                    var ent = estado.Entradas[i];
                    _entradas[ent.CriaturaId] = ent;
                }
            }
            EstadoMudou?.Invoke();
        }

        private void AoProgressoAtualizado(ProgressoDoBestiarioAtualizado notificacao)
        {
            if (notificacao?.Entrada != null)
            {
                _entradas[notificacao.Entrada.CriaturaId] = notificacao.Entrada;
                EstadoMudou?.Invoke();
            }
        }
    }
}
