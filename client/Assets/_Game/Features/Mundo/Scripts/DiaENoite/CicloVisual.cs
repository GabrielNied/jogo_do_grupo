using JogoDoGrupo.Client.Features.Mundo.Hub;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.DiaENoite
{
    /// <summary>
    /// Contas puras da aparência pela hora (issue #382): quanto está escuro e de onde vem a luz
    /// principal. De dia ela é o sol, ao meio-dia no ângulo calibrado de <see cref="ConfigIluminacao"/>;
    /// à noite é a lua, a mesma luz do outro lado do céu. Só aparência.
    /// </summary>
    public static class CicloVisual
    {
        /// <summary>
        /// Quanto é noite, de 0 (dia) a 1 (noite fechada). A troca é suave no crepúsculo e passa
        /// de 0,5 no início e no fim da noite de <see cref="RegrasDiaENoite"/>.
        /// </summary>
        public static float Escuridao(float hora)
        {
            var h = RegrasDiaENoite.Normalizar(hora);
            var meia = ConfigDiaENoite.DuracaoDoCrepusculoHoras / 2f;
            var anoitecer = RegrasDiaENoite.InicioDaNoite - meia;
            if (h >= anoitecer && h < RegrasDiaENoite.InicioDaNoite + meia)
            {
                return Mathf.SmoothStep(
                    0f,
                    1f,
                    (h - anoitecer) / ConfigDiaENoite.DuracaoDoCrepusculoHoras
                );
            }

            var amanhecer = RegrasDiaENoite.FimDaNoite - meia;
            if (h >= amanhecer && h < RegrasDiaENoite.FimDaNoite + meia)
            {
                return Mathf.SmoothStep(
                    1f,
                    0f,
                    (h - amanhecer) / ConfigDiaENoite.DuracaoDoCrepusculoHoras
                );
            }

            return RegrasDiaENoite.EhNoite(h) ? 1f : 0f;
        }

        /// <summary>Altura da luz principal no céu, em graus.</summary>
        public static float AlturaDaLuz(float hora)
        {
            var fase = Fase(hora, out var dia);
            var maxima = dia ? ConfigIluminacao.AlturaDoSol : ConfigDiaENoite.AlturaDaLua;
            return ConfigDiaENoite.AlturaMinima
                + (maxima - ConfigDiaENoite.AlturaMinima) * Mathf.Sin(Mathf.PI * fase);
        }

        /// <summary>
        /// Giro da luz principal, em graus. Ao meio do dia é o <see cref="ConfigIluminacao.GiroDoSol"/>
        /// calibrado; a lua anda do outro lado. Dá a volta inteira em um dia, sem pulo.
        /// </summary>
        public static float GiroDaLuz(float hora)
        {
            var fase = Fase(hora, out var dia);
            var giro =
                ConfigIluminacao.GiroDoSol
                + (fase - 0.5f) * ConfigDiaENoite.VarreduraDoCeu
                + (dia ? 0f : 180f);
            return Mathf.Repeat(giro, 360f);
        }

        // Quanto do dia (do fim ao início da noite) ou da noite já passou, de 0 a 1.
        private static float Fase(float hora, out bool dia)
        {
            var h = RegrasDiaENoite.Normalizar(hora);
            var nasce = RegrasDiaENoite.FimDaNoite;
            var poe = RegrasDiaENoite.InicioDaNoite;
            var duracaoDoDia = poe - nasce;
            dia = !RegrasDiaENoite.EhNoite(h);
            if (dia)
            {
                return (h - nasce) / duracaoDoDia;
            }

            var desdeQuePos = Mathf.Repeat(h - poe, RegrasDiaENoite.HorasPorDia);
            return desdeQuePos / (RegrasDiaENoite.HorasPorDia - duracaoDoDia);
        }
    }
}
