using System.Collections.Generic;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Percebe uma esquiva bem-sucedida só com o que o servidor mandou: o jogador estava dentro do
    /// círculo de um golpe avisado, esquivou antes do golpe, estava fora quando o preparo acabou e
    /// nenhuma perda de vida chegou logo depois. Não prevê dano: o aviso só aparece depois que o
    /// tempo do golpe passou e o servidor não tirou vida. Uma mensagem própria do servidor para
    /// "esquivou" tornaria isto exato.
    /// </summary>
    internal sealed class DetectorDeEsquiva
    {
        /// <summary>Espera, depois do fim do preparo, pelo aviso de vida perdida (latência).</summary>
        public const float GracaSegundos = 0.4f;

        private sealed class Ameaca
        {
            public float PrazoDoGolpe;
            public bool Esquivou;
            public bool ForaNoGolpe;
            public bool AmostrouNoGolpe;
            public readonly List<(float x, float z, float raio)> Areas =
                new List<(float x, float z, float raio)>();
        }

        private readonly List<Ameaca> _ameacas = new List<Ameaca>();

        public int Pendentes => _ameacas.Count;

        public static bool EstaDentro(float x, float z, float centroX, float centroZ, float raio)
        {
            var dx = x - centroX;
            var dz = z - centroZ;
            return dx * dx + dz * dz <= raio * raio;
        }

        /// <summary>
        /// Um golpe foi avisado. Só passa a valer se o jogador está dentro de alguma área agora.
        /// Devolve se está (para o aviso "saia da área").
        /// </summary>
        public bool AoAvisoDeGolpe(
            float agora,
            float preparoSegundos,
            IEnumerable<(float x, float z, float raio)> areas,
            float jogadorX,
            float jogadorZ
        )
        {
            var ameaca = new Ameaca { PrazoDoGolpe = agora + preparoSegundos };
            var dentro = false;
            foreach (var area in areas)
            {
                ameaca.Areas.Add(area);
                dentro |= EstaDentro(jogadorX, jogadorZ, area.x, area.z, area.raio);
            }

            if (dentro)
            {
                _ameacas.Add(ameaca);
            }

            return dentro;
        }

        /// <summary>O jogador começou uma esquiva (confirmada pelo servidor).</summary>
        public void AoEsquivar(float agora)
        {
            foreach (var ameaca in _ameacas)
            {
                if (agora <= ameaca.PrazoDoGolpe)
                {
                    ameaca.Esquivou = true;
                }
            }
        }

        /// <summary>O servidor tirou vida do jogador: golpes que acabaram de cair não foram esquivados.</summary>
        public void AoPerderVida(float agora)
        {
            for (var i = _ameacas.Count - 1; i >= 0; i--)
            {
                if (agora >= _ameacas[i].PrazoDoGolpe - GracaSegundos)
                {
                    _ameacas.RemoveAt(i);
                }
            }
        }

        /// <summary>Avança o tempo; devolve quantas esquivas bem-sucedidas fecharam agora.</summary>
        public int Atualizar(float agora, float jogadorX, float jogadorZ)
        {
            var esquivas = 0;
            for (var i = _ameacas.Count - 1; i >= 0; i--)
            {
                var ameaca = _ameacas[i];
                if (!ameaca.AmostrouNoGolpe && agora >= ameaca.PrazoDoGolpe)
                {
                    ameaca.AmostrouNoGolpe = true;
                    var dentro = false;
                    foreach (var area in ameaca.Areas)
                    {
                        dentro |= EstaDentro(jogadorX, jogadorZ, area.x, area.z, area.raio);
                    }

                    ameaca.ForaNoGolpe = !dentro;
                }

                if (agora >= ameaca.PrazoDoGolpe + GracaSegundos)
                {
                    if (ameaca.Esquivou && ameaca.ForaNoGolpe)
                    {
                        esquivas++;
                    }

                    _ameacas.RemoveAt(i);
                }
            }

            return esquivas;
        }

        public void Limpar() => _ameacas.Clear();
    }
}
