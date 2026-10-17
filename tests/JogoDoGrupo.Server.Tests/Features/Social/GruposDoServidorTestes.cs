using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Shared.Features.Social;
using static JogoDoGrupo.Server.Tests.Features.Social.AmbienteDeGrupo;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>Regras do grupo (caminho feliz, erros e limites), com relógio e saída falsos.</summary>
public class GruposDoServidorTestes
{
    private readonly AmbienteDeGrupo _a = new();

    // ── Caminho feliz ───────────────────────────────────────────────────────────

    [Fact]
    public void Convidar_e_aceitar_cria_o_grupo_com_o_convidante_lider()
    {
        _a.Avancar(1);
        _a.Grupos.Convidar(A, "Bia");
        var convite = Assert.Single(_a.Saida.De<ConviteRecebido>(B));
        Assert.Equal("Ana", convite.NomeDoConvidante);
        Assert.Equal(RegrasGrupo.ValidadeDoConviteSegundos, convite.ValidadeSegundos);
        Assert.Equal(0, _a.Grupos.QuantidadeDeGrupos);

        _a.Saida.Limpar();
        _a.Grupos.Responder(B, convite.ConviteId, true);

        foreach (var quem in new[] { A, B })
        {
            var lista = _a.Saida.UltimaLista(quem);
            Assert.Equal(A, lista.LiderId);
            Assert.NotEqual(0, lista.GrupoId);
            Assert.Equal([A, B], lista.Membros.Select(m => m.PersonagemId));
            Assert.All(lista.Membros, m => Assert.True(m.Online));
            Assert.Equal(["Ana", "Bia"], lista.Membros.Select(m => m.Nome));
        }

        var entrou = Assert.Single(_a.Saida.Avisos(A, TipoDeAvisoDoGrupo.EntrouNoGrupo));
        Assert.Equal(B, entrou.PersonagemId);
        Assert.Equal("Bia", entrou.Nome);
        Assert.Equal(1, _a.Grupos.QuantidadeDeGrupos);
        Assert.Equal(0, _a.Grupos.ConvitesPendentes);
    }

    [Fact]
    public void Recusar_avisa_o_convidante_e_nao_cria_grupo()
    {
        var id = _a.Convidar(A, B);
        _a.Saida.Limpar();

        _a.Grupos.Responder(B, id, false);

        var aviso = Assert.Single(_a.Saida.Avisos(A, TipoDeAvisoDoGrupo.ConviteRecusado));
        Assert.Equal(B, aviso.PersonagemId);
        Assert.Equal("Bia", aviso.Nome);
        Assert.Equal(0, _a.Grupos.QuantidadeDeGrupos);
        Assert.Equal(0, _a.Grupos.ConvitesPendentes);
        Assert.Empty(_a.Grupos.ListaDe(B).Membros);
    }

    [Fact]
    public void Lider_convida_ate_cinco_e_todos_recebem_a_lista_a_cada_entrada()
    {
        var esperados = new List<long> { A };
        foreach (var novo in new[] { B, C, D, E })
        {
            _a.Juntar(A, novo);
            esperados.Add(novo);
            foreach (var quem in esperados)
            {
                Assert.Equal(
                    esperados,
                    _a.Saida.UltimaLista(quem).Membros.Select(m => m.PersonagemId)
                );
            }
        }

        Assert.Equal(1, _a.Grupos.QuantidadeDeGrupos);
        Assert.Equal(A, _a.Grupos.ListaDe(E).LiderId);
    }

    [Fact]
    public void Membro_sai_e_os_outros_recebem_aviso_e_lista_nova()
    {
        _a.FormarGrupo(A, B, C);

        _a.Grupos.Sair(B);

        Assert.Equal([A, C], _a.Ids(A));
        Assert.Empty(_a.Saida.UltimaLista(B).Membros);
        foreach (var quem in new[] { A, C })
        {
            var aviso = Assert.Single(_a.Saida.Avisos(quem, TipoDeAvisoDoGrupo.SaiuDoGrupo));
            Assert.Equal(B, aviso.PersonagemId);
            Assert.Equal([A, C], _a.Saida.UltimaLista(quem).Membros.Select(m => m.PersonagemId));
        }

        Assert.Equal(A, _a.Grupos.ListaDe(A).LiderId);
    }

