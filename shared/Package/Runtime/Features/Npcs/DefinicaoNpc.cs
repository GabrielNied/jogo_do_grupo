using System;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>
    /// Definição canônica de um NPC no mundo: identidade, papel, coordenadas e raio de interação (#1063).
    /// </summary>
    [Serializable]
    public class DefinicaoNpc
    {
        public int Id;
        public string Nome = "";
        public PapelNpc Papel;
        public string Area = "";
        public float X;
        public float Y;
        public float Z;
        public float RotacaoY;
        public float RaioInteracao;

        public DefinicaoNpc() { }

        public DefinicaoNpc(
            int id,
            string nome,
            PapelNpc papel,
            string area,
            float x,
            float y,
            float z,
            float rotacaoY = 0f,
            float raioInteracao = 4f
        )
        {
            Id = id;
            Nome = nome;
            Papel = papel;
            Area = area;
            X = x;
            Y = y;
            Z = z;
            RotacaoY = rotacaoY;
            RaioInteracao = raioInteracao;
        }

        /// <summary>Verifica se uma coordenada horizontal está ao alcance deste NPC.</summary>
        public bool Alcanca(float posX, float posZ)
        {
            float dx = posX - X;
            float dz = posZ - Z;
            return (dx * dx + dz * dz) <= (RaioInteracao * RaioInteracao);
        }
    }
}
