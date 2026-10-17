using UnityEditor;

namespace JogoDoGrupo.Client.Features.Mundo.Editor
{
    /// <summary>Coloca os modelos licenciados da mata em Resources para o jogo poder carregá-los.</summary>
    public static class ImportadorMataEstilizada
    {
        private const string Origem = "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit/Modelos";
        private const string Destino =
            "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit/Resources/Mundo/MataEstilizada";

        private static readonly string[] Modelos =
        {
            "CommonTree_1",
            "CommonTree_2",
            "CommonTree_3",
            "CommonTree_4",
            "CommonTree_5",
            "Bush_Common",
            "Bush_Common_Flowers",
            "Mushroom_Common",
            "Mushroom_Laetiporus",
            "Rock_Medium_1",
            "Rock_Medium_2",
            "Rock_Medium_3",
        };

        public static void Importar()
        {
            CriarPastas();
            foreach (var modelo in Modelos)
            {
                var origem = Origem + "/" + modelo + ".fbx";
                var destino = Destino + "/" + modelo + ".fbx";
                if (AssetDatabase.LoadMainAssetAtPath(destino) != null)
                {
                    continue;
                }

                var erro = AssetDatabase.MoveAsset(origem, destino);
                if (!string.IsNullOrEmpty(erro))
                {
                    throw new System.InvalidOperationException(erro);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CriarPastas()
        {
            var pasta = "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit";
            foreach (var parte in new[] { "Resources", "Mundo", "MataEstilizada" })
            {
                var proxima = pasta + "/" + parte;
                if (!AssetDatabase.IsValidFolder(proxima))
                {
                    AssetDatabase.CreateFolder(pasta, parte);
                }

                pasta = proxima;
            }
        }
    }
}
