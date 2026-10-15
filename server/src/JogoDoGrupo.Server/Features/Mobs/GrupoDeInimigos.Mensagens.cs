using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Montagem das mensagens a enviar (parte de <see cref="GrupoDeInimigos"/>).</summary>
public sealed partial class GrupoDeInimigos
{
    private static InimigoNoMundo Retrato(Inimigo inimigo) =>
        new()
        {
            InimigoId = inimigo.Id,
            CriaturaId = inimigo.Criatura.Definicao.Id,
            Variante = inimigo.Criatura.Variante,
            Nivel = inimigo.Criatura.Nivel,
            X = (float)inimigo.X,
            Y = Altura(inimigo),
            Z = (float)inimigo.Z,
            RotacaoY = (float)inimigo.Rotacao,
            Vida = inimigo.Vida,
            VidaMaxima = inimigo.Criatura.VidaMaxima,
            Estado = inimigo.Estado,
            AlvoId = inimigo.Alvo,
        };

    private static InimigoMudou Mudanca(Inimigo inimigo) =>
        new()
        {
            InimigoId = inimigo.Id,
            X = (float)inimigo.X,
            Y = Altura(inimigo),
            Z = (float)inimigo.Z,
            RotacaoY = (float)inimigo.Rotacao,
            Estado = inimigo.Estado,
            AlvoId = inimigo.Alvo,
            Vida = inimigo.Vida,
            Sequencia = inimigo.Sequencia,
        };

    // O inimigo anda no plano; a altura é a do chão do ponto, a mesma que o client desenha.
    private static float Altura(Inimigo inimigo) =>
        MapaDoMundo.AlturaEm((float)inimigo.X, (float)inimigo.Z);
}
