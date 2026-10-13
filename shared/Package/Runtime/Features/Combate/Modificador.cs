namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Uma parcela de um atributo secundário, com a origem. Quem publica o modificador declara
    /// tudo: nada é somado ou multiplicado por suposição.
    /// </summary>
    [System.Serializable]
    public class Modificador
    {
        public AtributoSecundario Atributo;
        public FonteDeModificador Fonte;

        /// <summary>Texto para o jogador ("Base", "Espada de ferro", "Maestria ofensiva").</summary>
        public string Origem = "";
        public OperacaoDeModificador Operacao;

        /// <summary>Passos de menor ordem entram primeiro (veja <see cref="ConfigAtributosSecundarios"/>).</summary>
        public int Ordem;

        /// <summary>
        /// Grupo de acúmulo, obrigatório. Multiplicações do mesmo grupo e da mesma ordem somam os
        /// bônus; grupos diferentes se multiplicam entre si.
        /// </summary>
        public string Grupo = "";
        public double Valor;
    }

    /// <summary>Valor final de um secundário e as parcelas que o formaram, na ordem em que entraram.</summary>
    [System.Serializable]
    public class SecundarioCalculado
    {
        public AtributoSecundario Atributo;
        public double Valor;
        public Modificador[] Parcelas = new Modificador[0];
    }

    /// <summary>Os nove valores finais, já limitados.</summary>
    [System.Serializable]
    public class AtributosSecundarios
    {
        public double RegeneracaoVida;
        public double RegeneracaoStamina;
        public double RegeneracaoMana;

        /// <summary>Probabilidade de 0 a 1.</summary>
        public double ChanceCritica;

        /// <summary>Multiplicador do dano no crítico (1,5 = 150%).</summary>
        public double DanoCritico;

        /// <summary>Multiplicador da velocidade de execução (1 = normal).</summary>
        public double VelocidadeAtaqueFisico;
        public double VelocidadeAtaqueMagico;

        /// <summary>Fração da defesa física do alvo ignorada, de 0 a 1.</summary>
        public double PenetracaoFisica;

        /// <summary>Fração da defesa mágica do alvo ignorada, de 0 a 1.</summary>
        public double PenetracaoMagica;

        public double Valor(AtributoSecundario atributo)
        {
            switch (atributo)
            {
                case AtributoSecundario.RegeneracaoVida:
                    return RegeneracaoVida;
                case AtributoSecundario.RegeneracaoStamina:
                    return RegeneracaoStamina;
                case AtributoSecundario.RegeneracaoMana:
                    return RegeneracaoMana;
                case AtributoSecundario.ChanceCritica:
                    return ChanceCritica;
                case AtributoSecundario.DanoCritico:
                    return DanoCritico;
                case AtributoSecundario.VelocidadeAtaqueFisico:
                    return VelocidadeAtaqueFisico;
                case AtributoSecundario.VelocidadeAtaqueMagico:
                    return VelocidadeAtaqueMagico;
                case AtributoSecundario.PenetracaoFisica:
                    return PenetracaoFisica;
                default:
                    return PenetracaoMagica;
            }
        }

        public void Definir(AtributoSecundario atributo, double valor)
        {
            switch (atributo)
            {
                case AtributoSecundario.RegeneracaoVida:
                    RegeneracaoVida = valor;
                    break;
                case AtributoSecundario.RegeneracaoStamina:
                    RegeneracaoStamina = valor;
                    break;
                case AtributoSecundario.RegeneracaoMana:
                    RegeneracaoMana = valor;
                    break;
                case AtributoSecundario.ChanceCritica:
                    ChanceCritica = valor;
                    break;
                case AtributoSecundario.DanoCritico:
                    DanoCritico = valor;
                    break;
                case AtributoSecundario.VelocidadeAtaqueFisico:
                    VelocidadeAtaqueFisico = valor;
                    break;
                case AtributoSecundario.VelocidadeAtaqueMagico:
                    VelocidadeAtaqueMagico = valor;
                    break;
                case AtributoSecundario.PenetracaoFisica:
                    PenetracaoFisica = valor;
                    break;
                default:
                    PenetracaoMagica = valor;
                    break;
            }
        }
    }
}
