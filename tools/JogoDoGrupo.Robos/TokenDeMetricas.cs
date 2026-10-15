using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JogoDoGrupo.Robos;

/// <summary>
/// Lê <c>GET /admin/metricas</c>. A rota exige a claim <c>permissao=administrar</c> e nenhuma conta
/// tem essa claim; o robô assina um token curto (5 min) com a chave JWT que a pessoa passar. Só
/// funciona onde ela já tem a chave do servidor (no desenvolvimento, a do
/// appsettings.Development.json, que o servidor recusa fora de Development).
/// </summary>
public static class TokenDeMetricas
{
    public const string Rota = "/admin/metricas";

    public static async Task<TickDoServidor?> LerAsync(
        HttpClient http,
        OpcoesDeCarga opcoes,
        CancellationToken parada
    )
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, Rota);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Assinar(opcoes));
        using var resposta = await http.SendAsync(pedido, parada);
        if (!resposta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"{Rota} respondeu {(int)resposta.StatusCode} (a chave JWT é a do servidor?)"
            );
        }

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(parada);
        var passos = corpo.GetProperty("Passos");
        return new TickDoServidor
        {
            JogadoresOnline = corpo.GetProperty("JogadoresOnline").GetInt32(),
            ErrosNoUltimoMinuto = corpo.GetProperty("ErrosNoUltimoMinuto").GetInt32(),
            PassosPorSegundo = passos.GetProperty("PassosPorSegundo").GetDouble(),
            DuracaoP50Ms = passos.GetProperty("DuracaoP50Ms").GetDouble(),
            DuracaoP95Ms = passos.GetProperty("DuracaoP95Ms").GetDouble(),
            DuracaoMaximaMs = passos.GetProperty("DuracaoMaximaMs").GetDouble(),
        };
    }

    // JWT HS256 mínimo, com os mesmos emissor e audiência que o servidor valida.
    private static string Assinar(OpcoesDeCarga opcoes)
    {
        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cabecalho = Base64Url(
            JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" })
        );
        var carga = Base64Url(
            JsonSerializer.SerializeToUtf8Bytes(
                new Dictionary<string, object>
                {
                    ["sub"] = "0",
                    ["permissao"] = "administrar",
                    ["iss"] = opcoes.EmissorJwt,
                    ["aud"] = opcoes.AudienciaJwt,
                    ["iat"] = agora,
                    ["nbf"] = agora,
                    ["exp"] = agora + 300,
                }
            )
        );
        var assinado = $"{cabecalho}.{carga}";
        var assinatura = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(opcoes.ChaveJwt!),
            Encoding.ASCII.GetBytes(assinado)
        );
        return $"{assinado}.{Base64Url(assinatura)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
