using JogoDoGrupo.Shared.Features.Pets;

namespace JogoDoGrupo.Shared.Tests.Features.Pets;

public class RegrasPetsTestes
{
    private const float Passo = 0.1f;

    [Fact]
    public void Faixa_de_ids_comeca_em_17000()
    {
        Assert.Equal(17000, PetsMessageIds.Faixa.Inicio);
        Assert.Equal(17999, PetsMessageIds.Faixa.Fim);
    }

    [Fact]
    public void Dono_longe_o_pet_anda_na_velocidade_dele_sem_passar_do_ponto_de_parada()
    {
        var passo = RegrasPets.Seguir(0, 0, 0, 0, 10, andando: false, Passo);

        Assert.True(passo.Andando);
        Assert.False(passo.Alcancou);
        Assert.Equal(ConfigPets.VelocidadeMetrosPorSegundo * Passo, passo.Z, 3);
        Assert.Equal(0f, passo.X, 3);
        Assert.Equal(0f, passo.RotacaoY, 3);
    }

    [Fact]
    public void Pet_para_a_dois_metros_do_dono_e_nao_fica_em_cima_dele()
    {
        float x = 0,
            z = 0,
            rot = 0;
        var andando = false;
        for (var i = 0; i < 200; i++)
        {
            var passo = RegrasPets.Seguir(x, z, rot, 6, 8, andando, Passo);
            (x, z, rot, andando) = (passo.X, passo.Z, passo.RotacaoY, passo.Andando);
        }

        Assert.False(andando);
        Assert.Equal(ConfigPets.DistanciaDeSeguidaMetros, RegrasPets.Distancia(x, z, 6, 8), 3);
    }

    [Fact]
    public void Parado_dentro_da_folga_o_pet_nao_treme()
    {
        var dentro = ConfigPets.DistanciaDeSeguidaMetros + ConfigPets.FolgaDeSeguidaMetros - 0.01f;

        var passo = RegrasPets.Seguir(0, 0, 45, 0, dentro, andando: false, Passo);

        Assert.False(passo.Andando);
        Assert.Equal(0f, passo.Z);
        Assert.Equal(45f, passo.RotacaoY);
    }

    [Fact]
    public void Parado_alem_da_folga_o_pet_volta_a_andar()
    {
        var fora = ConfigPets.DistanciaDeSeguidaMetros + ConfigPets.FolgaDeSeguidaMetros + 0.1f;

        Assert.True(RegrasPets.Seguir(0, 0, 0, 0, fora, andando: false, Passo).Andando);
    }

    [Fact]
    public void Passando_de_vinte_metros_o_pet_volta_para_a_distancia_de_seguida()
    {
        var passo = RegrasPets.Seguir(
            0,
            0,
            0,
            0,
            ConfigPets.DistanciaParaAlcancarMetros + 5,
            andando: true,
            Passo
        );

        Assert.True(passo.Alcancou);
        Assert.False(passo.Andando);
        Assert.Equal(
            ConfigPets.DistanciaDeSeguidaMetros,
            RegrasPets.Distancia(passo.X, passo.Z, 0, ConfigPets.DistanciaParaAlcancarMetros + 5),
            3
        );
    }

    [Fact]
    public void Exatamente_no_limite_de_vinte_metros_ainda_anda()
    {
        var passo = RegrasPets.Seguir(
            0,
            0,
            0,
            0,
            ConfigPets.DistanciaParaAlcancarMetros,
            andando: true,
            Passo
        );

        Assert.False(passo.Alcancou);
        Assert.True(passo.Andando);
    }

    [Fact]
    public void Ir_ate_o_item_para_a_distancia_de_pegar()
    {
        float x = 0,
            z = 0;
        for (var i = 0; i < 200; i++)
        {
            var passo = RegrasPets.IrAte(x, z, 0, 3, 4, ConfigPets.DistanciaParaPegarMetros, Passo);
            (x, z) = (passo.X, passo.Z);
        }

        Assert.Equal(ConfigPets.DistanciaParaPegarMetros, RegrasPets.Distancia(x, z, 3, 4), 3);
    }

    [Fact]
    public void Chamado_o_pet_nasce_atras_do_dono_a_dois_metros()
    {
        // Dono olhando para +X (90 graus): atrás é -X.
        RegrasPets.PontoDeChegada(10, 10, 90, out var x, out var z);

        Assert.Equal(8f, x, 3);
        Assert.Equal(10f, z, 3);
    }

    [Fact]
    public void Direcao_zero_aponta_para_mais_z_e_noventa_para_mais_x()
    {
        Assert.Equal(0f, RegrasPets.Direcao(0, 1), 3);
        Assert.Equal(90f, RegrasPets.Direcao(1, 0), 3);
    }

    [Fact]
    public void Catalogo_de_pets_reconhece_cervo_jovem_do_sino()
    {
        Assert.Equal(
            EspecieDePet.CervoJovem,
            RegrasPets.EspecieDoItem(
                JogoDoGrupo.Shared.Features.Itens.CatalogoItens.SinoDoCervoJovem
            )
        );
        Assert.Equal((EspecieDePet)0, RegrasPets.EspecieDoItem(999));
        Assert.True(RegrasPets.EspecieValida(EspecieDePet.CervoJovem));
        Assert.False(RegrasPets.EspecieValida((EspecieDePet)999));
        Assert.False(RegrasPets.DesbloqueadoPorPadrao(EspecieDePet.CervoJovem));
        Assert.Equal("Cervo Jovem", RegrasPets.ObterNome(EspecieDePet.CervoJovem));
        Assert.Contains("Drop raro", RegrasPets.ObterOrigem(EspecieDePet.CervoJovem));
        Assert.NotEmpty(RegrasPets.ObterDescricao(EspecieDePet.CervoJovem));
    }

    [Fact]
    public void Mensagens_e_erros_de_pets_estao_definidos()
    {
        Assert.Equal(17006, PetsMessageIds.UsarItemPet);
        Assert.Equal(17007, PetsMessageIds.ColecaoPets);
        Assert.Equal(17008, PetsMessageIds.PetDesbloqueado);

        Assert.Equal("EmCombate", CodigosErroPets.EmCombate);
        Assert.Equal("JaPossuiPet", CodigosErroPets.JaPossuiPet);
        Assert.Equal("PetNaoDesbloqueado", CodigosErroPets.PetNaoDesbloqueado);
        Assert.Equal("ItemNaoEncontrado", CodigosErroPets.ItemNaoEncontrado);
        Assert.Equal("ItemNaoEPet", CodigosErroPets.ItemNaoEPet);
    }
}
