using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Serviço autoritativo de consulta e avanço de progresso do bestiário (#922).
/// </summary>
public sealed class ServicoDeBestiario
{
    private readonly JogoDbContext _db;
    private readonly ILogger<ServicoDeBestiario> _logger;

    public ServicoDeBestiario(JogoDbContext db, ILogger<ServicoDeBestiario> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Retorna o estado de conhecimento de todas as criaturas do catálogo para o personagem.
    /// </summary>
    public async Task<EstadoDoBestiario> ObterBestiarioAsync(long personagemId)
    {
        var registros = await _db.Set<BestiarioPersonagemEntidade>()
            .AsNoTracking()
            .Where(b => b.PersonagemId == personagemId)
            .ToDictionaryAsync(b => b.CriaturaId);

        var entradas = new List<EntradaBestiarioDto>(CatalogoCriaturas.Todas.Length);

        foreach (var criatura in CatalogoCriaturas.Todas)
        {
            if (registros.TryGetValue(criatura.Id, out var reg))
            {
                var nivel = RegrasBestiario.CalcularNivel(criatura.Categoria, reg.Derrotas);
                entradas.Add(new EntradaBestiarioDto(criatura.Id, reg.Derrotas, nivel));
            }
            else
            {
                entradas.Add(
                    new EntradaBestiarioDto(
                        criatura.Id,
                        0,
                        NivelDeConhecimentoBestiario.Desconhecido
                    )
                );
            }
        }

        return new EstadoDoBestiario(entradas.ToArray());
    }

    /// <summary>
    /// Registra a derrota de uma espécie de criatura, avançando seu contador e nível de conhecimento.
    /// </summary>
    public async Task<EntradaBestiarioDto?> RegistrarDerrotaAsync(long personagemId, int criaturaId)
    {
        var criatura = CatalogoCriaturas.Obter(criaturaId);
        if (criatura == null)
        {
            _logger.LogWarning(
                "Tentativa de registrar derrota para criatura inexistente no catálogo: {CriaturaId}",
                criaturaId
            );
            return null;
        }

        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var registro = await _db.Set<BestiarioPersonagemEntidade>()
            .FirstOrDefaultAsync(b => b.PersonagemId == personagemId && b.CriaturaId == criaturaId);

        if (registro == null)
        {
            registro = new BestiarioPersonagemEntidade
            {
                PersonagemId = personagemId,
                CriaturaId = criaturaId,
                Derrotas = 1,
                PrimeiroRegistroEm = agora,
                UltimoRegistroEm = agora,
            };
            _db.Set<BestiarioPersonagemEntidade>().Add(registro);
        }
        else
        {
            registro.Derrotas++;
            registro.UltimoRegistroEm = agora;
        }

        await _db.SaveChangesAsync();

        var novoNivel = RegrasBestiario.CalcularNivel(criatura.Categoria, registro.Derrotas);
        return new EntradaBestiarioDto(criaturaId, registro.Derrotas, novoNivel);
    }
}
