using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Morte, reposição e montagem do grupo (parte de <see cref="GrupoDeInimigos"/>).</summary>
public sealed partial class GrupoDeInimigos
{
    private void Matar(Inimigo inimigo)
    {
        Mudar(inimigo, EstadoDeInimigo.Morto);
        inimigo.Alvo = RegrasInimigo.SemAlvo;
        inimigo.Preparando = false;
        inimigo.Ameacas.Limpar();

        if (_aparicao != null)
        {
            // O chefe raro não volta por tempo fixo: a espera recomeça e o sorteio decide.
            _aparicao.RegistrarMorte(Instante(_relogioMs()));
        }
        else if (!_semReposicao)
        {
            if (inimigo.ContaNoLimite)
            {
                _limite?.Liberar();
            }

            // Depois do intervalo da ficha (com uma folga sorteada, para os grupos não voltarem
            // juntos), encurtado pela lotação do ponto (ConfigRespawn): sem sobrevivente o grupo todo volta contado da última morte; com
            // sobrevivente, o próprio inimigo volta (veja ReporMortos).
            var intervalo = inimigo.Criatura.Definicao.IntervaloDeAparicaoSegundos;
            if (intervalo > 0)
            {
                var espera = EmMs(
                    ConfigRespawn.Intervalo(intervalo, _lotacao)
                        * (
                            1.0
                            + (ConfigReposicaoDeInimigos.VariacaoDoIntervalo * _sorteador.Proximo())
                        )
                );
                if (_inimigos.All(i => i.Estado == EstadoDeInimigo.Morto))
                {
                    _ondaEmMs = _relogioMs() + espera;
                }
                else if (inimigo.Reponivel)
                {
                    inimigo.ReporEmMs = _relogioMs() + espera;
                }
            }
        }

        _pendente.Mortes.Add(new InimigoMorreu { InimigoId = inimigo.Id });
        _pendente.Derrotas.Add(
            new InimigoDerrotado(
                inimigo.Id,
                inimigo.Criatura.Definicao.Id,
                inimigo.Criatura.Variante,
                inimigo.Criatura.Experiencia,
                inimigo.Criatura.MultiplicadorDeRecompensa,
                Area,
                inimigo.X,
                inimigo.Z,
                inimigo.Participantes.Order().ToArray(),
                _proximaMorte++,
                inimigo.Criatura.Nivel
            )
        );
    }

    // Reposição individual: com ao menos um sobrevivente e o grupo todo fora de luta, o morto
    // cujo intervalo passou volta no lugar dele, se houver vaga no teto de vivos. Em luta ou sem
    // vaga ele espera (sem vaga, tenta de novo daqui a pouco).
    private void ReporMortos(long agora, List<InimigoApareceu> aparicoes)
    {
        if (
            _ondaEmMs != null
            || !_inimigos.Any(i => i.ReporEmMs is { } quando && agora >= quando)
            || !_inimigos.Any(i => RegrasInimigo.EstaVivo(i.Estado))
            || !_inimigos.All(ForaDeLuta)
        )
        {
            return;
        }

        for (var i = 0; i < _inimigos.Length; i++)
        {
            var morto = _inimigos[i];
            if (morto.ReporEmMs is not { } quando || agora < quando)
            {
                continue;
            }

            if (_limite != null && _limite.Reservar(1) == 0)
            {
                morto.ReporEmMs = agora + EmMs(ConfigInimigosDoCampo.TentativaDeVagaSegundos);
                continue;
            }

            // Uma manada em marcha repõe o membro no ponto da rota para onde ela vai.
            var lugarX = morto.OrigemX;
            var lugarZ = morto.OrigemZ;
            if (_rota != null)
            {
                lugarX = _rota.Pontos[_pontoDaRota].X + morto.DesvioX;
                lugarZ = _rota.Pontos[_pontoDaRota].Z + morto.DesvioZ;
            }

            var novo = Criar(lugarX, lugarZ, _limite != null);
            novo.DesvioX = morto.DesvioX;
            novo.DesvioZ = morto.DesvioZ;
            _inimigos[i] = novo;
            aparicoes.Add(new InimigoApareceu { Inimigo = Retrato(novo) });
        }
    }

    // Sorteia o tamanho do grupo e o põe no mundo, respeitando o teto de vivos. Sem vaga, tenta
    // de novo daqui a pouco.
    private void MontarGrupo(long agora, List<InimigoApareceu>? aparicoes)
    {
        var desejado =
            _tamanhoMinimo == Capacidade
                ? Capacidade
                : EscalaPorNivel.Sortear(_tamanhoMinimo, Capacidade, _sorteador.Proximo());
        var vagas = _limite?.Reservar(desejado) ?? desejado;
        if (vagas == 0)
        {
            _inimigos = [];
            _ondaEmMs = agora + EmMs(ConfigInimigosDoCampo.TentativaDeVagaSegundos);
            return;
        }

        var novos = new Inimigo[vagas];
        for (var i = 0; i < vagas; i++)
        {
            novos[i] = CriarNoCirculo(i, vagas, _limite != null);
        }

        _inimigos = novos;
        _ondaEmMs = null;
        _pontoDaRota = 0;
        _andando = false;
        _esperaAteMs = agora + EmMs(_rota?.Pontos[0].EsperaSegundos ?? 0.0);
        if (_semReposicao)
        {
            foreach (var inimigo in novos)
            {
                inimigo.Reponivel = false;
            }
        }

        aparicoes?.AddRange(novos.Select(n => new InimigoApareceu { Inimigo = Retrato(n) }));
    }

    // Os pontos de origem ficam num círculo em volta do centro, a distâncias iguais.
    private Inimigo CriarNoCirculo(int indice, int total, bool contaNoLimite)
    {
        var angulo = 2.0 * Math.PI * indice / total;
        return Criar(
            _pontoX + (Math.Sin(angulo) * ConfigInimigosDoCampo.RaioDoEspalhamentoMetros),
            _pontoZ + (Math.Cos(angulo) * ConfigInimigosDoCampo.RaioDoEspalhamentoMetros),
            contaNoLimite
        );
    }
}
