using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Estado e regras dos grupos de jogadores, em memória (não vai ao banco; reiniciar o servidor
/// desfaz tudo). Não conhece rede: tudo que precisa dizer vai para <see cref="ISaidaDoGrupo"/>, e
/// "quem está online" é informado por <see cref="AoConectar"/> e <see cref="AoDesconectar"/>.
/// Regras em docs/features/social.md; números em <see cref="RegrasGrupo"/> (shared).
/// <para>
/// Thread-safe: uma trava só protege todo o estado, então as operações (de qualquer grupo) são
/// processadas uma de cada vez, na ordem em que pegam a trava. Só a consulta de nome ao banco
/// (<see cref="Convidar"/>) acontece fora dela. O tempo vem do relógio injetado, e
/// <see cref="Varrer"/> aplica o que o tempo venceu (convites de 60 s, vagas guardadas de 120 s);
/// toda operação também aplica isso antes de decidir.
/// </para>
/// </summary>
public sealed class GruposDoServidor(
    IDiretorioDePersonagens diretorio,
    ISaidaDoGrupo saida,
    Func<long> relogioMs,
    Func<long, long, bool>? contatoBarradoEntrePersonagens = null
) : IConsultaDeGrupo
{
    private readonly Lock _trava = new();
    private readonly HashSet<long> _online = [];
    private readonly Dictionary<long, string> _nomes = [];
    private readonly Dictionary<long, Grupo> _grupos = [];
    private readonly Dictionary<long, Grupo> _grupoDe = [];
    private readonly Dictionary<long, Convite> _convites = [];
    private readonly Dictionary<long, long> _ultimoConviteMs = [];
    private readonly List<Expulsao> _expulsoes = [];
    private long _proximoGrupo;
    private long _proximoConvite;

    public GruposDoServidor(IDiretorioDePersonagens diretorio, ISaidaDoGrupo saida)
        : this(diretorio, saida, () => Environment.TickCount64, null) { }

    // ── Consultas ───────────────────────────────────────────────────────────────

    /// <summary>A lista atual do grupo do personagem (vazia se ele não tem grupo).</summary>
    public ListaDoGrupo ListaDe(long personagemId)
    {
        lock (_trava)
        {
            return _grupoDe.TryGetValue(personagemId, out var grupo) ? Lista(grupo) : new();
        }
    }

    /// <summary>Consulta atomicamente os IDs atuais do grupo, ou null se o personagem está solo.</summary>
    public GrupoAtual? Consultar(long personagemId)
    {
        lock (_trava)
        {
            if (!_grupoDe.TryGetValue(personagemId, out var grupo))
            {
                return null;
            }

            var membros = Array.AsReadOnly(grupo.Membros.Select(m => m.PersonagemId).ToArray());
            return new GrupoAtual(grupo.Id, grupo.LiderId, membros);
        }
    }

    /// <summary>Quantos grupos existem agora.</summary>
    public int QuantidadeDeGrupos
    {
        get
        {
            lock (_trava)
            {
                return _grupos.Count;
            }
        }
    }

    /// <summary>Convites que ainda aguardam resposta (não conta os já encerrados).</summary>
    public int ConvitesPendentes
    {
        get
        {
            lock (_trava)
            {
                return _convites.Values.Count(c => c.Fim == null);
            }
        }
    }

    // ── Presença ────────────────────────────────────────────────────────────────

    /// <summary>
    /// O personagem ficou online (autenticou no tempo real). Se tem vaga guardada em um grupo,
    /// recebe o grupo de volta e os outros veem "voltou".
    /// </summary>
    public void AoConectar(long personagemId)
    {
        lock (_trava)
        {
            var agora = relogioMs();
            Manutencao(agora);
            if (!_online.Add(personagemId))
            {
                return;
            }

            if (!_grupoDe.TryGetValue(personagemId, out var grupo))
            {
                return;
            }

            var membro = grupo.Membros.First(m => m.PersonagemId == personagemId);
            membro.Online = true;
            AvisarMembros(
                grupo.Membros.Where(m => m.PersonagemId != personagemId),
                NovoAviso(TipoDeAvisoDoGrupo.MembroVoltou, membro)
            );
            EnviarLista(grupo);
        }
    }

    /// <summary>
    /// O personagem ficou offline (saiu ou caiu). A vaga fica guardada; convites para ele caem na
    /// hora e os dele caem agora (sem grupo) ou junto com a vaga.
    /// </summary>
    public void AoDesconectar(long personagemId)
    {
        lock (_trava)
        {
            var agora = relogioMs();
            Manutencao(agora);
            if (!_online.Remove(personagemId))
            {
                return;
            }

            foreach (var convite in Pendentes().Where(c => c.Para == personagemId).ToList())
            {
                Cancelar(convite);
            }

            if (!_grupoDe.TryGetValue(personagemId, out var grupo))
            {
                foreach (var convite in Pendentes().Where(c => c.De == personagemId).ToList())
                {
                    Encerrar(convite, MotivoFim.ConvidanteOffline, agora);
                }

                return;
            }

            var membro = grupo.Membros.First(m => m.PersonagemId == personagemId);
            membro.Online = false;
            membro.OfflineDesdeMs = agora;
            AvisarMembros(grupo.Membros, NovoAviso(TipoDeAvisoDoGrupo.MembroOffline, membro));
            EnviarLista(grupo);
        }
    }

    /// <summary>
    /// Aplica o que o tempo venceu: convites que passaram de 60 s e vagas guardadas que passaram
    /// de 120 s. O servidor chama de tempos em tempos; as operações também chamam.
    /// </summary>
    public void Varrer()
    {
        lock (_trava)
        {
            Manutencao(relogioMs());
        }
    }

    // ── Operações dos jogadores ─────────────────────────────────────────────────

    /// <summary>
    /// Convida o personagem de nome <paramref name="nome"/>. Quem convida vem da sessão;
    /// só o nome vem do client.
    /// </summary>
    public void Convidar(long de, string? nome)
    {
        // Etapa 1 (rápida): quem não pode convidar nem deve gastar uma consulta ao banco.
        lock (_trava)
        {
            var agora = relogioMs();
            Manutencao(agora);
            if (_grupoDe.TryGetValue(de, out var g) && g.LiderId != de)
            {
                Recusar(de, CodigosErroSocial.NaoELider);
                return;
            }

            if (
                _ultimoConviteMs.TryGetValue(de, out var ultimo)
                && RegrasGrupo.ConviteRapidoDemais((agora - ultimo) / 1000.0)
            )
            {
                Recusar(de, CodigosErroSocial.ConvitesRapidosDemais);
                return;
            }

            _ultimoConviteMs[de] = agora;
        }

        // Banco fora da trava: leituras curtas, sem segurar os outros grupos.
        var nomeDe = NomeDe(de);
        var achou = diretorio.TentarObterPorNome(nome ?? "", out var alvo);
        var nomeAlvo = achou ? NomeDe(alvo) : "";

        // Etapa 2: o estado pode ter mudado durante a consulta, então valida tudo de novo.
        lock (_trava)
        {
            var agora = relogioMs();
            Manutencao(agora);
            if (!_online.Contains(de))
            {
                return;
            }

            if (!achou)
            {
                Recusar(de, CodigosErroSocial.AlvoNaoEncontrado);
                return;
            }

            if (alvo == de)
            {
                Recusar(de, CodigosErroSocial.ConviteParaSiMesmo);
                return;
            }

            if (contatoBarradoEntrePersonagens?.Invoke(de, alvo) == true)
            {
                Recusar(de, CodigosErroSocial.AlvoNaoEncontrado);
                return;
            }

            if (!_online.Contains(alvo))
            {
                Recusar(de, CodigosErroSocial.AlvoNaoEncontrado);
                return;
            }

            _grupoDe.TryGetValue(de, out var grupo);
            if (grupo != null && grupo.LiderId != de)
            {
                Recusar(de, CodigosErroSocial.NaoELider);
                return;
            }

            if (_grupoDe.ContainsKey(alvo))
            {
                Recusar(de, CodigosErroSocial.AlvoJaEmGrupo);
                return;
            }

            if (ReconviteBloqueado(de, grupo, alvo, agora))
            {
                Recusar(de, CodigosErroSocial.ReconviteBloqueado);
                return;
            }

            var pendentes = Pendentes().ToList();
            if (pendentes.Any(c => c.De == de && c.Para == alvo))
            {
                Recusar(de, CodigosErroSocial.ConviteJaPendente);
                return;
            }

            var membros = grupo?.Membros.Count ?? 1;
            if (!RegrasGrupo.CabeMaisUmConvite(membros, pendentes.Count(c => c.De == de)))
            {
                Recusar(de, CodigosErroSocial.GrupoCheio);
                return;
            }

            if (RegrasGrupo.AlvoComConvitesDemais(pendentes.Count(c => c.Para == alvo)))
            {
                Recusar(de, CodigosErroSocial.AlvoComConvitesDemais);
                return;
            }

            var convite = new Convite
            {
                Id = ++_proximoConvite,
                De = de,
                Para = alvo,
                NomeDe = nomeDe,
                NomePara = nomeAlvo,
                CriadoMs = agora,
            };
            _convites[convite.Id] = convite;
            EnviarSePossivel(
                alvo,
                () =>
                    saida.Convite(
                        alvo,
                        new ConviteRecebido
                        {
                            ConviteId = convite.Id,
                            NomeDoConvidante = nomeDe,
                            ValidadeSegundos = RegrasGrupo.ValidadeRestanteSegundos(0),
                        }
                    )
            );
        }
    }

    /// <summary>Aceita ou recusa um convite. Só o alvo do convite pode responder.</summary>
    public void Responder(long quem, long conviteId, bool aceitar)
    {
        lock (_trava)
        {
            var agora = relogioMs();
            Manutencao(agora);
            if (!_convites.TryGetValue(conviteId, out var convite) || convite.Para != quem)
            {
                Recusar(quem, CodigosErroSocial.ConviteNaoEncontrado);
                return;
            }

            if (convite.Fim is { } fim)
            {
                // Convite que já tinha acabado: responde o porquê e esquece.
                _convites.Remove(conviteId);
                Recusar(
                    quem,
                    !aceitar ? CodigosErroSocial.ConviteNaoEncontrado
                        : fim == MotivoFim.Expirado ? CodigosErroSocial.ConviteExpirado
                        : fim == MotivoFim.ConvidanteOffline ? CodigosErroSocial.ConvidanteOffline
                        : CodigosErroSocial.AlvoJaEmGrupo
                );
                return;
            }

            if (!aceitar)
            {
                _convites.Remove(conviteId);
                Avisar(
                    [convite.De],
                    new AvisoDoGrupo
                    {
                        Tipo = TipoDeAvisoDoGrupo.ConviteRecusado,
                        PersonagemId = convite.Para,
                        Nome = convite.NomePara,
                    }
                );
                return;
            }

            Aceitar(convite, agora);
        }
    }

    /// <summary>Sai do grupo em que está (qualquer membro, inclusive o líder).</summary>
    public void Sair(long quem)
    {
        lock (_trava)
        {
            Manutencao(relogioMs());
            if (!_grupoDe.TryGetValue(quem, out var grupo))
            {
                Recusar(quem, CodigosErroSocial.NaoEstaEmGrupo);
                return;
            }

            Desligar(grupo, Membro(grupo, quem)!, FormaDeSair.Saiu);
        }
    }

    /// <summary>O líder expulsa um membro (que pode estar offline com a vaga guardada).</summary>
    public void Expulsar(long lider, long alvo)
    {
        lock (_trava)
        {
            var agora = relogioMs();
            Manutencao(agora);
            if (!_grupoDe.TryGetValue(lider, out var grupo))
            {
                Recusar(lider, CodigosErroSocial.NaoEstaEmGrupo);
                return;
            }

            if (grupo.LiderId != lider)
            {
                Recusar(lider, CodigosErroSocial.NaoELider);
                return;
            }

            if (alvo == lider)
            {
                Recusar(lider, CodigosErroSocial.NaoPodeExpulsarLider);
                return;
            }

            var membro = Membro(grupo, alvo);
            if (membro == null)
            {
                Recusar(lider, CodigosErroSocial.AlvoNaoEstaNoGrupo);
                return;
            }

            _expulsoes.Add(new Expulsao(grupo.Id, lider, alvo, agora));
            Desligar(grupo, membro, FormaDeSair.Expulso);
        }
    }

    /// <summary>O líder passa a liderança a outro membro online.</summary>
    public void PassarLideranca(long lider, long alvo)
    {
        lock (_trava)
        {
            Manutencao(relogioMs());
            if (!_grupoDe.TryGetValue(lider, out var grupo))
            {
                Recusar(lider, CodigosErroSocial.NaoEstaEmGrupo);
                return;
            }

            if (grupo.LiderId != lider)
            {
                Recusar(lider, CodigosErroSocial.NaoELider);
                return;
            }

            if (alvo == lider)
            {
                Recusar(lider, CodigosErroSocial.AlvoInvalido);
                return;
            }

            var novo = Membro(grupo, alvo);
            if (novo == null)
            {
                Recusar(lider, CodigosErroSocial.AlvoNaoEstaNoGrupo);
                return;
            }

            if (!novo.Online)
            {
                Recusar(lider, CodigosErroSocial.AlvoInvalido);
                return;
            }

            grupo.LiderId = alvo;
            CancelarConvitesDe(lider);
            AvisarMembros(grupo.Membros, NovoAviso(TipoDeAvisoDoGrupo.LiderMudou, novo));
            EnviarLista(grupo);
        }
    }

    // ── Dentro da trava ─────────────────────────────────────────────────────────

    private void Aceitar(Convite convite, long agora)
    {
        var quem = convite.Para;
        if (_grupoDe.ContainsKey(quem))
        {
            Recusar(quem, CodigosErroSocial.AlvoJaEmGrupo);
            return;
        }

        if (!_online.Contains(convite.De))
        {
            Recusar(quem, CodigosErroSocial.ConvidanteOffline);
            return;
        }

        _grupoDe.TryGetValue(convite.De, out var grupo);
        if (grupo != null && grupo.LiderId != convite.De)
        {
            // Não deveria acontecer (convites de quem deixa a liderança caem), mas não confia.
            _convites.Remove(convite.Id);
            Recusar(quem, CodigosErroSocial.ConviteNaoEncontrado);
            return;
        }

        if (!RegrasGrupo.CabeMaisUmMembro(grupo?.Membros.Count ?? 1))
        {
            Recusar(quem, CodigosErroSocial.GrupoCheio);
            return;
        }

        _convites.Remove(convite.Id);
        if (grupo == null)
        {
            grupo = new Grupo { Id = ++_proximoGrupo, LiderId = convite.De };
            grupo.Membros.Add(
                new MembroEmGrupo
                {
                    PersonagemId = convite.De,
                    Nome = convite.NomeDe,
                    Online = true,
                }
            );
            _grupos[grupo.Id] = grupo;
            _grupoDe[convite.De] = grupo;
        }

        var novo = new MembroEmGrupo
        {
            PersonagemId = quem,
            Nome = convite.NomePara,
            Online = true,
        };
        grupo.Membros.Add(novo);
        _grupoDe[quem] = grupo;

        // Quem entra num grupo deixa de poder aceitar os outros convites e de convidar.
        foreach (var outro in Pendentes().Where(c => c.Para == quem).ToList())
        {
            Encerrar(outro, MotivoFim.AlvoJaEmGrupo, agora);
        }

        CancelarConvitesDe(quem);

        AvisarMembros(
            grupo.Membros.Where(m => m.PersonagemId != quem),
            NovoAviso(TipoDeAvisoDoGrupo.EntrouNoGrupo, novo)
        );
        EnviarLista(grupo);
    }

    /// <summary>Tira um membro (saiu, foi expulso ou perdeu a vaga) e arruma o que sobrou.</summary>
    private void Desligar(Grupo grupo, MembroEmGrupo saiu, FormaDeSair motivo)
    {
        var eraLider = saiu.PersonagemId == grupo.LiderId;
        grupo.Membros.Remove(saiu);
        _grupoDe.Remove(saiu.PersonagemId);
        if (eraLider)
        {
            CancelarConvitesDe(saiu.PersonagemId);
        }

        var aviso = NovoAviso(
            motivo == FormaDeSair.Expulso
                ? TipoDeAvisoDoGrupo.ExpulsoDoGrupo
                : TipoDeAvisoDoGrupo.SaiuDoGrupo,
            saiu
        );
        AvisarMembros(grupo.Membros, aviso);
        if (motivo == FormaDeSair.Expulso)
        {
            Avisar([saiu.PersonagemId], aviso);
        }

        EnviarListaVazia(saiu.PersonagemId);

        if (RegrasGrupo.DeveDesfazer(grupo.Membros.Count))
        {
            Desfazer(grupo);
            return;
        }

        if (eraLider)
        {
            var online = grupo.Membros.Select(m => m.Online).ToList();
            var herdeiro = grupo.Membros[RegrasGrupo.PosicaoDoHerdeiro(online, -1)];
            grupo.LiderId = herdeiro.PersonagemId;
            AvisarMembros(grupo.Membros, NovoAviso(TipoDeAvisoDoGrupo.LiderMudou, herdeiro));
        }

        EnviarLista(grupo);
    }

    /// <summary>O grupo ficou com gente de menos: quem sobrou volta a ficar sem grupo.</summary>
    private void Desfazer(Grupo grupo)
    {
        _grupos.Remove(grupo.Id);
        foreach (var ultimo in grupo.Membros)
        {
            _grupoDe.Remove(ultimo.PersonagemId);
            CancelarConvitesDe(ultimo.PersonagemId);
            Avisar([ultimo.PersonagemId], NovoAviso(TipoDeAvisoDoGrupo.GrupoDesfeito, ultimo));
            EnviarListaVazia(ultimo.PersonagemId);
        }

        grupo.Membros.Clear();
    }

    /// <summary>Aplica o tempo: expira convites e remove vagas guardadas vencidas.</summary>
    private void Manutencao(long agora)
    {
        foreach (var convite in Pendentes().ToList())
        {
            if (RegrasGrupo.ConviteExpirou((agora - convite.CriadoMs) / 1000.0))
            {
                Encerrar(convite, MotivoFim.Expirado, agora);
            }
        }

        var retencaoMs = ConfigGrupoServidor.RetencaoDeConviteEncerradoSegundos * 1000L;
        foreach (
            var convite in _convites
                .Values.Where(c => c.Fim != null && agora - c.FimMs >= retencaoMs)
                .ToList()
        )
        {
            _convites.Remove(convite.Id);
        }

        foreach (var grupo in _grupos.Values.ToList())
        {
            foreach (var membro in grupo.Membros.ToList())
            {
                if (
                    !membro.Online
                    && _grupos.ContainsKey(grupo.Id)
                    && RegrasGrupo.VagaGuardadaVenceu((agora - membro.OfflineDesdeMs) / 1000.0)
                )
                {
                    Desligar(grupo, membro, FormaDeSair.VagaVenceu);
                }
            }
        }

        _expulsoes.RemoveAll(e => !RegrasGrupo.ReconviteBloqueado((agora - e.EmMs) / 1000.0));
        var ultimoMs = ConfigGrupoServidor.RetencaoDoUltimoConviteSegundos * 1000L;
        foreach (
            var velho in _ultimoConviteMs
                .Where(p => agora - p.Value >= ultimoMs)
                .Select(p => p.Key)
                .ToList()
        )
        {
            _ultimoConviteMs.Remove(velho);
        }
    }

    private bool ReconviteBloqueado(long de, Grupo? grupo, long alvo, long agora) =>
        _expulsoes.Any(e =>
            e.AlvoId == alvo
            && (e.LiderId == de || (grupo != null && e.GrupoId == grupo.Id))
            && RegrasGrupo.ReconviteBloqueado((agora - e.EmMs) / 1000.0)
        );

    private IEnumerable<Convite> Pendentes() => _convites.Values.Where(c => c.Fim == null);

    /// <summary>Cancela os convites que o personagem tinha feito (ex.: deixou de ser líder).</summary>
    private void CancelarConvitesDe(long de)
    {
        foreach (var convite in Pendentes().Where(c => c.De == de).ToList())
        {
            Cancelar(convite);
        }
    }

    /// <summary>Convite que some sem deixar rastro: ninguém mais consegue responder.</summary>
    private void Cancelar(Convite convite)
    {
        _convites.Remove(convite.Id);
        AvisarConviteEncerrado(convite);
    }

    /// <summary>
    /// Convite que acabou, mas fica lembrado por um tempo para o alvo receber o motivo certo se
    /// ainda tentar aceitar.
    /// </summary>
    private void Encerrar(Convite convite, MotivoFim motivo, long agora)
    {
        convite.Fim = motivo;
        convite.FimMs = agora;
        AvisarConviteEncerrado(convite);
    }

    private void AvisarConviteEncerrado(Convite convite)
    {
        Avisar(
            [convite.Para],
            new AvisoDoGrupo
            {
                Tipo = TipoDeAvisoDoGrupo.ConviteExpirou,
                PersonagemId = convite.De,
                Nome = convite.NomeDe,
            }
        );
        Avisar(
            [convite.De],
            new AvisoDoGrupo
            {
                Tipo = TipoDeAvisoDoGrupo.ConviteExpirou,
                PersonagemId = convite.Para,
                Nome = convite.NomePara,
            }
        );
    }

    // ── Envio (só para quem está online) ────────────────────────────────────────

    private void Recusar(long personagemId, string codigo) =>
        EnviarSePossivel(personagemId, () => saida.Recusada(personagemId, codigo));

    private void AvisarMembros(IEnumerable<MembroEmGrupo> destinos, AvisoDoGrupo aviso) =>
        Avisar(destinos.Select(m => m.PersonagemId).ToList(), aviso);

    private void Avisar(IEnumerable<long> destinos, AvisoDoGrupo aviso)
    {
        foreach (var id in destinos)
        {
            EnviarSePossivel(id, () => saida.Aviso(id, aviso));
        }
    }

    private void EnviarLista(Grupo grupo)
    {
        var lista = Lista(grupo);
        foreach (var membro in grupo.Membros)
        {
            EnviarSePossivel(membro.PersonagemId, () => saida.Lista(membro.PersonagemId, lista));
        }
    }

    private void EnviarListaVazia(long personagemId) =>
        EnviarSePossivel(personagemId, () => saida.Lista(personagemId, new ListaDoGrupo()));

    private void EnviarSePossivel(long personagemId, Action enviar)
    {
        if (_online.Contains(personagemId))
        {
            enviar();
        }
    }

    // ── Pequenos ajudantes ──────────────────────────────────────────────────────

    private static ListaDoGrupo Lista(Grupo grupo) =>
        new()
        {
            GrupoId = grupo.Id,
            LiderId = grupo.LiderId,
            Membros = grupo
                .Membros.Select(m => new MembroDoGrupo
                {
                    PersonagemId = m.PersonagemId,
                    Nome = m.Nome,
                    Online = m.Online,
                })
                .ToArray(),
        };

    private static AvisoDoGrupo NovoAviso(TipoDeAvisoDoGrupo tipo, MembroEmGrupo membro) =>
        new()
        {
            Tipo = tipo,
            PersonagemId = membro.PersonagemId,
            Nome = membro.Nome,
        };

    private static MembroEmGrupo? Membro(Grupo grupo, long personagemId) =>
        grupo.Membros.FirstOrDefault(m => m.PersonagemId == personagemId);

    /// <summary>O nome do personagem, lembrado depois da primeira consulta (nome não muda).</summary>
    private string NomeDe(long personagemId)
    {
        lock (_trava)
        {
            if (_nomes.TryGetValue(personagemId, out var conhecido))
            {
                return conhecido;
            }
        }

        if (!diretorio.TentarObterNome(personagemId, out var nome))
        {
            return "";
        }

        lock (_trava)
        {
            _nomes[personagemId] = nome;
        }

        return nome;
    }

    // ── Tipos internos ──────────────────────────────────────────────────────────

    private enum FormaDeSair
    {
        Saiu,
        Expulso,
        VagaVenceu,
    }

    /// <summary>Por que um convite acabou sem resposta (define o código se o alvo ainda aceitar).</summary>
    private enum MotivoFim
    {
        Expirado,
        ConvidanteOffline,
        AlvoJaEmGrupo,
    }

    private sealed class Grupo
    {
        public long Id;
        public long LiderId;
        public List<MembroEmGrupo> Membros = [];
    }

    private sealed class MembroEmGrupo
    {
        public long PersonagemId;
        public string Nome = "";
        public bool Online;
        public long OfflineDesdeMs;
    }

    private sealed class Convite
    {
        public long Id;
        public long De;
        public long Para;
        public string NomeDe = "";
        public string NomePara = "";
        public long CriadoMs;
        public MotivoFim? Fim;
        public long FimMs;
    }

    private sealed record Expulsao(long GrupoId, long LiderId, long AlvoId, long EmMs);
}
