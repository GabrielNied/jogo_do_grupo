using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Regras da troca direta entre jogadores (#175): convite, oferta dos dois lados, dupla
/// confirmação e encerramento. O estado vive só na memória (uma troca por jogador); o que mexe no
/// banco é do <see cref="IExecutorDaTroca"/>, e nada muda de dono antes dele confirmar. Os pedidos
/// entram um de cada vez, na ordem de chegada (a fila é o <see cref="_fila"/>).
/// </summary>
public sealed class TrocaDiretaDoServidor(
    IDiretorioDePersonagens diretorio,
    ISaidaDeTroca saida,
    IExecutorDaTroca executor,
    Func<long, bool> estaOnline,
    Func<IPosicoesDoMundo?> obterPosicoes,
    Func<IEstadoDeCombate?> obterCombate,
    Func<long, long, bool> contatoBarrado,
    Func<long> agoraMs,
    Action<long> aoMovimentarCoroas
)
{
    private sealed class Troca
    {
        public long Id;
        public long A; // quem convidou
        public long B; // quem foi convidado
        public bool Aberta;
        public long ConviteExpiraEmMs;
        public int Versao;
        public OfertaPedida OfertaA = OfertaPedida.Vazia;
        public OfertaPedida OfertaB = OfertaPedida.Vazia;
        public OfertaDeTroca DescricaoA = new();
        public OfertaDeTroca DescricaoB = new();
        public bool AceiteA;
        public bool AceiteB;

        public long Outro(long personagemId) => personagemId == A ? B : A;
    }

    private readonly SemaphoreSlim _fila = new(1, 1);
    private readonly Dictionary<long, Troca> _porPersonagem = [];
    private long _proximoId;

    /// <summary>A troca em que o personagem está (convite ou aberta), para consulta e teste.</summary>
    public bool EstaEmTroca(long personagemId)
    {
        lock (_porPersonagem)
            return _porPersonagem.ContainsKey(personagemId);
    }

    public async Task PedirAsync(long solicitanteId, string nome, CancellationToken c = default)
    {
        await _fila.WaitAsync(c);
        try
        {
            if (
                string.IsNullOrWhiteSpace(nome)
                || !diretorio.TentarObterPorNome(nome.Trim(), out var alvoId)
                || !estaOnline(alvoId)
                || (alvoId != solicitanteId && contatoBarrado(solicitanteId, alvoId))
            )
            {
                Recusar(solicitanteId, 0, CodigosErroTroca.AlvoNaoEncontrado);
                return;
            }

            if (alvoId == solicitanteId)
            {
                Recusar(solicitanteId, 0, CodigosErroTroca.TrocaComSiMesmo);
                return;
            }

            PurgarConvitesExpirados();
            if (EstaEmTroca(solicitanteId))
            {
                Recusar(solicitanteId, 0, CodigosErroTroca.VoceJaEstaEmTroca);
                return;
            }

            if (EstaEmTroca(alvoId))
            {
                Recusar(solicitanteId, 0, CodigosErroTroca.AlvoJaEstaEmTroca);
                return;
            }

            var motivo = ChecarCondicoes(solicitanteId, alvoId);
            if (motivo != null)
            {
                Recusar(solicitanteId, 0, motivo);
                return;
            }

            var troca = new Troca
            {
                Id = Interlocked.Increment(ref _proximoId),
                A = solicitanteId,
                B = alvoId,
                ConviteExpiraEmMs = agoraMs() + RegrasTroca.ValidadeDoConviteSegundos * 1000L,
            };
            lock (_porPersonagem)
            {
                _porPersonagem[solicitanteId] = troca;
                _porPersonagem[alvoId] = troca;
            }

            diretorio.TentarObterNome(solicitanteId, out var nomeDeQuemPede);
            saida.Convite(
                alvoId,
                new ConviteDeTroca
                {
                    TrocaId = troca.Id,
                    NomeDe = nomeDeQuemPede,
                    ValidadeSegundos = RegrasTroca.ValidadeDoConviteSegundos,
                }
            );
        }
        finally
        {
            _fila.Release();
        }
    }

    public async Task ResponderAsync(
        long personagemId,
        long trocaId,
        bool aceitar,
        CancellationToken c = default
    )
    {
        await _fila.WaitAsync(c);
        try
        {
            var troca = Achar(personagemId, trocaId);
            if (troca == null || troca.Aberta || troca.B != personagemId)
            {
                Recusar(personagemId, trocaId, CodigosErroTroca.TrocaNaoEncontrada);
                return;
            }

            if (agoraMs() > troca.ConviteExpiraEmMs)
            {
                Remover(troca);
                saida.Encerrada(
                    troca.A,
                    Encerrada(troca, CodigosErroTroca.ConviteExpirado, troca.B)
                );
                Recusar(personagemId, trocaId, CodigosErroTroca.ConviteExpirado);
                return;
            }

            if (!aceitar)
            {
                Remover(troca);
                saida.Encerrada(
                    troca.A,
                    Encerrada(troca, CodigosErroTroca.ConviteRecusado, troca.B)
                );
                return;
            }

            var motivo = !estaOnline(troca.A)
                ? CodigosErroTroca.AlvoNaoEncontrado
                : ChecarCondicoes(troca.A, troca.B);
            if (motivo != null)
            {
                Remover(troca);
                saida.Encerrada(troca.A, Encerrada(troca, motivo, troca.B));
                Recusar(personagemId, trocaId, motivo);
                return;
            }

            troca.Aberta = true;
            EnviarEstadoAosDois(troca);
            await EnviarBolsaAsync(troca.A, troca.Id, c);
            await EnviarBolsaAsync(troca.B, troca.Id, c);
        }
        finally
        {
            _fila.Release();
        }
    }

    public async Task AtualizarOfertaAsync(
        long personagemId,
        long trocaId,
        long coroas,
        long[]? instanciaIds,
        CancellationToken c = default
    )
    {
        await _fila.WaitAsync(c);
        try
        {
            var troca = Achar(personagemId, trocaId);
            if (troca == null || !troca.Aberta)
            {
                Recusar(personagemId, trocaId, CodigosErroTroca.TrocaNaoEncontrada);
                return;
            }

            var ids = instanciaIds ?? [];
            if (!RegrasTroca.OfertaValida(coroas, ids))
            {
                Recusar(personagemId, troca.Id, CodigosErroTroca.OfertaInvalida);
                return;
            }

            var oferta = new OfertaPedida(coroas, ids);
            var (erro, descricao) = await executor.ValidarOfertaAsync(personagemId, oferta, c);
            if (erro != null || descricao == null)
            {
                Recusar(personagemId, troca.Id, erro ?? CodigosErroTroca.OfertaInvalida);
                return;
            }

            if (personagemId == troca.A)
            {
                troca.OfertaA = oferta;
                troca.DescricaoA = descricao;
            }
            else
            {
                troca.OfertaB = oferta;
                troca.DescricaoB = descricao;
            }

            // Qualquer mudança de oferta desfaz o aceite dos dois lados.
            troca.Versao++;
            troca.AceiteA = false;
            troca.AceiteB = false;
            EnviarEstadoAosDois(troca);
        }
        finally
        {
            _fila.Release();
        }
    }

    public async Task ConfirmarAsync(
        long personagemId,
        long trocaId,
        int versaoDaOferta,
        CancellationToken c = default
    )
    {
        await _fila.WaitAsync(c);
        try
        {
            var troca = Achar(personagemId, trocaId);
            if (troca == null || !troca.Aberta)
            {
                Recusar(personagemId, trocaId, CodigosErroTroca.TrocaNaoEncontrada);
                return;
            }

            // O jogador aceitou uma oferta que já mudou: não vale, e ele vê a atual.
            if (versaoDaOferta != troca.Versao)
            {
                saida.Estado(personagemId, EstadoPara(troca, personagemId));
                return;
            }

            if (personagemId == troca.A)
                troca.AceiteA = true;
            else
                troca.AceiteB = true;
            EnviarEstadoAosDois(troca);
            if (!troca.AceiteA || !troca.AceiteB)
                return;

            var motivo = ChecarCondicoes(troca.A, troca.B);
            if (motivo != null)
            {
                Remover(troca);
                saida.Encerrada(troca.A, Encerrada(troca, motivo, troca.B));
                saida.Encerrada(troca.B, Encerrada(troca, motivo, troca.A));
                return;
            }

            var resultado = await executor.ExecutarAsync(
                troca.A,
                troca.OfertaA,
                troca.B,
                troca.OfertaB,
                c
            );
            if (resultado.Erro == null)
            {
                Remover(troca);
                saida.Encerrada(troca.A, Encerrada(troca, CodigosErroTroca.Concluida, troca.B));
                saida.Encerrada(troca.B, Encerrada(troca, CodigosErroTroca.Concluida, troca.A));
                aoMovimentarCoroas(troca.A);
                aoMovimentarCoroas(troca.B);
                return;
            }

            // Nada mudou de dono (o executor desfez tudo). A troca reabre sem aceites.
            troca.Versao++;
            troca.AceiteA = false;
            troca.AceiteB = false;
            Recusar(troca.A, troca.Id, resultado.Erro);
            Recusar(troca.B, troca.Id, resultado.Erro);
            EnviarEstadoAosDois(troca);
            await EnviarBolsaAsync(troca.A, troca.Id, c);
            await EnviarBolsaAsync(troca.B, troca.Id, c);
        }
        finally
        {
            _fila.Release();
        }
    }

    public async Task CancelarAsync(long personagemId, long trocaId, CancellationToken c = default)
    {
        await _fila.WaitAsync(c);
        try
        {
            // TrocaId 0 quer dizer "a minha": quem convidou ainda não conhece o id do convite.
            var troca =
                trocaId == 0 ? AchaDoPersonagem(personagemId) : Achar(personagemId, trocaId);
            if (troca == null)
                return;

            Remover(troca);
            var outro = troca.Outro(personagemId);
            saida.Encerrada(
                personagemId,
                Encerrada(troca, CodigosErroTroca.CanceladaPorVoce, outro)
            );
            saida.Encerrada(
                outro,
                Encerrada(troca, CodigosErroTroca.CanceladaPeloOutro, personagemId)
            );
        }
        finally
        {
            _fila.Release();
        }
    }

    public async Task ConsultarBolsaAsync(
        long personagemId,
        long trocaId,
        CancellationToken c = default
    )
    {
        await _fila.WaitAsync(c);
        try
        {
            var troca = Achar(personagemId, trocaId);
            if (troca == null || !troca.Aberta)
                return;
            await EnviarBolsaAsync(personagemId, troca.Id, c);
        }
        finally
        {
            _fila.Release();
        }
    }

    /// <summary>Quem desconecta cancela a troca, sem perda: nada foi movido antes do commit.</summary>
    public async Task AoDesconectarAsync(long personagemId, CancellationToken c = default)
    {
        await _fila.WaitAsync(c);
        try
        {
            var troca = AchaDoPersonagem(personagemId);
            if (troca == null)
                return;

            Remover(troca);
            var outro = troca.Outro(personagemId);
            saida.Encerrada(outro, Encerrada(troca, CodigosErroTroca.Desconectou, personagemId));
        }
        finally
        {
            _fila.Release();
        }
    }

    // ── Apoio ─────────────────────────────────────────────────────────────────────

    private Troca? AchaDoPersonagem(long personagemId)
    {
        lock (_porPersonagem)
            return _porPersonagem.TryGetValue(personagemId, out var troca) ? troca : null;
    }

    private Troca? Achar(long personagemId, long trocaId)
    {
        var troca = AchaDoPersonagem(personagemId);
        return troca != null && troca.Id == trocaId ? troca : null;
    }

    private void Remover(Troca troca)
    {
        lock (_porPersonagem)
        {
            _porPersonagem.Remove(troca.A);
            _porPersonagem.Remove(troca.B);
        }
    }

    /// <summary>Convite que ninguém respondeu não prende quem convidou nem quem foi convidado.</summary>
    private void PurgarConvitesExpirados()
    {
        List<Troca> vencidas;
        lock (_porPersonagem)
        {
            vencidas = _porPersonagem
                .Values.Where(t => !t.Aberta && agoraMs() > t.ConviteExpiraEmMs)
                .Distinct()
                .ToList();
        }

        foreach (var troca in vencidas)
        {
            Remover(troca);
            saida.Encerrada(troca.A, Encerrada(troca, CodigosErroTroca.ConviteExpirado, troca.B));
        }
    }

    /// <summary>
    /// Os dois na mesma área, a até 10 m, e nenhum em combate. Devolve o motivo da recusa, ou
    /// nulo se pode trocar.
    /// </summary>
    private string? ChecarCondicoes(long a, long b)
    {
        var posicoes = obterPosicoes();
        if (posicoes != null)
        {
            if (!posicoes.TentarObter(a, out var pa) || !posicoes.TentarObter(b, out var pb))
                return CodigosErroTroca.AlvoNaoEncontrado;
            if (
                !string.Equals(pa.Area, pb.Area, StringComparison.Ordinal)
                || !RegrasTroca.DentroDaDistancia(pa.X, pa.Y, pa.Z, pb.X, pb.Y, pb.Z)
            )
                return CodigosErroTroca.LongeDemais;
        }

        var combate = obterCombate();
        if (combate != null && (combate.EmCombate(a) || combate.EmCombate(b)))
            return CodigosErroTroca.EmCombate;

        return null;
    }

    private void Recusar(long personagemId, long trocaId, string codigo) =>
        saida.Recusada(personagemId, new TrocaRecusada { TrocaId = trocaId, Codigo = codigo });

    private TrocaEncerrada Encerrada(Troca troca, string motivo, long outroId)
    {
        diretorio.TentarObterNome(outroId, out var nome);
        return new TrocaEncerrada
        {
            TrocaId = troca.Id,
            Motivo = motivo,
            NomeDoOutro = nome,
        };
    }

    private void EnviarEstadoAosDois(Troca troca)
    {
        saida.Estado(troca.A, EstadoPara(troca, troca.A));
        saida.Estado(troca.B, EstadoPara(troca, troca.B));
    }

    private EstadoDaTroca EstadoPara(Troca troca, long personagemId)
    {
        var souA = personagemId == troca.A;
        diretorio.TentarObterNome(troca.Outro(personagemId), out var nomeDoOutro);
        return new EstadoDaTroca
        {
            TrocaId = troca.Id,
            NomeDoOutro = nomeDoOutro,
            VersaoDaOferta = troca.Versao,
            Minha = souA ? troca.DescricaoA : troca.DescricaoB,
            Dele = souA ? troca.DescricaoB : troca.DescricaoA,
            MeuAceite = souA ? troca.AceiteA : troca.AceiteB,
            AceiteDele = souA ? troca.AceiteB : troca.AceiteA,
        };
    }

    private async Task EnviarBolsaAsync(long personagemId, long trocaId, CancellationToken c) =>
        saida.Bolsa(personagemId, await executor.BolsaAsync(personagemId, trocaId, c));
}
