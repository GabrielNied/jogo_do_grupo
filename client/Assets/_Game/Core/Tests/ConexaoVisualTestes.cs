using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Core.Api;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class ConexaoVisualTestes
    {
        private sealed class FonteFalsa : IFonteDaConexao
        {
            private Action<EstadoTempoReal> _estadoMudou;
            public int Assinaturas { get; private set; }
            public EstadoTempoReal Estado { get; set; }
            public string UltimoErro { get; set; } = "";
            public int Tentativa { get; set; }
            public int Latencia { get; set; } = -1;
            public event Action<EstadoTempoReal> EstadoMudou
            {
                add
                {
                    _estadoMudou += value;
                    Assinaturas++;
                }
                remove
                {
                    _estadoMudou -= value;
                    Assinaturas--;
                }
            }

            public void Mudar(EstadoTempoReal estado)
            {
                Estado = estado;
                _estadoMudou?.Invoke(estado);
            }
        }

        [Test]
        public void EstadoDaRede_EscolheLatenciaReconexaoEFalha()
        {
            Assert.AreEqual(
                AvisoDaConexao.Nenhum,
                ApresentacaoDaConexao.Aviso(EstadoTempoReal.Desconectado, "")
            );
            Assert.AreEqual(
                AvisoDaConexao.Latencia,
                ApresentacaoDaConexao.Aviso(EstadoTempoReal.Conectado, "")
            );
            Assert.AreEqual(
                AvisoDaConexao.Reconectando,
                ApresentacaoDaConexao.Aviso(EstadoTempoReal.Reconectando, "")
            );
            Assert.AreEqual(
                AvisoDaConexao.Reconectando,
                ApresentacaoDaConexao.Aviso(EstadoTempoReal.Conectando, "")
            );
            Assert.AreEqual(
                AvisoDaConexao.Reconectando,
                ApresentacaoDaConexao.Aviso(EstadoTempoReal.Autenticando, "")
            );
            Assert.AreEqual(
                AvisoDaConexao.Perdida,
                ApresentacaoDaConexao.Aviso(
                    EstadoTempoReal.Desconectado,
                    CodigosErroCliente.SemConexao
                )
            );
            Assert.AreEqual(
                AvisoDaConexao.AcessoNegado,
                ApresentacaoDaConexao.Aviso(EstadoTempoReal.Desconectado, CodigosErro.AcessoNegado)
            );
            Assert.AreEqual("Reconectando (2/3)...", ApresentacaoDaConexao.TextoDaFaixa(2, 3));
            Assert.AreEqual("42 ms", ApresentacaoDaConexao.TextoDaLatencia(42));
        }

        [Test]
        public void Apresentador_AtualizaEAoRemoverDesinscreve()
        {
            var fonte = new FonteFalsa();
            var container = new VisualElement();
            var area = new VisualElement();
            var apresentador = new ApresentadorDaConexao(
                fonte,
                container,
                area,
                () => { },
                () => { }
            );
            Assert.AreEqual(1, fonte.Assinaturas);

            fonte.Tentativa = 2;
            fonte.Mudar(EstadoTempoReal.Reconectando);
            Assert.AreEqual(
                DisplayStyle.Flex,
                container.Q<VisualElement>("core.conexao.reconectando").style.display.value
            );

            fonte.Latencia = 42;
            fonte.Mudar(EstadoTempoReal.Conectado);
            Assert.AreEqual("42 ms", area.Q<Label>("core.conexao.latencia").text);

            fonte.UltimoErro = CodigosErroCliente.SemConexao;
            fonte.Mudar(EstadoTempoReal.Desconectado);
            Assert.AreEqual(
                DisplayStyle.Flex,
                container.Q<VisualElement>("core.conexao.perdida").style.display.value
            );

            apresentador.Dispose();
            Assert.AreEqual(0, fonte.Assinaturas);
            Assert.IsNull(container.Q<VisualElement>("core.conexao.perdida"));
            Assert.IsNull(area.Q<Label>("core.conexao.latencia"));
        }
    }
}