    [Fact]
    public void Lider_sai_e_a_lideranca_vai_para_o_mais_antigo_online()
    {
        _a.FormarGrupo(A, B, C, D);
        _a.Grupos.AoDesconectar(B);
        _a.Saida.Limpar();

        _a.Grupos.Sair(A);

        var lista = _a.Grupos.ListaDe(C);
        Assert.Equal(C, lista.LiderId);
        Assert.Equal([B, C, D], lista.Membros.Select(m => m.PersonagemId));
        var aviso = Assert.Single(_a.Saida.Avisos(D, TipoDeAvisoDoGrupo.LiderMudou));
        Assert.Equal(C, aviso.PersonagemId);
    }

    [Fact]
    public void Lider_sai_e_sem_ninguem_online_a_lideranca_vai_para_o_mais_antigo_offline()
    {
        _a.FormarGrupo(A, B, C);
        _a.Grupos.AoDesconectar(B);
        _a.Grupos.AoDesconectar(C);

        _a.Grupos.Sair(A);

        // Sobraram B e C offline: o grupo continua (2 membros) e B, o mais antigo, lidera.
        var lista = _a.Grupos.ListaDe(B);
        Assert.Equal(B, lista.LiderId);
        Assert.Equal([B, C], lista.Membros.Select(m => m.PersonagemId));
    }

    [Fact]
    public void Lider_passa_a_lideranca_e_os_convites_dele_caem()
    {
        _a.FormarGrupo(A, B, C);
        var pendente = _a.Convidar(A, D);
        _a.Saida.Limpar();

        _a.Grupos.PassarLideranca(A, B);

        foreach (var quem in new[] { A, B, C })
        {
            Assert.Equal(B, _a.Saida.UltimaLista(quem).LiderId);
        }

        var aviso = Assert.Single(_a.Saida.Avisos(C, TipoDeAvisoDoGrupo.LiderMudou));
        Assert.Equal(B, aviso.PersonagemId);
        Assert.Contains(
            _a.Saida.Avisos(D, TipoDeAvisoDoGrupo.ConviteExpirou),
            a => a.PersonagemId == A
        );

        _a.Saida.Limpar();
        _a.Grupos.Responder(D, pendente, true);
        _a.AssertSoRecusa(D, CodigosErroSocial.ConviteNaoEncontrado);
    }

    [Fact]
    public void Lider_expulsa_e_o_expulso_recebe_o_aviso_e_a_lista_vazia()
    {
        _a.FormarGrupo(A, B, C);

        _a.Grupos.Expulsar(A, C);

        var aviso = Assert.Single(_a.Saida.Avisos(C, TipoDeAvisoDoGrupo.ExpulsoDoGrupo));
        Assert.Equal(C, aviso.PersonagemId);
        Assert.Empty(_a.Saida.UltimaLista(C).Membros);
        foreach (var quem in new[] { A, B })
        {
            Assert.Contains(
                _a.Saida.Avisos(quem, TipoDeAvisoDoGrupo.ExpulsoDoGrupo),
                a => a.PersonagemId == C
            );
            Assert.Equal([A, B], _a.Saida.UltimaLista(quem).Membros.Select(m => m.PersonagemId));
        }
    }

