using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Perseguição, cerco em anel, ataque telegrafado e retorno (parte de <see cref="GrupoDeInimigos"/>).</summary>
public sealed partial class GrupoDeInimigos
{
    private void Perseguir(
        Inimigo inimigo,
        long agora,
        double segundos,
        Dictionary<long, JogadorNoCampo> presentes,
        long[] candidatos,
        List<long> perdidos,
        PassoDosInimigos passo
    )
    {
        inimigo.Alvo = Escolher(inimigo, candidatos, presentes, agora);
        if (inimigo.Alvo == RegrasInimigo.SemAlvo)
        {
            Retornar(inimigo);
            return;
        }

        // Vaga de atacante: só RegrasCerco.LimiteDeAtacantesPorAlvo atacam o mesmo alvo ao mesmo
        // tempo. Quem espera ronda no anel de fora, e isso não é kite (não conta no limite).
        var podeAtacar = RegrasCerco.PodeAtacar(AtacantesContra(inimigo));
        if (podeAtacar)
        {
            inimigo.SegundosSemAtacar += segundos;
        }
        else
        {
            inimigo.SegundosSemAtacar = 0.0;
        }

        var daOrigem = RegrasInimigo.Distancia(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ
        );
        if (inimigo.SegundosSemAtacar >= ConfigInimigosDoCampo.LimiteSemAtacarSegundos)
        {
            // Kite na borda: o alvo nunca fica ao alcance. Desiste como no limite de distância.
            perdidos.Add(inimigo.Alvo);
            Retornar(inimigo);
            return;
        }

        if (
            _rota == null
                ? RegrasInimigo.DeveDesistir(daOrigem)
                : RegrasManada.DeveDesistir(daOrigem, _rota.LimiteDePerseguicaoMetros)
        )
        {
            perdidos.Add(inimigo.Alvo);
            Retornar(inimigo);
            return;
        }

        if (!presentes.TryGetValue(inimigo.Alvo, out var alvo) || _queda.EstaCaido(inimigo.Alvo))
        {
            if (inimigo.Alvo != RegrasInimigo.SemAlvo)
            {
                perdidos.Add(inimigo.Alvo);
            }

            Retornar(inimigo);
            return;
        }

        // Vai ao posto dele no anel em volta do alvo (o de atacante, ou o de ronda se não há
        // vaga), em vez de em linha reta até o alvo, sem entrar no corpo dos outros.
        RegrasCerco.Posto(
            alvo.X,
            alvo.Z,
            Array.IndexOf(_inimigos, inimigo),
            !podeAtacar,
            out var postoX,
            out var postoZ
        );
        RegrasInimigo.Avancar(
            inimigo.X,
            inimigo.Z,
            postoX,
            postoZ,
            _comportamento.VelocidadeDePerseguicaoMetrosPorSegundo,
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
            _comportamento.VelocidadeDePerseguicaoMetrosPorSegundo * segundos
        );
        if (NaZonaUrbana(novoX, novoZ))
        {
            // A borda da cidade: a manada não passa. Encerra a perseguição e volta.
            perdidos.Add(inimigo.Alvo);
            Retornar(inimigo);
            return;
        }

        inimigo.X = novoX;
        inimigo.Z = novoZ;

        OlharParaOAlvo(inimigo, presentes, segundos);
        if (podeAtacar && NoAlcance(inimigo, alvo))
        {
            ComecarAtaque(inimigo, alvo, agora, passo);
        }
    }

    // Quantos do grupo (fora ele) já atacam o alvo dele: preparando o golpe ou se recuperando.
    private int AtacantesContra(Inimigo inimigo) =>
        _inimigos.Count(i =>
            i != inimigo && i.Estado == EstadoDeInimigo.Ataque && i.Alvo == inimigo.Alvo
        );

    // Empurra o ponto para fora dos corpos dos outros inimigos vivos do grupo.
    private void Afastar(
        Inimigo inimigo,
        double x,
        double z,
        out double novoX,
        out double novoZ,
        double maxDeslocamento = double.MaxValue
    )
    {
        Span<double> vizinhosX =
            _inimigos.Length <= 64
                ? stackalloc double[_inimigos.Length]
                : new double[_inimigos.Length];
        Span<double> vizinhosZ =
            _inimigos.Length <= 64
                ? stackalloc double[_inimigos.Length]
                : new double[_inimigos.Length];
        int count = 0;
        for (int i = 0; i < _inimigos.Length; i++)
        {
            var outro = _inimigos[i];
            if (outro != inimigo && RegrasInimigo.EstaVivo(outro.Estado))
            {
                vizinhosX[count] = outro.X;
                vizinhosZ[count] = outro.Z;
                count++;
            }
        }

        RegrasCerco.Afastar(
            x,
            z,
            vizinhosX.Slice(0, count),
            vizinhosZ.Slice(0, count),
            out novoX,
            out novoZ,
            maxDeslocamento
        );
    }

