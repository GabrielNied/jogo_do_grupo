using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>
    /// Os inimigos da área, conforme as mensagens do servidor. Só guarda e mostra: dano, alvo e
    /// morte são decididos no servidor.
    /// </summary>
    internal sealed class RegistroDeInimigos
    {
        private const float MilissegundosPorSegundo = 1000f;

        private readonly Dictionary<long, InimigoNoClient> _inimigos =
            new Dictionary<long, InimigoNoClient>();

        public IReadOnlyCollection<InimigoNoClient> Todos => _inimigos.Values;

        public int Quantidade => _inimigos.Count;

        public InimigoNoClient Obter(long inimigoId) =>
            _inimigos.TryGetValue(inimigoId, out var inimigo) ? inimigo : null;

        /// <summary>A lista da área substitui a anterior. Devolve quem saiu, para o visual sumir.</summary>
        public List<InimigoNoClient> SubstituirArea(InimigosDaArea mensagem)
        {
            var removidos = new List<InimigoNoClient>(_inimigos.Values);
            _inimigos.Clear();
            if (mensagem?.Inimigos != null)
            {
                foreach (var dados in mensagem.Inimigos)
                {
                    Guardar(dados);
                }
            }

            return removidos;
        }

        /// <summary>Cria (ou recria) um inimigo. Devolve o que foi substituído, se havia.</summary>
        public InimigoNoClient Aparecer(InimigoApareceu mensagem)
        {
            if (mensagem?.Inimigo == null)
            {
                return null;
            }

            _inimigos.TryGetValue(mensagem.Inimigo.InimigoId, out var anterior);
            Guardar(mensagem.Inimigo);
            return anterior;
        }

        /// <summary>
        /// Aplica uma atualização. Descarta a de inimigo desconhecido e a cuja <c>Sequencia</c>
        /// não é maior que a última aceita do mesmo inimigo.
        /// </summary>
        public bool Mudar(InimigoMudou mensagem)
        {
            if (mensagem == null || !_inimigos.TryGetValue(mensagem.InimigoId, out var inimigo))
            {
                return false;
            }

            if (mensagem.Sequencia <= inimigo.UltimaSequencia)
            {
                return false;
            }

            inimigo.UltimaSequencia = mensagem.Sequencia;
            inimigo.AlvoX = mensagem.X;
            inimigo.AlvoY = mensagem.Y;
            inimigo.AlvoZ = mensagem.Z;
            inimigo.AlvoRotacaoY = mensagem.RotacaoY;
            inimigo.Estado = mensagem.Estado;
            inimigo.AlvoId = mensagem.AlvoId;
            inimigo.Vida = mensagem.Vida;
            return true;
        }

        /// <summary>Remove o inimigo (e o aviso dele). Id desconhecido não faz nada.</summary>
        public InimigoNoClient Morrer(InimigoMorreu mensagem)
        {
            if (mensagem == null || !_inimigos.TryGetValue(mensagem.InimigoId, out var inimigo))
            {
                return null;
            }

            _inimigos.Remove(mensagem.InimigoId);
            inimigo.Aviso = null;
            inimigo.Vida = 0;
            return inimigo;
        }

        /// <summary>Começa o círculo de aviso do golpe. Inimigo desconhecido é ignorado.</summary>
        public bool PrepararAtaque(InimigoPreparaAtaque mensagem)
        {
            if (mensagem == null || !_inimigos.TryGetValue(mensagem.InimigoId, out var inimigo))
            {
                return false;
            }

            inimigo.GolpesPreparados++;
            inimigo.Aviso = new AvisoDeGolpe(
                mensagem.CentroX,
                mensagem.CentroZ,
                mensagem.Raio,
                mensagem.PreparoMs / MilissegundosPorSegundo
            );
            return true;
        }

        /// <summary>
        /// A barra segue o golpe do jogador: o servidor manda a vida que sobrou em cada acerto
        /// (<c>AlvoId</c> = <c>InimigoId</c>). Alvos que não são inimigos são ignorados.
        /// </summary>
        public void AplicarAcertos(AtaqueAcertou mensagem)
        {
            if (mensagem?.Acertos == null)
            {
                return;
            }

            foreach (var acerto in mensagem.Acertos)
            {
                if (acerto != null && _inimigos.TryGetValue(acerto.AlvoId, out var inimigo))
                {
                    inimigo.Vida = acerto.VidaRestante;
                }
            }
        }

        public void Atualizar(float segundos)
        {
            foreach (var inimigo in _inimigos.Values)
            {
                inimigo.Atualizar(segundos);
            }
        }

        public void Limpar() => _inimigos.Clear();

        private void Guardar(InimigoNoMundo dados)
        {
            if (dados != null)
            {
                _inimigos[dados.InimigoId] = new InimigoNoClient(dados);
            }
        }
    }
}
