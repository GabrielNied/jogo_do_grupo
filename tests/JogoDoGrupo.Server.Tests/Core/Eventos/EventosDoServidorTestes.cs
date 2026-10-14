using JogoDoGrupo.Server.Core.Eventos;

namespace JogoDoGrupo.Server.Tests.Core.Eventos;

public class EventosDoServidorTestes
{
    private sealed class Chegou
    {
        public int Numero;
    }

    private sealed class Saiu
    {
        public int Numero;
    }

    private class Base
    {
        public int Numero;
    }

    private sealed class Derivado : Base
    {
        public int Outro;
    }

    [Fact]
    public void Evento_chega_a_todos_os_assinantes_na_ordem_de_inscricao()
    {
        var eventos = new EventosDoServidor();
        var ordem = new List<string>();
        eventos.Assinar<Chegou>(e => ordem.Add($"primeiro {e.Numero}"));
        eventos.Assinar<Chegou>(e => ordem.Add($"segundo {e.Numero}"));
        eventos.Assinar<Chegou>(e => ordem.Add($"terceiro {e.Numero}"));

        eventos.Publicar(new Chegou { Numero = 7 });

        Assert.Equal(new[] { "primeiro 7", "segundo 7", "terceiro 7" }, ordem);
    }

    [Fact]
    public void Evento_sem_assinante_nao_falha()
    {
        var eventos = new EventosDoServidor();

        eventos.Publicar(new Chegou { Numero = 1 });
    }

    [Fact]
    public void Assinante_so_recebe_o_tipo_que_assinou()
    {
        var eventos = new EventosDoServidor();
        var chegadas = 0;
        var saidas = 0;
        eventos.Assinar<Chegou>(_ => chegadas++);
        eventos.Assinar<Saiu>(_ => saidas++);

        eventos.Publicar(new Chegou());
        eventos.Publicar(new Chegou());
        eventos.Publicar(new Saiu());

        Assert.Equal(2, chegadas);
        Assert.Equal(1, saidas);
    }

    [Fact]
    public void Assinatura_vale_para_o_tipo_exato_e_nao_para_derivados()
    {
        var eventos = new EventosDoServidor();
        var daBase = 0;
        var doDerivado = 0;
        eventos.Assinar<Base>(_ => daBase++);
        eventos.Assinar<Derivado>(_ => doDerivado++);

        eventos.Publicar(new Derivado());
        eventos.Publicar<Base>(new Derivado());

        Assert.Equal(1, daBase);
        Assert.Equal(1, doDerivado);
    }

    [Fact]
    public void Mesmo_manipulador_inscrito_duas_vezes_e_chamado_duas_vezes()
    {
        var eventos = new EventosDoServidor();
        var chamadas = 0;
        Action<Chegou> manipulador = _ => chamadas++;
        eventos.Assinar(manipulador);
        eventos.Assinar(manipulador);

        eventos.Publicar(new Chegou());

        Assert.Equal(2, chamadas);
    }

    [Fact]
    public void Falha_de_um_assinante_nao_impede_os_outros_e_sai_no_fim()
    {
        var eventos = new EventosDoServidor();
        var recebidos = new List<string>();
        eventos.Assinar<Chegou>(_ => recebidos.Add("antes"));
        eventos.Assinar<Chegou>(_ => throw new InvalidOperationException("quebrou"));
        eventos.Assinar<Chegou>(_ => recebidos.Add("depois"));

        eventos.Publicar(new Chegou());

        Assert.Equal(new[] { "antes", "depois" }, recebidos);
    }

    [Fact]
    public void Depois_de_uma_falha_o_proximo_evento_e_entregue_normalmente()
    {
        var eventos = new EventosDoServidor();
        var falhar = true;
        var entregues = 0;
        eventos.Assinar<Chegou>(_ =>
        {
            if (falhar)
            {
                throw new InvalidOperationException("quebrou");
            }

            entregues++;
        });

        eventos.Publicar(new Chegou());
        falhar = false;
        eventos.Publicar(new Chegou());

        Assert.Equal(1, entregues);
    }