    [Fact]
    public void Quem_sai_do_jogo_fica_offline_na_lista_e_ao_voltar_recebe_o_grupo()
    {
        _a.FormarGrupo(A, B, C);

        _a.Grupos.AoDesconectar(B);

        Assert.Contains(
            _a.Saida.Avisos(A, TipoDeAvisoDoGrupo.MembroOffline),
            x => x.PersonagemId == B
        );
        var offline = _a.Saida.UltimaLista(A).Membros.Single(m => m.PersonagemId == B);
        Assert.False(offline.Online);
        Assert.Empty(_a.Saida.De<ListaDoGrupo>(B));

        _a.Avancar(RegrasGrupo.VagaGuardadaSegundos - 1);
        _a.Saida.Limpar();
        _a.Grupos.AoConectar(B);

        var lista = _a.Saida.UltimaLista(B);
        Assert.Equal([A, B, C], lista.Membros.Select(m => m.PersonagemId));
        Assert.All(lista.Membros, m => Assert.True(m.Online));
        Assert.Contains(
            _a.Saida.Avisos(A, TipoDeAvisoDoGrupo.MembroVoltou),
            x => x.PersonagemId == B
        );
        Assert.All(
            new[] { A, C },
            quem => Assert.All(_a.Saida.UltimaLista(quem).Membros, m => Assert.True(m.Online))
        );
    }

    [Fact]
    public void Lider_que_cai_e_volta_a_tempo_continua_lider()
    {
        _a.FormarGrupo(A, B);
        _a.FormarGrupo(A, C);
        _a.Grupos.AoDesconectar(A);
        _a.Avancar(RegrasGrupo.VagaGuardadaSegundos - 1);

        _a.Grupos.AoConectar(A);

        Assert.Equal(A, _a.Grupos.ListaDe(B).LiderId);
    }

    [Fact]
    public void Lider_que_fica_offline_alem_da_vaga_perde_a_vaga_e_a_lideranca_passa()
    {
        _a.FormarGrupo(A, B, C);
        _a.Grupos.AoDesconectar(A);
        _a.Avancar(RegrasGrupo.VagaGuardadaSegundos + 1);
        _a.Saida.Limpar();

        _a.Grupos.Varrer();

        var lista = _a.Grupos.ListaDe(B);
        Assert.Equal(B, lista.LiderId);
        Assert.Equal([B, C], lista.Membros.Select(m => m.PersonagemId));
        Assert.Contains(
            _a.Saida.Avisos(C, TipoDeAvisoDoGrupo.SaiuDoGrupo),
            x => x.PersonagemId == A
        );
        Assert.Contains(
            _a.Saida.Avisos(C, TipoDeAvisoDoGrupo.LiderMudou),
            x => x.PersonagemId == B
        );
    }

    [Fact]
    public void Convites_para_quem_desconecta_caem_na_hora()
    {
        var id = _a.Convidar(A, B);
        _a.Saida.Limpar();

        _a.Grupos.AoDesconectar(B);
        _a.Grupos.AoConectar(B);

        Assert.Contains(
            _a.Saida.Avisos(A, TipoDeAvisoDoGrupo.ConviteExpirou),
            x => x.PersonagemId == B
        );
        Assert.Equal(0, _a.Grupos.ConvitesPendentes);
        _a.Saida.Limpar();
        _a.Grupos.Responder(B, id, true);
        _a.AssertSoRecusa(B, CodigosErroSocial.ConviteNaoEncontrado);
    }

    [Fact]
    public void Quem_entra_num_grupo_perde_os_outros_convites_e_os_que_fez()
    {
        var deA = _a.Convidar(A, C);
        _a.Convidar(B, C);
        _a.Convidar(C, D);
        _a.Saida.Limpar();

        _a.Grupos.Responder(C, deA, true);

        Assert.Equal(0, _a.Grupos.ConvitesPendentes);
        Assert.Contains(
            _a.Saida.Avisos(B, TipoDeAvisoDoGrupo.ConviteExpirou),
            x => x.PersonagemId == C
        );
        Assert.Contains(
            _a.Saida.Avisos(D, TipoDeAvisoDoGrupo.ConviteExpirou),
            x => x.PersonagemId == C
        );
    }

    [Fact]
    public void Expirar_convite_avisa_os_dois_lados_na_varredura()
    {
        _a.Convidar(A, B);
        _a.Saida.Limpar();
        _a.Avancar(RegrasGrupo.ValidadeDoConviteSegundos);

        _a.Grupos.Varrer();

        Assert.Contains(
            _a.Saida.Avisos(A, TipoDeAvisoDoGrupo.ConviteExpirou),
            x => x.PersonagemId == B
        );
        Assert.Contains(
            _a.Saida.Avisos(B, TipoDeAvisoDoGrupo.ConviteExpirou),
            x => x.PersonagemId == A
        );
        Assert.Equal(0, _a.Grupos.ConvitesPendentes);
    }

