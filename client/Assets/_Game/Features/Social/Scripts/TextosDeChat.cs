using System;
using System.Globalization;
using System.Text;
using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>Os textos do chat que a pessoa lê: linhas, canais e recusas do servidor.</summary>
    internal static class TextosDeChat
    {
        public const string AjudaDeComandos = "Digite /comandos para ver a lista completa.";

        public const string ChatSemConexao =
            "Sem conexão com o servidor: a mensagem não foi enviada.";

        public static string NomeDoCanal(CanalDeChat canal)
        {
            switch (canal)
            {
                case CanalDeChat.Grupo:
                    return "Grupo";
                case CanalDeChat.Sussurro:
                    return "Sussurro";
                case CanalDeChat.ProcurarGrupo:
                    return "Procurar Grupo";
                default:
                    return "Cidade";
            }
        }

        public static string ComandoDesconhecido(string comando) =>
            $"Comando desconhecido: /{comando}. {AjudaDeComandos}";

        public static string ListaCompletaDeComandos()
        {
            var texto = new StringBuilder();
            texto.AppendLine(
                "Chat: /c texto (cidade), /g texto (grupo), /pg texto (procurar grupo), /s Nome texto (sussurro),"
            );
            texto.Append("/r texto (responder), /ignorar Nome, /designorar Nome.");
            var extras = ComandosDeBarraNoChat.LinhasDoCatalogo();
            if (extras.Count > 0)
            {
                texto.Append(" Teste:");
                foreach (var linha in extras)
                {
                    texto.Append(' ').Append(linha).Append(';');
                }
            }

            return texto.ToString().TrimEnd(';');
        }

        /// <summary>O texto para o motivo de recusa do servidor; código desconhecido tem texto genérico.</summary>
        public static string Recusa(string codigo, int segundosRestantes)
        {
            switch (codigo)
            {
                case CodigosErroSocial.MensagemVazia:
                    return "Escreva uma mensagem antes de enviar.";
                case CodigosErroSocial.MensagemLongaDemais:
                    return $"Mensagem longa demais: o máximo é {RegrasChat.TamanhoMaximo} caracteres.";
                case CodigosErroSocial.MensagensRapidasDemais:
                    return "Você está falando rápido demais. Espere um pouco.";
                case CodigosErroSocial.MensagemRepetida:
                    return "Você já disse isso agora há pouco.";
                case CodigosErroSocial.Silenciado:
                    return segundosRestantes > 0
                        ? $"Você está em silêncio por mais {segundosRestantes} s."
                        : "Você está em silêncio.";
                case CodigosErroSocial.CanalIndisponivel:
                    return "Esse canal não está disponível aqui (a Cidade só vale na cidade).";
                case CodigosErroSocial.NaoEstaEmGrupo:
                    return "Você não está em um grupo.";
                case CodigosErroSocial.AlvoNaoEncontrado:
                    return "Jogador não encontrado ou fora do jogo.";
                case CodigosErroSocial.SussurroParaSiMesmo:
                    return "Você não pode sussurrar para si mesmo.";
                default:
                    return "A mensagem não foi enviada.";
            }
        }

        /// <summary>
        /// A linha como aparece no painel, com hora, canal e nome (ex.:
        /// <c>[14:05] [Cidade] Ana: oi</c>). <paramref name="fuso"/> é a diferença do horário
        /// local para o UTC.
        /// </summary>
        public static string Linha(LinhaDeChat linha, TimeSpan fuso)
        {
            var hora = Hora(linha.HoraUnixMs, fuso);
            switch (linha.Canal)
            {
                case CanalDaLinha.Sistema:
                    return $"[{hora}] [Sistema] {linha.Texto}";
                case CanalDaLinha.Sussurro:
                    return linha.Propria
                        ? $"[{hora}] [Sussurro] Para {linha.NomeDoAlvo}: {linha.Texto}"
                        : $"[{hora}] [Sussurro] {linha.Nome} sussurra: {linha.Texto}";
                case CanalDaLinha.Grupo:
                    return $"[{hora}] [Grupo] {linha.Nome}: {linha.Texto}";
                default:
                    return $"[{hora}] [Cidade] {linha.Nome}: {linha.Texto}";
            }
        }

        /// <summary>Hora e minuto (<c>HH:mm</c>); uma hora fora do intervalo válido vira <c>--:--</c>.</summary>
        public static string Hora(long horaUnixMs, TimeSpan fuso)
        {
            try
            {
                return DateTimeOffset
                    .FromUnixTimeMilliseconds(horaUnixMs)
                    .ToOffset(fuso)
                    .ToString("HH:mm", CultureInfo.InvariantCulture);
            }
            catch (ArgumentOutOfRangeException)
            {
                return "--:--";
            }
        }
    }
}