    // A direção trava aqui (a de Rotacao, olhando o alvo); o passo do golpe é medido já, até onde o caminho está livre.
    // Só começa se o golpe pegar o alvo de onde o passo termina; se um vizinho barra o passo,
    // devolve false e ele segue para o posto, em vez de errar o golpe parado fora do cone (#856).
    private bool ComecarAtaque(
        Inimigo inimigo,
        JogadorNoCampo alvo,
        long agora,
        PassoDosInimigos passo
    )
    {
        var direcao = RegrasInimigo.DirecaoPara(
            inimigo.X,
            inimigo.Z,
            alvo.X,
            alvo.Z,
            inimigo.Rotacao
        );
        var ate = RegrasInimigo.Distancia(inimigo.X, inimigo.Z, alvo.X, alvo.Z);
        var passoLivre = PassoLivre(
            inimigo,
            direcao,
            Math.Max(0.0, ate - RegrasInimigo.ParadaDoGolpeMetros)
        );
        var radianos = direcao * Math.PI / 180.0;
        if (
            !RegrasInimigo.EstaNoGolpe(
                inimigo.X + (Math.Sin(radianos) * passoLivre),
                inimigo.Z + (Math.Cos(radianos) * passoLivre),
                direcao,
                alvo.X,
                alvo.Z
            )
        )
        {
            return false;
        }

        inimigo.SegundosSemAtacar = 0.0;
        inimigo.Rotacao = direcao;
        inimigo.DirecaoDoGolpe = direcao;
        inimigo.PassoRestante = passoLivre;
        inimigo.Preparando = true;
        inimigo.FaseAteMs = agora + _comportamento.PreparoMs;
        Mudar(inimigo, EstadoDeInimigo.Ataque);
        // O golpe comum não tem área no chão: o aviso é o preparo do modelo.
        passo.Preparos.Add(
            new InimigoPreparaAtaque
            {
                InimigoId = inimigo.Id,
                PreparoMs = (int)_comportamento.PreparoMs,
                CentroX = (float)inimigo.X,
                CentroZ = (float)inimigo.Z,
                Raio = 0f,
                DirecaoY = (float)inimigo.DirecaoDoGolpe,
                Tipo = TipoDeAtaqueDeInimigo.Golpe,
                Areas = [],
            }
        );
        return true;
    }

    // Quanto do passo do golpe cabe na direção travada: para antes da borda da cidade (a
    // parede do campo) e antes de chegar a menos de 1,2 m de outro inimigo.
    private double PassoLivre(Inimigo inimigo, double direcao, double maximo) =>
        PassoDoGolpe.Livre(
            inimigo.X,
            inimigo.Z,
            direcao,
            (deX, deZ, x, z) => PodeIrPara(inimigo, deX, deZ, x, z),
            maximo
        );

    // Se o inimigo pode ir de um ponto a outro: fora da zona urbana e sem se aproximar de outro
    // inimigo vivo a menos da separação mínima.
    private bool PodeIrPara(Inimigo inimigo, double deX, double deZ, double x, double z)
    {
        if (NaZonaUrbana(x, z))
        {
            return false;
        }

        foreach (var outro in _inimigos)
        {
            if (
                outro != inimigo
                && RegrasInimigo.EstaVivo(outro.Estado)
                && !PassoDoGolpe.RespeitaVizinho(deX, deZ, x, z, outro.X, outro.Z)
            )
            {
                return false;
            }
        }

        return true;
    }

    // Durante o preparo ele avança o passo travado, em velocidade constante, até o fim do aviso.
    private void AvancarNoPreparo(Inimigo inimigo, double segundos)
    {
        if (inimigo.PassoRestante <= 0.0)
        {
            return;
        }

        var velocidade =
            RegrasInimigo.PassoDoGolpeMetros / Math.Max(0.001, _comportamento.PreparoMs / 1000.0);
        var andar = Math.Min(velocidade * segundos, inimigo.PassoRestante);
        var radianos = inimigo.DirecaoDoGolpe * Math.PI / 180.0;
        var novoX = inimigo.X + Math.Sin(radianos) * andar;
        var novoZ = inimigo.Z + Math.Cos(radianos) * andar;
        if (!PodeIrPara(inimigo, inimigo.X, inimigo.Z, novoX, novoZ))
        {
            inimigo.PassoRestante = 0.0;
            return;
        }

        inimigo.X = novoX;
        inimigo.Z = novoZ;
        inimigo.PassoRestante -= andar;
    }

