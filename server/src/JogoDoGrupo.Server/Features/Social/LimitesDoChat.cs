using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// O que <see cref="LimitesDoChat.Conferir"/> decidiu. <see cref="Codigo"/> nulo quer dizer que
/// a mensagem passou nos limites; <see cref="Texto"/> é o texto já limpo.
/// </summary>
public readonly record struct ResultadoDosLimites(
    string? Codigo,
    int SegundosRestantes,
    string Texto
)
{
    public bool Passou => Codigo == null;
}

/// <summary>
/// Limites do chat por personagem: tamanho, frequência, repetição e silêncio automático. Os
/// números e as contas vêm de <see cref="RegrasChat"/>; aqui fica só a memória de quem falou e
/// quando. Nada vai para o banco: reiniciar o servidor limpa tudo. Pode ser chamado de qualquer
/// thread.
/// </summary>
public sealed class LimitesDoChat(TimeProvider relogio) : ISilenciadorDoChat
{
    private sealed class Estado
    {
        public readonly Queue<DateTimeOffset> Aceitas = new();
        public readonly Queue<DateTimeOffset> Estouros = new();
        public readonly Dictionary<CanalDeChat, (string Chave, DateTimeOffset Em)> Ultimas = [];
        public DateTimeOffset? SilenciadoAte;
    }

    private readonly Lock _trava = new();
    private readonly Dictionary<long, Estado> _estados = [];

    /// <summary>
    /// Confere se o personagem pode mandar este texto agora. Não conta a mensagem: quem chama
    /// ainda vai ver se o canal e o alvo servem, e só então chama <see cref="Registrar"/>.
    /// Estourar a frequência fica anotado, e o terceiro estouro silencia.
    /// </summary>
    public ResultadoDosLimites Conferir(long personagemId, CanalDeChat canal, string? texto)
    {
        var agora = relogio.GetUtcNow();
        lock (_trava)
        {
            var estado = Obter(personagemId);
            Podar(estado, agora);

            if (estado.SilenciadoAte is { } silenciadoAte)
            {
                if (agora < silenciadoAte)
                {
                    var restante = (int)
                        Math.Max(1, Math.Ceiling((silenciadoAte - agora).TotalSeconds));
                    return new(CodigosErroSocial.Silenciado, restante, "");
                }

                estado.SilenciadoAte = null;
            }

            var limpo = RegrasChat.Limpar(texto);
            var erro = RegrasChat.Validar(limpo);
            if (erro != null)
            {
                return new(erro, 0, "");
            }

            if (RegrasChat.FrequenciaEstourou(estado.Aceitas.Count))
            {
                estado.Estouros.Enqueue(agora);
                if (!RegrasChat.DeveSilenciar(estado.Estouros.Count))
                {
                    return new(CodigosErroSocial.MensagensRapidasDemais, 0, "");
                }

                estado.Estouros.Clear();
                estado.SilenciadoAte = agora + TimeSpan.FromSeconds(RegrasChat.SilencioSegundos);
                return new(CodigosErroSocial.Silenciado, RegrasChat.SilencioSegundos, "");
            }

            if (
                estado.Ultimas.TryGetValue(canal, out var ultima)
                && ultima.Chave == RegrasChat.ChaveDeRepeticao(limpo)
                && RegrasChat.Repetida((agora - ultima.Em).TotalSeconds)
            )
            {
                return new(CodigosErroSocial.MensagemRepetida, 0, "");
            }

            return new(null, 0, limpo);
        }
    }

    /// <summary>Silencia o personagem por uma duração explícita (ex.: moderação/GM).</summary>
    public void Silenciar(long personagemId, TimeSpan duracao)
    {
        if (duracao <= TimeSpan.Zero)
        {
            return;
        }

        var agora = relogio.GetUtcNow();
        lock (_trava)
        {
            var estado = Obter(personagemId);
            var ate = agora + duracao;
            if (estado.SilenciadoAte == null || ate > estado.SilenciadoAte.Value)
            {
                estado.SilenciadoAte = ate;
            }
        }
    }

    /// <summary>Conta uma mensagem que foi aceita e entregue.</summary>
    public void Registrar(long personagemId, CanalDeChat canal, string textoLimpo)
    {
        var agora = relogio.GetUtcNow();
        lock (_trava)
        {
            var estado = Obter(personagemId);
            estado.Aceitas.Enqueue(agora);
            estado.Ultimas[canal] = (RegrasChat.ChaveDeRepeticao(textoLimpo), agora);
        }
    }

    /// <summary>
    /// Chamado quando o personagem sai do jogo: solta a memória dele se não houver nada que
    /// ainda valha. Silêncio e estouros recentes ficam, para sair e voltar não zerar a punição.
    /// </summary>
    public void Esquecer(long personagemId)
    {
        var agora = relogio.GetUtcNow();
        lock (_trava)
        {
            if (!_estados.TryGetValue(personagemId, out var estado))
            {
                return;
            }

            Podar(estado, agora);
            var silenciado = estado.SilenciadoAte is { } ate && agora < ate;
            if (!silenciado && estado.Estouros.Count == 0 && estado.Aceitas.Count == 0)
            {
                _estados.Remove(personagemId);
            }
        }
    }

    /// <summary>Quantos personagens têm memória guardada (para os testes).</summary>
    public int PersonagensLembrados
    {
        get
        {
            lock (_trava)
            {
                return _estados.Count;
            }
        }
    }

    private Estado Obter(long personagemId)
    {
        if (!_estados.TryGetValue(personagemId, out var estado))
        {
            estado = new Estado();
            _estados[personagemId] = estado;
        }

        return estado;
    }

    private static void Podar(Estado estado, DateTimeOffset agora)
    {
        while (
            estado.Aceitas.TryPeek(out var aceita)
            && !RegrasChat.ContaNaFrequencia((agora - aceita).TotalSeconds)
        )
        {
            estado.Aceitas.Dequeue();
        }

        while (
            estado.Estouros.TryPeek(out var estouro)
            && !RegrasChat.ContaComoEstouro((agora - estouro).TotalSeconds)
        )
        {
            estado.Estouros.Dequeue();
        }
    }
}
