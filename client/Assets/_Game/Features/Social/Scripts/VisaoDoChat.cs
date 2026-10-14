using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;

namespace JogoDoGrupo.Client.Features.Social
{
    internal enum FiltroDoChat
    {
        Todos,
        Cidade,
        Grupo,
        Sussurro,
        Sistema,
    }

    /// <summary>Filtra apenas a apresentação; o histórico recebido permanece intacto.</summary>
    internal static class VisaoDoChat
    {
        internal static List<LinhaDeChat> Filtrar(
            IReadOnlyList<LinhaDeChat> linhas,
            FiltroDoChat filtro,
            bool mostrarSistema,
            bool mostrarCombate
        )
        {
            var resultado = new List<LinhaDeChat>();
            foreach (var linha in linhas)
            {
                if (linha.Canal == CanalDaLinha.Sistema)
                {
                    if (
                        linha.Categoria == CategoriaDeAviso.Combate
                            ? !mostrarCombate
                            : !mostrarSistema
                    )
                    {
                        continue;
                    }
                }

                if (
                    filtro != FiltroDoChat.Todos
                    && !(filtro == FiltroDoChat.Cidade && linha.Canal == CanalDaLinha.Cidade)
                    && !(filtro == FiltroDoChat.Grupo && linha.Canal == CanalDaLinha.Grupo)
                    && !(filtro == FiltroDoChat.Sussurro && linha.Canal == CanalDaLinha.Sussurro)
                    && !(filtro == FiltroDoChat.Sistema && linha.Canal == CanalDaLinha.Sistema)
                )
                {
                    continue;
                }

                resultado.Add(linha);
            }

            return resultado;
        }
    }
}
