using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Conquistas;

namespace JogoDoGrupo.Client.Features.Conquistas
{
    internal sealed class EstadoDeConquistas : IDisposable
    {
        private static EstadoDeConquistas _instancia;

        public static EstadoDeConquistas Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    _instancia = new EstadoDeConquistas();
                }
                return _instancia;
            }
        }

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public DadosDeConquistas Dados { get; private set; }
        public string UltimoErro { get; private set; } = "";

        public event Action Mudou;
        public event Action<ConquistaDesbloqueada> Desbloqueada;

        public EstadoDeConquistas()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<DadosDeConquistas>(
                    ConquistasMessageIds.DadosDeConquistas,
                    AoReceberDados
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ConquistaDesbloqueada>(
                    ConquistasMessageIds.ConquistaDesbloqueada,
                    AoReceberDesbloqueio
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<TituloEquipado>(
                    ConquistasMessageIds.TituloEquipado,
                    AoReceberTituloEquipado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<OperacaoDeConquistaRecusada>(
                    ConquistasMessageIds.OperacaoDeConquistaRecusada,
                    AoReceberRecusa
                )
            );
        }

        public void Consultar()
        {
            ConexaoTempoReal.Enviar(
                ConquistasMessageIds.ConsultarConquistas,
                new ConsultarConquistas(),
                EntregaTempoReal.Conviavel
            );
        }

        public void EquiparTitulo(string titulo)
        {
            UltimoErro = "";
            ConexaoTempoReal.Enviar(
                ConquistasMessageIds.EquiparTitulo,
                new EquiparTitulo { Titulo = titulo ?? "" },
                EntregaTempoReal.Conviavel
            );
        }

        private void AoReceberDados(DadosDeConquistas dados)
        {
            Dados = dados;
            UltimoErro = "";
            Mudou?.Invoke();
        }

        private void AoReceberDesbloqueio(ConquistaDesbloqueada desbloqueada)
        {
            Desbloqueada?.Invoke(desbloqueada);
        }

        private void AoReceberTituloEquipado(TituloEquipado equipou)
        {
            if (Dados != null)
            {
                Dados.TituloAtivo = equipou.Titulo ?? "";
                Mudou?.Invoke();
            }
        }

        private void AoReceberRecusa(OperacaoDeConquistaRecusada recusa)
        {
            UltimoErro = recusa != null ? recusa.Codigo : "";
            Mudou?.Invoke();
        }

        public void Dispose()
        {
            foreach (var sub in _assinaturas)
            {
                sub.Dispose();
            }
            _assinaturas.Clear();
        }
    }
}
