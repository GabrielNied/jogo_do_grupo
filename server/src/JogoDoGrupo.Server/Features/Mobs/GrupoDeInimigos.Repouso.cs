using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Rotas da manada, vagar em repouso e zona urbana (parte de <see cref="GrupoDeInimigos"/>).</summary>
public sealed partial class GrupoDeInimigos
{
    // A manada segue a rota: espera no ponto e, quando todos estão em repouso, parte para o
    // próximo, cada um para o seu lugar (o mesmo desvio em relação ao ponto de antes).
    private void AtualizarRota(long agora)
    {
        if (_rota == null)
        {
            return;
        }

        var seguidores = _inimigos
            .Where(i => i.Segue && RegrasInimigo.EstaVivo(i.Estado))
            .ToArray();
        if (seguidores.Length == 0)
        {
            return;
        }

        var todosParados = seguidores.All(i => i.Estado == EstadoDeInimigo.Repouso);
        if (_andando)
        {
            if (todosParados && seguidores.All(ChegouAoLugar))
            {
                _andando = false;
                _esperaAteMs = agora + EmMs(_rota.Pontos[_pontoDaRota].EsperaSegundos);
            }

            return;
        }

        if (agora < _esperaAteMs || !todosParados)
        {
            return;
        }

        _pontoDaRota = (_pontoDaRota + 1) % _rota.Pontos.Count;
        var ponto = _rota.Pontos[_pontoDaRota];
        foreach (var inimigo in seguidores)
        {
            inimigo.OrigemX = ponto.X + inimigo.DesvioX;
            inimigo.OrigemZ = ponto.Z + inimigo.DesvioZ;
        }

        _andando = true;
    }

    private static bool ChegouAoLugar(Inimigo inimigo) =>
        RegrasInimigo.Distancia(inimigo.X, inimigo.Z, inimigo.OrigemX, inimigo.OrigemZ)
        <= ConfigInimigosDoCampo.ToleranciaDeChegadaMetros;

    // Anda em direção ao lugar na rota, devagar, sem olhar para os jogadores.
    private void SeguirRota(Inimigo inimigo, double segundos)
    {
        if (ChegouAoLugar(inimigo))
        {
            return;
        }

        var direcaoDesejada = RegrasInimigo.DirecaoPara(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ,
            inimigo.Rotacao
        );
        inimigo.Rotacao = RegrasInimigo.GirarPara(
            inimigo.Rotacao,
            direcaoDesejada,
            RegrasInimigo.VelocidadeDeGiroGrausPorSegundo,
            segundos
        );
        RegrasInimigo.Avancar(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ,
            _rota!.VelocidadeMetrosPorSegundo,
            segundos,
            0.0,
            out var novoX,
            out var novoZ
        );
        inimigo.X = novoX;
        inimigo.Z = novoZ;
        if (ChegouAoLugar(inimigo))
        {
            inimigo.X = inimigo.OrigemX;
            inimigo.Z = inimigo.OrigemZ;
        }
    }

    // Fora de combate, a criatura comum vaga devagar num raio curto ao redor da sua origem.
    private void VagarEmRepouso(Inimigo inimigo, long agora, double segundos)
    {
        if (!inimigo.Vagando)
        {
            if (agora < inimigo.PausaVagarAteMs)
            {
                return;
            }

            var sorteioAngulo = _sorteador.Proximo();
            var sorteioDistancia = _sorteador.Proximo();
            RegrasInimigo.PontoDeVagar(
                inimigo.OrigemX,
                inimigo.OrigemZ,
                sorteioAngulo,
                sorteioDistancia,
                RegrasInimigo.RaioDeVagarEmRepousoMetros,
                out inimigo.DestinoVagarX,
                out inimigo.DestinoVagarZ
            );
            inimigo.Vagando = true;
        }

        var falta = RegrasInimigo.Distancia(
            inimigo.X,
            inimigo.Z,
            inimigo.DestinoVagarX,
            inimigo.DestinoVagarZ
        );
        if (falta <= ConfigInimigosDoCampo.ToleranciaDeChegadaMetros)
        {
            inimigo.X = inimigo.DestinoVagarX;
            inimigo.Z = inimigo.DestinoVagarZ;
            inimigo.Vagando = false;
            var esperaSegundos =
                RegrasInimigo.TempoParadoVagarMinimoSegundos
                + (
                    RegrasInimigo.TempoParadoVagarMaximoSegundos
                    - RegrasInimigo.TempoParadoVagarMinimoSegundos
                ) * _sorteador.Proximo();
            inimigo.PausaVagarAteMs = agora + EmMs(esperaSegundos);
            return;
        }

        var direcaoDesejada = RegrasInimigo.DirecaoPara(
            inimigo.X,
            inimigo.Z,
            inimigo.DestinoVagarX,
            inimigo.DestinoVagarZ,
            inimigo.Rotacao
        );
        inimigo.Rotacao = RegrasInimigo.GirarPara(
            inimigo.Rotacao,
            direcaoDesejada,
            RegrasInimigo.VelocidadeDeGiroGrausPorSegundo,
            segundos
        );

        RegrasInimigo.Avancar(
            inimigo.X,
            inimigo.Z,
            inimigo.DestinoVagarX,
            inimigo.DestinoVagarZ,
            RegrasInimigo.VelocidadeDeVagarEmRepousoMetrosPorSegundo,
            segundos,
            0.0,
            out var novoX,
            out var novoZ
        );

        Afastar(
            inimigo,
            novoX,
            novoZ,
            out novoX,
            out novoZ,
            RegrasInimigo.VelocidadeDeVagarEmRepousoMetrosPorSegundo * segundos
        );
        if (NaZonaUrbana(novoX, novoZ))
        {
            inimigo.Vagando = false;
            inimigo.PausaVagarAteMs = agora + EmMs(RegrasInimigo.TempoParadoVagarMinimoSegundos);
            return;
        }

        inimigo.X = novoX;
        inimigo.Z = novoZ;
    }

    // Só a manada tem zona urbana; os outros grupos ignoram a cidade como sempre.
    private bool NaZonaUrbana(double x, double z) =>
        _rota != null && RegrasManada.EstaNaZonaUrbana(x, z);
}
