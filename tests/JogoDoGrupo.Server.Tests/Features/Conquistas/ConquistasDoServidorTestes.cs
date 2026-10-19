using System.Collections.Generic;
using JogoDoGrupo.Server.Features.Conquistas;
using JogoDoGrupo.Shared.Features.Conquistas;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Conquistas;

public class ConquistasDoServidorTestes
{
    private sealed class SaidaGravadora : ISaidaDeConquistas
    {
        public List<(long PersonagemId, DadosDeConquistas Dados)> DadosEnviados { get; } = [];
        public List<(
            long PersonagemId,
            ConquistaDesbloqueada Desbloqueada
        )> ConquistasDesbloqueadas { get; } = [];
        public List<(long PersonagemId, string Titulo)> TitulosEquipados { get; } = [];
        public List<(long PersonagemId, string CodigoErro)> Recusas { get; } = [];

        public void Dados(long personagemId, DadosDeConquistas dados) =>
            DadosEnviados.Add((personagemId, dados));

        public void Desbloqueada(long personagemId, ConquistaDesbloqueada desbloqueada) =>
            ConquistasDesbloqueadas.Add((personagemId, desbloqueada));

        public void TituloEquipado(long personagemId, string titulo) =>
            TitulosEquipados.Add((personagemId, titulo));

        public void Recusada(long personagemId, string codigo) =>
            Recusas.Add((personagemId, codigo));

        public void Limpar()
        {
            DadosEnviados.Clear();
            ConquistasDesbloqueadas.Clear();
            TitulosEquipados.Clear();
            Recusas.Clear();
        }
    }

    [Fact]
    public void Consultar_inicialmente_retorna_catalogo_zerado_e_sem_titulos()
    {
        var saida = new SaidaGravadora();
        var servidor = new ConquistasDoServidor(saida);

        servidor.Consultar(contaId: 100, personagemId: 1);

        Assert.Single(saida.DadosEnviados);
        var dados = saida.DadosEnviados[0].Dados;
        Assert.Equal(0, dados.PontosTotais);
        Assert.Equal("", dados.TituloAtivo);
        Assert.Empty(dados.TitulosDesbloqueados);
        Assert.Equal(CatalogoDeConquistas.Todas.Count, dados.Conquistas.Length);
        Assert.All(dados.Conquistas, p => Assert.False(p.Concluida));
    }

    [Fact]
    public void AvancarProgresso_incrementa_e_desbloqueia_quando_atinge_meta()
    {
        var saida = new SaidaGravadora();
        var servidor = new ConquistasDoServidor(saida);

        // MatadorDeMobs precisa de 10 kills
        servidor.AvancarProgresso(
            contaId: 100,
            personagemId: 1,
            CatalogoDeConquistas.MatadorDeMobs,
            incrementoOuValor: 5
        );
        Assert.Empty(saida.ConquistasDesbloqueadas);

        servidor.AvancarProgresso(
            contaId: 100,
            personagemId: 1,
            CatalogoDeConquistas.MatadorDeMobs,
            incrementoOuValor: 5
        );
        Assert.Single(saida.ConquistasDesbloqueadas);

        var desbloqueada = saida.ConquistasDesbloqueadas[0].Desbloqueada;
        Assert.Equal(CatalogoDeConquistas.MatadorDeMobs, desbloqueada.ConquistaId);
        Assert.Equal("Matador de Mobs", desbloqueada.Nome);
        Assert.Equal(15, desbloqueada.Pontos);
        Assert.Equal("O Caçador", desbloqueada.TituloDesbloqueado);

        // Agora o título "O Caçador" está desbloqueado
        saida.Limpar();
        servidor.Consultar(contaId: 100, personagemId: 1);
        var dados = saida.DadosEnviados[0].Dados;
        Assert.Equal(15, dados.PontosTotais);
        Assert.Contains("O Caçador", dados.TitulosDesbloqueados);
    }

    [Fact]
    public void AvancarProgresso_apos_concluir_nao_dispara_novamente()
    {
        var saida = new SaidaGravadora();
        var servidor = new ConquistasDoServidor(saida);

        servidor.AvancarProgresso(
            contaId: 100,
            personagemId: 1,
            CatalogoDeConquistas.OFimDoPredador,
            incrementoOuValor: 1
        );
        Assert.Single(saida.ConquistasDesbloqueadas);

        saida.Limpar();
        // Derrota novamente
        servidor.AvancarProgresso(
            contaId: 100,
            personagemId: 1,
            CatalogoDeConquistas.OFimDoPredador,
            incrementoOuValor: 1
        );
        Assert.Empty(saida.ConquistasDesbloqueadas);
    }

    [Fact]
    public void EquiparTitulo_recusa_se_titulo_nao_desbloqueado()
    {
        var saida = new SaidaGravadora();
        var servidor = new ConquistasDoServidor(saida);

        servidor.EquiparTitulo(contaId: 100, personagemId: 1, "O Explorador");

        Assert.Single(saida.Recusas);
        Assert.Equal(CodigosErroConquistas.TituloNaoConquistado, saida.Recusas[0].CodigoErro);
        Assert.Empty(saida.TitulosEquipados);
    }

    [Fact]
    public void EquiparTitulo_aceita_apos_desbloquear_e_permite_desequipar()
    {
        var saida = new SaidaGravadora();
        var servidor = new ConquistasDoServidor(saida);

        // Desbloqueia PrimeirosPassos (nível 2 -> título "O Iniciante")
        servidor.AvancarProgresso(
            contaId: 100,
            personagemId: 1,
            CatalogoDeConquistas.PrimeirosPassos,
            incrementoOuValor: 2,
            ehValorAbsoluto: true
        );
        saida.Limpar();

        servidor.EquiparTitulo(contaId: 100, personagemId: 1, "O Iniciante");

        Assert.Single(saida.TitulosEquipados);
        Assert.Equal("O Iniciante", saida.TitulosEquipados[0].Titulo);
        Assert.Empty(saida.Recusas);

        // Desequipa com título vazio
        saida.Limpar();
        servidor.EquiparTitulo(contaId: 100, personagemId: 1, "");
        Assert.Single(saida.TitulosEquipados);
        Assert.Equal("", saida.TitulosEquipados[0].Titulo);
    }

    [Fact]
    public void RegistrarEntradaEmArea_acumula_areas_unicas()
    {
        var saida = new SaidaGravadora();
        var servidor = new ConquistasDoServidor(saida);

        servidor.RegistrarEntradaEmArea(contaId: 100, personagemId: 1, "Floresta");
        servidor.RegistrarEntradaEmArea(contaId: 100, personagemId: 1, "Floresta"); // Repetida
        servidor.RegistrarEntradaEmArea(contaId: 100, personagemId: 1, "Cidade");
        Assert.Empty(saida.ConquistasDesbloqueadas);

        // 3ª área diferente -> Desbravador de Asterra
        servidor.RegistrarEntradaEmArea(contaId: 100, personagemId: 1, "Caverna");
        Assert.Single(saida.ConquistasDesbloqueadas);
        Assert.Equal(
            CatalogoDeConquistas.DesbravadorDeAsterra,
            saida.ConquistasDesbloqueadas[0].Desbloqueada.ConquistaId
        );
        Assert.Equal(
            "O Explorador",
            saida.ConquistasDesbloqueadas[0].Desbloqueada.TituloDesbloqueado
        );
    }
}
