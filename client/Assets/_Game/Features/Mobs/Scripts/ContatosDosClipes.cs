using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Qual clipe de golpe toca para cada tipo de ataque do inimigo e em que ponto dele (de 0 a
    /// 1) fica a pose de contato. Os clipes saem do Blender (arte/blender/lib/quadrupede.py,
    /// <c>CONTATO_*</c>, mesmos valores daqui) e o <c>GeradorDeCriaturas</c> liga cada um a um
    /// estado do Animator, disparado pelo gatilho de mesmo nome.
    /// </summary>
    public static class ContatosDosClipes
    {
        /// <summary>Golpe comum de qualquer criatura (também o clipe de reserva).</summary>
        public const string Atacar = "Atacar";

        public const string AtacarInvestida = "AtacarInvestida";
        public const string AtacarRaios = "AtacarRaios";
        public const string AtacarPisada = "AtacarPisada";

        public const float ContatoAtacar = 0.71f;
        public const float ContatoInvestida = 0.85f;
        public const float ContatoRaios = 0.85f;
        public const float ContatoPisada = 0.85f;

        /// <summary>Clipe sem marca: o meio do golpe.</summary>
        public const float ContatoPadrao = 0.5f;

        /// <summary>Clipes de preparo exclusivos do chefe de dungeon.</summary>
        public static readonly string[] ClipesDoChefe =
        {
            AtacarInvestida,
            AtacarRaios,
            AtacarPisada,
        };

        /// <summary>Nome do clipe (e do gatilho do Animator) do ataque.</summary>
        public static string ClipeDoAtaque(TipoDeAtaqueDeInimigo tipo)
        {
            switch (tipo)
            {
                case TipoDeAtaqueDeInimigo.Investida:
                    return AtacarInvestida;
                case TipoDeAtaqueDeInimigo.Raios:
                    return AtacarRaios;
                case TipoDeAtaqueDeInimigo.Pisada:
                    return AtacarPisada;
                default:
                    return Atacar;
            }
        }

        /// <summary>Fração do clipe em que cai o contato; clipe desconhecido usa o meio.</summary>
        public static float Contato(string clipe)
        {
            switch (clipe)
            {
                case Atacar:
                    return ContatoAtacar;
                case AtacarInvestida:
                    return ContatoInvestida;
                case AtacarRaios:
                    return ContatoRaios;
                case AtacarPisada:
                    return ContatoPisada;
                default:
                    return ContatoPadrao;
            }
        }
    }
}
