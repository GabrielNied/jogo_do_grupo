using NUnit.Framework;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Montarias.Tests
{
    public class VisualDaMontariaTestes
    {
        [Test]
        public void Criar_AcoplaVisualAoDono()
        {
            var avatar = new GameObject("AvatarTeste");
            try
            {
                var visual = VisualDaMontaria.Criar(avatar.transform, 1);
                Assert.IsNotNull(visual);
                Assert.AreEqual(avatar.transform, visual.transform.parent);
                Assert.IsTrue(visual.transform.childCount > 0);
            }
            finally
            {
                Object.DestroyImmediate(avatar);
            }
        }
    }
}
