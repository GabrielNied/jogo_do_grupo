using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Montarias;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Montarias
{
    /// <summary>
    /// Controla o estado de montaria do jogador local e a visualização dos outros
    /// jogadores montados na cena. Tecla de atalho: T.
    /// </summary>
    public sealed class ControleDeMontaria : MonoBehaviour
    {
        public static ControleDeMontaria Instancia { get; private set; }

        private readonly Dictionary<long, VisualDaMontaria> _visuaisAtivos =
            new Dictionary<long, VisualDaMontaria>();
        private readonly Dictionary<long, int> _estadosPendentes = new Dictionary<long, int>();
        private IDisposable _assinatura;

        public bool MontadoLocal { get; private set; }
        public event Action<bool> EstadoMudou;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;
        }

        private void OnEnable()
        {
            AssinarRede();
        }

        private void OnDisable()
        {
            _assinatura?.Dispose();
            _assinatura = null;
        }

        public void Iniciar()
        {
            AssinarRede();
        }

        public void Reiniciar()
        {
            _assinatura?.Dispose();
            _assinatura = null;
            LimparVisuais();
            MontadoLocal = false;
        }

        private void AssinarRede()
        {
            if (_assinatura != null)
            {
                return;
            }

            _assinatura = ConexaoTempoReal.Assinar<EstadoMontaria>(
                MontariasMessageIds.EstadoMontaria,
                AoReceberEstadoMontaria
            );
        }

        private void Update()
        {
            if (!EntradaNoJogo.NoJogo || EntradaNoJogo.Jogador == null)
            {
                return;
            }

            // Tecla T alterna montar / desmontar
            if (
                Keyboard.current != null
                && Keyboard.current.tKey.wasPressedThisFrame
                && !JanelasDoJogo.AlgoAberto
            )
            {
                AlternarMontaria();
            }

            // Tenta aplicar visuais pendentes para avatares que acabaram de ser instanciados na cena
            AplicarPendentes();
        }

        public void AlternarMontaria()
        {
            if (MontadoLocal)
            {
                Desmontar();
            }
            else
            {
                ChamarMontaria();
            }
        }

        public void ChamarMontaria()
        {
            ConexaoTempoReal.Enviar(MontariasMessageIds.ChamarMontaria, new ChamarMontaria());
        }

        public void Desmontar()
        {
            ConexaoTempoReal.Enviar(MontariasMessageIds.Desmontar, new Desmontar());
        }

        public void AoReceberEstadoMontaria(EstadoMontaria msg)
        {
            if (msg == null)
            {
                return;
            }

            if (msg.PersonagemId == EntradaNoJogo.PersonagemId)
            {
                var mudou = MontadoLocal != msg.Montado;
                MontadoLocal = msg.Montado;

                if (mudou)
                {
                    AvisosDoJogo.Mostrar(
                        msg.Montado ? "Montaria invocada" : "Desmontado",
                        TipoDeAviso.Informacao
                    );
                    EstadoMudou?.Invoke(msg.Montado);
                }

                if (msg.Montado)
                {
                    AplicarVisual(msg.PersonagemId, msg.TipoMontaria);
                }
                else
                {
                    RemoverVisual(msg.PersonagemId);
                }
            }
            else
            {
                if (msg.Montado)
                {
                    AplicarVisual(msg.PersonagemId, msg.TipoMontaria);
                }
                else
                {
                    RemoverVisual(msg.PersonagemId);
                }
            }
        }

        private void AplicarVisual(long personagemId, int tipo)
        {
            var avatar = ObterAvatar(personagemId);
            if (avatar == null)
            {
                _estadosPendentes[personagemId] = tipo;
                return;
            }

            _estadosPendentes.Remove(personagemId);

            if (_visuaisAtivos.TryGetValue(personagemId, out var existente) && existente != null)
            {
                return;
            }

            var visual = VisualDaMontaria.Criar(avatar.transform, tipo);
            _visuaisAtivos[personagemId] = visual;
        }

        private void RemoverVisual(long personagemId)
        {
            _estadosPendentes.Remove(personagemId);

            if (_visuaisAtivos.TryGetValue(personagemId, out var visual))
            {
                _visuaisAtivos.Remove(personagemId);
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                }
            }
        }

        private void AplicarPendentes()
        {
            if (_estadosPendentes.Count == 0)
            {
                return;
            }

            var resolvidos = new List<long>();
            foreach (var par in _estadosPendentes)
            {
                var avatar = ObterAvatar(par.Key);
                if (avatar != null)
                {
                    var visual = VisualDaMontaria.Criar(avatar.transform, par.Value);
                    _visuaisAtivos[par.Key] = visual;
                    resolvidos.Add(par.Key);
                }
            }

            for (var i = 0; i < resolvidos.Count; i++)
            {
                _estadosPendentes.Remove(resolvidos[i]);
            }
        }

        private void LimparVisuais()
        {
            foreach (var par in _visuaisAtivos)
            {
                if (par.Value != null)
                {
                    Destroy(par.Value.gameObject);
                }
            }

            _visuaisAtivos.Clear();
            _estadosPendentes.Clear();
        }

        public static GameObject ObterAvatar(long personagemId)
        {
            if (EntradaNoJogo.NoJogo && EntradaNoJogo.PersonagemId == personagemId)
            {
                return EntradaNoJogo.Jogador;
            }

            return GameObject.Find($"Jogador_{personagemId}");
        }

        private void OnDestroy()
        {
            _assinatura?.Dispose();
            _assinatura = null;
            LimparVisuais();
            if (Instancia == this)
            {
                Instancia = null;
            }
        }
    }
}