    // O ataque, uma vez começado, vai até o fim: aviso, golpe e recuperação.
    private void Atacar(
        Inimigo inimigo,
        long agora,
        double segundos,
        Dictionary<long, JogadorNoCampo> presentes,
        JogadorNoCampo[] emOrdem,
        long[] candidatos,
        PassoDosInimigos passo
    )
    {
        if (agora < inimigo.FaseAteMs)
        {
            if (inimigo.Preparando)
            {
                AvancarNoPreparo(inimigo, segundos);
            }

            return;
        }

        if (inimigo.Preparando)
        {
            AvancarNoPreparo(inimigo, segundos);
            // O golpe: pega quem está no cone agora, de onde ele parou, tenha o inimigo visto ou não.
            foreach (var jogador in emOrdem)
            {
                if (
                    !_queda.EstaCaido(jogador.PersonagemId)
                    && !NaZonaUrbana(jogador.X, jogador.Z)
                    && RegrasInimigo.EstaNoGolpe(
                        inimigo.X,
                        inimigo.Z,
                        inimigo.DirecaoDoGolpe,
                        jogador.X,
                        jogador.Z
                    )
                )
                {
                    passo.Golpes.Add(
                        new GolpeDeInimigoAcertou(
                            inimigo.Id,
                            inimigo.Criatura.Definicao.Id,
                            jogador.PersonagemId,
                            inimigo.Criatura.Dano,
                            Area,
                            TipoDeAtaqueDeInimigo.Golpe
                        )
                    );
                    passo.Acertos.Add(
                        new InimigoAcertou { InimigoId = inimigo.Id, AlvoId = jogador.PersonagemId }
                    );
                }
            }

            inimigo.Preparando = false;
            inimigo.FaseAteMs = agora + _comportamento.RecuperacaoMs;
            return;
        }

        inimigo.Alvo = Escolher(inimigo, candidatos, presentes, agora);
        if (inimigo.Alvo == RegrasInimigo.SemAlvo)
        {
            Retornar(inimigo);
        }
        else
        {
            Mudar(inimigo, EstadoDeInimigo.Perseguicao);
        }
    }

    // Volta ao ponto de origem sem olhar para os jogadores.
    private static void Voltar(Inimigo inimigo, long agora, double segundos)
    {
        inimigo.Rotacao = RegrasInimigo.DirecaoPara(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ,
            inimigo.Rotacao
        );
        RegrasInimigo.Avancar(
            inimigo.X,
            inimigo.Z,
            inimigo.OrigemX,
            inimigo.OrigemZ,
            RegrasInimigo.VelocidadeDeRetornoMetrosPorSegundo,
            segundos,
            0.0,
            out var novoX,
            out var novoZ
        );
        inimigo.X = novoX;
        inimigo.Z = novoZ;

        var falta = RegrasInimigo.Distancia(inimigo.X, inimigo.Z, inimigo.OrigemX, inimigo.OrigemZ);
        if (falta <= ConfigInimigosDoCampo.ToleranciaDeChegadaMetros)
        {
            inimigo.X = inimigo.OrigemX;
            inimigo.Z = inimigo.OrigemZ;
            inimigo.Rotacao = ConfigInimigosDoCampo.DirecaoDeRepousoGraus;
            inimigo.Vagando = false;
            inimigo.PausaVagarAteMs = agora + EmMs(RegrasInimigo.TempoParadoVagarMinimoSegundos);
            Mudar(inimigo, EstadoDeInimigo.Repouso);
        }
    }

    // Desiste: larga o alvo e recupera a vida toda. A variante continua a mesma.
    private static void Retornar(Inimigo inimigo)
    {
        inimigo.Alvo = RegrasInimigo.SemAlvo;
        inimigo.Preparando = false;
        inimigo.Vagando = false;
        inimigo.SegundosSemAtacar = 0.0;
        inimigo.Participantes.Clear();
        inimigo.Ameacas.Limpar();
        if (inimigo.Vida != inimigo.Criatura.VidaMaxima)
        {
            inimigo.Vida = inimigo.Criatura.VidaMaxima;
            inimigo.VidaMudou = true;
        }

        Mudar(inimigo, EstadoDeInimigo.Retorno);
    }
}