    // ── Erros: um teste por código; nada muda e só quem pediu é avisado ─────────

    [Fact]
    public void AlvoNaoEncontrado_nome_inexistente()
    {
        var antes = _a.Foto();
        _a.Avancar(1);
        _a.Grupos.Convidar(A, "Ninguem");
        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoNaoEncontrado);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void AlvoNaoEncontrado_nome_nulo_ou_vazio()
    {
        _a.Avancar(1);
        _a.Grupos.Convidar(A, null);
        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoNaoEncontrado);
    }

    [Fact]
    public void AlvoNaoEncontrado_personagem_offline_com_a_mesma_resposta()
    {
        _a.Grupos.AoDesconectar(B);
        _a.Saida.Limpar();
        var antes = _a.Foto();
        _a.Avancar(1);

        _a.Grupos.Convidar(A, "Bia");

        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoNaoEncontrado);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void AlvoJaEmGrupo_em_outro_grupo()
    {
        _a.FormarGrupo(B, C);
        var antes = _a.Foto();
        _a.Avancar(1);

        _a.Grupos.Convidar(A, "Bia");

        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoJaEmGrupo);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void AlvoJaEmGrupo_no_proprio_grupo_do_convidante()
    {
        _a.FormarGrupo(A, B);
        var antes = _a.Foto();
        _a.Avancar(1);

        _a.Grupos.Convidar(A, "Bia");

        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoJaEmGrupo);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void ConviteParaSiMesmo_mesmo_com_maiusculas_diferentes()
    {
        var antes = _a.Foto();
        _a.Avancar(1);

        _a.Grupos.Convidar(A, "ANA");

        _a.AssertSoRecusa(A, CodigosErroSocial.ConviteParaSiMesmo);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void GrupoCheio_com_cinco_membros()
    {
        _a.FormarGrupo(A, B, C, D, E);
        var antes = _a.Foto();
        _a.Avancar(1);

        _a.Grupos.Convidar(A, "Fabi");

        _a.AssertSoRecusa(A, CodigosErroSocial.GrupoCheio);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void GrupoCheio_conta_as_vagas_ja_prometidas_em_convites()
    {
        _a.FormarGrupo(A, B, C, D);
        _a.Convidar(A, E);
        var antes = _a.Foto();
        _a.Avancar(1);
        _a.Saida.Limpar();

        _a.Grupos.Convidar(A, "Fabi");

        _a.AssertSoRecusa(A, CodigosErroSocial.GrupoCheio);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void GrupoCheio_quem_nao_tem_grupo_so_faz_quatro_convites_pendentes()
    {
        foreach (var alvo in new[] { B, C, D, E })
        {
            _a.Convidar(A, alvo);
        }

        _a.Avancar(1);
        _a.Saida.Limpar();
        var antes = _a.Foto();

        _a.Grupos.Convidar(A, "Fabi");

        _a.AssertSoRecusa(A, CodigosErroSocial.GrupoCheio);
        Assert.Equal(antes, _a.Foto());
        Assert.Equal(RegrasGrupo.MaximoDeConvitesPorConvidante, _a.Grupos.ConvitesPendentes);
    }

    [Fact]
    public void NaoELider_membro_comum_nao_convida()
    {
        _a.FormarGrupo(A, B);
        var antes = _a.Foto();
        _a.Avancar(1);

        _a.Grupos.Convidar(B, "Caio");

        _a.AssertSoRecusa(B, CodigosErroSocial.NaoELider);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void NaoELider_membro_comum_nao_expulsa()
    {
        _a.FormarGrupo(A, B, C);
        var antes = _a.Foto();

        _a.Grupos.Expulsar(B, C);

        _a.AssertSoRecusa(B, CodigosErroSocial.NaoELider);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void NaoELider_membro_comum_nao_passa_a_lideranca()
    {
        _a.FormarGrupo(A, B, C);
        var antes = _a.Foto();

        _a.Grupos.PassarLideranca(B, C);

        _a.AssertSoRecusa(B, CodigosErroSocial.NaoELider);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void ConviteJaPendente_mesmo_convidante_e_mesmo_alvo()
    {
        _a.Convidar(A, B);
        _a.Avancar(1);
        _a.Saida.Limpar();
        var antes = _a.Foto();

        _a.Grupos.Convidar(A, "Bia");

        _a.AssertSoRecusa(A, CodigosErroSocial.ConviteJaPendente);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void AlvoComConvitesDemais_com_cinco_pendentes_de_convidantes_diferentes()
    {
        foreach (var de in new[] { A, B, C, D, E })
        {
            _a.Convidar(de, F);
        }

        Assert.Equal(RegrasGrupo.MaximoDeConvitesPorAlvo, _a.Grupos.ConvitesPendentes);
        _a.Saida.Limpar();
        var antes = _a.Foto();
        _a.Avancar(1);

        _a.Grupos.Convidar(G, "Fabi");

        _a.AssertSoRecusa(G, CodigosErroSocial.AlvoComConvitesDemais);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void ConvitesRapidosDemais_dois_convites_em_menos_de_um_segundo()
    {
        _a.Avancar(1);
        _a.Grupos.Convidar(A, "Bia");
        _a.Saida.Limpar();
        var antes = _a.Foto();
        _a.Avancar(0.5);

        _a.Grupos.Convidar(A, "Caio");

        _a.AssertSoRecusa(A, CodigosErroSocial.ConvitesRapidosDemais);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void ConviteNaoEncontrado_id_que_nao_existe()
    {
        var antes = _a.Foto();

        _a.Grupos.Responder(B, 999, true);

        _a.AssertSoRecusa(B, CodigosErroSocial.ConviteNaoEncontrado);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void ConviteNaoEncontrado_responder_convite_de_outra_pessoa()
    {
        var id = _a.Convidar(A, B);
        _a.Saida.Limpar();
        var antes = _a.Foto();

        _a.Grupos.Responder(C, id, true);

        _a.AssertSoRecusa(C, CodigosErroSocial.ConviteNaoEncontrado);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void ConviteNaoEncontrado_recusar_convite_que_nao_existe_mais()
    {
        var id = _a.Convidar(A, B);
        _a.Grupos.Responder(B, id, false);
        _a.Saida.Limpar();

        _a.Grupos.Responder(B, id, false);

        _a.AssertSoRecusa(B, CodigosErroSocial.ConviteNaoEncontrado);
    }

    [Fact]
    public void ConviteNaoEncontrado_convite_aceito_nao_serve_de_novo()
    {
        var id = _a.Convidar(A, B);
        _a.Grupos.Responder(B, id, true);
        _a.Grupos.Sair(B);
        _a.Saida.Limpar();

        _a.Grupos.Responder(B, id, true);

        _a.AssertSoRecusa(B, CodigosErroSocial.ConviteNaoEncontrado);
    }

    [Fact]
    public void ConviteExpirado_ao_aceitar_depois_de_60_segundos()
    {
        var id = _a.Convidar(A, B);
        _a.Avancar(RegrasGrupo.ValidadeDoConviteSegundos);
        _a.Saida.Limpar();
        var antes = _a.Foto();

        _a.Grupos.Responder(B, id, true);

        // O aviso de "expirou" vai aos dois lados; a recusa, só a quem tentou aceitar.
        Assert.Equal(CodigosErroSocial.ConviteExpirado, _a.Saida.UltimaRecusa(B));
        Assert.Single(_a.Saida.Avisos(A, TipoDeAvisoDoGrupo.ConviteExpirou));
        Assert.Equal(0, _a.Grupos.QuantidadeDeGrupos);
        Assert.Equal(antes.Replace("|c1", "|c0"), _a.Foto());
    }

    [Fact]
    public void ConvidanteOffline_convidante_sem_grupo_que_cai_antes_do_aceite()
    {
        var id = _a.Convidar(A, B);
        _a.Grupos.AoDesconectar(A);
        _a.Saida.Limpar();
        var antes = _a.Foto();

        _a.Grupos.Responder(B, id, true);

        _a.AssertSoRecusa(B, CodigosErroSocial.ConvidanteOffline);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void ConvidanteOffline_lider_com_vaga_guardada_que_cai_antes_do_aceite()
    {
        _a.FormarGrupo(A, B);
        var id = _a.Convidar(A, C);
        _a.Grupos.AoDesconectar(A);
        _a.Saida.Limpar();
        var antes = _a.Foto();

        _a.Grupos.Responder(C, id, true);

        _a.AssertSoRecusa(C, CodigosErroSocial.ConvidanteOffline);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void NaoEstaEmGrupo_sair_expulsar_e_passar_lideranca()
    {
        var antes = _a.Foto();

        _a.Grupos.Sair(A);
        _a.AssertSoRecusa(A, CodigosErroSocial.NaoEstaEmGrupo);
        _a.Saida.Limpar();
        _a.Grupos.Expulsar(A, B);
        _a.AssertSoRecusa(A, CodigosErroSocial.NaoEstaEmGrupo);
        _a.Saida.Limpar();
        _a.Grupos.PassarLideranca(A, B);
        _a.AssertSoRecusa(A, CodigosErroSocial.NaoEstaEmGrupo);

        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void AlvoNaoEstaNoGrupo_ao_expulsar_e_ao_passar_lideranca()
    {
        _a.FormarGrupo(A, B);
        var antes = _a.Foto();

        _a.Grupos.Expulsar(A, C);
        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoNaoEstaNoGrupo);
        _a.Saida.Limpar();
        _a.Grupos.PassarLideranca(A, C);
        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoNaoEstaNoGrupo);

        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void AlvoInvalido_passar_lideranca_para_si_mesmo()
    {
        _a.FormarGrupo(A, B);
        var antes = _a.Foto();

        _a.Grupos.PassarLideranca(A, A);

        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoInvalido);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void AlvoInvalido_passar_lideranca_para_membro_offline()
    {
        _a.FormarGrupo(A, B, C);
        _a.Grupos.AoDesconectar(B);
        _a.Saida.Limpar();
        var antes = _a.Foto();

        _a.Grupos.PassarLideranca(A, B);

        _a.AssertSoRecusa(A, CodigosErroSocial.AlvoInvalido);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void NaoPodeExpulsarLider_o_proprio_lider_nao_se_expulsa()
    {
        _a.FormarGrupo(A, B);
        var antes = _a.Foto();

        _a.Grupos.Expulsar(A, A);

        _a.AssertSoRecusa(A, CodigosErroSocial.NaoPodeExpulsarLider);
        Assert.Equal(antes, _a.Foto());
    }

    [Fact]
    public void ReconviteBloqueado_por_30_segundos_depois_da_expulsao()
    {
        _a.FormarGrupo(A, B, C);
        _a.Grupos.Expulsar(A, C);
        _a.Avancar(RegrasGrupo.BloqueioDeReconviteSegundos - 1);
        _a.Saida.Limpar();
        var antes = _a.Foto();

        _a.Grupos.Convidar(A, "Caio");

        _a.AssertSoRecusa(A, CodigosErroSocial.ReconviteBloqueado);
        Assert.Equal(antes, _a.Foto());

        // Com 30 s, volta a poder convidar.
        _a.Avancar(1);
        _a.Convidar(A, C);
    }

    [Fact]
    public void ReconviteBloqueado_mesmo_quando_a_expulsao_desfez_o_grupo()
    {
        _a.FormarGrupo(A, B);
        _a.Grupos.Expulsar(A, B);
        Assert.Equal(0, _a.Grupos.QuantidadeDeGrupos);
        _a.Avancar(2);
        _a.Saida.Limpar();

        _a.Grupos.Convidar(A, "Bia");

        _a.AssertSoRecusa(A, CodigosErroSocial.ReconviteBloqueado);
    }

    // ── Limites ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Exatamente_cinco_membros_e_aceito()
    {
        _a.FormarGrupo(A, B, C, D, E);

        Assert.Equal(RegrasGrupo.MaximoDeMembros, _a.Ids(A).Count);
    }

    [Fact]
    public void Convite_com_59_segundos_ainda_e_aceito()
    {
        var id = _a.Convidar(A, B);
        _a.Avancar(RegrasGrupo.ValidadeDoConviteSegundos - 1);

        _a.Grupos.Responder(B, id, true);

        Assert.Equal([A, B], _a.Ids(A));
    }

    [Fact]
    public void Convite_com_60_segundos_ja_expirou()
    {
        var id = _a.Convidar(A, B);
        _a.Avancar(RegrasGrupo.ValidadeDoConviteSegundos);
        _a.Saida.Limpar();

        _a.Grupos.Responder(B, id, true);

        Assert.Equal(CodigosErroSocial.ConviteExpirado, _a.Saida.UltimaRecusa(B));
        Assert.Empty(_a.Grupos.ListaDe(B).Membros);
    }

    [Fact]
    public void Vaga_guardada_com_60_segundos_mantem_e_depois_remove()
    {
        _a.FormarGrupo(A, B, C);
        _a.Grupos.AoDesconectar(B);

        _a.Avancar(RegrasGrupo.VagaGuardadaSegundos);
        _a.Grupos.Varrer();
        Assert.Equal([A, B, C], _a.Ids(A));

        _a.Avancar(0.001);
        _a.Grupos.Varrer();
        Assert.Equal([A, C], _a.Ids(A));
        Assert.Contains(
            _a.Saida.Avisos(A, TipoDeAvisoDoGrupo.SaiuDoGrupo),
            x => x.PersonagemId == B
        );
    }

    [Fact]
    public void Voltar_depois_da_vaga_vencida_nao_devolve_o_grupo()
    {
        _a.FormarGrupo(A, B, C);
        _a.Grupos.AoDesconectar(B);
        _a.Avancar(RegrasGrupo.VagaGuardadaSegundos + 1);
        _a.Saida.Limpar();

        _a.Grupos.AoConectar(B);

        Assert.Empty(_a.Grupos.ListaDe(B).Membros);
        Assert.Empty(_a.Saida.De<ListaDoGrupo>(B));
        Assert.Equal([A, C], _a.Ids(A));
    }

    [Fact]
    public void Grupo_com_dois_membros_desfaz_quando_um_sai()
    {
        _a.FormarGrupo(A, B);

        _a.Grupos.Sair(B);

        Assert.Equal(0, _a.Grupos.QuantidadeDeGrupos);
        Assert.Empty(_a.Grupos.ListaDe(A).Membros);
        Assert.Single(_a.Saida.Avisos(A, TipoDeAvisoDoGrupo.GrupoDesfeito));
        Assert.Empty(_a.Saida.UltimaLista(A).Membros);
        Assert.Empty(_a.Saida.UltimaLista(B).Membros);
    }

    [Fact]
    public void Grupo_desfeito_pela_vaga_vencida_libera_quem_sobrou()
    {
        _a.FormarGrupo(A, B);
        _a.Grupos.AoDesconectar(B);
        _a.Avancar(RegrasGrupo.VagaGuardadaSegundos + 1);

        _a.Grupos.Varrer();

        Assert.Equal(0, _a.Grupos.QuantidadeDeGrupos);
        Assert.Single(_a.Saida.Avisos(A, TipoDeAvisoDoGrupo.GrupoDesfeito));
        // Quem sobrou pode formar outro grupo.
        _a.Juntar(A, C);
    }

    [Fact]
    public void Nome_com_maiusculas_diferentes_acha_o_mesmo_personagem()
    {
        _a.Avancar(1);

        _a.Grupos.Convidar(A, "bIA");

        Assert.Single(_a.Saida.De<ConviteRecebido>(B));
    }

    [Fact]
    public void Convite_expirado_e_esquecido_depois_da_retencao()
    {
        var id = _a.Convidar(A, B);
        _a.Avancar(RegrasGrupo.ValidadeDoConviteSegundos);
        _a.Grupos.Varrer();
        _a.Avancar(
            JogoDoGrupo
                .Server
                .Features
                .Social
                .ConfigGrupoServidor
                .RetencaoDeConviteEncerradoSegundos
        );
        _a.Grupos.Varrer();
        _a.Saida.Limpar();

        _a.Grupos.Responder(B, id, true);

        _a.AssertSoRecusa(B, CodigosErroSocial.ConviteNaoEncontrado);
    }

    [Fact]
    public void Limite_de_convites_pendentes_do_convidante_cai_quando_um_expira()
    {
        foreach (var alvo in new[] { B, C, D, E })
        {
            _a.Convidar(A, alvo);
        }

        _a.Avancar(RegrasGrupo.ValidadeDoConviteSegundos);
        _a.Grupos.Varrer();

        _a.Convidar(A, F);
    }

    [Fact]
    public void Id_do_grupo_nunca_se_repete()
    {
        _a.FormarGrupo(A, B);
        var grupo1 = _a.Grupos.ListaDe(A).GrupoId;
        _a.Grupos.Sair(B);
        _a.FormarGrupo(A, B);

        // Ids de grupo nunca se repetem.
        Assert.NotEqual(grupo1, _a.Grupos.ListaDe(A).GrupoId);
    }

    [Fact]
    public void Nomes_da_lista_vem_do_diretorio()
    {
        var id = _a.Convidar(A, B);
        _a.Grupos.Responder(B, id, true);

        var membros = _a.Grupos.ListaDe(A).Membros;

        Assert.Equal(["Ana", "Bia"], membros.Select(m => m.Nome));
    }

    [Fact]
    public void AtualizarVida_atualiza_vida_e_vida_maxima_do_membro_na_lista()
    {
        _a.FormarGrupo(A, B);

        _a.Grupos.AtualizarVida(B, 75, 120);

        var membros = _a.Grupos.ListaDe(A).Membros;
        var membroB = Assert.Single(membros, m => m.PersonagemId == B);
        Assert.Equal(75, membroB.Vida);
        Assert.Equal(120, membroB.VidaMaxima);
    }

    [Fact]
    public void AtualizarVida_sem_grupo_nao_lanca_excecao()
    {
        _a.Grupos.AtualizarVida(A, 50, 100);
    }

    [Fact]
    public void AtualizarArea_atualiza_area_do_membro_na_lista_e_retorna_mudanca()
    {
        _a.FormarGrupo(A, B);

        var mudou = _a.Grupos.AtualizarArea(B, "Dungeon");
        Assert.True(mudou);

        var membros = _a.Grupos.ListaDe(A).Membros;
        var membroB = Assert.Single(membros, m => m.PersonagemId == B);
        Assert.Equal("Dungeon", membroB.Area);

        Assert.False(_a.Grupos.AtualizarArea(B, "Dungeon"));
    }

    [Fact]
    public void AtualizarArea_sem_grupo_retorna_false()
    {
        Assert.False(_a.Grupos.AtualizarArea(A, "Cidade"));
    }

    [Fact]
    public void Lista_com_posicoes_preenche_area_dos_membros()
    {
        var posicoes = new PosicoesFalsas();
        posicoes.NaCidade.Add(A);
        posicoes.NaCidade.Add(B);

        var grupos = new GruposDoServidor(
            _a.Diretorio,
            _a.Saida,
            () => 1_000_000,
            null,
            () => posicoes
        );
        grupos.AoConectar(A);
        grupos.AoConectar(B);

        grupos.Convidar(A, "Bia");
        var convite = Assert.Single(_a.Saida.De<ConviteRecebido>(B));
        grupos.Responder(B, convite.ConviteId, true);

        var lista = grupos.ListaDe(A);
        Assert.Equal(2, lista.Membros.Length);
        Assert.Equal("Cidade", lista.Membros[0].Area);
        Assert.Equal("Cidade", lista.Membros[1].Area);
    }
}
