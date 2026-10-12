using System;
using JogoDoGrupo.Shared.Features.Itens;
using NUnit.Framework;

namespace JogoDoGrupo.Client.Features.Itens.Tests
{
    public class IconesEquipamentoTestes
    {
        [Test]
        public void TodoTipoDeEquipamentoTemIcone()
        {
            foreach (TipoEquipamento tipo in Enum.GetValues(typeof(TipoEquipamento)))
            {
                Assert.IsNotNull(IconesEquipamento.Carregar(tipo), $"Sem ícone para {tipo}");
            }
        }
    }
}
