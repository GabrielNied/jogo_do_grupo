namespace JogoDoGrupo.Client.Core.Jogo
{
    internal enum QualidadeGrafica
    {
        Baixa = 0,
        Media = 1,
        Alta = 2,
    }

    /// <summary>
    /// Opções deste computador (volume, qualidade, tela, sensibilidade, idioma). Valor fora
    /// da faixa é limitado; texto inválido volta ao padrão. Não decide regra de jogo.
    /// </summary>
    internal sealed class OpcoesLocais
    {
        public const int VolumeGeralPadrao = 80;
        public const int VolumeMusicaPadrao = 70;
        public const int VolumeEfeitosPadrao = 80;
        public const float SensibilidadePadrao = 1f;
        public const string IdiomaPadrao = "Português";

        public int VolumeGeral { get; private set; } = VolumeGeralPadrao;

        public int VolumeMusica { get; private set; } = VolumeMusicaPadrao;

        public int VolumeEfeitos { get; private set; } = VolumeEfeitosPadrao;

        public QualidadeGrafica NivelQualidade { get; private set; } = QualidadeGrafica.Media;

        public string Qualidade =>
            NivelQualidade switch
            {
                QualidadeGrafica.Baixa => "Baixa",
                QualidadeGrafica.Alta => "Alta",
                _ => "Média",
            };

        public bool TelaCheia { get; private set; } = true;

        public float Sensibilidade { get; private set; } = SensibilidadePadrao;

        public string Idioma { get; private set; } = IdiomaPadrao;

        public int Largura { get; private set; }

        public int Altura { get; private set; }

        public static OpcoesLocais Atual { get; private set; } = new OpcoesLocais();

        public static OpcoesLocais Padrao() => new OpcoesLocais();

        public static int LimitarVolume(int valor)
        {
            if (valor < 0)
            {
                valor = 0;
            }

            if (valor > 100)
            {
                valor = 100;
            }

            return (valor + 2) / 5 * 5;
        }

        public static float LimitarSensibilidade(float valor)
        {
            if (valor < 0.5f)
            {
                valor = 0.5f;
            }

            if (valor > 2f)
            {
                valor = 2f;
            }

            return (float)System.Math.Round(valor * 10f) / 10f;
        }

        public void DefinirVolumeGeral(int valor) => VolumeGeral = LimitarVolume(valor);

        public void DefinirVolumeMusica(int valor) => VolumeMusica = LimitarVolume(valor);

        public void DefinirVolumeEfeitos(int valor) => VolumeEfeitos = LimitarVolume(valor);

        public void DefinirSensibilidade(float valor) =>
            Sensibilidade = LimitarSensibilidade(valor);

        public void DefinirQualidade(string qualidade)
        {
            NivelQualidade = qualidade switch
            {
                "Baixa" => QualidadeGrafica.Baixa,
                "Alta" => QualidadeGrafica.Alta,
                _ => QualidadeGrafica.Media,
            };
        }

        public void DefinirQualidade(QualidadeGrafica qualidade) =>
            NivelQualidade =
                qualidade >= QualidadeGrafica.Baixa && qualidade <= QualidadeGrafica.Alta
                    ? qualidade
                    : QualidadeGrafica.Media;

        public void DefinirTelaCheia(bool cheia) => TelaCheia = cheia;

        /// <summary>0 e 0 significam a resolução atual do monitor.</summary>
        public void DefinirResolucao(int largura, int altura)
        {
            if (largura <= 0 || altura <= 0)
            {
                Largura = 0;
                Altura = 0;
                return;
            }

            Largura = largura;
            Altura = altura;
        }

        public string Gravar() =>
            "geral="
            + VolumeGeral
            + "\nmusica="
            + VolumeMusica
            + "\nefeitos="
            + VolumeEfeitos
            + "\nqualidade="
            + Qualidade
            + "\ntelaCheia="
            + (TelaCheia ? "sim" : "nao")
            + "\nsensibilidade="
            + Sensibilidade.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "\nidioma="
            + Idioma
            + "\nlargura="
            + Largura
            + "\naltura="
            + Altura
            + TeclasDoJogo.Gravar();

        /// <summary>Lê o texto gravado. Vazio, nulo ou ilegível volta ao padrão.</summary>
        public static OpcoesLocais Ler(string texto)
        {
            var opcoes = Padrao();
            if (string.IsNullOrWhiteSpace(texto) || !texto.Contains("geral="))
            {
                TeclasDoJogo.Carregar(null);
                return opcoes;
            }

            foreach (var linha in texto.Split('\n'))
            {
                var partes = linha.Trim().Split('=');
                if (partes.Length != 2)
                {
                    continue;
                }

                switch (partes[0])
                {
                    case "geral":
                        opcoes.DefinirVolumeGeral(Inteiro(partes[1], VolumeGeralPadrao));
                        break;
                    case "musica":
                        opcoes.DefinirVolumeMusica(Inteiro(partes[1], VolumeMusicaPadrao));
                        break;
                    case "efeitos":
                        opcoes.DefinirVolumeEfeitos(Inteiro(partes[1], VolumeEfeitosPadrao));
                        break;
                    case "qualidade":
                        opcoes.DefinirQualidade(partes[1]);
                        break;
                    case "telaCheia":
                        opcoes.DefinirTelaCheia(partes[1] != "nao");
                        break;
                    case "sensibilidade":
                        opcoes.DefinirSensibilidade(Decimal(partes[1], SensibilidadePadrao));
                        break;
                    case "largura":
                        opcoes.Largura = Inteiro(partes[1], 0);
                        break;
                    case "altura":
                        opcoes.Altura = Inteiro(partes[1], 0);
                        break;
                }
            }

            opcoes.DefinirResolucao(opcoes.Largura, opcoes.Altura);
            TeclasDoJogo.Carregar(texto);
            return opcoes;
        }

        public static void Substituir(OpcoesLocais opcoes)
        {
            Atual = opcoes ?? Padrao();
        }

        private static int Inteiro(string texto, int padrao) =>
            int.TryParse(texto, out var valor) ? valor : padrao;

        private static float Decimal(string texto, float padrao) =>
            float.TryParse(
                texto,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var valor
            )
                ? valor
                : padrao;
    }
}
