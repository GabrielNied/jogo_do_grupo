using JogoDoGrupo.Shared.Features.Social;
using static JogoDoGrupo.Server.Tests.Features.Social.AmbienteDeGrupo;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>
/// Pedidos simultâneos: cada teste repete várias vezes para pegar ordens diferentes e confere
/// que o resultado é consistente (um líder só, sem membros fantasmas, sem erro duplicado).
/// </summary>
public class GruposConcorrenciaTestes
{
    private const int Repeticoes = 100;

    /// <summary>Roda as ações ao mesmo tempo, largando juntas.</summary>
    private static void Juntos(params Action[] acoes)
    {
        using var largada = new Barrier(acoes.Length);
        var tarefas = acoes
            .Select(acao =>
                Task.Factory.StartNew(
                    () =>
                    {
                        largada.SignalAndWait();
                        acao();
                    },
                    TaskCreationOptions.LongRunning
                )
            )
            .ToArray();
        Task.WaitAll(tarefas);
    }

    /// <summary>Todo grupo tem um líder que é membro, de 2 a 5 pessoas, e todos veem a mesma lista.</summary>
    private static void AssertConsistente(AmbienteDeGrupo a)
    {
        var grupos = 0;
        foreach (var id in Todos)
        {
            var lista = a.Grupos.ListaDe(id);
            if (lista.Membros.Length == 0)
            {
                continue;
            }

            var ids = lista.Membros.Select(m => m.PersonagemId).ToList();
            Assert.Contains(id, ids);
            Assert.Contains(lista.LiderId, ids);
            Assert.InRange(ids.Count, RegrasGrupo.MinimoDeMembros, RegrasGrupo.MaximoDeMembros);
            Assert.Equal(ids.Count, ids.Distinct().Count());
            foreach (var outro in ids)
            {
                var deles = a.Grupos.ListaDe(outro);
                Assert.Equal(lista.GrupoId, deles.GrupoId);
                Assert.Equal(lista.LiderId, deles.LiderId);
                Assert.Equal(ids, deles.Membros.Select(m => m.PersonagemId));
            }

            if (ids[0] == id)
            {
                grupos++;
            }
        }

        Assert.Equal(grupos, a.Grupos.QuantidadeDeGrupos);
    }

    [Fact]
    public void Quatro_aceites_ao_mesmo_tempo_enchem_o_grupo_sem_passar_de_cinco()
    {
        for (var i = 0; i < Repeticoes; i++)
        {
            var a = new AmbienteDeGrupo();
            var convites = new[] { B, C, D, E }.ToDictionary(
                alvo => alvo,
                alvo => a.Convidar(A, alvo)
            );
            a.Saida.Limpar();

            Juntos([
                .. convites.Select(c => (Action)(() => a.Grupos.Responder(c.Key, c.Value, true))),
            ]);

            Assert.Equal(5, a.Ids(A).Count);
            Assert.Equal(A, a.Grupos.ListaDe(A).LiderId);
            Assert.All(Todos, id => Assert.Null(a.Saida.UltimaRecusa(id)));
            AssertConsistente(a);
        }
    }

    [Fact]
    public void Dois_convites_de_grupos_diferentes_o_alvo_entra_em_um_so()
    {
        for (var i = 0; i < Repeticoes; i++)
        {
            var a = new AmbienteDeGrupo();
            var deA = a.Convidar(A, C);
            var deB = a.Convidar(B, C);
            a.Saida.Limpar();

            Juntos(() => a.Grupos.Responder(C, deA, true), () => a.Grupos.Responder(C, deB, true));

            var lista = a.Grupos.ListaDe(C);
            Assert.Equal(2, lista.Membros.Length);
            Assert.Contains(lista.LiderId, new[] { A, B });
            Assert.Equal([CodigosErroSocial.AlvoJaEmGrupo], a.Saida.De<string>(C));
            Assert.Equal(1, a.Grupos.QuantidadeDeGrupos);
            Assert.Equal(0, a.Grupos.ConvitesPendentes);
            AssertConsistente(a);
        }
    }

