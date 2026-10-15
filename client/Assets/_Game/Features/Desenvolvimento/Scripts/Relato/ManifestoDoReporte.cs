using System;
using System.Globalization;

namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>O que a pessoa escolheu na janela do report.</summary>
    internal sealed class DadosDoReporte
    {
        public string Frase = "";
        public string Tipo = "bug";
        public string Severidade = "media";

        /// <summary>Funcionalidade (ex.: <c>"Combate"</c>); vazio quando a pessoa não sabe.</summary>
        public string Area = "";
    }

    /// <summary>Opções da janela do report: o texto mostrado e o valor gravado no manifesto.</summary>
    internal static class OpcoesDoReporte
    {
        public const string AreaNaoSei = "Não sei";

        /// <summary>Severidade que a janela já traz marcada.</summary>
        public const string SeveridadePadrao = "Média";

        public static readonly (string Rotulo, string Valor)[] Tipos =
        {
            ("Bug", "bug"),
            ("Sensação", "sensacao"),
            ("Melhoria", "melhoria"),
            ("UX / interface", "ux"),
        };

        public static readonly (string Rotulo, string Valor)[] Severidades =
        {
            ("Crítica", "critica"),
            ("Alta", "alta"),
            ("Média", "media"),
            ("Baixa", "baixa"),
        };

        /// <summary>Escopos usados nos títulos das issues (<c>[Combate]</c>, <c>[Mobs]</c>...).</summary>
        public static readonly string[] Areas =
        {
            AreaNaoSei,
            "Combate",
            "Mobs",
            "Personagem",
            "Itens",
            "Mundo",
            "Instâncias",
            "Missões",
            "NPCs",
            "Social",
            "Talentos",
            "Skills",
            "Profissões",
            "Economia",
            "PvP",
            "Core",
            "Desenvolvimento",
        };

        /// <summary>Valor da opção com esse rótulo, ou o da primeira.</summary>
        public static string Valor((string Rotulo, string Valor)[] opcoes, string rotulo)
        {
            foreach (var opcao in opcoes)
            {
                if (opcao.Rotulo == rotulo)
                {
                    return opcao.Valor;
                }
            }

            return opcoes[0].Valor;
        }
    }

    /// <summary>
    /// O <c>reporte.json</c> de cada report: lido pelo <c>scripts/relatos/reporte.mjs</c>, que
    /// cria a issue e grava de volta <see cref="Status"/>, <see cref="Issue"/> e
    /// <see cref="Url"/>. Os nomes dos campos são o contrato com o script.
    /// </summary>
    [Serializable]
    internal sealed class ManifestoDoReporte
    {
        public const string Pendente = "pendente";

        public string Id = "";
        public string Tipo = "";
        public string Severidade = "";
        public string Area = "";
        public string Frase = "";

        /// <summary>Hora local, <c>AAAA-MM-DD HH:MM:SS</c>.</summary>
        public string Quando = "";

        /// <summary>Hora UTC em ISO 8601, para achar o trecho do log do servidor.</summary>
        public string QuandoUtc = "";
        public string Versao = "";
        public bool Editor;
        public string Tela = "";
        public int LatenciaMs;

        /// <summary>X, Y e Z do jogador; vazio sem personagem.</summary>
        public float[] Posicao = new float[0];
        public string Cena = "";
        public string JanelaAberta = "";
        public string AjustesF10 = "";
        public string Branch = "";
        public string Commit = "";
        public float SegundosDaLinhaDoTempo;
        public float SegundosDeQuadros;
        public string Status = Pendente;
        public int Issue;
        public string Url = "";
        public string Erro = "";

        /// <summary>Nome da pasta e id do report: a hora local, <c>AAAA-MM-DD_HHMMSS</c>.</summary>
        public static string NovoId(DateTime agora)
        {
            return agora.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
        }
    }
}
