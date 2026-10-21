using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Npcs.Editor
{
    /// <summary>
    /// Gera de forma reproduzivel o visual dos NPCs por papel (#1064): material, clipes de falar e
    /// trabalhar, controladores e um prefab por papel em <c>Resources/Npcs/Npc_&lt;Papel&gt;</c>. A base
    /// e o Monk do Quaternius (CC0, <c>ThirdParty/Quaternius/RPGCharacters</c>); roupa e acessorio
    /// sao formas simples presas aos ossos, para cada papel ser reconhecido de longe. A variacao dos
    /// moradores (cabelo, barba, cor, altura) fica pronta no prefab e e sorteada por semente em
    /// <c>AparenciaDeNpc</c>. Menu: Jogo do Grupo/Npcs/Gerar visual dos NPCs.
    /// </summary>
    public static class GeradorDeVisualDeNpcs
    {
        public const string PastaArte = "Assets/_Game/Features/Npcs/Art";
        public const string PastaClipes = PastaArte + "/Clipes";
        public const string PastaMateriais = PastaArte + "/Materiais";
        public const string PastaResources = "Assets/_Game/Features/Npcs/Resources/Npcs";
        public const string CaminhoModelo = "Assets/ThirdParty/Quaternius/RPGCharacters/Monk.fbx";
        public const string CaminhoTextura =
            "Assets/ThirdParty/Quaternius/RPGCharacters/Monk_Texture.png";

        public const string ControladorPadrao = PastaArte + "/Npc_Padrao.controller";
        public const string ControladorMartelar = PastaArte + "/Npc_Martelar.controller";
        public const string ControladorMexer = PastaArte + "/Npc_Mexer.controller";

        // Altura final do corpo (m) e amplitudes dos gestos (graus): ajustes visuais.
        const float AlturaDoCorpo = 1.8f;
        const float TransicaoDoAnimator = 0.15f;
        const float GestoDeFalar = 28f;
        const float GestoDeMartelar = 70f;
        const float GestoDeMexer = 25f;

        enum Oficio
        {
            Nenhum,
            Martelar,
            Mexer,
        }

        sealed class Parte
        {
            public string Nome;
            public PrimitiveType Forma;
            public string Osso;
            public Vector3 Posicao; // no espaco do modelo, em metros, sobre o corpo de 1,8 m
            public Vector3 Tamanho; // dimensoes finais em metros
            public Vector3 Rotacao; // graus, no espaco do modelo
            public Color Cor;
            public bool Brilha;
        }

        sealed class Receita
        {
            public PapelNpc Papel;
            public Oficio Oficio;
            public Color Roupa = Color.white;
            public List<Parte> Partes = new List<Parte>();
        }

        static readonly Color Aco = new Color(0.62f, 0.66f, 0.72f);
        static readonly Color Couro = new Color(0.42f, 0.27f, 0.15f);
        static readonly Color Madeira = new Color(0.45f, 0.32f, 0.18f);
        static readonly Color Branco = new Color(0.92f, 0.9f, 0.84f);
        static readonly Color Ouro = new Color(0.9f, 0.72f, 0.2f);

        static Parte P(
            string nome,
            PrimitiveType forma,
            string osso,
            Vector3 posicao,
            Vector3 tamanho,
            Color cor,
            Vector3 rotacao = default,
            bool brilha = false
        ) =>
            new Parte
            {
                Nome = nome,
                Forma = forma,
                Osso = osso,
                Posicao = posicao,
                Tamanho = tamanho,
                Cor = cor,
                Rotacao = rotacao,
                Brilha = brilha,
            };

        // Coordenadas na pose parada, com o corpo de 1,8 m e a frente em +Z: cabeca de 1,38 a 1,80 m,
        // tronco em 1,0 m, quadril em 0,65 m. Partes presas a "Fist.*" usam a posicao como deslocamento
        // a partir da mao.
        static IEnumerable<Parte> Elmo(Color cor) =>
            new[]
            {
                P(
                    "Elmo",
                    PrimitiveType.Sphere,
                    "Head",
                    new Vector3(0, 1.62f, -0.08f),
                    new Vector3(0.36f, 0.48f, 0.4f),
                    cor
                ),
                P(
                    "Cimeira",
                    PrimitiveType.Cube,
                    "Head",
                    new Vector3(0, 1.9f, -0.08f),
                    new Vector3(0.05f, 0.12f, 0.3f),
                    new Color(0.75f, 0.2f, 0.15f)
                ),
            };

        static IEnumerable<Parte> Peitoral(Color cor) =>
            new[]
            {
                P(
                    "Peitoral",
                    PrimitiveType.Cube,
                    "Torso",
                    new Vector3(0, 1.04f, 0.04f),
                    new Vector3(0.5f, 0.4f, 0.34f),
                    cor
                ),
                P(
                    "OmbreiraE",
                    PrimitiveType.Sphere,
                    "Torso",
                    new Vector3(-0.3f, 1.22f, 0),
                    new Vector3(0.22f, 0.18f, 0.22f),
                    cor
                ),
                P(
                    "OmbreiraD",
                    PrimitiveType.Sphere,
                    "Torso",
                    new Vector3(0.3f, 1.22f, 0),
                    new Vector3(0.22f, 0.18f, 0.22f),
                    cor
                ),
            };

        static Parte Tunica(Color cor) =>
            P(
                "Tunica",
                PrimitiveType.Cube,
                "Torso",
                new Vector3(0, 1.0f, 0.03f),
                new Vector3(0.5f, 0.6f, 0.34f),
                cor
            );

        static Parte Avental(Color cor) =>
            P(
                "Avental",
                PrimitiveType.Cube,
                "Hips",
                new Vector3(0, 0.6f, 0.17f),
                new Vector3(0.38f, 0.55f, 0.04f),
                cor
            );

        static IEnumerable<Parte> ChapeuDeAba(Color cor, float altura) =>
            new[]
            {
                P(
                    "Aba",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.76f, -0.08f),
                    new Vector3(0.62f, 0.03f, 0.62f),
                    cor
                ),
                P(
                    "Copa",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.76f + altura / 2f, -0.08f),
                    new Vector3(0.38f, altura, 0.38f),
                    cor
                ),
            };

        static List<Receita> Receitas()
        {
            var lista = new List<Receita>();

            var ferreiro = new Receita
            {
                Papel = PapelNpc.Ferreiro,
                Oficio = Oficio.Martelar,
                Roupa = new Color(0.8f, 0.72f, 0.68f),
            };
            ferreiro.Partes.Add(Avental(Couro));
            ferreiro.Partes.Add(
                P(
                    "Martelo",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.2f, 0.1f),
                    new Vector3(0.05f, 0.5f, 0.05f),
                    Madeira
                )
            );
            ferreiro.Partes.Add(
                P(
                    "MartelaCabeca",
                    PrimitiveType.Cube,
                    "Fist.R",
                    new Vector3(0, 0.46f, 0.1f),
                    new Vector3(0.22f, 0.12f, 0.12f),
                    Aco
                )
            );
            lista.Add(ferreiro);

            var guarda = new Receita
            {
                Papel = PapelNpc.Guarda,
                Roupa = new Color(0.72f, 0.78f, 0.9f),
            };
            guarda.Partes.AddRange(Elmo(Aco));
            guarda.Partes.AddRange(Peitoral(Aco));
            guarda.Partes.Add(
                P(
                    "Lanca",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.7f, 0.02f),
                    new Vector3(0.045f, 2.2f, 0.045f),
                    Madeira
                )
            );
            guarda.Partes.Add(
                P(
                    "Ponta",
                    PrimitiveType.Cube,
                    "Fist.R",
                    new Vector3(0, 1.9f, 0.02f),
                    new Vector3(0.1f, 0.22f, 0.02f),
                    Aco,
                    new Vector3(0, 45, 0)
                )
            );
            lista.Add(guarda);

            var armeiro = new Receita
            {
                Papel = PapelNpc.Armeiro,
                Roupa = new Color(0.9f, 0.7f, 0.65f),
            };
            armeiro.Partes.Add(Tunica(new Color(0.72f, 0.18f, 0.14f)));
            armeiro.Partes.Add(Avental(Couro));
            armeiro.Partes.Add(
                P(
                    "Espada",
                    PrimitiveType.Cube,
                    "Fist.R",
                    new Vector3(0, 0.45f, 0.05f),
                    new Vector3(0.07f, 0.95f, 0.02f),
                    Aco
                )
            );
            armeiro.Partes.Add(
                P(
                    "Guarda",
                    PrimitiveType.Cube,
                    "Fist.R",
                    new Vector3(0, 0.0f, 0.05f),
                    new Vector3(0.26f, 0.04f, 0.05f),
                    Ouro
                )
            );
            lista.Add(armeiro);

            var armadureiro = new Receita
            {
                Papel = PapelNpc.Armadureiro,
                Roupa = new Color(0.72f, 0.75f, 0.82f),
                Oficio = Oficio.Martelar,
            };
            armadureiro.Partes.AddRange(Peitoral(new Color(0.5f, 0.62f, 0.78f)));
            armadureiro.Partes.Add(Avental(Couro));
            armadureiro.Partes.Add(
                P(
                    "Martelinho",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.16f, 0.1f),
                    new Vector3(0.04f, 0.36f, 0.04f),
                    Madeira
                )
            );
            armadureiro.Partes.Add(
                P(
                    "MartelinhoCabeca",
                    PrimitiveType.Cube,
                    "Fist.R",
                    new Vector3(0, 0.34f, 0.1f),
                    new Vector3(0.16f, 0.09f, 0.09f),
                    Aco
                )
            );
            lista.Add(armadureiro);

            var taverneiro = new Receita
            {
                Papel = PapelNpc.Taverneiro,
                Oficio = Oficio.Mexer,
                Roupa = new Color(1f, 0.9f, 0.7f),
            };
            taverneiro.Partes.Add(Avental(Branco));
            taverneiro.Partes.Add(
                P(
                    "Gorro",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.92f, -0.08f),
                    new Vector3(0.36f, 0.26f, 0.36f),
                    Branco
                )
            );
            taverneiro.Partes.Add(
                P(
                    "Panela",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.1f, 0.18f),
                    new Vector3(0.26f, 0.16f, 0.26f),
                    new Color(0.2f, 0.2f, 0.22f)
                )
            );
            taverneiro.Partes.Add(
                P(
                    "Colher",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.26f, 0.18f),
                    new Vector3(0.03f, 0.36f, 0.03f),
                    Madeira
                )
            );
            lista.Add(taverneiro);

            var merceeiro = new Receita
            {
                Papel = PapelNpc.Merceeiro,
                Roupa = new Color(1f, 0.92f, 0.65f),
            };
            merceeiro.Partes.AddRange(ChapeuDeAba(new Color(0.8f, 0.6f, 0.2f), 0.16f));
            merceeiro.Partes.Add(
                P(
                    "Bolsa",
                    PrimitiveType.Cube,
                    "Hips",
                    new Vector3(-0.28f, 0.6f, 0.1f),
                    new Vector3(0.18f, 0.22f, 0.12f),
                    Couro
                )
            );
            merceeiro.Partes.Add(Avental(new Color(0.85f, 0.7f, 0.3f)));
            lista.Add(merceeiro);

            var vendedor = new Receita
            {
                Papel = PapelNpc.Vendedor,
                Roupa = new Color(1f, 0.85f, 0.55f),
            };
            vendedor.Partes.AddRange(ChapeuDeAba(new Color(0.55f, 0.3f, 0.12f), 0.12f));
            vendedor.Partes.Add(Tunica(new Color(0.9f, 0.68f, 0.2f)));
            vendedor.Partes.Add(
                P(
                    "Bolsa",
                    PrimitiveType.Cube,
                    "Hips",
                    new Vector3(0.28f, 0.6f, 0.1f),
                    new Vector3(0.18f, 0.22f, 0.12f),
                    Couro
                )
            );
            lista.Add(vendedor);

            var banqueiro = new Receita
            {
                Papel = PapelNpc.Banqueiro,
                Roupa = new Color(0.65f, 0.85f, 0.75f),
            };
            banqueiro.Partes.Add(Tunica(new Color(0.12f, 0.45f, 0.3f)));
            banqueiro.Partes.Add(
                P(
                    "ChapeuAlto",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.94f, -0.08f),
                    new Vector3(0.34f, 0.34f, 0.34f),
                    new Color(0.08f, 0.3f, 0.2f)
                )
            );
            banqueiro.Partes.Add(
                P(
                    "ChapeuAba",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.77f, -0.08f),
                    new Vector3(0.5f, 0.03f, 0.5f),
                    new Color(0.08f, 0.3f, 0.2f)
                )
            );
            banqueiro.Partes.Add(
                P(
                    "Livro",
                    PrimitiveType.Cube,
                    "Fist.L",
                    new Vector3(0, 0.05f, 0.22f),
                    new Vector3(0.22f, 0.04f, 0.3f),
                    new Color(0.35f, 0.1f, 0.1f)
                )
            );
            banqueiro.Partes.Add(
                P(
                    "Corrente",
                    PrimitiveType.Cube,
                    "Torso",
                    new Vector3(0, 1.2f, 0.2f),
                    new Vector3(0.3f, 0.03f, 0.03f),
                    Ouro
                )
            );
            lista.Add(banqueiro);

            var leiloeiro = new Receita
            {
                Papel = PapelNpc.Leiloeiro,
                Roupa = new Color(0.7f, 0.82f, 1f),
            };
            leiloeiro.Partes.Add(Tunica(new Color(0.15f, 0.4f, 0.75f)));
            leiloeiro.Partes.Add(
                P(
                    "ChapeuAlto",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.94f, -0.08f),
                    new Vector3(0.34f, 0.34f, 0.34f),
                    new Color(0.1f, 0.25f, 0.55f)
                )
            );
            leiloeiro.Partes.Add(
                P(
                    "ChapeuAba",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.77f, -0.08f),
                    new Vector3(0.52f, 0.03f, 0.52f),
                    new Color(0.1f, 0.25f, 0.55f)
                )
            );
            leiloeiro.Partes.Add(
                P(
                    "Martelete",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.12f, 0.1f),
                    new Vector3(0.035f, 0.26f, 0.035f),
                    Madeira
                )
            );
            leiloeiro.Partes.Add(
                P(
                    "MarteleteCabeca",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.26f, 0.1f),
                    new Vector3(0.09f, 0.16f, 0.09f),
                    Madeira,
                    new Vector3(0, 0, 90)
                )
            );
            lista.Add(leiloeiro);

            var cavalarico = new Receita
            {
                Papel = PapelNpc.Cavalarico,
                Roupa = new Color(0.85f, 0.8f, 0.65f),
            };
            cavalarico.Partes.Add(
                P(
                    "ChapeuPalha",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.76f, -0.08f),
                    new Vector3(0.7f, 0.03f, 0.7f),
                    new Color(0.85f, 0.75f, 0.4f)
                )
            );
            cavalarico.Partes.Add(
                P(
                    "ChapeuCopa",
                    PrimitiveType.Sphere,
                    "Head",
                    new Vector3(0, 1.82f, -0.08f),
                    new Vector3(0.38f, 0.26f, 0.38f),
                    new Color(0.85f, 0.75f, 0.4f)
                )
            );
            cavalarico.Partes.Add(Tunica(new Color(0.55f, 0.4f, 0.22f)));
            cavalarico.Partes.Add(
                P(
                    "Forcado",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.6f, 0.02f),
                    new Vector3(0.04f, 2f, 0.04f),
                    Madeira
                )
            );
            cavalarico.Partes.Add(
                P(
                    "ForcadoPontas",
                    PrimitiveType.Cube,
                    "Fist.R",
                    new Vector3(0, 1.6f, 0.02f),
                    new Vector3(0.2f, 0.04f, 0.03f),
                    Aco
                )
            );
            lista.Add(cavalarico);

            var cuidador = new Receita
            {
                Papel = PapelNpc.CuidadorDePets,
                Roupa = new Color(0.8f, 1f, 0.8f),
            };
            cuidador.Partes.Add(
                P(
                    "Lenco",
                    PrimitiveType.Sphere,
                    "Head",
                    new Vector3(0, 1.76f, -0.08f),
                    new Vector3(0.42f, 0.22f, 0.44f),
                    new Color(0.2f, 0.65f, 0.35f)
                )
            );
            cuidador.Partes.Add(Tunica(new Color(0.3f, 0.7f, 0.4f)));
            cuidador.Partes.Add(
                P(
                    "Cesto",
                    PrimitiveType.Cylinder,
                    "Fist.L",
                    new Vector3(0, -0.02f, 0.14f),
                    new Vector3(0.3f, 0.2f, 0.3f),
                    Madeira
                )
            );
            cuidador.Partes.Add(
                P(
                    "CestoPano",
                    PrimitiveType.Cylinder,
                    "Fist.L",
                    new Vector3(0, 0.1f, 0.14f),
                    new Vector3(0.28f, 0.03f, 0.28f),
                    new Color(0.9f, 0.8f, 0.6f)
                )
            );
            lista.Add(cuidador);

            var dador = new Receita
            {
                Papel = PapelNpc.DadorDeMissao,
                Roupa = new Color(0.9f, 0.8f, 1f),
            };
            dador.Partes.Add(
                P(
                    "Capa",
                    PrimitiveType.Cube,
                    "Torso",
                    new Vector3(0, 0.95f, -0.2f),
                    new Vector3(0.54f, 0.9f, 0.04f),
                    new Color(0.5f, 0.2f, 0.7f),
                    new Vector3(8, 0, 0)
                )
            );
            dador.Partes.Add(
                P(
                    "Ombros",
                    PrimitiveType.Cube,
                    "Torso",
                    new Vector3(0, 1.26f, 0),
                    new Vector3(0.66f, 0.1f, 0.34f),
                    new Color(0.5f, 0.2f, 0.7f)
                )
            );
            dador.Partes.Add(
                P(
                    "Selo",
                    PrimitiveType.Sphere,
                    "Torso",
                    new Vector3(0, 1.12f, 0.2f),
                    new Vector3(0.09f, 0.09f, 0.05f),
                    Ouro,
                    default,
                    true
                )
            );
            lista.Add(dador);

            var morador = new Receita
            {
                Papel = PapelNpc.Morador,
                Roupa = new Color(0.95f, 0.9f, 0.85f),
            };
            morador.Partes.Add(Tunica(new Color(0.55f, 0.5f, 0.42f)));
            lista.Add(morador);

            var mago = new Receita { Papel = PapelNpc.Mago, Roupa = new Color(0.8f, 0.8f, 1f) };
            mago.Partes.Add(
                P(
                    "Manto",
                    PrimitiveType.Cube,
                    "Torso",
                    new Vector3(0, 0.82f, 0.02f),
                    new Vector3(0.56f, 1.0f, 0.38f),
                    new Color(0.2f, 0.25f, 0.6f)
                )
            );
            mago.Partes.Add(
                P(
                    "Capa",
                    PrimitiveType.Cube,
                    "Torso",
                    new Vector3(0, 0.9f, -0.22f),
                    new Vector3(0.58f, 0.95f, 0.04f),
                    new Color(0.12f, 0.15f, 0.45f),
                    new Vector3(6, 0, 0)
                )
            );
            mago.Partes.Add(
                P(
                    "ChapeuAba",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.77f, -0.08f),
                    new Vector3(0.62f, 0.03f, 0.62f),
                    new Color(0.15f, 0.2f, 0.5f)
                )
            );
            mago.Partes.Add(
                P(
                    "ChapeuBase",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 1.89f, -0.08f),
                    new Vector3(0.38f, 0.24f, 0.38f),
                    new Color(0.15f, 0.2f, 0.5f)
                )
            );
            mago.Partes.Add(
                P(
                    "ChapeuMeio",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 2.11f, -0.08f),
                    new Vector3(0.26f, 0.2f, 0.26f),
                    new Color(0.15f, 0.2f, 0.5f)
                )
            );
            mago.Partes.Add(
                P(
                    "ChapeuPonta",
                    PrimitiveType.Cylinder,
                    "Head",
                    new Vector3(0, 2.28f, -0.08f),
                    new Vector3(0.13f, 0.14f, 0.13f),
                    new Color(0.15f, 0.2f, 0.5f)
                )
            );
            mago.Partes.Add(
                P(
                    "Cajado",
                    PrimitiveType.Cylinder,
                    "Fist.R",
                    new Vector3(0, 0.7f, 0.05f),
                    new Vector3(0.05f, 1.7f, 0.05f),
                    Madeira
                )
            );
            mago.Partes.Add(
                P(
                    "Orbe",
                    PrimitiveType.Sphere,
                    "Fist.R",
                    new Vector3(0, 1.64f, 0.05f),
                    new Vector3(0.18f, 0.18f, 0.18f),
                    new Color(1f, 0.85f, 0.4f),
                    default,
                    true
                )
            );
            lista.Add(mago);

            return lista;
        }

        public static string CaminhoPrefab(PapelNpc papel) =>
            PastaResources + "/Npc_" + papel + ".prefab";

        [MenuItem("Jogo do Grupo/Npcs/Gerar visual dos NPCs")]
        public static void Gerar()
        {
            CriarPasta(PastaArte);
            CriarPasta(PastaClipes);
            CriarPasta(PastaMateriais);
            CriarPasta(PastaResources);

            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoModelo);
            if (modelo == null)
                throw new InvalidOperationException("Modelo nao encontrado: " + CaminhoModelo);
            var idle = ObterClipe("Idle");
            var andar = ObterClipe("Walk");

            var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoTextura);

            var relatorio = new StringBuilder();
            var clipes = CriarClipesDeGesto(modelo, idle, relatorio);
            CriarControladores(idle, andar, clipes);

            foreach (var receita in Receitas())
            {
                CriarPrefab(receita, modelo, textura, relatorio);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            File.WriteAllText(
                Path.Combine(Path.GetTempPath(), "gerador-npcs.txt"),
                relatorio.ToString()
            );
        }

        // ---------- conferencia visual ----------

        /// <summary>
        /// Desenha cada prefab (parado e no gesto) numa folha PNG em NPC_PRINTS (pasta de saida),
        /// para conferir o visual sem abrir o editor. Menu: Jogo do Grupo/Npcs/Capturar folha.
        /// </summary>
        [MenuItem("Jogo do Grupo/Npcs/Capturar folha")]
        public static void CapturarFolha()
        {
            var pasta = Environment.GetEnvironmentVariable("NPC_PRINTS");
            if (string.IsNullOrEmpty(pasta))
                pasta = Path.Combine(Path.GetTempPath(), "npcs-prints");
            Directory.CreateDirectory(pasta);

            const int largura = 220;
            const int altura = 360;
            var papeis = (PapelNpc[])Enum.GetValues(typeof(PapelNpc));
            var prefabs = new List<GameObject>();
            foreach (var papel in papeis)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab(papel));
                if (prefab != null)
                    prefabs.Add(prefab);
            }

            var colunas = 6;
            var linhas = Mathf.CeilToInt((prefabs.Count * 2 + 6) / (float)colunas);
            var folha = new Texture2D(
                colunas * largura,
                linhas * altura,
                TextureFormat.RGB24,
                false
            );
            var luzObj = new GameObject("Luz");
            var luz = luzObj.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(35, 150, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            var camObj = new GameObject("Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.5f, 0.55f);
            var alvo = new RenderTexture(largura, altura, 24);
            cam.targetTexture = alvo;
            cam.fieldOfView = 30f;
            var idle = ObterClipe("Idle");
            cam.Render();
            cam.Render();
            var celula = 0;
            foreach (var prefab in prefabs)
            {
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var animator = instancia.GetComponentInChildren<Animator>();
                var gesto =
                    animator != null && animator.runtimeAnimatorController != null
                        ? animator.runtimeAnimatorController.animationClips
                        : new AnimationClip[0];
                var oficio =
                    Array.Find(gesto, c => c.name == "Npc_Martelar" || c.name == "Npc_Mexer")
                    ?? AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        PastaClipes + "/Npc_Falar.anim"
                    );
                for (var pose = 0; pose < 2; pose++)
                {
                    var clipe = pose == 0 ? idle : oficio;
                    if (animator != null && clipe != null)
                        clipe.SampleAnimation(
                            animator.gameObject,
                            clipe.length * (pose == 0 ? 0f : 0.15f)
                        );
                    cam.transform.position = new Vector3(0.9f, 1.3f, 5.4f);
                    cam.transform.LookAt(new Vector3(0, 1.0f, 0));
                    instancia.transform.rotation = Quaternion.Euler(0, -15, 0);
                    cam.Render();
                    cam.Render();
                    RenderTexture.active = alvo;
                    var coluna = celula % colunas;
                    var linha = linhas - 1 - celula / colunas;
                    folha.ReadPixels(
                        new Rect(0, 0, largura, altura),
                        coluna * largura,
                        linha * altura
                    );
                    celula++;
                }

                UnityEngine.Object.DestroyImmediate(instancia);
            }

            // Variacoes dos moradores por semente, paradas.
            var morador = AssetDatabase.LoadAssetAtPath<GameObject>(
                CaminhoPrefab(PapelNpc.Morador)
            );
            for (var semente = 11; morador != null && semente < 17; semente++)
            {
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(morador);
                AparenciaDeNpc.Aplicar(instancia, PapelNpc.Morador, semente);
                var animator = instancia.GetComponentInChildren<Animator>();
                idle.SampleAnimation(animator.gameObject, 0f);
                cam.transform.position = new Vector3(0.9f, 1.3f, 5.4f);
                cam.transform.LookAt(new Vector3(0, 1.0f, 0));
                instancia.transform.rotation = Quaternion.Euler(0, -15, 0);
                cam.Render();
                cam.Render();
                RenderTexture.active = alvo;
                var coluna = celula % colunas;
                var linha = linhas - 1 - celula / colunas;
                folha.ReadPixels(new Rect(0, 0, largura, altura), coluna * largura, linha * altura);
                celula++;
                UnityEngine.Object.DestroyImmediate(instancia);
            }

            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(pasta, "folha-npcs.png"), folha.EncodeToPNG());
            var nomes = new StringBuilder();
            foreach (var prefab in prefabs)
                nomes.Append(prefab.name).Append(' ');
            File.WriteAllText(Path.Combine(pasta, "folha-npcs.txt"), nomes.ToString());
            UnityEngine.Object.DestroyImmediate(luzObj);
            UnityEngine.Object.DestroyImmediate(camObj);
            UnityEngine.Object.DestroyImmediate(alvo);
            UnityEngine.Object.DestroyImmediate(folha);
        }

        // ---------- clipes ----------

        sealed class Clipes
        {
            public AnimationClip Falar;
            public AnimationClip Martelar;
            public AnimationClip Mexer;
        }

        static AnimationClip ObterClipe(string nome)
        {
            foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(CaminhoModelo))
            {
                if (
                    objeto is AnimationClip clipe
                    && !clipe.name.StartsWith("__preview__")
                    && (clipe.name == nome || clipe.name.EndsWith("|" + nome))
                )
                    return clipe;
            }

            throw new InvalidOperationException(
                "Clipe nao encontrado em " + CaminhoModelo + ": " + nome
            );
        }

        static Clipes CriarClipesDeGesto(
            GameObject modelo,
            AnimationClip idle,
            StringBuilder relatorio
        )
        {
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            try
            {
                var raiz = instancia.transform;
                relatorio.AppendLine("Ossos:");
                foreach (var t in instancia.GetComponentsInChildren<Transform>())
                    relatorio.AppendLine(
                        "  "
                            + AnimationUtility.CalculateTransformPath(t, raiz)
                            + " pos="
                            + t.position
                            + " esc="
                            + t.lossyScale
                    );

                var falar = Derivar(
                    idle,
                    "Npc_Falar",
                    raiz,
                    new[]
                    {
                        Gesto("UpperArm.R", GestoDeFalar, -1f, 2f, 0f),
                        Gesto("LowerArm.R", GestoDeFalar * 0.8f, -1f, 3f, 0.25f),
                        Gesto("UpperArm.L", GestoDeFalar * 0.5f, -1f, 2f, 0.5f),
                        Gesto("Head", 8f, 1f, 2f, 0.1f),
                    }
                );
                var martelar = Derivar(
                    idle,
                    "Npc_Martelar",
                    raiz,
                    new[]
                    {
                        Gesto("UpperArm.R", GestoDeMartelar, -1f, 3f, 0f, -45f),
                        Gesto("LowerArm.R", GestoDeMartelar * 0.5f, -1f, 3f, 0.1f, -30f),
                        Gesto("Torso", 8f, -1f, 3f, 0.05f),
                    }
                );
                var mexer = Derivar(
                    idle,
                    "Npc_Mexer",
                    raiz,
                    new[]
                    {
                        Gesto("UpperArm.R", GestoDeMexer, -1f, 2f, 0f, -35f),
                        Gesto("LowerArm.R", GestoDeMexer * 1.2f, -1f, 2f, 0.25f, -30f),
                        Gesto("Torso", 5f, 1f, 2f, 0.1f),
                    }
                );
                return new Clipes
                {
                    Falar = falar,
                    Martelar = martelar,
                    Mexer = mexer,
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instancia);
            }
        }

        sealed class GestoDeOsso
        {
            public string Osso;
            public float Amplitude;
            public float Sentido; // -1: para frente (sobe o braco), 1: para tras
            public float Ciclos; // quantas voltas no clipe
            public float Defasagem; // fracao do ciclo
            public float Deslocamento; // graus fixos somados
        }

        static GestoDeOsso Gesto(
            string osso,
            float amplitude,
            float sentido,
            float ciclos,
            float defasagem,
            float deslocamento = 0f
        ) =>
            new GestoDeOsso
            {
                Osso = osso,
                Amplitude = amplitude,
                Sentido = sentido,
                Ciclos = ciclos,
                Defasagem = defasagem,
                Deslocamento = deslocamento,
            };

        // Copia o Idle e troca so as curvas de rotacao dos ossos do gesto; o resto continua respirando.
        static AnimationClip Derivar(
            AnimationClip origem,
            string nome,
            Transform raiz,
            GestoDeOsso[] gestos
        )
        {
            var caminho = PastaClipes + "/" + nome + ".anim";
            AssetDatabase.DeleteAsset(caminho);
            var clipe = new AnimationClip { name = nome, frameRate = origem.frameRate };
            foreach (var binding in AnimationUtility.GetCurveBindings(origem))
                AnimationUtility.SetEditorCurve(
                    clipe,
                    binding,
                    AnimationUtility.GetEditorCurve(origem, binding)
                );

            var duracao = Mathf.Max(origem.length, 1f);
            foreach (var gesto in gestos)
            {
                var osso = Achar(raiz, gesto.Osso);
                if (osso == null)
                    continue;
                var path = AnimationUtility.CalculateTransformPath(osso, raiz);
                var baseRot = RotacaoBase(origem, path, osso);
                // Eixo de balanco: o X do mundo (direita) visto da pose de referencia do osso.
                var eixo = Quaternion.Inverse(osso.rotation) * Vector3.right;

                var curvas = new AnimationCurve[4];
                for (var i = 0; i < 4; i++)
                    curvas[i] = new AnimationCurve();
                var quadros = Mathf.CeilToInt(duracao * 30f);
                var anterior = baseRot;
                for (var q = 0; q <= quadros; q++)
                {
                    var t = q / (float)quadros;
                    var graus =
                        gesto.Deslocamento
                        + gesto.Sentido
                            * gesto.Amplitude
                            * 0.5f
                            * (
                                1f + Mathf.Sin((t * gesto.Ciclos + gesto.Defasagem) * 2f * Mathf.PI)
                            );
                    var rot = baseRot * Quaternion.AngleAxis(graus, eixo);
                    if (Quaternion.Dot(rot, anterior) < 0f)
                        rot = new Quaternion(-rot.x, -rot.y, -rot.z, -rot.w);
                    anterior = rot;
                    var tempo = t * duracao;
                    curvas[0].AddKey(tempo, rot.x);
                    curvas[1].AddKey(tempo, rot.y);
                    curvas[2].AddKey(tempo, rot.z);
                    curvas[3].AddKey(tempo, rot.w);
                }

                var propriedades = new[]
                {
                    "m_LocalRotation.x",
                    "m_LocalRotation.y",
                    "m_LocalRotation.z",
                    "m_LocalRotation.w",
                };
                for (var i = 0; i < 4; i++)
                    AnimationUtility.SetEditorCurve(
                        clipe,
                        EditorCurveBinding.FloatCurve(path, typeof(Transform), propriedades[i]),
                        curvas[i]
                    );
            }

            var ajustes = AnimationUtility.GetAnimationClipSettings(clipe);
            ajustes.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clipe, ajustes);
            AssetDatabase.CreateAsset(clipe, caminho);
            return clipe;
        }

        static Quaternion RotacaoBase(AnimationClip origem, string path, Transform osso)
        {
            var eixos = new[] { "x", "y", "z", "w" };
            var valores = new float[4];
            for (var i = 0; i < 4; i++)
            {
                var curva = AnimationUtility.GetEditorCurve(
                    origem,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        "m_LocalRotation." + eixos[i]
                    )
                );
                if (curva == null || curva.length == 0)
                    return osso.localRotation;
                valores[i] = curva.Evaluate(0f);
            }

            var q = new Quaternion(valores[0], valores[1], valores[2], valores[3]);
            return q;
        }

        // ---------- controladores ----------

        static void CriarControladores(AnimationClip idle, AnimationClip andar, Clipes clipes)
        {
            CriarControlador(ControladorPadrao, idle, andar, clipes.Falar, null);
            CriarControlador(ControladorMartelar, null, null, clipes.Falar, clipes.Martelar);
            CriarControlador(ControladorMexer, null, null, clipes.Falar, clipes.Mexer);
        }

        static void CriarControlador(
            string caminho,
            AnimationClip parado,
            AnimationClip andar,
            AnimationClip falar,
            AnimationClip trabalho
        )
        {
            AssetDatabase.DeleteAsset(caminho);
            var controlador = AnimatorController.CreateAnimatorControllerAtPath(caminho);
            controlador.AddParameter(ParametrosDeNpc.Andando, AnimatorControllerParameterType.Bool);
            controlador.AddParameter(ParametrosDeNpc.Falando, AnimatorControllerParameterType.Bool);
            var maquina = controlador.layers[0].stateMachine;

            var estadoBase = maquina.AddState(trabalho != null ? "Trabalhar" : "Parado");
            estadoBase.motion = trabalho != null ? trabalho : parado;
            maquina.defaultState = estadoBase;
            var estadoFalar = maquina.AddState("Falar");
            estadoFalar.motion = falar;

            Ligar(estadoBase, estadoFalar, ParametrosDeNpc.Falando, true);
            Ligar(estadoFalar, estadoBase, ParametrosDeNpc.Falando, false);

            if (andar != null)
            {
                var estadoAndar = maquina.AddState("Andar");
                estadoAndar.motion = andar;
                Ligar(estadoBase, estadoAndar, ParametrosDeNpc.Andando, true);
                Ligar(estadoAndar, estadoBase, ParametrosDeNpc.Andando, false);
            }

            EditorUtility.SetDirty(controlador);
        }

        static void Ligar(AnimatorState de, AnimatorState para, string parametro, bool valor)
        {
            var transicao = de.AddTransition(para);
            transicao.hasExitTime = false;
            transicao.duration = TransicaoDoAnimator;
            transicao.AddCondition(
                valor ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0,
                parametro
            );
        }

        // ---------- prefabs ----------

        static void CriarPrefab(
            Receita receita,
            GameObject modelo,
            Texture2D textura,
            StringBuilder relatorio
        )
        {
            var raiz = new GameObject("Npc_" + receita.Papel);
            try
            {
                var corpo = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
                corpo.name = "Modelo";
                corpo.transform.SetParent(raiz.transform, false);

                // O Monk olha para -Z no FBX: gira meio giro para a frente ser +Z (regra do Core).
                corpo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                // Escala para 1,8 m (do chao ao alto da cabeca na pose parada) e pes no chao.
                var parado = ObterClipe("Idle");
                parado.SampleAnimation(corpo, 0f);
                var topo = Achar(corpo.transform, "Head_end").position.y;
                var chao = Mathf.Min(
                    Achar(corpo.transform, "Foot.L").position.y,
                    Achar(corpo.transform, "Foot.R").position.y
                );
                var fator = AlturaDoCorpo / Mathf.Max(topo - chao, 0.01f);
                corpo.transform.localScale = Vector3.one * fator;
                corpo.transform.localPosition = new Vector3(0f, -chao * fator, 0f);
                parado.SampleAnimation(corpo, 0f);
                relatorio.AppendLine(receita.Papel + ": fator " + fator);
                foreach (
                    var nomeOsso in new[]
                    {
                        "Head",
                        "Head_end",
                        "Neck",
                        "Torso",
                        "Hips",
                        "Shoulder.R",
                        "UpperArm.R",
                        "LowerArm.R",
                        "Fist.R",
                        "Fist.L",
                        "Foot.R",
                    }
                )
                    relatorio.AppendLine(
                        "  idle "
                            + nomeOsso
                            + " "
                            + Achar(corpo.transform, nomeOsso).position.ToString("F2")
                    );

                // Um material por papel: a cor da roupa multiplica a textura do Monk.
                var materialBase = CriarMaterial(
                    "Npc_Corpo_" + receita.Papel,
                    receita.Roupa,
                    textura,
                    false
                );
                foreach (var renderer in corpo.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var materiais = renderer.sharedMaterials;
                    for (var i = 0; i < materiais.Length; i++)
                        materiais[i] = materialBase;
                    renderer.sharedMaterials = materiais;
                    renderer.gameObject.name = "Roupa_" + renderer.gameObject.name;
                }

                var animator = corpo.GetComponent<Animator>();
                if (animator == null)
                    animator = corpo.AddComponent<Animator>();
                animator.avatar = ObterAvatar();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
                animator.runtimeAnimatorController =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        receita.Oficio == Oficio.Martelar ? ControladorMartelar
                        : receita.Oficio == Oficio.Mexer ? ControladorMexer
                        : ControladorPadrao
                    );

                var ossos = new Dictionary<string, Transform>();
                foreach (var parte in receita.Partes)
                    AnexarParte(corpo.transform, parte, ossos);

                if (receita.Papel == PapelNpc.Morador)
                    AnexarVariacoes(corpo.transform, ossos);

                var caminho = CaminhoPrefab(receita.Papel);
                AssetDatabase.DeleteAsset(caminho);
                PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        static Avatar ObterAvatar()
        {
            foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(CaminhoModelo))
            {
                if (objeto is Avatar avatar)
                    return avatar;
            }

            return null;
        }

        static Bounds LimitesDasMalhas(GameObject corpo)
        {
            var tem = false;
            var limites = new Bounds();
            foreach (var renderer in corpo.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // Pose de referencia: a malha transformada pelo osso raiz, medida no mundo do editor.
                var b = renderer.bounds;
                if (!tem)
                {
                    limites = b;
                    tem = true;
                }
                else
                    limites.Encapsulate(b);
            }

            return limites;
        }

        static Transform Achar(Transform raiz, string nome)
        {
            if (raiz.name == nome)
                return raiz;
            for (var i = 0; i < raiz.childCount; i++)
            {
                var achado = Achar(raiz.GetChild(i), nome);
                if (achado != null)
                    return achado;
            }

            return null;
        }

        // A forma fica presa ao osso e posta pelo mundo do modelo (o osso pode ter escala e giro).
        static GameObject AnexarParte(
            Transform corpo,
            Parte parte,
            Dictionary<string, Transform> cache
        )
        {
            var osso = Achar(corpo, parte.Osso);
            if (osso == null)
                throw new InvalidOperationException("Osso nao encontrado: " + parte.Osso);
            cache[parte.Osso] = osso;

            var objeto = GameObject.CreatePrimitive(parte.Forma);
            objeto.name = parte.Nome;
            var colisor = objeto.GetComponent<Collider>();
            if (colisor != null)
                UnityEngine.Object.DestroyImmediate(colisor);

            objeto.transform.SetParent(osso, false);
            objeto.transform.position =
                (parte.Osso.StartsWith("Fist") ? osso.position : Vector3.zero) + parte.Posicao;
            objeto.transform.rotation = Quaternion.Euler(parte.Rotacao);

            // Tamanho final em metros: cilindro e capsula tem altura 2 na escala 1.
            var alturaBase =
                parte.Forma == PrimitiveType.Cylinder || parte.Forma == PrimitiveType.Capsule
                    ? 2f
                    : 1f;
            var mundo = new Vector3(parte.Tamanho.x, parte.Tamanho.y / alturaBase, parte.Tamanho.z);
            var pai = osso.lossyScale;
            objeto.transform.localScale = new Vector3(
                mundo.x / Mathf.Max(pai.x, 0.0001f),
                mundo.y / Mathf.Max(pai.y, 0.0001f),
                mundo.z / Mathf.Max(pai.z, 0.0001f)
            );

            var renderer = objeto.GetComponent<Renderer>();
            renderer.sharedMaterial = CriarMaterial(
                "Npc_" + ParaHex(parte.Cor) + (parte.Brilha ? "_Brilho" : ""),
                parte.Cor,
                null,
                parte.Brilha
            );
            return objeto;
        }

        // ---------- variacoes de rosto, cabelo e cor ----------

        public const string PrefixoCabelo = "Cabelo_";
        public const string PrefixoBarba = "Barba_";
        public const string NomeTunica = "Tunica";

        static void AnexarVariacoes(Transform corpo, Dictionary<string, Transform> ossos)
        {
            var cinza = new Color(0.3f, 0.2f, 0.12f);
            var cabeca = Achar(corpo, "Head");
            ossos["Head"] = cabeca;

            // Cabelo 1: curto; 2: longo; 3: coque. Barba 1: curta; 2: longa. Tudo desligado: o sorteio liga.
            var curto = Grupo(corpo, cabeca, PrefixoCabelo + "1");
            Variacao(
                corpo,
                curto,
                "Topo",
                PrimitiveType.Sphere,
                new Vector3(0, 1.7f, -0.08f),
                new Vector3(0.34f, 0.32f, 0.38f),
                cinza
            );
            var longo = Grupo(corpo, cabeca, PrefixoCabelo + "2");
            Variacao(
                corpo,
                longo,
                "Topo",
                PrimitiveType.Sphere,
                new Vector3(0, 1.7f, -0.08f),
                new Vector3(0.34f, 0.32f, 0.38f),
                cinza
            );
            Variacao(
                corpo,
                longo,
                "Costas",
                PrimitiveType.Cube,
                new Vector3(0, 1.55f, -0.2f),
                new Vector3(0.28f, 0.42f, 0.08f),
                cinza
            );
            var coque = Grupo(corpo, cabeca, PrefixoCabelo + "3");
            Variacao(
                corpo,
                coque,
                "Topo",
                PrimitiveType.Sphere,
                new Vector3(0, 1.7f, -0.08f),
                new Vector3(0.34f, 0.32f, 0.38f),
                cinza
            );
            Variacao(
                corpo,
                coque,
                "Coque",
                PrimitiveType.Sphere,
                new Vector3(0, 1.84f, -0.2f),
                new Vector3(0.16f, 0.16f, 0.16f),
                cinza
            );
            var barbaCurta = Grupo(corpo, cabeca, PrefixoBarba + "1");
            Variacao(
                corpo,
                barbaCurta,
                "Queixo",
                PrimitiveType.Sphere,
                new Vector3(0, 1.44f, 0.04f),
                new Vector3(0.24f, 0.14f, 0.16f),
                cinza
            );
            var barbaLonga = Grupo(corpo, cabeca, PrefixoBarba + "2");
            Variacao(
                corpo,
                barbaLonga,
                "Queixo",
                PrimitiveType.Sphere,
                new Vector3(0, 1.44f, 0.04f),
                new Vector3(0.24f, 0.14f, 0.16f),
                cinza
            );
            Variacao(
                corpo,
                barbaLonga,
                "Ponta",
                PrimitiveType.Capsule,
                new Vector3(0, 1.3f, 0.08f),
                new Vector3(0.16f, 0.32f, 0.12f),
                cinza
            );
        }

        static Transform Grupo(Transform corpo, Transform osso, string nome)
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(osso, false);
            objeto.SetActive(false);
            return objeto.transform;
        }

        static void Variacao(
            Transform corpo,
            Transform grupo,
            string nome,
            PrimitiveType forma,
            Vector3 posicao,
            Vector3 tamanho,
            Color cor
        )
        {
            var parte = P(nome, forma, grupo.parent.name, posicao, tamanho, cor);
            var objeto = AnexarParte(corpo, parte, new Dictionary<string, Transform>());
            objeto.transform.SetParent(grupo, true);
        }

        // ---------- materiais e pastas ----------

        static string ParaHex(Color cor) => ColorUtility.ToHtmlStringRGB(cor);

        static Material CriarMaterial(string nome, Color cor, Texture2D textura, bool emissao)
        {
            var caminho = PastaMateriais + "/" + nome + ".mat";
            var existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (existente != null)
                return existente;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("Shader URP Lit nao encontrado.");
            var material = new Material(shader) { name = nome };
            material.SetColor("_BaseColor", cor);
            if (textura != null)
                material.SetTexture("_BaseMap", textura);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.15f);
            if (emissao)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", cor * 2.5f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            AssetDatabase.CreateAsset(material, caminho);
            return material;
        }

        static void CriarPasta(string caminho)
        {
            if (AssetDatabase.IsValidFolder(caminho))
                return;
            var pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
            CriarPasta(pai);
            AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
        }
    }
}