    [Fact]
    public void Lider_que_sai_enquanto_expulsa_deixa_o_grupo_consistente()
    {
        for (var i = 0; i < Repeticoes; i++)
        {
            var a = new AmbienteDeGrupo();
            a.FormarGrupo(A, B, C);

            Juntos(() => a.Grupos.Sair(A), () => a.Grupos.Expulsar(A, C));

            AssertConsistente(a);
            Assert.Empty(a.Grupos.ListaDe(A).Membros);
            // Erro só do tipo esperado: Expulsar depois de Sair vira NaoEstaEmGrupo.
            Assert.All(
                a.Saida.De<string>(A),
                codigo => Assert.Equal(CodigosErroSocial.NaoEstaEmGrupo, codigo)
            );
        }
    }

    [Fact]
    public void Lider_que_sai_enquanto_convida_deixa_o_grupo_consistente()
    {
        for (var i = 0; i < Repeticoes; i++)
        {
            var a = new AmbienteDeGrupo();
            a.FormarGrupo(A, B, C);
            a.Avancar(2);

            Juntos(() => a.Grupos.Sair(A), () => a.Grupos.Convidar(A, "Duda"));

            AssertConsistente(a);
            Assert.Empty(a.Grupos.ListaDe(A).Membros);
            // Convidar antes de sair: o convite cai com a liderança. Depois de sair: A convida sozinho.
            Assert.InRange(a.Grupos.ConvitesPendentes, 0, 1);
        }
    }

    [Fact]
    public void Dois_membros_saem_ao_mesmo_tempo_e_o_grupo_desfaz_uma_vez_so()
    {
        for (var i = 0; i < Repeticoes; i++)
        {
            var a = new AmbienteDeGrupo();
            a.FormarGrupo(A, B, C);

            Juntos(() => a.Grupos.Sair(B), () => a.Grupos.Sair(C));

            Assert.Single(a.Saida.Avisos(A, TipoDeAvisoDoGrupo.GrupoDesfeito));
            Assert.Null(a.Saida.UltimaRecusa(B));
            Assert.Null(a.Saida.UltimaRecusa(C));
            Assert.Equal(0, a.Grupos.QuantidadeDeGrupos);
            AssertConsistente(a);
        }
    }

    [Fact]
    public void Convite_aceito_enquanto_o_convidante_cai_nunca_deixa_grupo_pela_metade()
    {
        for (var i = 0; i < Repeticoes; i++)
        {
            var a = new AmbienteDeGrupo();
            var id = a.Convidar(A, B);
            a.Saida.Limpar();

            Juntos(() => a.Grupos.Responder(B, id, true), () => a.Grupos.AoDesconectar(A));

            AssertConsistente(a);
            if (a.Grupos.QuantidadeDeGrupos == 0)
            {
                Assert.Equal(CodigosErroSocial.ConvidanteOffline, a.Saida.UltimaRecusa(B));
            }
            else
            {
                Assert.Equal([A, B], a.Ids(B));
                Assert.Null(a.Saida.UltimaRecusa(B));
                Assert.False(a.Grupos.ListaDe(B).Membros.Single(m => m.PersonagemId == A).Online);
            }
        }
    }

    [Fact]
    public void Duas_expulsoes_do_mesmo_alvo_uma_vale_e_a_outra_recebe_AlvoNaoEstaNoGrupo()
    {
        for (var i = 0; i < Repeticoes; i++)
        {
            var a = new AmbienteDeGrupo();
            a.FormarGrupo(A, B, C);

            Juntos(() => a.Grupos.Expulsar(A, C), () => a.Grupos.Expulsar(A, C));

            Assert.Single(a.Saida.Avisos(C, TipoDeAvisoDoGrupo.ExpulsoDoGrupo));
            Assert.Equal([CodigosErroSocial.AlvoNaoEstaNoGrupo], a.Saida.De<string>(A));
            Assert.Equal([A, B], a.Ids(A));
            AssertConsistente(a);
        }
    }

    [Fact]
    public void Operacoes_de_grupos_diferentes_em_paralelo_nao_se_misturam()
    {
        for (var i = 0; i < Repeticoes / 4; i++)
        {
            var a = new AmbienteDeGrupo();
            a.FormarGrupo(A, B, C);
            a.FormarGrupo(D, E, F);

            Juntos(
                () => a.Grupos.Sair(B),
                () => a.Grupos.Sair(E),
                () => a.Grupos.PassarLideranca(D, F),
                () => a.Grupos.Expulsar(A, C)
            );

            AssertConsistente(a);
            Assert.Equal(1, a.Grupos.QuantidadeDeGrupos);
            Assert.Equal([D, F], a.Ids(D));
            Assert.Equal(F, a.Grupos.ListaDe(D).LiderId);
        }
    }
}
