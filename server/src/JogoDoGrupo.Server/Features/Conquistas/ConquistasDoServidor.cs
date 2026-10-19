using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JogoDoGrupo.Shared.Features.Conquistas;

namespace JogoDoGrupo.Server.Features.Conquistas;

/// <summary>
/// Gerenciador autoritativo de conquistas e títulos da conta, em memória.
/// Garante unicidade de concessão, avanço cumulativo e validação de títulos equipados.
/// </summary>
public sealed class ConquistasDoServidor(ISaidaDeConquistas saida)
{
    private sealed class EstadoConta
    {
        public long ContaId { get; init; }
        public string TituloAtivo { get; set; } = "";
        public HashSet<string> TitulosDesbloqueados { get; } =
            new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, ProgressoConquista> Conquistas { get; } = [];
        public HashSet<string> AreasVisitadas { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly Lock _trava = new();
    private readonly Dictionary<long, EstadoConta> _contas = [];

    private EstadoConta ObterOuCriarEstado(long contaId)
    {
        if (!_contas.TryGetValue(contaId, out var estado))
        {
            estado = new EstadoConta { ContaId = contaId };
            foreach (var def in CatalogoDeConquistas.Todas)
            {
                estado.Conquistas[def.Id] = new ProgressoConquista
                {
                    ConquistaId = def.Id,
                    ProgressoAtual = 0,
                    Meta = def.Meta,
                    Concluida = false,
                    ConcluidaEmUnix = 0,
                };
            }
            _contas[contaId] = estado;
        }
        return estado;
    }

    public void Consultar(long contaId, long personagemId)
    {
        lock (_trava)
        {
            var estado = ObterOuCriarEstado(contaId);
            saida.Dados(personagemId, GerarDados(estado));
        }
    }

    public void EquiparTitulo(long contaId, long personagemId, string? titulo)
    {
        lock (_trava)
        {
            var estado = ObterOuCriarEstado(contaId);
            var tituloLimpo = (titulo ?? "").Trim();

            if (
                !RegrasConquistas.PodeEquiparTitulo(
                    tituloLimpo,
                    estado.TitulosDesbloqueados.ToArray()
                )
            )
            {
                saida.Recusada(personagemId, CodigosErroConquistas.TituloNaoConquistado);
                return;
            }

            estado.TituloAtivo = tituloLimpo;
            saida.TituloEquipado(personagemId, estado.TituloAtivo);
            saida.Dados(personagemId, GerarDados(estado));
        }
    }

    public void AvancarProgresso(
        long contaId,
        long personagemId,
        int conquistaId,
        int incrementoOuValor,
        bool ehValorAbsoluto = false
    )
    {
        lock (_trava)
        {
            var estado = ObterOuCriarEstado(contaId);
            if (!CatalogoDeConquistas.TentarObter(conquistaId, out var def))
            {
                return;
            }

            if (!estado.Conquistas.TryGetValue(conquistaId, out var progresso))
            {
                progresso = new ProgressoConquista
                {
                    ConquistaId = def.Id,
                    ProgressoAtual = 0,
                    Meta = def.Meta,
                    Concluida = false,
                    ConcluidaEmUnix = 0,
                };
                estado.Conquistas[conquistaId] = progresso;
            }

            if (progresso.Concluida)
            {
                return;
            }

            if (ehValorAbsoluto)
            {
                progresso.ProgressoAtual = Math.Max(progresso.ProgressoAtual, incrementoOuValor);
            }
            else
            {
                progresso.ProgressoAtual += incrementoOuValor;
            }

            if (progresso.ProgressoAtual >= progresso.Meta)
            {
                progresso.ProgressoAtual = progresso.Meta;
                progresso.Concluida = true;
                progresso.ConcluidaEmUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (!string.IsNullOrWhiteSpace(def.TituloRecompensa))
                {
                    estado.TitulosDesbloqueados.Add(def.TituloRecompensa);
                }

                saida.Desbloqueada(
                    personagemId,
                    new ConquistaDesbloqueada
                    {
                        ConquistaId = def.Id,
                        Nome = def.Nome,
                        Pontos = def.Pontos,
                        TituloDesbloqueado = def.TituloRecompensa,
                    }
                );

                saida.Dados(personagemId, GerarDados(estado));
            }
        }
    }

    public void RegistrarEntradaEmArea(long contaId, long personagemId, string area)
    {
        lock (_trava)
        {
            var estado = ObterOuCriarEstado(contaId);
            if (string.IsNullOrWhiteSpace(area))
            {
                return;
            }

            if (estado.AreasVisitadas.Add(area.Trim()))
            {
                AvancarProgresso(
                    contaId,
                    personagemId,
                    CatalogoDeConquistas.DesbravadorDeAsterra,
                    estado.AreasVisitadas.Count,
                    ehValorAbsoluto: true
                );
            }
        }
    }

    private static DadosDeConquistas GerarDados(EstadoConta estado)
    {
        var progressos = estado.Conquistas.Values.ToArray();
        return new DadosDeConquistas
        {
            PontosTotais = RegrasConquistas.SomarPontos(progressos),
            TituloAtivo = estado.TituloAtivo,
            TitulosDesbloqueados = estado.TitulosDesbloqueados.ToArray(),
            Conquistas = progressos,
        };
    }
}
