using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JogoDoGrupo.Client.Core.Tests
{
    public class ExecucaoClientTestes
    {
        [UnityTest]
        public IEnumerator EntrarEmPlay_MantemExecucaoSemFocoParaProcessarARede()
        {
            Application.runInBackground = false;

            yield return new EnterPlayMode();

            Assert.IsTrue(
                Application.runInBackground,
                "O client online deve continuar processando a rede quando perde o foco."
            );

            yield return new ExitPlayMode();
        }
    }
}
