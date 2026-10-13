using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Mobs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>Uma área de aviso no chão: centro no plano e raio, em metros.</summary>
    internal readonly struct AreaDoAviso
    {
        public AreaDoAviso(float centroX, float centroZ, float raio)
        {
            CentroX = centroX;
            CentroZ = centroZ;
            Raio = raio;
        }

        public float CentroX { get; }
        public float CentroZ { get; }
        public float Raio { get; }
    }

    /// <summary>
    /// O aviso do golpe no chão, enquanto o preparo não acaba: uma marca por área do ataque,
    /// com cor e forma pelo tipo.
    /// </summary>
    internal sealed class AvisoDeGolpe
    {
        public AvisoDeGolpe(TipoDeAtaqueDeInimigo tipo, AreaDoAviso[] areas, float duracaoSegundos)
        {
            Tipo = tipo;
            Areas = areas;
            DuracaoSegundos = duracaoSegundos;
            RestanteSegundos = duracaoSegundos;
        }

        public TipoDeAtaqueDeInimigo Tipo { get; }
        public AreaDoAviso[] Areas { get; }
        public float DuracaoSegundos { get; }
        public float RestanteSegundos { get; set; }

        /// <summary>De 0 (acabou de começar) a 1 (o golpe cai).</summary>
        public float Progresso =>
            DuracaoSegundos <= 0f ? 1f : 1f - Mathf.Clamp01(RestanteSegundos / DuracaoSegundos);
    }

    /// <summary>
    /// O que o client sabe de um inimigo: o último estado que o servidor mandou e a pose
    /// suavizada que se mostra. Nada aqui decide dano, alvo ou morte.
    /// </summary>
    internal sealed class InimigoNoClient
    {
        private readonly InterpoladorDeSnapshots _interpolador = new InterpoladorDeSnapshots();
        private readonly Queue<double> _golpesPendentes = new Queue<double>();
        private int _golpesPreparados;
        private int _golpesParaAnimar;

        public InimigoNoClient(InimigoNoMundo dados)
        {
            InimigoId = dados.InimigoId;
            CriaturaId = dados.CriaturaId;
            Variante = dados.Variante;
            Vida = dados.Vida;
            VidaMaxima = dados.VidaMaxima;
            Estado = dados.Estado;
            AlvoId = dados.AlvoId;
            AlvoX = X = dados.X;
            AlvoY = Y = dados.Y;
            AlvoZ = Z = dados.Z;
            AlvoRotacaoY = RotacaoY = dados.RotacaoY;
            EstadoMostrado = dados.Estado;
            // Pose inicial no instante que o render pede ao nascer (relógio 0).
            _interpolador.Adicionar(
                new SnapshotDePose(
                    -ConfigMovimentoDoInimigo.AtrasoDeRenderSegundos,
                    X,
                    Y,
                    Z,
                    RotacaoY,
                    (int)dados.Estado
                )
            );
        }

        /// <summary>
        /// Relógio local do inimigo (soma do que <see cref="Atualizar"/> recebeu). Carimba os
        /// pacotes e define o instante de render, atrás dele pelo
        /// <see cref="ConfigMovimentoDoInimigo.AtrasoDeRenderSegundos"/>.
        /// </summary>
        public double Relogio { get; private set; }

        /// <summary>
        /// Estado que o modelo mostra: o do pacote da pose que se vê, não o mais novo (o modelo
        /// só para e golpeia quando a pose atrasada chega ao ponto onde o servidor parou).
        /// </summary>
        public EstadoDeInimigo EstadoMostrado { get; private set; }

        public long InimigoId { get; }
        public int CriaturaId { get; }
        public VarianteDeCriatura Variante { get; }
        public int Vida { get; set; }
        public int VidaMaxima { get; }
        public EstadoDeInimigo Estado { get; set; }
        public long AlvoId { get; set; }

        /// <summary>Última <c>Sequencia</c> aceita de <see cref="InimigoMudou"/> (0 = nenhuma ainda).</summary>
        public int UltimaSequencia { get; set; }

        /// <summary>Pose mostrada (vai atrás da pose do servidor).</summary>
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        public float RotacaoY { get; private set; }

        /// <summary>Pose mais recente do servidor.</summary>
        public float AlvoX { get; private set; }
        public float AlvoY { get; private set; }
        public float AlvoZ { get; private set; }
        public float AlvoRotacaoY { get; private set; }

        /// <summary>
        /// Guarda a pose e o estado de um pacote do servidor no buffer, carimbados com o
        /// <see cref="Relogio"/>. Devolve falso se o interpolador a descartou.
        /// </summary>
        public bool ReceberPose(float x, float y, float z, float rotacaoY, EstadoDeInimigo estado)
        {
            AlvoX = x;
            AlvoY = y;
            AlvoZ = z;
            AlvoRotacaoY = rotacaoY;
            return _interpolador.Adicionar(
                new SnapshotDePose(Relogio, x, y, z, rotacaoY, (int)estado)
            );
        }

        public AvisoDeGolpe Aviso { get; set; }

        /// <summary>Por quanto o dano é multiplicado enquanto exposto (1 fora da janela).</summary>
        public float ExpostoMultiplicador { get; set; } = 1f;

        /// <summary>Quanto falta da janela exposta, em segundos (zero = fora dela).</summary>
        public float ExpostoRestanteSegundos { get; set; }

        public bool Exposto => ExpostoRestanteSegundos > 0f;

        /// <summary>Quantos avisos de golpe já chegaram; cada um dispara a animação uma vez.</summary>
        public int GolpesPreparados
        {
            get => _golpesPreparados;
            set
            {
                // O modelo só golpeia depois do atraso de render: o servidor já parou o inimigo
                // e a pose atrasada ainda está chegando ao ponto do golpe.
                for (var i = _golpesPreparados; i < value; i++)
                {
                    _golpesPendentes.Enqueue(
                        Relogio + ConfigMovimentoDoInimigo.AtrasoDeRenderSegundos
                    );
                }

                _golpesPreparados = value;
            }
        }

        /// <summary>
        /// Duração do preparo para ajustar o clipe: a do servidor menos o atraso de render, para a
        /// pose de contato cair perto do golpe do servidor (nunca menos da metade).
        /// </summary>
        public float PreparoParaOClipeSegundos =>
            Mathf.Max(
                PreparoDoGolpeSegundos - ConfigMovimentoDoInimigo.AtrasoDeRenderSegundos,
                PreparoDoGolpeSegundos * 0.5f
            );

        /// <summary>Duração do preparo do último golpe, em segundos (zero antes do primeiro).</summary>
        public float PreparoDoGolpeSegundos { get; set; }

        /// <summary>Decide andar e atacar do modelo.</summary>
        public AnimacaoDoInimigo Animacao { get; } = new AnimacaoDoInimigo();

        /// <summary>Vida de 0 a 1; máximo zero não estoura.</summary>
        public float FracaoDeVida
        {
            get
            {
                if (VidaMaxima <= 0)
                {
                    return 0f;
                }

                return Mathf.Clamp01((float)Vida / VidaMaxima);
            }
        }

        public bool Fortalecido => Variante == VarianteDeCriatura.Fortalecida;

        public float Escala => RegrasCriaturas.Escala(Variante);

        /// <summary>Nome e nível do <see cref="CatalogoCriaturas"/>; criatura desconhecida cai no id.</summary>
        public string Rotulo
        {
            get
            {
                var ficha = CatalogoCriaturas.Obter(CriaturaId);
                return ficha == null ? "Criatura " + CriaturaId : ficha.Nome + " Nv " + ficha.Nivel;
            }
        }

        public void Atualizar(float segundos)
        {
            Relogio += segundos;
            while (_golpesPendentes.Count > 0 && _golpesPendentes.Peek() <= Relogio)
            {
                _golpesPendentes.Dequeue();
                _golpesParaAnimar++;
            }

            var pose = _interpolador.Amostrar(
                Relogio - ConfigMovimentoDoInimigo.AtrasoDeRenderSegundos
            );
            X = pose.X;
            Y = pose.Y;
            Z = pose.Z;
            RotacaoY = pose.RotacaoY;
            EstadoMostrado = (EstadoDeInimigo)pose.Etiqueta;
            // A velocidade vem dos pacotes, não da pose mostrada: constante no servidor, constante aqui.
            Animacao.Atualizar(
                EstadoMostrado,
                pose.Velocidade * segundos,
                segundos,
                _golpesParaAnimar
            );

            if (ExpostoRestanteSegundos > 0f)
            {
                ExpostoRestanteSegundos -= segundos;
                if (ExpostoRestanteSegundos <= 0f)
                {
                    ExpostoRestanteSegundos = 0f;
                    ExpostoMultiplicador = 1f;
                }
            }

            if (Aviso != null)
            {
                Aviso.RestanteSegundos -= segundos;
                if (Aviso.RestanteSegundos <= 0f)
                {
                    Aviso = null;
                }
            }
        }
    }
}
