using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// O chat sem tela: junta o que a pessoa digita (interpretador), o que o servidor responde
    /// (mensagem, recusa, histórico da Cidade), o histórico de 100 linhas e os ignorados. A rede
    /// entra por delegado, então roda sem servidor. Não decide nada: o servidor valida e entrega.
    /// </summary>
    internal sealed class ServicoDeChat
    {
        public const int DuracaoDaRecusaSocialSegundos = 6;

        private readonly Func<EnviarMensagemDeChat, bool> _enviar;
        private readonly Func<long> _meuId;
        private readonly Func<long> _agoraUnixMs;
        private readonly List<MensagemTemporaria> _mensagensTemporarias =
            new List<MensagemTemporaria>();

        private readonly Func<string, bool> _ignorar;
        private readonly Func<long, bool> _deixarDeIgnorar;

        public ServicoDeChat(
            Func<EnviarMensagemDeChat, bool> enviar,
            Func<long> meuId,
            Func<long> agoraUnixMs,
            EstadoDeAmigos amigos = null,
            Func<string, bool> ignorar = null,
            Func<long, bool> deixarDeIgnorar = null
        )
        {
            _enviar = enviar;
            _meuId = meuId;
            _agoraUnixMs = agoraUnixMs;
            _ignorar = ignorar;
            _deixarDeIgnorar = deixarDeIgnorar;
            Amigos = amigos ?? new EstadoDeAmigos();
            Amigos.Mudou += AoMudarAmigos;
            Amigos.OperacaoRecusada += AoReceberRecusaDeAmizade;
        }

        public HistoricoDeChat Historico { get; } = new HistoricoDeChat();
        public EstadoDeAmigos Amigos { get; }
        public EstadoDoEnvio Estado { get; } = new EstadoDoEnvio();

        /// <summary>Algo novo no histórico ou nos ignorados: a tela deve se redesenhar.</summary>
        public event Action Mudou;

        public void Dispose()
        {
            Amigos.Mudou -= AoMudarAmigos;
            Amigos.OperacaoRecusada -= AoReceberRecusaDeAmizade;
        }

        public void Atualizar()
        {
            var agora = _agoraUnixMs();
            var mudou = false;
            for (var i = _mensagensTemporarias.Count - 1; i >= 0; i--)
            {
                if (_mensagensTemporarias[i].ExpiraEmUnixMs > agora)
                {
                    continue;
                }

                Historico.Remover(_mensagensTemporarias[i].Linha);
                _mensagensTemporarias.RemoveAt(i);
                mudou = true;
            }

            if (mudou)
            {
                Mudou?.Invoke();
            }
        }

        /// <summary>
        /// Trata o que a pessoa digitou ao apertar Enter. Devolve <c>true</c> quando a caixa pode
        /// fechar; <c>false</c> quando o texto deve ficar para a pessoa corrigir (longo demais,
        /// sem conexão).
        /// </summary>
        public bool Enviar(string entrada)
        {
            var resultado = InterpretadorDeComandos.Interpretar(entrada, Estado);
            switch (resultado.Acao)
            {
                case AcaoDoComando.Nada:
                    return true;
                case AcaoDoComando.Erro:
                    AdicionarSistema(resultado.Mensagem);
                    return !resultado.ManterAberto;
                case AcaoDoComando.Informativo:
                    AdicionarSistema(resultado.Mensagem);
                    return true;
                case AcaoDoComando.Registrado:
                    return ExecutarRegistrado(resultado);
                case AcaoDoComando.MudarCanal:
                    Estado.Registrar(resultado);
                    AdicionarSistema(TextoDaTroca(resultado));
                    return true;
                case AcaoDoComando.Ignorar:
                    AdicionarSistema(Ignorar(resultado.Nome));
                    return true;
                case AcaoDoComando.Designorar:
                    AdicionarSistema(Designorar(resultado.Nome));
                    return true;
                default:
                    return Despachar(resultado);
            }
        }

        public void AoReceberMensagem(MensagemDeChat mensagem)
        {
            if (mensagem == null || !RegrasChat.CanalValido(mensagem.Canal))
            {
                return;
            }

            var meuId = _meuId();
            var propria = meuId != 0 && mensagem.RemetenteId == meuId;
            Historico.Adicionar(Converter(mensagem, propria));

            if (
                mensagem.Canal == CanalDeChat.Sussurro
                && !propria
                && !string.IsNullOrEmpty(mensagem.NomeDoRemetente)
                && !NomeEstaIgnorado(mensagem.NomeDoRemetente)
            )
            {
                Estado.UltimoSussurroRecebido = mensagem.NomeDoRemetente;
            }

            Mudou?.Invoke();
        }

        public void AoReceberRecusa(MensagemDeChatRecusada recusa)
        {
            if (recusa == null)
            {
                return;
            }

            // Sem grupo, o canal Grupo deixa de ser o "último canal": volta para a Cidade.
            if (
                recusa.Codigo == CodigosErroSocial.NaoEstaEmGrupo
                && Estado.UltimoCanal == CanalDeChat.Grupo
            )
            {
                Estado.UltimoCanal = CanalDeChat.Cidade;
            }

            AdicionarSistema(TextosDeChat.Recusa(recusa.Codigo, recusa.SegundosRestantes));
        }

        public void AoReceberHistorico(HistoricoDaCidade historico)
        {
            if (historico?.Mensagens == null)
            {
                return;
            }

            var meuId = _meuId();
            var linhas = new List<LinhaDeChat>();
            foreach (var mensagem in historico.Mensagens)
            {
                if (mensagem != null && RegrasChat.CanalValido(mensagem.Canal))
                {
                    linhas.Add(Converter(mensagem, meuId != 0 && mensagem.RemetenteId == meuId));
                }
            }

            if (Historico.AdicionarHistorico(linhas) > 0)
            {
                Mudou?.Invoke();
            }
        }

        /// <summary>Uma linha de Sistema, só sua (não passa pelo servidor).</summary>
        public void AdicionarSistema(string texto) =>
            AdicionarSistema(texto, CategoriaDeAviso.Sistema);

        public void AdicionarSistema(string texto, CategoriaDeAviso categoria)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return;
            }

            Historico.Adicionar(LinhaDeChat.Sistema(texto, _agoraUnixMs(), categoria));
            Mudou?.Invoke();
        }

        public void AdicionarSistemaTemporario(string texto, int duracaoSegundos)
        {
            if (string.IsNullOrEmpty(texto) || duracaoSegundos <= 0)
            {
                return;
            }

            var agora = _agoraUnixMs();
            var linha = LinhaDeChat.Sistema(texto, agora);
            Historico.Adicionar(linha);
            _mensagensTemporarias.Add(
                new MensagemTemporaria
                {
                    Linha = linha,
                    ExpiraEmUnixMs = agora + duracaoSegundos * 1000L,
                }
            );
            Mudou?.Invoke();
        }

        private bool ExecutarRegistrado(ResultadoDoComando resultado)
        {
            var contexto = new ContextoDeComandoDeBarra { MostrarSistema = AdicionarSistema };
            var barra = ComandosDeBarraNoChat.Executar(
                resultado.ComandoBarra,
                resultado.RestoBarra,
                contexto
            );
            if (barra == null)
            {
                AdicionarSistema(TextosDeChat.ComandoDesconhecido(resultado.ComandoBarra));
                return true;
            }

            return barra.FecharCaixa && !barra.ManterAberto;
        }

        private bool Despachar(ResultadoDoComando resultado)
        {
            Estado.Registrar(resultado);
            var enviada = _enviar(
                new EnviarMensagemDeChat
                {
                    Canal = resultado.Canal,
                    Texto = resultado.Texto,
                    NomeDoAlvo = resultado.Canal == CanalDeChat.Sussurro ? resultado.Nome : "",
                }
            );
            if (!enviada)
            {
                AdicionarSistema(TextosDeChat.ChatSemConexao);
            }

            return enviada;
        }

        private string Ignorar(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return "Use /ignorar Nome.";
            }

            if (_ignorar == null || !_ignorar(nome))
            {
                return TextosDeChat.ChatSemConexao;
            }

            return null;
        }

        private string Designorar(string nome)
        {
            if (!Amigos.TentarIgnorado(nome, out var ignorado))
            {
                return $"{nome} não está na sua lista de ignorados.";
            }

            if (_deixarDeIgnorar == null || !_deixarDeIgnorar(ignorado.IgnoradoId))
            {
                return TextosDeChat.ChatSemConexao;
            }

            return null;
        }

        private bool NomeEstaIgnorado(string nome) => Amigos.Contem(nome);

        private void AoMudarAmigos() => Mudou?.Invoke();

        private void AoReceberRecusaDeAmizade(string codigo)
        {
            AdicionarSistemaTemporario(
                TextoDaRecusaDeAmizade(codigo),
                DuracaoDaRecusaSocialSegundos
            );
        }

        private static string TextoDaRecusaDeAmizade(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroSocial.AlvoNaoEncontrado:
                    return "Jogador não encontrado.";
                case CodigosErroSocial.AmizadeConsigoMesmo:
                    return "Você não pode adicionar a si mesmo.";
                case CodigosErroSocial.AlvoIgnorado:
                    return "Esse jogador está na sua lista de ignorados.";
                case CodigosErroSocial.JaSaoAmigos:
                    return "Vocês já são amigos.";
                case CodigosErroSocial.PedidoJaPendente:
                    return "Já existe um pedido de amizade pendente.";
                case CodigosErroSocial.PedidosRapidosDemais:
                    return "Você está enviando pedidos rápido demais.";
                case CodigosErroSocial.ListaDeAmigosCheia:
                    return "Sua lista de amigos está cheia.";
                case CodigosErroSocial.AlvoComPedidosDemais:
                    return "Esse jogador tem pedidos de amizade demais.";
                case CodigosErroSocial.PedidoNaoEncontrado:
                    return "Esse pedido de amizade não existe mais.";
                case CodigosErroSocial.AmigoNaoEncontrado:
                    return "Esse jogador não está na sua lista de amigos.";
                case CodigosErroSocial.IgnorarASiMesmo:
                    return "Você não pode ignorar a si mesmo.";
                case CodigosErroSocial.JaIgnorado:
                    return "Esse jogador já está na sua lista de ignorados.";
                case CodigosErroSocial.ListaDeIgnoradosCheia:
                    return "Sua lista de ignorados está cheia.";
                case CodigosErroSocial.IgnoradoNaoEncontrado:
                    return "Esse jogador não está na sua lista de ignorados.";
                default:
                    return "Não foi possível completar a operação de amizade.";
            }
        }

        private sealed class MensagemTemporaria
        {
            public LinhaDeChat Linha;
            public long ExpiraEmUnixMs;
        }

        private static string TextoDaTroca(ResultadoDoComando resultado) =>
            resultado.Canal == CanalDeChat.Sussurro
                ? $"Agora você sussurra para {resultado.Nome}."
                : $"Agora você fala no canal {TextosDeChat.NomeDoCanal(resultado.Canal)}.";

        private static LinhaDeChat Converter(MensagemDeChat mensagem, bool propria) =>
            new LinhaDeChat(
                DoCanal(mensagem.Canal),
                mensagem.RemetenteId,
                mensagem.NomeDoRemetente,
                mensagem.NomeDoAlvo,
                mensagem.Texto,
                mensagem.HoraUnixMs,
                propria
            );

        private static CanalDaLinha DoCanal(CanalDeChat canal)
        {
            switch (canal)
            {
                case CanalDeChat.Grupo:
                    return CanalDaLinha.Grupo;
                case CanalDeChat.Sussurro:
                    return CanalDaLinha.Sussurro;
                default:
                    return CanalDaLinha.Cidade;
            }
        }
    }
}
