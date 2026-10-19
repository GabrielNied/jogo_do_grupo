namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Escolha do alvo mostrado no topo do HUD (proposta em docs/features/interface.md). O client
    /// só aplica; vida e posição vêm do servidor.
    /// </summary>
    public static class RegrasAlvoDoHud
    {
        /// <summary>Alvo comum ou boss só conta até esta distância horizontal do jogador.</summary>
        public const float DistanciaMaximaMetros = 30f;

        /// <summary>Último acerto dado ou recebido ainda vale por este tempo.</summary>
        public const double JanelaDoUltimoContatoSegundos = 5.0;

        /// <summary>Raio na viewport (0–1) em torno do centro para "sob a mira".</summary>
        public const float RaioDaMiraViewport = 0.08f;

        /// <summary>Dados mínimos de um inimigo para a escolha (sem Unity).</summary>
        public readonly struct Candidato
        {
            public Candidato(
                long inimigoId,
                int criaturaId,
                int vida,
                int vidaMaxima,
                float distanciaMetros,
                float distanciaAoCentroViewport,
                long alvoId
            )
            {
                InimigoId = inimigoId;
                CriaturaId = criaturaId;
                Vida = vida;
                VidaMaxima = vidaMaxima;
                DistanciaMetros = distanciaMetros;
                DistanciaAoCentroViewport = distanciaAoCentroViewport;
                AlvoId = alvoId;
            }

            public long InimigoId { get; }
            public int CriaturaId { get; }
            public int Vida { get; }
            public int VidaMaxima { get; }
            public float DistanciaMetros { get; }
            public float DistanciaAoCentroViewport { get; }
            public long AlvoId { get; }
        }

        public static bool EstaVivo(Candidato candidato) =>
            candidato.Vida > 0 && candidato.VidaMaxima > 0;

        public static bool DentroDoAlcance(Candidato candidato) =>
            candidato.DistanciaMetros <= DistanciaMaximaMetros;

        public static bool EhChefe(int criaturaId)
        {
            var ficha = CatalogoCriaturas.Obter(criaturaId);
            if (ficha == null)
            {
                return false;
            }

            return ficha.Categoria == CategoriaDeCriatura.Chefe
                || RegrasCriaturas.AvisaNaArea(ficha.Categoria);
        }

        /// <summary>
        /// Alvo comum: (1) vivo sob a mira dentro do alcance; (2) senão o último contato recente.
        /// </summary>
        public static long? EscolherAlvoComum(
            Candidato[] candidatos,
            long? inimigoSobMiraId,
            long? ultimoContatoId,
            double segundosDesdeUltimoContato
        )
        {
            if (candidatos == null || candidatos.Length == 0)
            {
                return null;
            }

            if (
                inimigoSobMiraId != null
                && CandidatoValido(candidatos, inimigoSobMiraId.Value, chefe: false)
            )
            {
                return inimigoSobMiraId;
            }

            if (
                ultimoContatoId != null
                && segundosDesdeUltimoContato >= 0.0
                && segundosDesdeUltimoContato <= JanelaDoUltimoContatoSegundos
                && CandidatoValido(candidatos, ultimoContatoId.Value, chefe: false)
            )
            {
                return ultimoContatoId;
            }

            return null;
        }

        /// <summary>
        /// Boss no topo: chefe vivo em luta (perto, perseguindo o jogador ou contato recente).
        /// Vários: o mais perto do jogador.
        /// </summary>
        public static long? EscolherBoss(
            Candidato[] candidatos,
            long personagemId,
            long? ultimoContatoComBossId,
            double segundosDesdeContatoComBoss
        )
        {
            if (candidatos == null || candidatos.Length == 0)
            {
                return null;
            }

            long? escolhido = null;
            var menorDistancia = float.MaxValue;
            for (var i = 0; i < candidatos.Length; i++)
            {
                var c = candidatos[i];
                if (!EstaVivo(c) || !EhChefe(c.CriaturaId))
                {
                    continue;
                }

                var emLuta =
                    DentroDoAlcance(c)
                    || c.AlvoId == personagemId
                    || (
                        ultimoContatoComBossId == c.InimigoId
                        && segundosDesdeContatoComBoss >= 0.0
                        && segundosDesdeContatoComBoss <= JanelaDoUltimoContatoSegundos
                    );
                if (!emLuta)
                {
                    continue;
                }

                if (c.DistanciaMetros < menorDistancia)
                {
                    menorDistancia = c.DistanciaMetros;
                    escolhido = c.InimigoId;
                }
            }

            return escolhido;
        }

        /// <summary>
        /// Alvo comum quando o boss ocupa o slot principal: outro inimigo sob mira ou contato
        /// recente, desde que não seja o boss já mostrado acima.
        /// </summary>
        public static long? EscolherAlvoSecundario(
            Candidato[] candidatos,
            long bossId,
            long? inimigoSobMiraId,
            long? ultimoContatoId,
            double segundosDesdeUltimoContato
        )
        {
            var alvo = EscolherAlvoComum(
                candidatos,
                inimigoSobMiraId,
                ultimoContatoId,
                segundosDesdeUltimoContato
            );
            if (alvo == null || alvo.Value == bossId)
            {
                return null;
            }

            return alvo;
        }

        private static bool CandidatoValido(Candidato[] candidatos, long id, bool chefe)
        {
            for (var i = 0; i < candidatos.Length; i++)
            {
                var c = candidatos[i];
                if (c.InimigoId != id)
                {
                    continue;
                }

                if (!EstaVivo(c) || !DentroDoAlcance(c))
                {
                    return false;
                }

                var ehChefe = EhChefe(c.CriaturaId);
                return chefe ? ehChefe : !ehChefe;
            }

            return false;
        }
    }
}
