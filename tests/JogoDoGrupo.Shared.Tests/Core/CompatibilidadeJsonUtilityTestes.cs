using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JogoDoGrupo.Shared.Core.Contas;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Core;

/// <summary>
/// O client lê e grava as mensagens com o <c>JsonUtility</c> do Unity, que só enxerga campos
/// públicos, e só entra em objetos aninhados marcados com <c>[Serializable]</c>. Uma propriedade
/// (<c>{ get; set; }</c>) num DTO some em silêncio no client: o servidor manda o valor e o client
/// vê o padrão (ou manda <c>{}</c>). Foi assim que o diálogo com NPC, o diário de missões e os
/// efeitos de status pararam de funcionar sem nenhum erro. Estes testes barram a volta disso.
/// </summary>
public class CompatibilidadeJsonUtilityTestes
{
    private static readonly Assembly Shared = typeof(LoginRequest).Assembly;

    // Campos que nunca passam pelo client: resultado de cálculo local e relatório só do painel de GM.
    private static readonly HashSet<string> SoNoServidor = new HashSet<string>
    {
        "JogoDoGrupo.Shared.Features.Talentos.ValidacaoDePerfil.RanksAplicaveis",
        "JogoDoGrupo.Shared.Core.Api.RelatorioRecusasContaDto.RecusasPorMotivo",
    };

    private static IEnumerable<Type> TiposDoShared() =>
        Shared
            .GetTypes()
            .Where(t =>
                t.IsPublic
                && t.Namespace != null
                && t.Namespace.StartsWith("JogoDoGrupo.Shared", StringComparison.Ordinal)
                && (t.IsClass || (t.IsValueType && !t.IsEnum))
                && !(t.IsAbstract && t.IsSealed)
            );

    [Fact]
    public void Nenhuma_classe_do_shared_tem_propriedade_publica_com_set()
    {
        var problemas = new List<string>();
        foreach (var tipo in TiposDoShared())
        {
            foreach (
                var propriedade in tipo.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                )
            )
            {
                if (propriedade.SetMethod != null && propriedade.SetMethod.IsPublic)
                {
                    problemas.Add(tipo.FullName + "." + propriedade.Name);
                }
            }
        }

        Assert.True(
            problemas.Count == 0,
            "Troque por campo público (o JsonUtility do Unity ignora propriedades): "
                + string.Join(", ", problemas)
        );
    }

    [Fact]
    public void Objetos_aninhados_em_campos_publicos_sao_serializaveis()
    {
        var problemas = new List<string>();
        foreach (var tipo in TiposDoShared())
        {
            foreach (
                var campo in tipo.GetFields(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                )
            )
            {
                if (SoNoServidor.Contains(tipo.FullName + "." + campo.Name))
                {
                    continue;
                }

                var elemento = TipoDoElemento(campo.FieldType);
                if (
                    elemento.IsGenericType
                    && elemento.GetGenericTypeDefinition() == typeof(Nullable<>)
                )
                {
                    problemas.Add(
                        tipo.FullName + "." + campo.Name + " (Nullable não vai no JSON do Unity)"
                    );
                    continue;
                }

                if (
                    elemento.IsGenericType
                    && elemento.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                )
                {
                    problemas.Add(
                        tipo.FullName + "." + campo.Name + " (Dictionary não vai no JSON do Unity)"
                    );
                    continue;
                }

                if (elemento.Assembly != Shared || elemento.IsEnum)
                {
                    continue;
                }

                if (!elemento.IsSerializable)
                {
                    problemas.Add(
                        tipo.FullName
                            + "."
                            + campo.Name
                            + " → "
                            + elemento.Name
                            + " sem [Serializable]"
                    );
                }
            }
        }

        Assert.True(
            problemas.Count == 0,
            "O JsonUtility do Unity não entra nesses objetos: " + string.Join("; ", problemas)
        );
    }

    private static Type TipoDoElemento(Type tipo)
    {
        if (tipo.IsArray)
        {
            return tipo.GetElementType()!;
        }

        if (tipo.IsGenericType && tipo.GetGenericTypeDefinition() == typeof(List<>))
        {
            return tipo.GetGenericArguments()[0];
        }

        return tipo;
    }
}
