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

    [Theory]
    [InlineData(2067)]
    [InlineData(1555)]
    public void Reconhece_unicidade_do_sqlite(int codigoEstendido)
    {
        var erro = new SqliteException("unico", 19, codigoEstendido);
        Assert.True(new DbUpdateException("falha", erro).EhViolacaoDeUnicidade());
    }

    [Theory]
    [InlineData("23505", true)]
    [InlineData("23503", false)]
    [InlineData("40001", false)]
    public void Reconhece_unicidade_pelo_sqlstate(string estado, bool esperado)
    {
        Assert.Equal(
            esperado,
            new DbUpdateException("falha", new ErroComSqlState(estado)).EhViolacaoDeUnicidade()
        );
    }

    [Fact]
    public void Outros_erros_nao_sao_unicidade()
    {
        Assert.False(
            new DbUpdateException(
                "falha",
                new SqliteException("outro", 19, 787)
            ).EhViolacaoDeUnicidade()
        );
        Assert.False(new DbUpdateException("falha").EhViolacaoDeUnicidade());
        Assert.False(
            new DbUpdateException("falha", new Exception("23505")).EhViolacaoDeUnicidade()
        );
    }

    private sealed class ErroComSqlState(string estado) : DbException
    {
        public override string SqlState => estado;
    }
}
