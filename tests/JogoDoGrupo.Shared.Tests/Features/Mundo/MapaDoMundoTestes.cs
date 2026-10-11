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
    [InlineData(0f, 250f, Bioma.Campo)]
    [InlineData(0f, 300f, Bioma.Campo)]
    [InlineData(250f, -100f, Bioma.Mata)]
    [InlineData(300f, -200f, Bioma.Mata)]
    [InlineData(-250f, -100f, Bioma.Colinas)]
    [InlineData(-300f, -200f, Bioma.Colinas)]
    [InlineData(0f, 1000f, Bioma.Neve)]
    [InlineData(0f, 1200f, Bioma.Neve)]
    [InlineData(0f, -1000f, Bioma.Lava)]
    [InlineData(0f, -1200f, Bioma.Lava)]
    [InlineData(-1000f, 0f, Bioma.Deserto)]
    [InlineData(-1200f, 0f, Bioma.Deserto)]
    [InlineData(1000f, 0f, Bioma.Pantano)]
    [InlineData(1200f, 0f, Bioma.Pantano)]
    public void Cada_bioma_fica_do_seu_lado(float x, float z, Bioma esperado)
    {
        Assert.Equal(esperado, MapaDoMundo.BiomaEm(x, z));
        Assert.Equal(1f, MapaDoMundo.PesosEm(x, z).De(esperado), 0.001f);
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

    [Fact]
    public void Cidade_e_a_faixa_em_volta_ficam_planas_em_zero()
    {
        foreach (var (x, z, altura) in Pontos(1))
        {
            if (Math.Max(Math.Abs(x), Math.Abs(z)) <= MapaDoMundo.RaioPlano)
            {
                Assert.Equal(0f, altura);
            }
        }

        // O nascimento, a praça e o fim da estrada do portão (80 m ao norte).
        Assert.Equal(0f, MapaDoMundo.AlturaEm(0f, -24f));
        Assert.Equal(0f, MapaDoMundo.AlturaEm(3.3f, 7.1f));
        Assert.Equal(0f, MapaDoMundo.AlturaEm(0f, 80f));
        Assert.Equal(0f, MapaDoMundo.AlturaEm(-119f, 119f));
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
        // Até 430 m do centro só há campo, mata e colinas: nada de montanha nem de paredão.
        for (var i = 107; i < 193; i++)
        {
            for (var j = 107; j < 193; j++)
            {
                var altura = MapaDoMundo.AlturaNaGrade(i, j);
                Assert.InRange(altura, 0f, 25f);

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

        // Os quatro biomas de fora ocupam, cada um, perto de um quinto do mundo.
        foreach (var bioma in new[] { Bioma.Deserto, Bioma.Neve, Bioma.Lava, Bioma.Pantano })
        {
            Assert.InRange(pontos[bioma], 1500, 2900);
        }

        foreach (var bioma in new[] { Bioma.Campo, Bioma.Mata, Bioma.Colinas })
        {
            Assert.InRange(pontos[bioma], 150, 900);
        }

        Assert.InRange(maximo[Bioma.Neve], 100f, MapaDoMundo.AlturaMaxima);
        Assert.InRange(maximo[Bioma.Lava], 60f, MapaDoMundo.AlturaMaxima);
        Assert.True(maximo[Bioma.Neve] > maximo[Bioma.Lava], "A neve tem os picos mais altos.");
        Assert.InRange(maximo[Bioma.Colinas], 8f, 25f);
        Assert.InRange(minimo[Bioma.Deserto], 1f, 10f);
        Assert.InRange(minimo[Bioma.Neve], 1f, 20f);
        Assert.InRange(minimo[Bioma.Pantano], -1f, MapaDoMundo.NivelDaAgua);
        Assert.InRange(alagados, 300, 1200);
        Assert.InRange(comLava, 40, 400);
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
