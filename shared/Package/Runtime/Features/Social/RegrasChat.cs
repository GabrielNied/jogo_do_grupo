#nullable enable
using System;
using System.Text;

namespace JogoDoGrupo.Shared.Features.Social
{
    /// <summary>
    /// Números e contas puras do chat de texto. Regras em docs/features/social.md (proposta). O
    /// client usa <see cref="Limpar"/> e <see cref="Validar"/> para avisar antes de enviar; o
    /// servidor limpa e valida de novo e é quem decide.
    /// </summary>
    public static class RegrasChat
    {
        // ── Balanceamento (proposta, a calibrar) ─────────────────────────────────

        /// <summary>Tamanho máximo do texto, em caracteres, depois de limpo.</summary>
        public const int TamanhoMaximo = 200;

        /// <summary>Mensagens aceitas por personagem em cada janela, somando os canais.</summary>
        public const int MensagensPorJanela = 5;

        /// <summary>Tamanho da janela de frequência, em segundos.</summary>
        public const int JanelaDeFrequenciaSegundos = 10;

        /// <summary>Segundos em que a mesma mensagem no mesmo canal conta como repetida.</summary>
        public const int IntervaloDeRepeticaoSegundos = 30;

        /// <summary>Estouros de frequência, dentro da janela de estouros, que silenciam.</summary>
        public const int EstourosParaSilenciar = 3;

        /// <summary>Tamanho da janela em que os estouros são contados, em segundos.</summary>
        public const int JanelaDeEstourosSegundos = 300;

        /// <summary>Duração do silêncio automático, em segundos, em todos os canais.</summary>
        public const int SilencioSegundos = 60;

        /// <summary>Quantas mensagens da Cidade o servidor guarda para quem entra.</summary>
        public const int HistoricoDaCidadeMensagens = 20;

        /// <summary>Idade máxima, em segundos, de uma mensagem no histórico da Cidade.</summary>
        public const int HistoricoDaCidadeSegundos = 600;

        // ── Texto ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Texto pronto para medir e entregar: quebras de linha e caracteres de controle viram
        /// espaço, e os espaços do começo e do fim saem. Nulo vira vazio. Acentos, números e
        /// pontuação ficam como estão.
        /// </summary>
        public static string Limpar(string? texto)
        {
            string bruto = texto ?? "";
            var limpo = new StringBuilder(bruto.Length);
            foreach (char c in bruto)
            {
                limpo.Append(EhControleOuQuebra(c) ? ' ' : c);
            }
            return limpo.ToString().Trim();
        }

        /// <summary>
        /// Confere o tamanho de um texto (limpa antes de medir).
        /// </summary>
        /// <returns>
        /// <see cref="CodigosErroSocial.MensagemVazia"/>,
        /// <see cref="CodigosErroSocial.MensagemLongaDemais"/> ou <c>null</c> se o texto serve.
        /// </returns>
        public static string? Validar(string? texto)
        {
            string limpo = Limpar(texto);
            if (limpo.Length == 0)
            {
                return CodigosErroSocial.MensagemVazia;
            }
            if (limpo.Length > TamanhoMaximo)
            {
                return CodigosErroSocial.MensagemLongaDemais;
            }
            return null;
        }

        /// <summary>
        /// Forma de comparar duas mensagens para a regra de repetição: ignora maiúsculas e todos
        /// os espaços ("Oi  gente" e "oi gente" e "OIGENTE" são a mesma).
        /// </summary>
        public static string ChaveDeRepeticao(string? texto)
        {
            string limpo = Limpar(texto);
            var chave = new StringBuilder(limpo.Length);
            foreach (char c in limpo)
            {
                if (!char.IsWhiteSpace(c))
                {
                    chave.Append(char.ToLowerInvariant(c));
                }
            }
            return chave.ToString();
        }

        /// <summary>Se o valor recebido da rede é um canal que existe.</summary>
        public static bool CanalValido(CanalDeChat canal) =>
            canal == CanalDeChat.Cidade
            || canal == CanalDeChat.Grupo
            || canal == CanalDeChat.Sussurro;

        // ── Limites ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Se mais uma mensagem estoura a frequência. Recebe quantas o personagem já teve aceitas
        /// na janela: com 4 a quinta passa; com 5 a sexta é recusada.
        /// </summary>
        public static bool FrequenciaEstourou(int mensagensAceitasNaJanela) =>
            mensagensAceitasNaJanela >= MensagensPorJanela;

        /// <summary>Se uma mensagem aceita há esse tempo ainda conta na janela de frequência.</summary>
        public static bool ContaNaFrequencia(double segundosDesdeAMensagem) =>
            segundosDesdeAMensagem < JanelaDeFrequenciaSegundos;

        /// <summary>Repetição com 29 s é recusada; com 30 s passa.</summary>
        public static bool Repetida(double segundosDesdeAMensagemIgual) =>
            segundosDesdeAMensagemIgual < IntervaloDeRepeticaoSegundos;

        /// <summary>Se o personagem deve ser silenciado, contando o estouro de agora.</summary>
        public static bool DeveSilenciar(int estourosNaJanela) =>
            estourosNaJanela >= EstourosParaSilenciar;

        /// <summary>Se um estouro de frequência há esse tempo ainda conta para o silêncio.</summary>
        public static bool ContaComoEstouro(double segundosDesdeOEstouro) =>
            segundosDesdeOEstouro < JanelaDeEstourosSegundos;

        /// <summary>
        /// Segundos que faltam de silêncio, arredondados para cima; zero quando já acabou. Vai em
        /// <see cref="MensagemDeChatRecusada.SegundosRestantes"/>.
        /// </summary>
        public static int SilencioRestanteSegundos(double segundosDesdeOSilencio)
        {
            double restante = Math.Ceiling(SilencioSegundos - segundosDesdeOSilencio);
            return (int)Math.Max(0.0, Math.Min(SilencioSegundos, restante));
        }

        /// <summary>Se a mensagem já é velha demais para o histórico da Cidade.</summary>
        public static bool VelhaDemaisParaOHistorico(double segundosDesdeAMensagem) =>
            segundosDesdeAMensagem > HistoricoDaCidadeSegundos;

        private static bool EhControleOuQuebra(char c) =>
            char.IsControl(c) || c == '\u2028' || c == '\u2029';
    }
}
