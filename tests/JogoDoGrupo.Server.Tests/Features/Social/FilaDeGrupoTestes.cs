using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Shared.Features.Social;
using Xunit;
using static JogoDoGrupo.Server.Tests.Features.Social.AmbienteDeGrupo;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public sealed class FilaDeGrupoTestes
{
    private readonly AmbienteDeGrupo _a = new();
    private readonly List<(long Destinatario, ushort Id, object Corpo)> _mensagensEnviadas = [];

    private FilaDeGrupoDoServidor CriarFila()
    {
        return new FilaDeGrupoDoServidor(
            _a.Grupos,
            (dest, id, corpo) => _mensagensEnviadas.Add((dest, id, corpo)),
            () => _a.AgoraMs
        );
    }

    [Fact]
    public void Entrar_com_papel_invalido_retorna_false()
    {
        var fila = CriarFila();

        var entrou = fila.Entrar(A, PapelNoGrupo.Nenhum);

        Assert.False(entrou);
        Assert.Equal(0, fila.TotalEmFila);
    }

    [Fact]
    public void Entrar_jogador_ja_em_grupo_retorna_false()
    {
        _a.Grupos.Convidar(A, "Bia");
        _a.Grupos.Responder(B, 1, true); // Grupo com A e B

        var fila = CriarFila();
        var entrou = fila.Entrar(A, PapelNoGrupo.Tank);

        Assert.False(entrou);
        Assert.Equal(0, fila.TotalEmFila);
    }

    [Fact]
    public void Entrar_adiciona_a_fila_e_envia_status()
    {
        var fila = CriarFila();

        var entrou = fila.Entrar(A, PapelNoGrupo.Tank);

        Assert.True(entrou);
        Assert.Equal(1, fila.TotalEmFila);
        Assert.Equal(1, fila.QuantidadeTanques);
        Assert.Equal(0, fila.QuantidadeHealers);
        Assert.Equal(0, fila.QuantidadeDps);

        var status = fila.ObterStatus(A);
        Assert.True(status.EmFila);
        Assert.Equal(PapelNoGrupo.Tank, status.Papel);
        Assert.Equal(1, status.Tanques);

        var msg = Assert.Single(_mensagensEnviadas);
        Assert.Equal(A, msg.Destinatario);
        Assert.Equal(SocialMessageIds.StatusDaFilaDeGrupo, msg.Id);
        var statusMsg = Assert.IsType<StatusDaFilaDeGrupo>(msg.Corpo);
        Assert.True(statusMsg.EmFila);
        Assert.Equal(PapelNoGrupo.Tank, statusMsg.Papel);
    }

    [Fact]
    public void Sair_remove_da_fila_e_notifica_em_fila_falso()
    {
        var fila = CriarFila();
        fila.Entrar(A, PapelNoGrupo.Healer);
        _mensagensEnviadas.Clear();

        var saiu = fila.Sair(A);

        Assert.True(saiu);
        Assert.Equal(0, fila.TotalEmFila);

        var msg = Assert.Single(_mensagensEnviadas);
        Assert.Equal(A, msg.Destinatario);
        Assert.Equal(SocialMessageIds.StatusDaFilaDeGrupo, msg.Id);
        var statusMsg = Assert.IsType<StatusDaFilaDeGrupo>(msg.Corpo);
        Assert.False(statusMsg.EmFila);
    }

    [Fact]
    public void AoDesconectar_remove_jogador_da_fila()
    {
        var fila = CriarFila();
        fila.Entrar(A, PapelNoGrupo.Dps);

        fila.AoDesconectar(A);

        Assert.Equal(0, fila.TotalEmFila);
        var status = fila.ObterStatus(A);
        Assert.False(status.EmFila);
    }

    [Fact]
    public void Formacao_automatica_de_5_com_1_tank_1_healer_3_dps()
    {
        var fila = CriarFila();

        // Inscreve 1 Tank (A), 1 Healer (B), 2 DPS (C, D)
        fila.Entrar(A, PapelNoGrupo.Tank);
        fila.Entrar(B, PapelNoGrupo.Healer);
        fila.Entrar(C, PapelNoGrupo.Dps);
        fila.Entrar(D, PapelNoGrupo.Dps);

        Assert.Equal(4, fila.TotalEmFila);
        Assert.Null(_a.Grupos.Consultar(A));

        // Entra o 3º DPS (E) -> forma o grupo de 5 na hora!
        _mensagensEnviadas.Clear();
        fila.Entrar(E, PapelNoGrupo.Dps);

        // A fila deve esvaziar
        Assert.Equal(0, fila.TotalEmFila);

        // Grupo deve ter sido criado no servidor
        var grupo = _a.Grupos.Consultar(A);
        Assert.NotNull(grupo);
        Assert.Equal(5, grupo.Membros.Count);
        Assert.Contains(A, grupo.Membros);
        Assert.Contains(B, grupo.Membros);
        Assert.Contains(C, grupo.Membros);
        Assert.Contains(D, grupo.Membros);
        Assert.Contains(E, grupo.Membros);

        // Verifica que todos os 5 receberam GrupoDeFilaFormado e StatusDaFilaDeGrupo
        var avisosFormados = _mensagensEnviadas
            .Where(m => m.Id == SocialMessageIds.GrupoDeFilaFormado)
            .ToList();
        Assert.Equal(5, avisosFormados.Count);

        var statusFila = _mensagensEnviadas
            .Where(m => m.Id == SocialMessageIds.StatusDaFilaDeGrupo)
            .ToList();
        Assert.Equal(5, statusFila.Count);
        Assert.All(statusFila, s => Assert.False(((StatusDaFilaDeGrupo)s.Corpo).EmFila));
    }
}
