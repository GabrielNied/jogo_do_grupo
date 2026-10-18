using System;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Features.Pets
{
    /// <summary>Onde o pet fica depois de um passo e o que ele fez.</summary>
    public readonly struct PassoDoPet
    {
        public readonly float X;
        public readonly float Z;
        public readonly float RotacaoY;
        public readonly bool Andando;

        /// <summary>O pet estava longe demais e foi colocado perto do dono.</summary>
        public readonly bool Alcancou;

        public PassoDoPet(float x, float z, float rotacaoY, bool andando, bool alcancou)
        {
            X = x;
            Z = z;
            RotacaoY = rotacaoY;
            Andando = andando;
            Alcancou = alcancou;
        }
    }

    /// <summary>
    /// Movimento do pet no plano do chão, sem engine nem rede (o servidor decide a posição e o
    /// client só mostra). Direção em graus: 0 aponta para +Z e cresce para +X.
    /// </summary>
    public static class RegrasPets
    {
        /// <summary>
        /// Folga para dar a chegada por feita. Sem ela, o erro de arredondamento deixa o pet a
        /// 2,0000002 m e ele nunca para.
        /// </summary>
        private const float ToleranciaDeChegadaMetros = 0.01f;

        /// <summary>Direção, em graus, de (0,0) até (dx,dz).</summary>
        public static float Direcao(float dx, float dz)
        {
            return (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
        }

        public static float Distancia(float ax, float az, float bx, float bz)
        {
            var dx = bx - ax;
            var dz = bz - az;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Onde o pet nasce ao ser chamado: na distância de seguida, atrás do dono (oposto ao
        /// lado para onde ele olha).
        /// </summary>
        public static void PontoDeChegada(
            float donoX,
            float donoZ,
            float donoRotacaoY,
            out float x,
            out float z
        )
        {
            var radianos = donoRotacaoY * Math.PI / 180.0;
            x = donoX - (float)Math.Sin(radianos) * ConfigPets.DistanciaDeSeguidaMetros;
            z = donoZ - (float)Math.Cos(radianos) * ConfigPets.DistanciaDeSeguidaMetros;
        }

        /// <summary>
        /// Um passo de seguir o dono. Passando de <see cref="ConfigPets.DistanciaParaAlcancarMetros"/>
        /// o pet vai para a distância de seguida, do lado em que estava. Senão anda até parar na
        /// distância de seguida; parado, só recomeça passando da folga.
        /// </summary>
        public static PassoDoPet Seguir(
            float petX,
            float petZ,
            float petRotacaoY,
            float donoX,
            float donoZ,
            bool andando,
            float dt
        )
        {
            var distancia = Distancia(petX, petZ, donoX, donoZ);
            if (distancia > ConfigPets.DistanciaParaAlcancarMetros)
            {
                var dx = (petX - donoX) / distancia;
                var dz = (petZ - donoZ) / distancia;
                return new PassoDoPet(
                    donoX + dx * ConfigPets.DistanciaDeSeguidaMetros,
                    donoZ + dz * ConfigPets.DistanciaDeSeguidaMetros,
                    Direcao(-dx, -dz),
                    false,
                    true
                );
            }

            var limite = andando
                ? ConfigPets.DistanciaDeSeguidaMetros
                : ConfigPets.DistanciaDeSeguidaMetros + ConfigPets.FolgaDeSeguidaMetros;
            if (distancia <= limite)
            {
                return new PassoDoPet(petX, petZ, petRotacaoY, false, false);
            }

            return IrAte(
                petX,
                petZ,
                petRotacaoY,
                donoX,
                donoZ,
                ConfigPets.DistanciaDeSeguidaMetros,
                dt
            );
        }

        /// <summary>
        /// Um passo em linha reta até o ponto, parando a <paramref name="distanciaParar"/> dele.
        /// Nunca passa do ponto de parada.
        /// </summary>
        public static PassoDoPet IrAte(
            float petX,
            float petZ,
            float petRotacaoY,
            float alvoX,
            float alvoZ,
            float distanciaParar,
            float dt
        )
        {
            var distancia = Distancia(petX, petZ, alvoX, alvoZ);
            var falta = distancia - distanciaParar;
            if (falta <= ToleranciaDeChegadaMetros || distancia <= 0f)
            {
                return new PassoDoPet(petX, petZ, petRotacaoY, false, false);
            }

            var anda = Math.Min(falta, ConfigPets.VelocidadeMetrosPorSegundo * dt);
            var dx = (alvoX - petX) / distancia;
            var dz = (alvoZ - petZ) / distancia;
            return new PassoDoPet(petX + dx * anda, petZ + dz * anda, Direcao(dx, dz), true, false);
        }

        /// <summary>Todas as espécies do catálogo (tecla U), em ordem.</summary>
        public static readonly EspecieDePet[] EspeciesDoCatalogo =
        {
            EspecieDePet.CervoJovem,
            EspecieDePet.FilhoteDeJavali,
            EspecieDePet.LoboFilhote,
            EspecieDePet.TatuPequeno,
            EspecieDePet.CorujaDaClareira,
            EspecieDePet.MariposaLuz,
            EspecieDePet.CoelhoDoPasto,
            EspecieDePet.LoboDeGeadaFilhote,
            EspecieDePet.CarapacaJovem,
            EspecieDePet.MosquitoDoPantano,
            EspecieDePet.CachorroComum,
            EspecieDePet.GatoComum,
            EspecieDePet.GalinhaComum,
        };

        /// <summary>Qual espécie de pet o item consumível desbloqueia, ou 0 se não for pet.</summary>
        public static EspecieDePet EspecieDoItem(int itemId) =>
            itemId switch
            {
                CatalogoItens.SinoDoCervoJovem => EspecieDePet.CervoJovem,
                CatalogoItens.SinoDoFilhoteDeJavali => EspecieDePet.FilhoteDeJavali,
                CatalogoItens.SinoDoLoboFilhote => EspecieDePet.LoboFilhote,
                CatalogoItens.SinoDoTatuPequeno => EspecieDePet.TatuPequeno,
                CatalogoItens.SinoDaCorujaDaClareira => EspecieDePet.CorujaDaClareira,
                CatalogoItens.FrascoDaMariposaLuz => EspecieDePet.MariposaLuz,
                CatalogoItens.SinoDoCoelhoDoPasto => EspecieDePet.CoelhoDoPasto,
                CatalogoItens.SinoDoLoboDeGeadaFilhote => EspecieDePet.LoboDeGeadaFilhote,
                CatalogoItens.ColarDaCarapacaJovem => EspecieDePet.CarapacaJovem,
                CatalogoItens.FrascoDoMosquitoDoPantano => EspecieDePet.MosquitoDoPantano,
                _ => (EspecieDePet)0,
            };

        /// <summary>Se o item é consumível de pet.</summary>
        public static bool EhItemDePet(int itemId) => EspecieDoItem(itemId) != 0;

        /// <summary>Se a espécie é válida no catálogo do jogo.</summary>
        public static bool EspecieValida(EspecieDePet especie)
        {
            for (var i = 0; i < EspeciesDoCatalogo.Length; i++)
            {
                if (EspeciesDoCatalogo[i] == especie)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Se a espécie já começa desbloqueada em toda conta nova.</summary>
        public static bool DesbloqueadoPorPadrao(EspecieDePet especie) => false;

        /// <summary>Nome de exibição da espécie.</summary>
        public static string ObterNome(EspecieDePet especie) =>
            especie switch
            {
                EspecieDePet.CervoJovem => "Cervo Jovem",
                EspecieDePet.FilhoteDeJavali => "Filhote de Javali",
                EspecieDePet.LoboFilhote => "Lobo Filhote",
                EspecieDePet.TatuPequeno => "Tatu Pequeno",
                EspecieDePet.CorujaDaClareira => "Coruja da Clareira",
                EspecieDePet.MariposaLuz => "Mariposa-luz",
                EspecieDePet.CoelhoDoPasto => "Coelho do Pasto",
                EspecieDePet.LoboDeGeadaFilhote => "Lobo de Geada Filhote",
                EspecieDePet.CarapacaJovem => "Carapaça Jovem",
                EspecieDePet.MosquitoDoPantano => "Mosquito do Pântano",
                EspecieDePet.CachorroComum => "Cachorro Doméstico",
                EspecieDePet.GatoComum => "Gato Rajado",
                EspecieDePet.GalinhaComum => "Galinha do Vilarejo",
                _ => "Desconhecido",
            };

        /// <summary>Texto informativo sobre onde obter o pet.</summary>
        public static string ObterOrigem(EspecieDePet especie) =>
            especie switch
            {
                EspecieDePet.CervoJovem => "Drop raro do Cervo de Galhada Oca (2%)",
                EspecieDePet.FilhoteDeJavali => "Drop raro do Javali do Campo ou Ferrugem (2%)",
                EspecieDePet.LoboFilhote => "Drop raro do Lobo do Campo (2%)",
                EspecieDePet.TatuPequeno => "Drop raro do Tatu Rochoso (2%)",
                EspecieDePet.CorujaDaClareira => "Drop raro do Morcego das Sombras (2%)",
                EspecieDePet.MariposaLuz => "Drop raro da Mariposa de Breu (2%)",
                EspecieDePet.CoelhoDoPasto => "Drop raro do Javali do Campo (2%)",
                EspecieDePet.LoboDeGeadaFilhote => "Drop raro do Lobo de Geada (2%)",
                EspecieDePet.CarapacaJovem => "Drop raro da Carapaça de Brasa (2%)",
                EspecieDePet.MosquitoDoPantano => "Drop raro do Morcego das Sombras (2%)",
                EspecieDePet.CachorroComum =>
                    "Comprado com a Cuidadora Mirna na Brasa Mansa (100 coroas)",
                EspecieDePet.GatoComum =>
                    "Comprado com a Cuidadora Mirna na Brasa Mansa (75 coroas)",
                EspecieDePet.GalinhaComum =>
                    "Comprado com a Cuidadora Mirna na Brasa Mansa (50 coroas)",
                _ => string.Empty,
            };

        /// <summary>Descrição de apresentação do pet.</summary>
        public static string ObterDescricao(EspecieDePet especie) =>
            especie switch
            {
                EspecieDePet.CervoJovem =>
                    "Um filhote dócil de cervo que o segue pelo mundo e ajuda a recolher tesouros caídos ao chão.",
                EspecieDePet.FilhoteDeJavali =>
                    "Porquinho teimoso que imita o javali do campo; ajuda a recolher loot como os demais pets.",
                EspecieDePet.LoboFilhote =>
                    "Filhote esguio que corre atrás do dono e busca itens caídos por perto.",
                EspecieDePet.TatuPequeno =>
                    "Filhote blindado, passos pesados no visual; mesma regra de coleta automática.",
                EspecieDePet.CorujaDaClareira =>
                    "Ave silenciosa, boa companhia para quem caça de noite.",
                EspecieDePet.MariposaLuz =>
                    "Enxame miniatura bioluminescente; um pet só, visual de enxame.",
                EspecieDePet.CoelhoDoPasto =>
                    "Companheiro leve do pasto; ideal como troféu de farm no campo.",
                EspecieDePet.LoboDeGeadaFilhote =>
                    "Filhote de pelagem gelada; segue e recolhe loot como os outros.",
                EspecieDePet.CarapacaJovem =>
                    "Réplica pequena da criatura de lava; só visual, sem combate.",
                EspecieDePet.MosquitoDoPantano =>
                    "Enxame irritante do pântano; mesmo comportamento de coleta.",
                EspecieDePet.CachorroComum =>
                    "Companheiro fiel e leal que fareja tesouros caídos e segue o dono em qualquer jornada.",
                EspecieDePet.GatoComum =>
                    "Felino ágil e curioso que vagueia ao seu lado e ajuda a recolher espólios pelo chão.",
                EspecieDePet.GalinhaComum =>
                    "Galinha dócil e veloz do vilarejo que cisca e busca itens pelo chão.",
                _ => string.Empty,
            };
    }
}
