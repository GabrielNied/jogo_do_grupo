using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Economia
{
    /// <summary>
    /// Gerencia a sincronização de barracas de jogadores e vendedores offline no client (#905).
    /// </summary>
    internal sealed class EstadoDeBarracas : MonoBehaviour
    {
        private static EstadoDeBarracas _instancia;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public static BarracaResumo[] BarracasAtivas { get; private set; } =
            Array.Empty<BarracaResumo>();
        public static DadosDaBarraca BarracaAberta { get; private set; }

        public static event Action BarracasMudaram;
        public static event Action<DadosDaBarraca> BarracaMudou;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
            BarracasAtivas = Array.Empty<BarracaResumo>();
            BarracaAberta = null;
            BarracasMudaram = null;
            BarracaMudou = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
                return;

            var objeto = new GameObject("Economia.Barracas");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<EstadoDeBarracas>();
        }

        private void Awake()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<BarracasNaArea>(
                    EconomiaMessageIds.BarracasNaArea,
                    AoReceberBarracasNaArea
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<DadosDaBarraca>(
                    EconomiaMessageIds.DadosDaBarraca,
                    AoReceberDadosDaBarraca
                )
            );
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _assinaturas.Count; i++)
            {
                _assinaturas[i]?.Dispose();
            }
            _assinaturas.Clear();
        }

        private void AoReceberBarracasNaArea(BarracasNaArea msg)
        {
            BarracasAtivas = msg?.Barracas ?? Array.Empty<BarracaResumo>();
            BarracasMudaram?.Invoke();
        }

        private void AoReceberDadosDaBarraca(DadosDaBarraca msg)
        {
            BarracaAberta = msg;
            BarracaMudou?.Invoke(msg);
        }

        public static void ConsultarBarracasNaArea()
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ConsultarBarraca,
                new ConsultarBarraca { PersonagemId = 0 }
            );
        }

        public static void ConsultarBarraca(long personagemId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ConsultarBarraca,
                new ConsultarBarraca { PersonagemId = personagemId }
            );
        }

        public static void MontarBarraca(string titulo, float x, float z)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.MontarBarraca,
                new MontarBarraca
                {
                    Titulo = titulo,
                    PosicaoX = x,
                    PosicaoZ = z,
                }
            );
        }

        public static void DesmontarBarraca()
        {
            ConexaoTempoReal.Enviar(EconomiaMessageIds.DesmontarBarraca, new DesmontarBarraca());
        }

        public static void ContratarVendedorOffline(string titulo, int dias, float x, float z)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ContratarVendedorOffline,
                new ContratarVendedorOffline
                {
                    Titulo = titulo,
                    Dias = dias,
                    PosicaoX = x,
                    PosicaoZ = z,
                }
            );
        }

        public static void EncerrarVendedorOffline()
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.EncerrarVendedorOffline,
                new EncerrarVendedorOffline()
            );
        }

        public static void ComprarDaBarraca(long ordemId, long vendedorId)
        {
            ConexaoTempoReal.Enviar(
                EconomiaMessageIds.ComprarDaBarraca,
                new ComprarDaBarraca { OrdemId = ordemId, VendedorId = vendedorId }
            );
        }
    }
}
