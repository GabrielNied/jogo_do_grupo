using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class PrimeiraCavernaTestes
{
    private const float Passo = 0.5f;

    [Fact]
    public void A_boca_fica_nas_colinas_apoiada_no_chao()
    {
        Assert.Equal(
            Bioma.Colinas,
            MapaDoMundo.BiomaEm(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ)
        );
        Assert.True(
            EstruturasDoMapa.ForaDaCidade(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ)
        );
        Assert.True(EstruturasDoMapa.ChaoSeco(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ));
        Assert.Equal(
            MapaDoMundo.AlturaEm(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ),
            PrimeiraCaverna.EntradaY
        );
    }

    [Fact]
    public void O_morro_levanta_o_chao_so_dentro_do_raio()
    {
        Assert.Equal(
            PrimeiraCaverna.MorroAltura,
            PrimeiraCaverna.AlturaDoMorro(
                PrimeiraCaverna.MorroCentroX,
                PrimeiraCaverna.MorroCentroZ
            ),
            3
        );
        Assert.Equal(
            0f,
            PrimeiraCaverna.AlturaDoMorro(
                PrimeiraCaverna.MorroCentroX + PrimeiraCaverna.MorroRaio + 1f,
                PrimeiraCaverna.MorroCentroZ
            )
        );
    }

    [Fact]
    public void A_boca_comeca_no_chao_do_mundo()
    {
        PrimeiraCaverna.PontoDoPercurso(0f, out var x, out var z);

        Assert.Equal(PrimeiraCaverna.EntradaX, x, 3);
        Assert.Equal(PrimeiraCaverna.EntradaZ, z, 3);
        Assert.Equal(
            PrimeiraCaverna.EntradaY,
            PrimeiraCaverna.PisoEm(x, z, PrimeiraCaverna.EntradaY)!.Value,
            3
        );
    }

    [Fact]
    public void O_piso_e_continuo_da_boca_ao_fim_e_desce()
    {
        var anterior = PrimeiraCaverna.EntradaY;
        var comprimento = PrimeiraCaverna.ComprimentoDoPercurso;
        for (var d = 0f; d <= comprimento; d += Passo)
        {
            PrimeiraCaverna.PontoDoPercurso(d, out var x, out var z);
            var piso = PrimeiraCaverna.PisoNoPlano(x, z);

            Assert.True(piso.HasValue, $"sem piso a {d} m da boca");
            Assert.InRange(Math.Abs(piso!.Value - anterior), 0f, 0.3f);
            anterior = piso.Value;
        }

        Assert.True(anterior < PrimeiraCaverna.EntradaY - 8f, "o fim fica bem abaixo da boca");
    }

    [Fact]
    public void Fora_do_tunel_nao_ha_piso()
    {
        Assert.Null(PrimeiraCaverna.PisoNoPlano(0f, 0f));
        Assert.Null(PrimeiraCaverna.TetoEm(0f, 0f));
        Assert.False(PrimeiraCaverna.DentroDoTunel(0f, 0f, 0f));

        // Antes da boca (sem ponta arredondada) e nas paredes do corredor.
        Assert.Null(
            PrimeiraCaverna.PisoNoPlano(PrimeiraCaverna.EntradaX, PrimeiraCaverna.EntradaZ - 1f)
        );
        Assert.Null(
            PrimeiraCaverna.PisoNoPlano(
                PrimeiraCaverna.EntradaX + 4f,
                PrimeiraCaverna.EntradaZ + 1f
            )
        );
    }

    [Fact]
    public void Pela_altura_o_ponto_so_conta_entre_o_piso_e_o_teto()
    {
        PrimeiraCaverna.PontoDoPercurso(30f, out var x, out var z);
        var piso = PrimeiraCaverna.PisoNoPlano(x, z)!.Value;
        var teto = PrimeiraCaverna.TetoEm(x, z)!.Value;

        Assert.True(PrimeiraCaverna.DentroDoTunel(x, z, piso));
        Assert.True(PrimeiraCaverna.DentroDoTunel(x, z, teto));
        Assert.False(PrimeiraCaverna.DentroDoTunel(x, z, teto + 0.5f));
        Assert.False(
            PrimeiraCaverna.DentroDoTunel(x, z, piso - PrimeiraCaverna.FolgaAbaixoDoPiso - 0.5f)
        );
        Assert.Null(PrimeiraCaverna.PisoEm(x, z, MapaDoMundo.AlturaEm(x, z) + 2f));
    }

    [Fact]
    public void O_teto_fica_acima_do_piso_com_altura_para_o_personagem()
    {
        var comprimento = PrimeiraCaverna.ComprimentoDoPercurso;
        for (var d = 0f; d <= comprimento; d += Passo)
        {
            PrimeiraCaverna.PontoDoPercurso(d, out var x, out var z);
            var piso = PrimeiraCaverna.PisoNoPlano(x, z)!.Value;
            var teto = PrimeiraCaverna.TetoEm(x, z)!.Value;

            Assert.True(
                teto - piso >= PrimeiraCaverna.AlturaMinimaDePassagem,
                $"teto baixo a {d} m"
            );
        }
    }

    [Fact]
    public void A_largura_deixa_o_personagem_passar_em_todo_o_percurso()
    {
        var comprimento = PrimeiraCaverna.ComprimentoDoPercurso;
        for (var d = Passo; d < comprimento - Passo; d += Passo)
        {
            PrimeiraCaverna.PontoDoPercurso(d - 0.25f, out var ax, out var az);
            PrimeiraCaverna.PontoDoPercurso(d + 0.25f, out var bx, out var bz);
            var dx = bx - ax;
            var dz = bz - az;
            var tamanho = MathF.Sqrt((dx * dx) + (dz * dz));
            var nx = -dz / tamanho;
            var nz = dx / tamanho;
            PrimeiraCaverna.PontoDoPercurso(d, out var x, out var z);

            // Quanto se anda para cada lado, a partir do eixo, antes de bater na parede.
            var largura = 0f;
            foreach (var lado in new[] { 1f, -1f })
            {
                var m = 0f;
                while (
                    m < 20f
                    && PrimeiraCaverna
                        .PisoNoPlano(x + (nx * lado * m), z + (nz * lado * m))
                        .HasValue
                )
                {
                    m += 0.05f;
                }

                largura += m;
            }

            Assert.True(
                largura >= PrimeiraCaverna.LarguraMinimaDePassagem,
                $"estreito a {d} m: {largura}"
            );
        }
    }

    [Fact]
    public void Ha_duas_camaras_a_media_e_a_grande_no_fim()
    {
        PrimeiraCaverna.PontoDoPercurso(0f, out _, out _);
        var maior = 0f;
        var larguras = new List<float>();
        var comprimento = PrimeiraCaverna.ComprimentoDoPercurso;
        for (var d = 0f; d <= comprimento; d += 1f)
        {
            PrimeiraCaverna.PontoDoPercurso(d, out var x, out var z);
            var largura = 0f;
            // Largura medida em X e em Z: o maior vale como "diâmetro" da câmara.
            foreach (var (ox, oz) in new[] { (1f, 0f), (0f, 1f) })
            {
                var m = 0f;
                while (m < 20f && PrimeiraCaverna.PisoNoPlano(x + (ox * m), z + (oz * m)).HasValue)
                {
                    m += 0.1f;
                }

                var n = 0f;
                while (n < 20f && PrimeiraCaverna.PisoNoPlano(x - (ox * n), z - (oz * n)).HasValue)
                {
                    n += 0.1f;
                }

                largura = MathF.Max(largura, m + n);
            }

            larguras.Add(largura);
            maior = MathF.Max(maior, largura);
        }

        Assert.True(maior >= 18f, "a câmara grande tem uns 19 m");
        Assert.Contains(larguras, l => l is >= 12f and < 16f);
        Assert.True(larguras[^1] >= 14f, "o percurso termina na câmara grande");
    }

    [Fact]
    public void O_beco_e_fechado_e_acaba_na_ponta()
    {
        var ponta = PrimeiraCaverna.PisoNoPlano(
            PrimeiraCaverna.PontaDoBecoX,
            PrimeiraCaverna.PontaDoBecoZ
        );
        Assert.True(ponta.HasValue, "a ponta do beco tem piso");

        // O beco acompanha o piso do corredor onde sai (sem degrau) e termina em parede reta.
        PrimeiraCaverna.PontoDoPercurso(0f, out _, out _);
        Assert.Equal(ponta!.Value, PrimeiraCaverna.PisoNoPlano(-216f, -17f)!.Value, 2);
        var dx = PrimeiraCaverna.PontaDoBecoX - -215f;
        var dz = PrimeiraCaverna.PontaDoBecoZ - -6f;
        var tamanho = MathF.Sqrt((dx * dx) + (dz * dz));
        Assert.Null(
            PrimeiraCaverna.PisoNoPlano(
                PrimeiraCaverna.PontaDoBecoX + (dx / tamanho * 1.5f),
                PrimeiraCaverna.PontaDoBecoZ + (dz / tamanho * 1.5f)
            )
        );
    }

    [Fact]
    public void O_terreno_so_e_furado_perto_da_boca()
    {
        PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);
        var furados = 0;
        for (var z = minZ; z <= maxZ; z += 1f)
        {
            for (var x = minX; x <= maxX; x += 1f)
            {
                if (!PrimeiraCaverna.FuraOTerreno(x, z))
                {
                    continue;
                }

                furados++;
                Assert.True(
                    PrimeiraCaverna.Distancia(
                        x,
                        z,
                        PrimeiraCaverna.EntradaX,
                        PrimeiraCaverna.EntradaZ
                    ) < 22f,
                    $"furo longe da boca em ({x}, {z})"
                );
            }
        }

        Assert.True(furados > 0);
    }

    [Fact]
    public void Do_furo_em_diante_o_morro_cobre_o_teto_em_todo_o_tunel()
    {
        PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);
        for (var z = minZ; z <= maxZ; z += 0.5f)
        {
            for (var x = minX; x <= maxX; x += 0.5f)
            {
                var teto = PrimeiraCaverna.TetoEm(x, z);
                if (!teto.HasValue || PrimeiraCaverna.FuraOTerreno(x, z))
                {
                    continue;
                }

                Assert.True(
                    MapaDoMundo.AlturaEm(x, z) >= teto.Value,
                    $"o teto passa do terreno em ({x}, {z})"
                );
            }
        }
    }

    [Fact]
    public void Os_limites_contem_o_percurso_e_o_beco()
    {
        PrimeiraCaverna.Limites(out var minX, out var minZ, out var maxX, out var maxZ);

        Assert.True(minX < PrimeiraCaverna.CamaraFinalX && maxX > PrimeiraCaverna.CamaraFinalX);
        Assert.True(minZ < PrimeiraCaverna.EntradaZ && maxZ > PrimeiraCaverna.PontaDoBecoZ);
    }
}
