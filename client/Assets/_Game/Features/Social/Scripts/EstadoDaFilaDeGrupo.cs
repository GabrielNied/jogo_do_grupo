using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Gerencia o estado da fila automática de grupo (Dungeon Finder) no client.
    /// Comunica intenções ao servidor e atualiza os contadores de papéis encontrados.
    /// </summary>
    internal sealed class EstadoDaFilaDeGrupo : IDisposable
    {
        private readonly IConexaoDoGrupo _conexao;
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public EstadoDaFilaDeGrupo(IConexaoDoGrupo conexao = null)
        {
            _conexao = conexao ?? new ConexaoDoGrupoReal();
            _assinaturas.Add(
                _conexao.Assinar<StatusDaFilaDeGrupo>(
                    SocialMessageIds.StatusDaFilaDeGrupo,
                    AoReceberStatus
                )
            );
            _assinaturas.Add(
                _conexao.Assinar<GrupoDeFilaFormado>(
                    SocialMessageIds.GrupoDeFilaFormado,
                    AoReceberGrupoFormado
                )
            );
        }

        public bool EmFila { get; private set; }
        public PapelNoGrupo Papel { get; private set; }
        public int Tanques { get; private set; }
        public int Healers { get; private set; }
        public int Dps { get; private set; }
        public int SegundosNaFila { get; private set; }
        public string UltimoAviso { get; private set; } = "";

        public event Action Mudou;
        public event Action<GrupoDeFilaFormado> GrupoFormado;

        public void Dispose()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura?.Dispose();
            }

            _assinaturas.Clear();
        }

        public void Entrar(PapelNoGrupo papel)
        {
            if (!RegrasFilaGrupo.PapelValido(papel))
            {
                UltimoAviso = "Selecione um papel válido (Tanque, Healer ou DPS).";
                Mudou?.Invoke();
                return;
            }

            Papel = papel;
            UltimoAviso = "Entrando na fila de dungeon...";
            _conexao.Enviar(
                SocialMessageIds.EntrarNaFilaDeGrupo,
                new EntrarNaFilaDeGrupo { Papel = papel },
                EntregaTempoReal.Confiavel
            );
            Mudou?.Invoke();
        }

        public void Sair()
        {
            UltimoAviso = "Saindo da fila...";
            _conexao.Enviar(
                SocialMessageIds.SairDaFilaDeGrupo,
                new SairDaFilaDeGrupo(),
                EntregaTempoReal.Confiavel
            );
            Mudou?.Invoke();
        }

        private float _acumuladorSegundos;

        public void IncrementarTempo()
        {
            if (EmFila)
            {
                SegundosNaFila++;
                Mudou?.Invoke();
            }
        }

        public void Atualizar(float segundos)
        {
            if (!EmFila)
            {
                _acumuladorSegundos = 0f;
                return;
            }

            _acumuladorSegundos += segundos;
            if (_acumuladorSegundos >= 1f)
            {
                var segs = (int)_acumuladorSegundos;
                _acumuladorSegundos -= segs;
                SegundosNaFila += segs;
                Mudou?.Invoke();
            }
        }

        private void AoReceberStatus(StatusDaFilaDeGrupo status)
        {
            if (status == null)
            {
                return;
            }

            _acumuladorSegundos = 0f;
            EmFila = status.EmFila;
            if (status.EmFila)
            {
                Papel = status.Papel;
                Tanques = status.Tanques;
                Healers = status.Healers;
                Dps = status.Dps;
                SegundosNaFila = status.SegundosNaFila;
                UltimoAviso = "Buscando membros para a dungeon...";
            }
            else
            {
                Tanques = 0;
                Healers = 0;
                Dps = 0;
                SegundosNaFila = 0;
            }

            Mudou?.Invoke();
        }

        private void AoReceberGrupoFormado(GrupoDeFilaFormado formado)
        {
            if (formado == null)
            {
                return;
            }

            EmFila = false;
            UltimoAviso = formado.Mensagem;
            Mudou?.Invoke();
            GrupoFormado?.Invoke(formado);
        }
    }
}
