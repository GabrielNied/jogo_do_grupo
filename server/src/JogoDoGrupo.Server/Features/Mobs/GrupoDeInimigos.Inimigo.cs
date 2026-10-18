using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>O estado de cada inimigo do grupo (parte de <see cref="GrupoDeInimigos"/>).</summary>
public sealed partial class GrupoDeInimigos
{
    private sealed class Inimigo(
        long id,
        CriaturaSorteada criatura,
        double origemX,
        double origemZ,
        bool contaNoLimite
    )
    {
        public long Id { get; } = id;

        /// <summary>Se ocupa uma vaga do teto de vivos (os de teste e o chefe raro não).</summary>
        public bool ContaNoLimite { get; } = contaNoLimite;
        public CriaturaSorteada Criatura { get; } = criatura;

        /// <summary>Ponto de origem: o lugar dele no ponto da rota onde a manada está.</summary>
        public double OrigemX { get; set; } = origemX;
        public double OrigemZ { get; set; } = origemZ;

        /// <summary>Onde ele fica em relação ao centro da manada, para andar em bando.</summary>
        public double DesvioX { get; set; }
        public double DesvioZ { get; set; }

        /// <summary>Se volta depois de morto (os de teste, não).</summary>
        public bool Reponivel { get; set; } = true;

        /// <summary>
        /// Quando este morto volta no lugar dele, com o grupo vivo; nulo se não há reposição
        /// marcada (o grupo todo morto volta pela onda).
        /// </summary>
        public long? ReporEmMs { get; set; }

        /// <summary>Se acompanha a rota (os de teste ficam onde nasceram).</summary>
        public bool Segue { get; set; } = true;

        public double X = origemX;
        public double Z = origemZ;
        public double Rotacao = ConfigInimigosDoCampo.DirecaoDeRepousoGraus;
        public EstadoDeInimigo Estado = EstadoDeInimigo.Repouso;
        public long Alvo = RegrasInimigo.SemAlvo;
        public int Vida = criatura.VidaMaxima;
        public int Sequencia;
        public int SlotCerco = -1;
        public bool SentidoOrbitalHorario = (id % 2 == 0);

        /// <summary>Destino atual do passo de vagar em repouso.</summary>
        public double DestinoVagarX = origemX;
        public double DestinoVagarZ = origemZ;

        /// <summary>Quando a pausa em repouso acaba e a criatura pode escolher outro ponto para vagar.</summary>
        public long PausaVagarAteMs;

        /// <summary>Se está em movimento em direção ao ponto de vagar.</summary>
        public bool Vagando;

        /// <summary>Quando o alerta acaba e a perseguição começa.</summary>
        public long AlertaAteMs;

        /// <summary>No ataque: verdadeiro durante o aviso, falso na recuperação.</summary>
        public bool Preparando;

        /// <summary>No ataque: quando a fase atual (aviso ou recuperação) acaba.</summary>
        public long FaseAteMs;
        public double SegundosSemAtacar;

        /// <summary>Direção travada no começo do preparo do golpe, em graus.</summary>
        public double DirecaoDoGolpe;

        /// <summary>Quanto do passo do golpe ainda falta andar, em metros.</summary>
        public double PassoRestante;

        /// <summary>
        /// Quem participa da luta desde que ele apareceu ou voltou da desistência: quem causou
        /// dano, quem foi alvo dele e quem curou um aliado que já participava (#909).
        /// </summary>
        public readonly HashSet<long> Participantes = [];

        /// <summary>Tabela de ameaça individual deste inimigo.</summary>
        public readonly TabelaDeAmeaca Ameacas = new();

        /// <summary>A vida mudou desde o último <see cref="InimigoMudou"/>.</summary>
        public bool VidaMudou;
    }
}
