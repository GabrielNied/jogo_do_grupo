using System.Data.Common;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Tests.Core.Persistencia;

public class ErrosBancoTestes
{
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void Reconhece_sqlite_ocupado_direto_e_dentro_do_ef(int codigo)
    {
        var erro = new SqliteException("ocupado", codigo);
        Assert.True(erro.EhBancoOcupado());
        Assert.True(
            new DbUpdateException("falha", new Exception("interna", erro)).EhBancoOcupado()
        );
    }

    [Theory]
    [InlineData(1)]
    [InlineData(19)]
    public void Outros_erros_sqlite_nao_sao_banco_ocupado(int codigo)
    {
        Assert.False(new SqliteException("outro erro", codigo).EhBancoOcupado());
    }

    [Theory]
    [InlineData("40001", true)]
    [InlineData("23505", false)]
    [InlineData("08006", false)]
    public void Reconhece_apenas_sqlstate_de_serializacao(string estado, bool esperado)
    {
        var erro = new ErroComSqlState(estado);
        Assert.Equal(esperado, erro.EhBancoOcupado());
        Assert.Equal(esperado, new DbUpdateException("falha", erro).EhBancoOcupado());
    }

    [Fact]
    public void Erro_generico_nao_e_banco_ocupado()
    {
        Assert.False(new Exception("SQLITE_BUSY 40001").EhBancoOcupado());
    }

    private sealed class ErroComSqlState(string estado) : DbException
    {
        public override string SqlState => estado;
    }
}
