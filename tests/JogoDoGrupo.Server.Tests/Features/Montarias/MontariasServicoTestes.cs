using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Montarias;
using JogoDoGrupo.Shared.Features.Montarias;
using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Montarias;

public class MontariasServicoTestes
{
    private sealed class ControleVelocidadeSpy : IControleDeVelocidadeDoMundo
    {
        public long UltimoPersonagemId { get; private set; }
        public float UltimoAndar { get; private set; }
        public float UltimoCorrer { get; private set; }
        public int Chamadas { get; private set; }

        public bool DefinirVelocidades(long personagemId, float andar, float correr)
        {
            UltimoPersonagemId = personagemId;
            UltimoAndar = andar;
            UltimoCorrer = correr;
            Chamadas++;
            return true;
        }
    }

    private sealed class CombateMock(bool emCombate) : IEstadoDeCombate
    {
        public bool EmCombate(long personagemId) => emCombate;
    }

    [Fact]
    public void ChamarMontaria_AumentaVelocidade_E_MarcaComoMontado()
    {
        var velocidade = new ControleVelocidadeSpy();
        var servico = new ServicoDeMontarias(controleVelocidade: velocidade);
        long personagemId = 42;

        var sucesso = servico.ChamarMontaria(personagemId);

        Assert.True(sucesso);
        Assert.True(servico.EstaMontado(personagemId));
        Assert.Equal(RegrasMontaria.TipoPadrao, servico.TipoDaMontaria(personagemId));
        Assert.Equal(1, velocidade.Chamadas);
        Assert.Equal(personagemId, velocidade.UltimoPersonagemId);
        Assert.Equal(RegrasMontaria.VelocidadeMontariaPadrao, velocidade.UltimoCorrer);
    }

    [Fact]
    public void Desmontar_RestauraVelocidadePadrao_E_RemoveEstado()
    {
        var velocidade = new ControleVelocidadeSpy();
        var servico = new ServicoDeMontarias(controleVelocidade: velocidade);
        long personagemId = 42;

        servico.ChamarMontaria(personagemId);
        var desmontou = servico.Desmontar(personagemId);

        Assert.True(desmontou);
        Assert.False(servico.EstaMontado(personagemId));
        Assert.Null(servico.TipoDaMontaria(personagemId));
        Assert.Equal(2, velocidade.Chamadas);
        Assert.Equal(RegrasVelocidade.CorrerPadrao, velocidade.UltimoCorrer);
    }

    [Fact]
    public void ChamarMontaria_Recusa_SeEstiverEmCombate()
    {
        var velocidade = new ControleVelocidadeSpy();
        var combate = new CombateMock(emCombate: true);
        var servico = new ServicoDeMontarias(controleVelocidade: velocidade, combate: combate);
        long personagemId = 42;

        var sucesso = servico.ChamarMontaria(personagemId);

        Assert.False(sucesso);
        Assert.False(servico.EstaMontado(personagemId));
        Assert.Equal(0, velocidade.Chamadas);
    }

    [Fact]
    public void Desconectar_LimpaEstadoMontado()
    {
        var servico = new ServicoDeMontarias();
        long personagemId = 42;

        servico.ChamarMontaria(personagemId);
        Assert.True(servico.EstaMontado(personagemId));

        servico.Desconectar(personagemId);
        Assert.False(servico.EstaMontado(personagemId));
    }

    [Fact]
    public void EquiparMontaria_AlteraMontariaEquipada_E_ChamaComNovaMontaria()
    {
        var servico = new ServicoDeMontarias();
        long personagemId = 42;

        Assert.Equal(RegrasMontaria.TipoPadrao, servico.ObterMontariaEquipada(personagemId));

        var equipou = servico.EquiparMontaria(personagemId, RegrasMontaria.TipoNoturno);
        Assert.True(equipou);
        Assert.Equal(RegrasMontaria.TipoNoturno, servico.ObterMontariaEquipada(personagemId));

        servico.ChamarMontaria(personagemId);
        Assert.True(servico.EstaMontado(personagemId));
        Assert.Equal(RegrasMontaria.TipoNoturno, servico.TipoDaMontaria(personagemId));
    }

    [Fact]
    public void EquiparMontaria_RecusaTipoInvalido()
    {
        var servico = new ServicoDeMontarias();
        long personagemId = 42;

        var equipou = servico.EquiparMontaria(personagemId, 999);
        Assert.False(equipou);
        Assert.Equal(RegrasMontaria.TipoPadrao, servico.ObterMontariaEquipada(personagemId));
    }

    [Fact]
    public void EquiparMontaria_QuandoJaMontado_AtualizaTipoAtivoImediatamente()
    {
        var servico = new ServicoDeMontarias();
        long personagemId = 42;

        servico.ChamarMontaria(personagemId);
        Assert.Equal(RegrasMontaria.TipoCastanho, servico.TipoDaMontaria(personagemId));

        servico.EquiparMontaria(personagemId, RegrasMontaria.TipoNoturno);
        Assert.Equal(RegrasMontaria.TipoNoturno, servico.TipoDaMontaria(personagemId));
    }
}
