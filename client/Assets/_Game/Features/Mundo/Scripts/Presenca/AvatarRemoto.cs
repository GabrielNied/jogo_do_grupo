using System;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Estado de um outro jogador na cidade: a posição mais recente do servidor (alvo) e a
    /// posição mostrada, interpolada suavemente por buffer de snapshots.
    /// </summary>
    public sealed class AvatarRemoto
    {
        public const float DistanciaDeSalto = ConfigInterpolacaoMundo.DistanciaDeSalto;

        private readonly InterpoladorDeMovimento _interpolador = new InterpoladorDeMovimento();

        public AvatarRemoto(
            long personagemId,
            Vector3 posicao,
            float rotacaoY,
            string nome = "",
            int raca = 0
        )
        {
            PersonagemId = personagemId;
            Nome = nome ?? string.Empty;
            Raca = raca;
            Posicao = posicao;
            Alvo = posicao;
            RotacaoY = rotacaoY;
            AlvoRotacaoY = rotacaoY;
            _interpolador.Teleportar(
                posicao.x,
                posicao.y,
                posicao.z,
                rotacaoY,
                -ConfigInterpolacaoMundo.AtrasoDeRenderSegundos
            );
        }

        public long PersonagemId { get; }

        /// <summary>Nome do personagem (vazio até o servidor informar).</summary>
        public string Nome { get; private set; }

        /// <summary>Raça como número (0 até o servidor informar).</summary>
        public int Raca { get; private set; }
        public Vector3 Posicao { get; private set; }
        public float RotacaoY { get; private set; }
        public Vector3 Alvo { get; private set; }
        public float AlvoRotacaoY { get; private set; }
        public float Velocidade { get; private set; }
        public double Relogio { get; private set; }

        /// <summary>
        /// Guarda nome e raça que o servidor informou. Informação vazia (nome em branco, raça 0)
        /// não apaga o que já se sabe. Devolve <c>true</c> se algo mudou.
        /// </summary>
        public bool DefinirIdentidade(string nome, int raca)
        {
            var mudou = false;
            if (!string.IsNullOrEmpty(nome) && nome != Nome)
            {
                Nome = nome;
                mudou = true;
            }

            if (raca != 0 && raca != Raca)
            {
                Raca = raca;
                mudou = true;
            }

            return mudou;
        }

        public bool DefinirAlvo(Vector3 posicao, float rotacaoY) =>
            AdicionarAmostra(posicao, rotacaoY, 0, 0);

        public bool AdicionarAmostra(
            Vector3 posicao,
            float rotacaoY,
            int sequencia,
            long tempoServidorMs
        )
        {
            Alvo = posicao;
            AlvoRotacaoY = rotacaoY;
            if ((posicao - Posicao).sqrMagnitude > DistanciaDeSalto * DistanciaDeSalto)
            {
                Teleportar(posicao, rotacaoY);
                return true;
            }

            return _interpolador.Adicionar(
                new SnapshotDeMovimento(
                    Relogio,
                    posicao.x,
                    posicao.y,
                    posicao.z,
                    rotacaoY,
                    sequencia,
                    tempoServidorMs
                )
            );
        }

        /// <summary>Posiciona direto, sem interpolar (entrada na cidade ou teleporte).</summary>
        public void Teleportar(Vector3 posicao, float rotacaoY)
        {
            Posicao = posicao;
            Alvo = posicao;
            RotacaoY = rotacaoY;
            AlvoRotacaoY = rotacaoY;
            Velocidade = 0f;
            _interpolador.Teleportar(
                posicao.x,
                posicao.y,
                posicao.z,
                rotacaoY,
                Relogio - ConfigInterpolacaoMundo.AtrasoDeRenderSegundos
            );
        }

        /// <summary>Avança a posição mostrada em direção ao alvo pelo buffer de snapshots.</summary>
        public void Atualizar(float delta)
        {
            Relogio += Math.Max(0f, delta);
            var pose = _interpolador.Amostrar(
                Relogio - ConfigInterpolacaoMundo.AtrasoDeRenderSegundos
            );
            Posicao = new Vector3(pose.X, pose.Y, pose.Z);
            RotacaoY = pose.RotacaoY;
            Velocidade = pose.Velocidade;
        }
    }
}
