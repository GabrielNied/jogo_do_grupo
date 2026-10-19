using JogoDoGrupo.Shared.Features.Social;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Social
{
    public class RegrasGuildaTestes
    {
        [Theory]
        [InlineData("Os Guardioes", true)]
        [InlineData("ABC", true)]
        [InlineData("Guilda dos Vinte e Quatr", true)] // 24 chars
        [InlineData("AB", false)] // < 3 chars
        [InlineData("Nome Muito Longo Para Uma Guilda Existir", false)] // > 24 chars
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(" Espaco No Inicio", false)]
        [InlineData("Espaco No Fim ", false)]
        [InlineData("Espaco  Duplo", false)]
        [InlineData("Guilda@123", false)]
        public void ValidarNome_regras_de_tamanho_e_caracteres(string nome, bool esperado)
        {
            Assert.Equal(esperado, RegrasGuilda.ValidarNome(nome));
        }

        [Fact]
        public void PodeConvidar_apenas_lider_e_oficial()
        {
            Assert.True(RegrasGuilda.PodeConvidar(CargoDaGuilda.Lider));
            Assert.True(RegrasGuilda.PodeConvidar(CargoDaGuilda.Oficial));
            Assert.False(RegrasGuilda.PodeConvidar(CargoDaGuilda.Membro));
            Assert.False(RegrasGuilda.PodeConvidar(CargoDaGuilda.Recruta));
        }

        [Fact]
        public void PodeExpulsar_hierarquia_de_cargos()
        {
            // Líder pode expulsar qualquer um menos outro líder
            Assert.False(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Lider, CargoDaGuilda.Lider));
            Assert.True(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Lider, CargoDaGuilda.Oficial));
            Assert.True(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Lider, CargoDaGuilda.Membro));
            Assert.True(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Lider, CargoDaGuilda.Recruta));

            // Oficial só expulsa membro e recruta
            Assert.False(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Oficial, CargoDaGuilda.Lider));
            Assert.False(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Oficial, CargoDaGuilda.Oficial));
            Assert.True(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Oficial, CargoDaGuilda.Membro));
            Assert.True(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Oficial, CargoDaGuilda.Recruta));

            // Membro e Recruta não expulsam ninguém
            Assert.False(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Membro, CargoDaGuilda.Recruta));
            Assert.False(RegrasGuilda.PodeExpulsar(CargoDaGuilda.Recruta, CargoDaGuilda.Recruta));
        }

        [Fact]
        public void PodeAlterarCargo_regras_de_promocao_e_rebaixamento()
        {
            // Ninguém altera para ou a partir de Líder via AlterarCargo
            Assert.False(
                RegrasGuilda.PodeAlterarCargo(
                    CargoDaGuilda.Lider,
                    CargoDaGuilda.Lider,
                    CargoDaGuilda.Oficial
                )
            );
            Assert.False(
                RegrasGuilda.PodeAlterarCargo(
                    CargoDaGuilda.Lider,
                    CargoDaGuilda.Oficial,
                    CargoDaGuilda.Lider
                )
            );

            // Líder altera entre Oficial, Membro e Recruta
            Assert.True(
                RegrasGuilda.PodeAlterarCargo(
                    CargoDaGuilda.Lider,
                    CargoDaGuilda.Membro,
                    CargoDaGuilda.Oficial
                )
            );
            Assert.True(
                RegrasGuilda.PodeAlterarCargo(
                    CargoDaGuilda.Lider,
                    CargoDaGuilda.Recruta,
                    CargoDaGuilda.Membro
                )
            );

            // Oficial só altera entre Membro e Recruta
            Assert.True(
                RegrasGuilda.PodeAlterarCargo(
                    CargoDaGuilda.Oficial,
                    CargoDaGuilda.Recruta,
                    CargoDaGuilda.Membro
                )
            );
            Assert.True(
                RegrasGuilda.PodeAlterarCargo(
                    CargoDaGuilda.Oficial,
                    CargoDaGuilda.Membro,
                    CargoDaGuilda.Recruta
                )
            );
            Assert.False(
                RegrasGuilda.PodeAlterarCargo(
                    CargoDaGuilda.Oficial,
                    CargoDaGuilda.Membro,
                    CargoDaGuilda.Oficial
                )
            );
        }

        [Fact]
        public void Banco_todos_depositam_apenas_lider_e_oficial_retiram()
        {
            Assert.True(RegrasGuilda.PodeDepositarNoBanco(CargoDaGuilda.Lider));
            Assert.True(RegrasGuilda.PodeDepositarNoBanco(CargoDaGuilda.Oficial));
            Assert.True(RegrasGuilda.PodeDepositarNoBanco(CargoDaGuilda.Membro));
            Assert.True(RegrasGuilda.PodeDepositarNoBanco(CargoDaGuilda.Recruta));

            Assert.True(RegrasGuilda.PodeRetirarDoBanco(CargoDaGuilda.Lider));
            Assert.True(RegrasGuilda.PodeRetirarDoBanco(CargoDaGuilda.Oficial));
            Assert.False(RegrasGuilda.PodeRetirarDoBanco(CargoDaGuilda.Membro));
            Assert.False(RegrasGuilda.PodeRetirarDoBanco(CargoDaGuilda.Recruta));
        }

        [Fact]
        public void Limites_canonicos_da_guilda()
        {
            Assert.Equal(100, RegrasGuilda.MaximoDeMembros);
            Assert.Equal(1000, RegrasGuilda.CustoCriacaoCoroas);
        }
    }
}
