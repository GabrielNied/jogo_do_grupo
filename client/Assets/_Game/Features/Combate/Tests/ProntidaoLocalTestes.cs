using JogoDoGrupo.Client.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Combate.Tests
{
    public class ProntidaoLocalTestes
    {
        [Test]
        public void SemVigorConhecido_AtaqueEEsquivaSeguem()
        {
            var prontidao = new ProntidaoLocal();

            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Pesado, 0));
            Assert.IsTrue(prontidao.PodeEsquivar(0));
        }

        [Test]
        public void SemVigor_PesadoEEsquivaNaoSeguem_LeveSegue()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarVigor(RegrasEsquiva.CustoDeVigor - 1);

            Assert.IsFalse(prontidao.PodeAtacar(TipoDeAtaque.Pesado, 0));
            Assert.IsFalse(prontidao.PodeEsquivar(0));
            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Leve, 0));
        }

        [Test]
        public void ComVigor_PesadoEEsquivaSeguem()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarVigor(RegrasAtaque.CustoDeVigorPesado);

            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Pesado, 0));
            Assert.IsTrue(prontidao.PodeEsquivar(0));
        }

        [Test]
        public void Recarga_BloqueiaAteADuracaoAcabar()
        {
            var prontidao = new ProntidaoLocal();
            prontidao.RegistrarVigor(100);
            prontidao.MarcarAtaque(TipoDeAtaque.Leve, 10);

            var durante = 10 + RegrasAtaque.DuracaoSegundos(TipoDeAtaque.Leve) - 0.01;
            Assert.IsFalse(prontidao.PodeAtacar(TipoDeAtaque.Leve, durante));
            Assert.IsFalse(prontidao.PodeEsquivar(durante));

            var depois = 10 + RegrasAtaque.DuracaoSegundos(TipoDeAtaque.Leve);
            Assert.IsTrue(prontidao.PodeAtacar(TipoDeAtaque.Leve, depois));
            Assert.IsTrue(prontidao.PodeEsquivar(depois));
        }
    }
}
