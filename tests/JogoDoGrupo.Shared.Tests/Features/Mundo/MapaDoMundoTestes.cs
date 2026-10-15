using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class MapaDoMundoTestes
{
    // Um ponto da grade a cada 30 m: 101 x 101 pontos cobrem o mundo inteiro.
    private const int Salto = 3;

    [Fact]
    public void Mundo_tem_3_km_e_a_grade_cobre_ele_inteiro()
    {
        Assert.Equal(3000f, MapaDoMundo.Lado);
        Assert.Equal(-1500f, MapaDoMundo.CoordenadaDaGrade(0));
        Assert.Equal(1500f, MapaDoMundo.CoordenadaDaGrade(MapaDoMundo.CelulasPorLado));
        Assert.Equal(MapaDoMundo.CelulasPorLado + 1, MapaDoMundo.PontosPorLado);
    }

    [Theory]
    [InlineData(1500f, -1500f, true)]
    [InlineData(0f, 0f, true)]
    [InlineData(1500.5f, 0f, false)]
    [InlineData(0f, -1500.5f, false)]
    [InlineData(float.NaN, 0f, false)]
    public void Dentro_do_mundo_vai_ate_a_borda(float x, float z, bool esperado) =>
        Assert.Equal(esperado, MapaDoMundo.DentroDoMundo(x, z));

    [Fact]
    public void Mesmo_ponto_da_sempre_o_mesmo_bioma_e_a_mesma_altura()
    {
        for (var i = 0; i <= MapaDoMundo.CelulasPorLado; i += 17)
        {
            for (var j = 0; j <= MapaDoMundo.CelulasPorLado; j += 13)
            {
                var x = MapaDoMundo.CoordenadaDaGrade(i) + 3.7f;
                var z = MapaDoMundo.CoordenadaDaGrade(j) - 1.2f;
                Assert.Equal(MapaDoMundo.AlturaEm(x, z), MapaDoMundo.AlturaEm(x, z));
                Assert.Equal(MapaDoMundo.BiomaEm(x, z), MapaDoMundo.BiomaEm(x, z));
            }
        }
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0f, -24f)]
    [InlineData(40f, 40f)]
    [InlineData(-40f, 12f)]
    public void Dentro_dos_muros_e_cidade(float x, float z) =>
        Assert.Equal(Bioma.Cidade, MapaDoMundo.BiomaEm(x, z));

    [Theory]
    [InlineData(350f, -250f, Bioma.Mata)]
    [InlineData(160f, -180f, Bioma.Campo)]
    [InlineData(-300f, 180f, Bioma.Colinas)]
    [InlineData(-920f, 1030f, Bioma.Neve)]
    [InlineData(-1080f, -680f, Bioma.Deserto)]
    [InlineData(1090f, 250f, Bioma.Pantano)]
    [InlineData(790f, -1180f, Bioma.Lava)]
    public void Regioes_novas_tem_identidade_sem_setores_radiais(float x, float z, Bioma esperado)
    {
        Assert.Equal(esperado, MapaDoMundo.BiomaEm(x, z));
        Assert.True(MapaDoMundo.PesosEm(x, z).De(esperado) > 0.5f);
    }

    [Fact]
    public void Pesos_dos_biomas_somam_um_e_nenhum_e_negativo()
    {
        foreach (var (x, z, _) in Pontos(Salto))
        {
            var p = MapaDoMundo.PesosEm(x, z);
            var todos = new[] { p.Campo, p.Mata, p.Colinas, p.Deserto, p.Neve, p.Lava, p.Pantano };

            Assert.All(todos, peso => Assert.InRange(peso, 0f, 1f));
            Assert.Equal(1f, todos.Sum(), 0.001f);
            Assert.Equal(todos.Max(), p.De(p.Dominante));
            Assert.NotEqual(Bioma.Cidade, p.Dominante);
            Assert.Equal(0f, p.De(Bioma.Cidade));
        }
    }

    [Theory]
    [InlineData(400f)]
    [InlineData(900f)]
    [InlineData(1400f)]
    public void Centros_dos_rios_altos_coincidem_com_os_leitos(float z)
    {
        Assert.True(MapaDoMundo.ProximoDeRio(MapaDoMundo.CentroDoRioGlacial(z), z, out var leste));
        Assert.InRange(leste, 0f, 0.001f);
        if (z <= 1200f)
        {
            Assert.True(
                MapaDoMundo.ProximoDeRio(MapaDoMundo.CentroDoRioDasCascatas(z), z, out var oeste)
            );
            Assert.InRange(oeste, 0f, 0.001f);
        }

        var nivel = MapaDoMundo.NivelDoRioAlto(z);
        Assert.True(nivel > MapaDoMundo.NivelDaAgua);
        Assert.True(nivel < MapaDoMundo.NivelDoRioAlto(z + 10f));
        Assert.Equal(
            nivel - 0.42f,
            MapaDoMundo.AlturaEm(MapaDoMundo.CentroDoRioGlacial(z), z),
            0.05f
        );
        Assert.True(MapaDoMundo.DentroDoRioAlto(MapaDoMundo.CentroDoRioGlacial(z), z));
        Assert.False(MapaDoMundo.DentroDoRioAlto(MapaDoMundo.CentroDoRioGlacial(z) + 30f, z));
    }

    [Fact]
    public void Rio_novo_nao_corta_a_caverna_ou_os_marcos_existentes()
    {
        Assert.False(MapaDoMundo.ProximoDeRio(-250f, -100f, out var distanciaRuina));
        Assert.False(MapaDoMundo.ProximoDeRio(-300f, -200f, out var distanciaPassagem));
        Assert.True(distanciaRuina > 60f);
        Assert.True(distanciaPassagem > 60f);
    }

    [Fact]
    public void Dentro_dos_muros_o_chao_fica_plano_em_zero()
    {
        foreach (var (x, z, altura) in Pontos(1))
        {
            if (Math.Max(Math.Abs(x), Math.Abs(z)) <= MapaDoMundo.MetadeDaCidade)
            {
                Assert.Equal(0f, altura);
            }
        }

        Assert.Equal(0f, MapaDoMundo.AlturaEm(0f, -24f));
        Assert.Equal(0f, MapaDoMundo.AlturaEm(3.3f, 7.1f));
    }

    [Fact]
    public void Cidades_novas_tem_patios_planos_e_ficam_separadas_de_rios()
    {
        foreach (
            var (x, z, altura) in new[]
            {
                (
                    MapaDoMundo.CidadeDaNeveX,
                    MapaDoMundo.CidadeDaNeveZ,
                    MapaDoMundo.CidadeDaNeveAltura
                ),
                (
                    MapaDoMundo.CidadeDoDesertoX,
                    MapaDoMundo.CidadeDoDesertoZ,
                    MapaDoMundo.CidadeDoDesertoAltura
                ),
            }
        )
        {
            Assert.True(MapaDoMundo.DentroDoMundo(x, z));
            Assert.False(MapaDoMundo.ProximoDeRio(x, z, out _));
            Assert.Equal(altura, MapaDoMundo.AlturaEm(x, z), 0.05f);
            Assert.Equal(altura, MapaDoMundo.AlturaEm(x + 20f, z + 20f), 0.05f);
        }
    }

    [Fact]
    public void Trilha_leste_cruza_o_rio_por_vau_raso()
    {
        const float x = 385f;
        const float z = 109f;
        Assert.True(MapaDoMundo.ProximoDeRio(x, z, out _));
        Assert.InRange(
            MapaDoMundo.AlturaEm(x, z),
            MapaDoMundo.NivelDaAgua - 0.5f,
            MapaDoMundo.NivelDaAgua + 1f
        );
    }

    [Fact]
    public void Corredores_de_exploracao_tem_eixos_continuos_e_chao_caminhavel()
    {
        foreach (var rota in CaminhosDoMundo.Rotas)
        {
            for (var i = 0; i + 1 < rota.Length; i++)
            {
                var a = rota[i];
                var b = rota[i + 1];
                var comprimento = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Z - a.Z, 2));
                var passos = (int)Math.Ceiling(comprimento / 10d);
                for (var passo = 0; passo <= passos; passo++)
                {
                    var t = passo / (float)passos;
                    var x = a.X + (b.X - a.X) * t;
                    var z = a.Z + (b.Z - a.Z) * t;
                    var altura = MapaDoMundo.AlturaEm(x, z);
                    Assert.True(MapaDoMundo.DentroDoMundo(x, z));
                    Assert.True(
                        MapaDoMundo.PesosEm(x, z).Lava < 0.4f
                            || altura >= MapaDoMundo.NivelDaLava + 1f,
                        $"Lava na rota em ({x}, {z})."
                    );
                    Assert.InRange(CaminhosDoMundo.DistanciaAteCaminho(x, z), 0f, 0.05f);
                    if (passo < passos)
                    {
                        var proximoT = (passo + 1) / (float)passos;
                        var proximoX = a.X + (b.X - a.X) * proximoT;
                        var proximoZ = a.Z + (b.Z - a.Z) * proximoT;
                        var proximaAltura = MapaDoMundo.AlturaEm(proximoX, proximoZ);
                        var distancia = (float)
                            Math.Sqrt(Math.Pow(proximoX - x, 2) + Math.Pow(proximoZ - z, 2));
                        Assert.True(
                            Math.Abs(proximaAltura - altura) < distancia,
                            $"Declive acima de 45 graus na rota em ({x}, {z})."
                        );
                    }
                }
            }
        }
    }

    [Fact]
    public void Faixa_fora_dos_muros_sobe_devagar_e_a_estrada_norte_acompanha()
    {
        var noPortao = MapaDoMundo.AlturaEm(0f, 80f);
        var naLateral = MapaDoMundo.AlturaEm(90f, 90f);

        Assert.InRange(noPortao, 1f, MapaDoMundo.SubidaMaximaDaFaixaInicial);
        Assert.True(noPortao > naLateral, "A estrada do portão sobe mais que as laterais.");
        Assert.InRange(MapaDoMundo.AlturaEm(0f, 80f) - MapaDoMundo.AlturaEm(0f, 50f), 0.5f, 4f);
    }

    [Fact]
    public void Altura_do_chao_fica_dentro_dos_limites_declarados()
    {
        foreach (var (x, z, altura) in Pontos(1))
        {
            Assert.True(float.IsFinite(altura), $"Altura inválida em ({x}, {z}).");
            Assert.InRange(altura, MapaDoMundo.AlturaMinima, MapaDoMundo.AlturaMaxima);
        }
    }

    [Fact]
    public void Miolo_em_volta_da_cidade_e_baixo_e_andavel()
    {
        // Os primeiros 200 m da mata em torno da entrada continuam andaveis.
        for (var i = 107; i < 193; i++)
        {
            for (var j = 107; j < 193; j++)
            {
                var x = MapaDoMundo.CoordenadaDaGrade(i);
                var z = MapaDoMundo.CoordenadaDaGrade(j);
                if (x * x + z * z > 200f * 200f)
                {
                    continue;
                }

                var altura = MapaDoMundo.AlturaNaGrade(i, j);
                Assert.InRange(altura, -1.2f, 25f);

                // Menos de 10 m de desnível em 10 m de chão: abaixo dos 45 graus que o
                // personagem sobe.
                Assert.InRange(Math.Abs(MapaDoMundo.AlturaNaGrade(i + 1, j) - altura), 0f, 9.5f);
                Assert.InRange(Math.Abs(MapaDoMundo.AlturaNaGrade(i, j + 1) - altura), 0f, 9.5f);
            }
        }
    }

    [Fact]
    public void Cada_bioma_tem_o_relevo_que_promete()
    {
        var maximo = new Dictionary<Bioma, float>();
        var minimo = new Dictionary<Bioma, float>();
        var pontos = new Dictionary<Bioma, int>();
        var alagados = 0;
        var comLava = 0;

        foreach (var (x, z, altura) in Pontos(Salto))
        {
            var bioma = MapaDoMundo.BiomaEm(x, z);
            pontos[bioma] = pontos.GetValueOrDefault(bioma) + 1;
            maximo[bioma] = Math.Max(maximo.GetValueOrDefault(bioma, float.MinValue), altura);
            minimo[bioma] = Math.Min(minimo.GetValueOrDefault(bioma, float.MaxValue), altura);

            if (bioma == Bioma.Pantano && altura < MapaDoMundo.NivelDaAgua)
            {
                alagados++;
            }

            if (altura < MapaDoMundo.NivelDaLava)
            {
                // Só existe chão abaixo do nível da lava nas montanhas de lava.
                Assert.Equal(Bioma.Lava, bioma);
                comLava++;
            }
        }

        // As massas principais ocupam regioes expressivas sem quotas de quadrantes.
        foreach (var bioma in new[] { Bioma.Deserto, Bioma.Neve, Bioma.Lava, Bioma.Pantano })
        {
            Assert.InRange(pontos[bioma], 500, 5000);
        }

        foreach (var bioma in new[] { Bioma.Campo, Bioma.Mata, Bioma.Colinas })
        {
            Assert.True(pontos.GetValueOrDefault(bioma) > 0);
        }

        Assert.InRange(maximo[Bioma.Neve], 100f, MapaDoMundo.AlturaMaxima);
        Assert.InRange(maximo[Bioma.Lava], 60f, MapaDoMundo.AlturaMaxima);
        Assert.True(maximo[Bioma.Neve] > maximo[Bioma.Lava], "A neve tem os picos mais altos.");
        Assert.InRange(maximo[Bioma.Colinas], 8f, 70f);
        Assert.InRange(minimo[Bioma.Deserto], 1f, 10f);
        // O vale do rio glacial pode ficar abaixo do terreno nevado ao redor.
        Assert.InRange(minimo[Bioma.Neve], -1f, 20f);
        Assert.InRange(minimo[Bioma.Pantano], -1f, MapaDoMundo.NivelDaAgua);
        Assert.True(alagados > 100);
        Assert.True(comLava > 20);
    }

    [Fact]
    public void Altura_entre_os_pontos_da_grade_segue_os_dois_triangulos_da_celula()
    {
        for (var i = 0; i < MapaDoMundo.CelulasPorLado; i += 11)
        {
            for (var j = 0; j < MapaDoMundo.CelulasPorLado; j += 7)
            {
                var x = MapaDoMundo.CoordenadaDaGrade(i);
                var z = MapaDoMundo.CoordenadaDaGrade(j);
                var origem = MapaDoMundo.AlturaNaGrade(i, j);
                var leste = MapaDoMundo.AlturaNaGrade(i + 1, j);
                var norte = MapaDoMundo.AlturaNaGrade(i, j + 1);
                var oposto = MapaDoMundo.AlturaNaGrade(i + 1, j + 1);

                // Nos cantos, a altura é a do ponto da grade.
                Assert.Equal(origem, MapaDoMundo.AlturaEm(x, z), 0.01f);

                // No meio da diagonal, a média dos dois cantos que ela liga.
                Assert.Equal((leste + norte) / 2f, MapaDoMundo.AlturaEm(x + 5f, z + 5f), 0.02f);

                // No meio de cada triângulo, a média dos três cantos dele.
                Assert.Equal(
                    (origem + leste + norte) / 3f,
                    MapaDoMundo.AlturaEm(x + 10f / 3f, z + 10f / 3f),
                    0.02f
                );
                Assert.Equal(
                    (oposto + leste + norte) / 3f,
                    MapaDoMundo.AlturaEm(x + 20f / 3f, z + 20f / 3f),
                    0.02f
                );
            }
        }
    }

    [Theory]
    [InlineData(2000f, 0f, 1500f, 0f)]
    [InlineData(-9999f, 700f, -1500f, 700f)]
    [InlineData(300f, 1e9f, 300f, 1500f)]
    [InlineData(-1e9f, -1e9f, -1500f, -1500f)]
    public void Fora_do_mundo_vale_a_altura_da_borda(
        float x,
        float z,
        float bordaX,
        float bordaZ
    ) => Assert.Equal(MapaDoMundo.AlturaEm(bordaX, bordaZ), MapaDoMundo.AlturaEm(x, z), 0.01f);

    [Theory]
    [InlineData(float.NaN, 0f)]
    [InlineData(0f, float.NaN)]
    [InlineData(float.PositiveInfinity, float.NegativeInfinity)]
    public void Coordenada_invalida_nao_quebra_a_consulta_de_altura(float x, float z) =>
        Assert.True(float.IsFinite(MapaDoMundo.AlturaEm(x, z)));

    [Fact]
    public void Indice_fora_da_grade_vale_o_da_borda()
    {
        var ultimo = MapaDoMundo.CelulasPorLado;
        Assert.Equal(MapaDoMundo.AlturaNaGrade(0, 40), MapaDoMundo.AlturaNaGrade(-5, 40));
        Assert.Equal(MapaDoMundo.AlturaNaGrade(ultimo, 40), MapaDoMundo.AlturaNaGrade(999, 40));
        Assert.Equal(MapaDoMundo.AlturaNaGrade(40, ultimo), MapaDoMundo.AlturaNaGrade(40, 999));
    }

    [Fact]
    public void Amostra_da_grade_traz_a_altura_e_os_pesos_do_mesmo_ponto()
    {
        var altura = MapaDoMundo.AmostrarNaGrade(150, 270, out var pesos);

        Assert.Equal(MapaDoMundo.AlturaNaGrade(150, 270), altura);
        Assert.Equal(Bioma.Neve, pesos.Dominante);
        Assert.Equal(MapaDoMundo.PesosEm(0f, 1200f).Neve, pesos.Neve);
    }

    // Cada ponto da grade, de "salto" em "salto": as coordenadas e a altura do chão nele.
    private static IEnumerable<(float X, float Z, float Altura)> Pontos(int salto)
    {
        for (var i = 0; i <= MapaDoMundo.CelulasPorLado; i += salto)
        {
            for (var j = 0; j <= MapaDoMundo.CelulasPorLado; j += salto)
            {
                yield return (
                    MapaDoMundo.CoordenadaDaGrade(i),
                    MapaDoMundo.CoordenadaDaGrade(j),
                    MapaDoMundo.AlturaNaGrade(i, j)
                );
            }
        }
    }
}
