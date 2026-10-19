using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Conquistas;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Conquistas;

public sealed class ConquistasModule : IFeatureModule
{
    private ConquistasDoServidor? _conquistas;
    private ServidorTempoReal? _tempoReal;

    public string Nome => "Conquistas";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) { }

    public void MapearRotas(IEndpointRouteBuilder rotas) { }

    public void RegistrarMensagens(MensagensTempoReal mensagens) =>
        mensagens
            .Faixa(ConquistasMessageIds.Faixa)
            .Ao<ConsultarConquistas>(
                ConquistasMessageIds.ConsultarConquistas,
                (sessao, _) => _conquistas!.Consultar(sessao.ContaId, sessao.PersonagemId)
            )
            .Ao<EquiparTitulo>(
                ConquistasMessageIds.EquiparTitulo,
                (sessao, pedido) =>
                    _conquistas!.EquiparTitulo(sessao.ContaId, sessao.PersonagemId, pedido.Titulo)
            );

    public void Inicializar(ServerContext contexto)
    {
        _tempoReal = contexto.TempoReal;
        var saida = new SaidaTempoRealConquistas(contexto.TempoReal);
        _conquistas = new ConquistasDoServidor(saida);

        contexto.Eventos.Assinar<JogadorEntrouNaArea>(AoEntrarNaArea);
        contexto.Eventos.Assinar<InimigoDerrotado>(AoDerrotarInimigo);
    }

    private void AoEntrarNaArea(JogadorEntrouNaArea evento)
    {
        if (
            _tempoReal != null
            && _tempoReal.TentarObterSessao(evento.PersonagemId, out var sessao)
            && sessao != null
        )
        {
            _conquistas?.RegistrarEntradaEmArea(sessao.ContaId, sessao.PersonagemId, evento.Area);
        }
    }

    private void AoDerrotarInimigo(InimigoDerrotado evento)
    {
        foreach (var participanteId in evento.Participantes)
        {
            if (
                _tempoReal != null
                && _tempoReal.TentarObterSessao(participanteId, out var sessao)
                && sessao != null
            )
            {
                _conquistas?.AvancarProgresso(
                    sessao.ContaId,
                    sessao.PersonagemId,
                    CatalogoDeConquistas.MatadorDeMobs,
                    1
                );
                if (evento.CriaturaId == 10)
                {
                    _conquistas?.AvancarProgresso(
                        sessao.ContaId,
                        sessao.PersonagemId,
                        CatalogoDeConquistas.OFimDoPredador,
                        1
                    );
                }
            }
        }
    }

    private sealed class SaidaTempoRealConquistas(ServidorTempoReal tempoReal) : ISaidaDeConquistas
    {
        public void Dados(long personagemId, DadosDeConquistas dados) =>
            Enviar(personagemId, ConquistasMessageIds.DadosDeConquistas, dados);

        public void Desbloqueada(long personagemId, ConquistaDesbloqueada desbloqueada) =>
            Enviar(personagemId, ConquistasMessageIds.ConquistaDesbloqueada, desbloqueada);

        public void TituloEquipado(long personagemId, string titulo) =>
            Enviar(
                personagemId,
                ConquistasMessageIds.TituloEquipado,
                new TituloEquipado { PersonagemId = personagemId, Titulo = titulo }
            );

        public void Recusada(long personagemId, string codigo) =>
            Enviar(
                personagemId,
                ConquistasMessageIds.OperacaoDeConquistaRecusada,
                new OperacaoDeConquistaRecusada { Codigo = codigo }
            );

        private void Enviar<T>(long personagemId, ushort id, T mensagem)
        {
            if (tempoReal.TentarObterSessao(personagemId, out var sessao) && sessao != null)
            {
                tempoReal.Enviar(sessao, id, mensagem);
            }
        }
    }
}
