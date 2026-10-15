using System.Diagnostics;
using JogoDoGrupo.Server.Core.TempoReal;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

public class MedidorDePassosTestes
{
    private static readonly long Segundo = Stopwatch.Frequency;

    [Fact]
    public void Sem_passos_resume_tudo_zero()
    {
        Assert.Equal(new ResumoDosPassos(0, 0, 0, 0, 0), new MedidorDePassos().Resumir(0));
    }

    [Fact]
    public void Um_passo_da_a_duracao_sem_ritmo()
    {
        var medidor = new MedidorDePassos();
        medidor.Registrar(0, TimeSpan.FromMilliseconds(3));

        var resumo = medidor.Resumir(Segundo / 2);

        Assert.Equal(0, resumo.PassosPorSegundo);
        Assert.Equal(3, resumo.DuracaoP50Ms);
        Assert.Equal(3, resumo.DuracaoP95Ms);
        Assert.Equal(3, resumo.DuracaoMaximaMs);
        Assert.Equal(500, resumo.MsDesdeOUltimoPasso);
    }

    [Fact]
    public void Percentis_e_ritmo_saem_dos_passos_registrados()
    {
        var medidor = new MedidorDePassos();
        // 100 passos a 10 por segundo, com durações 1..100 ms fora de ordem.
        for (var i = 0; i < 100; i++)
        {
            var duracao = (i * 37 % 100) + 1;
            medidor.Registrar(i * Segundo / 10, TimeSpan.FromMilliseconds(duracao));
        }

        var resumo = medidor.Resumir(99 * Segundo / 10);

        Assert.Equal(10, resumo.PassosPorSegundo);
        Assert.Equal(50, resumo.DuracaoP50Ms);
        Assert.Equal(95, resumo.DuracaoP95Ms);
        Assert.Equal(100, resumo.DuracaoMaximaMs);
        Assert.Equal(0, resumo.MsDesdeOUltimoPasso);
    }

    [Fact]
    public void Janela_circular_descarta_os_passos_mais_antigos()
    {
        var medidor = new MedidorDePassos();
        // Passos antigos lentos (500 ms) saem da janela; ficam só os rápidos (1 ms).
        for (var i = 0; i < MedidorDePassos.Janela; i++)
        {
            medidor.Registrar(i * Segundo / 100, TimeSpan.FromMilliseconds(500));
        }

        for (var i = 0; i < MedidorDePassos.Janela; i++)
        {
            medidor.Registrar(
                (MedidorDePassos.Janela + i) * Segundo / 100,
                TimeSpan.FromMilliseconds(1)
            );
        }

        var resumo = medidor.Resumir((2 * MedidorDePassos.Janela - 1) * Segundo / 100);

        Assert.Equal(1, resumo.DuracaoMaximaMs);
        Assert.Equal(100, resumo.PassosPorSegundo);
    }
}