    [Fact]
    public void Evento_publicado_dentro_de_um_manipulador_espera_o_atual_chegar_a_todos()
    {
        var eventos = new EventosDoServidor();
        var ordem = new List<string>();
        eventos.Assinar<Chegou>(e =>
        {
            ordem.Add("a recebe chegou");
            eventos.Publicar(new Saiu { Numero = e.Numero });
        });
        eventos.Assinar<Chegou>(_ => ordem.Add("b recebe chegou"));
        eventos.Assinar<Saiu>(_ => ordem.Add("a recebe saiu"));
        eventos.Assinar<Saiu>(_ => ordem.Add("b recebe saiu"));

        eventos.Publicar(new Chegou { Numero = 3 });

        Assert.Equal(
            new[] { "a recebe chegou", "b recebe chegou", "a recebe saiu", "b recebe saiu" },
            ordem
        );
    }

    [Fact]
    public void Falha_num_evento_publicado_de_dentro_tambem_sai_no_fim()
    {
        var eventos = new EventosDoServidor();
        eventos.Assinar<Chegou>(_ => eventos.Publicar(new Saiu()));
        eventos.Assinar<Saiu>(_ => throw new InvalidOperationException("quebrou"));

        eventos.Publicar(new Chegou());
    }

    [Fact]
    public void Assinante_inscrito_durante_a_entrega_so_recebe_o_proximo_evento()
    {
        var eventos = new EventosDoServidor();
        var tardio = 0;
        var inscrito = false;
        eventos.Assinar<Chegou>(evento =>
        {
            if (!inscrito)
            {
                inscrito = true;
                eventos.Assinar<Chegou>(_ => tardio++);
            }
        });

        eventos.Publicar(new Chegou());
        Assert.Equal(0, tardio);

        eventos.Publicar(new Chegou());
        Assert.Equal(1, tardio);
    }

    [Fact]
    public void Manipulador_ou_evento_nulo_e_recusado()
    {
        var eventos = new EventosDoServidor();

        Assert.Throws<ArgumentNullException>(() => eventos.Assinar<Chegou>(null!));
        Assert.Throws<ArgumentNullException>(() => eventos.Publicar<Chegou>(null!));
    }

    [Fact]
    public async Task Publicacoes_de_threads_diferentes_nao_se_misturam_nem_se_perdem()
    {
        var eventos = new EventosDoServidor();
        var total = 0;
        eventos.Assinar<Chegou>(_ => Interlocked.Increment(ref total));

        void PublicarQuinhentos()
        {
            for (var i = 0; i < 500; i++)
            {
                eventos.Publicar(new Chegou());
            }
        }

        var tarefas = new List<Task>();
        for (var t = 0; t < 8; t++)
        {
            tarefas.Add(Task.Run(PublicarQuinhentos));
        }

        await Task.WhenAll(tarefas);

        Assert.Equal(4000, total);
    }

    [Fact]
    public async Task Evento_publicado_por_tarefa_criada_dentro_de_um_manipulador_e_entregue()
    {
        var eventos = new EventosDoServidor();
        var saiu = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        Task? tarefa = null;
        eventos.Assinar<Chegou>(_ =>
            tarefa = Task.Run(async () =>
            {
                await Task.Delay(50);
                eventos.Publicar(new Saiu { Numero = 3 });
            })
        );
        eventos.Assinar<Saiu>(e => saiu.TrySetResult(e.Numero));

        eventos.Publicar(new Chegou());
        await tarefa!;

        Assert.Equal(3, await saiu.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Assinar_e_publicar_ao_mesmo_tempo_nao_quebra()
    {
        var eventos = new EventosDoServidor();
        var total = 0;

        void AssinarDuzentos()
        {
            for (var i = 0; i < 200; i++)
            {
                eventos.Assinar<Chegou>(_ => Interlocked.Increment(ref total));
            }
        }

        void PublicarDuzentos()
        {
            for (var i = 0; i < 200; i++)
            {
                eventos.Publicar(new Chegou());
            }
        }

        await Task.WhenAll(Task.Run(AssinarDuzentos), Task.Run(PublicarDuzentos));

        eventos.Publicar(new Chegou());
        Assert.True(total >= 200);
    }
}
