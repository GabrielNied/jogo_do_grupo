namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Catálogo com as fichas de lore, regiões e fraquezas de combate para o bestiário (#922).
    /// </summary>
    public static class CatalogoBestiario
    {
        public static readonly FichaDeLoreBestiario[] Fichas =
        {
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.JavaliDoCampo,
                regiao: "Campos da Brasa Mansa",
                descricaoGeral: "Javali robusto com presas curtas que pasta nas cercanias da cidade. Defende seu território com agressividade se provocado.",
                sinaisDeAtaque: "Fareja o chão e raspa as patas traseiras antes de disparar uma investida veloz em linha reta.",
                fraquezas: "Errar a investida o deixa atordoado por 2 segundos. Golpes laterais ou pelas costas causam dano aumentado.",
                dropsConhecidos: "Carne de Caça, Couro Cru, Pelo Rústico",
                fraseBestiario: "Um é um problema pequeno. O perigo costuma ser esquecer quantos ouviram o primeiro grunhido."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.LoboDoCampo,
                regiao: "Colinas e Bosques da Brasa Mansa",
                descricaoGeral: "Predador ágil que caça em pequenas matilhas coordenadas, flanqueando alvos desatentos.",
                sinaisDeAtaque: "Rosna baixo e recua um passo antes de um salto ágil em direção à jugular.",
                fraquezas: "Após saltar, demora um breve momento para recuperar o equilíbrio. Dano contundente interrompe o bote.",
                dropsConhecidos: "Pele de Lobo, Presa Afiada, Carne Dura",
                fraseBestiario: "O primeiro lobo prende sua atenção. Os outros escolhem o lado por onde vão atacar."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.TatuRochoso,
                regiao: "Pedreiras e Margens Secas",
                descricaoGeral: "Criatura de carapaça espessa incrustada de minerais que absorve a maioria dos impactos diretos.",
                sinaisDeAtaque: "Encolhe-se em formato de esfera e gira em alta velocidade em direção ao adversário.",
                fraquezas: "Ataques mágicos elementais ou quebras de postura forçam a exposição de seu ventre macio e desprotegido.",
                dropsConhecidos: "Placa Rochosa, Cascalho Fino, Fragmento Mineral",
                fraseBestiario: "Bater na pedra só quebra a sua lâmina. Espere ele se desenrolar antes de gastar o golpe."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.CervoDeGalhadaOca,
                regiao: "Bosque dos Murmúrios",
                descricaoGeral: "Herbívoro nobre e cauteloso de chifres ressonantes. Prefere fugir a combater, mas desfere coices perigosos quando encurralado.",
                sinaisDeAtaque: "Ergue a galhada e bate os cascos ruidosamente antes de golpear com as patas dianteiras.",
                fraquezas: "Movimentos lentos e flechas precisas no flanco impedem que ele dispare em disparada.",
                dropsConhecidos: "Galhada Rara, Couro Fino, Essência Silvestre",
                fraseBestiario: "A floresta sempre avisa. Aprenda a ouvir e escolha com cuidado para onde vai correr."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.MariposaDeBreu,
                regiao: "Mata do Eco e Grutas Úmidas",
                descricaoGeral: "Inseto noturno com asas escuras salpicadas de poeira luminescente que ataca em nuvens.",
                sinaisDeAtaque: "Bate as asas freneticamente espalhando um pó que reduz a visão e causa queimação.",
                fraquezas: "Fogo em área queima suas asas frágeis instantaneamente. Fontes de luz forte desviam sua trajetória.",
                dropsConhecidos: "Seda Escura, Pó Luminoso, Olho Multifacetado",
                fraseBestiario: "A luz que mostra a trilha também desenha um convite cintilante no céu noturno."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.LoboDeGeada,
                regiao: "Serra Gelada",
                descricaoGeral: "Grande lobo adaptado às neves eternas. Seu sopro carrega geada que congela o solo e os passos dos viajantes.",
                sinaisDeAtaque: "Expele vapor gélido pelas ventas e salta com as garras dianteiras abertas.",
                fraquezas: "Ataques ígneos derretem sua camada externa de gelo e desarmam sua postura de ataque.",
                dropsConhecidos: "Pele Gélida, Presa Congelada, Essência de Frio",
                fraseBestiario: "O frio da serra não perdoa hesitações. Se as patas colarem na neve, o salto já foi desferido."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.CarapacaDeBrasa,
                regiao: "Caldeira Mineral",
                descricaoGeral: "Besouro de grande porte com placas térmicas incandescentes que emana calor intenso.",
                sinaisDeAtaque: "Aquecimento súbito do casco seguido por um jorro de vapor e choque de patas dianteiras.",
                fraquezas: "Água ou geada causam choque térmico, abrindo fissuras na armadura e expondo pontos vitais.",
                dropsConhecidos: "Placa Vulcânica, Núcleo de Calor, Cinza Concentrada",
                fraseBestiario: "Não tente vencer a fornalha pelo calor. Resfrie a casca antes de bater com o martelo."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.JavaliFerrugem,
                regiao: "Vale das Fogueiras",
                descricaoGeral: "Variante feroz do javali coberta por escamas de óxido e presas afiadas como lâminas serradas.",
                sinaisDeAtaque: "Gira em círculo bufando fumaça e dispara uma investida destrutiva com cabeçadas sequenciais.",
                fraquezas: "Desviar no último instante faz o javali colidir contra obstáculos e ficar exposto a golpes críticos.",
                dropsConhecidos: "Presa Oxidada, Couro Espesso, Fragmento de Metal Bruto",
                fraseBestiario: "A terra seca guarda marcas profundas de onde as manadas do vale passaram."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.ArqueiroDoCampo,
                regiao: "Acampamentos Renegados dos Campos",
                descricaoGeral: "Bandido mascarado com arco curto que atira à distância enquanto mantém distância segura.",
                sinaisDeAtaque: "Puxa a corda do arco mantendo mira por 1.5 segundo antes de disparar uma flecha perfurante.",
                fraquezas: "Combate corpo a corpo desestabiliza sua mira e o força a tentar fugir sem conseguir atacar.",
                dropsConhecidos: "Flechas Rústicas, Corda de Arco, Moedas Gastas",
                fraseBestiario: "Mantenha o escudo erguido até fechar a distância. O arqueiro só é perigoso longe."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.GuardiaoDoReflexo,
                regiao: "Mina do Reflexo (Dungeon)",
                descricaoGeral: "Construção ancestral de espelhos e cristal que protege o coração subterrâneo da mina.",
                sinaisDeAtaque: "Canaliza feixes luminosos pelos prismas e desfere golpes de impacto que ricocheteiam no piso.",
                fraquezas: "Interromper a canalização quebrando os prismas periféricos atordoa o guardião e cancela o raio.",
                dropsConhecidos: "Cristal do Reflexo, Núcleo Prismático, Essência Ancestral",
                fraseBestiario: "Quem olha para o espelho do guardião enxerga o próprio fim antes do golpe cair."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.PresaVelha,
                regiao: "Covil das Colinas (Chefe Raro)",
                descricaoGeral: "Lobo colossal lendário marcado por incontáveis batalhas. Seus uivos convocam alcateias inteiras.",
                sinaisDeAtaque: "Uivo atordoante em cone seguido de uma investida devastadora que derruba defensores.",
                fraquezas: "Foco concentrado de dano e quebra de postura durante o uivo impedem a chegada de reforços.",
                dropsConhecidos: "Manto da Presa-Velha, Garra Antiga, Troféu Alfa",
                fraseBestiario: "A montanha inteira silencia quando a Presa-Velha ergue a cabeça contra o vento."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.AnciaoTrovejante,
                regiao: "Planície das Tormentas (Chefe de Mundo)",
                descricaoGeral: "Titã colossal que vagueia pelas planícies trazendo tempestades elétricas por onde pisa.",
                sinaisDeAtaque: "Pisar sísmico eletrificado e tempestade de relâmpagos em círculo ao redor do corpo.",
                fraquezas: "Golpes coordenados nos cascos traseiros desestabilizam o colosso e forçam sua queda temporária.",
                dropsConhecidos: "Centelha Primordial, Fragmento Trovejante, Relíquia do Titã",
                fraseBestiario: "O chão não treme pelo vento. Quando o Ancião desperta, o próprio horizonte muda de lugar."
            ),
            new FichaDeLoreBestiario(
                criaturaId: CatalogoCriaturas.ColossoBasaltico,
                regiao: "Pico Incandescente (Chefe de Mundo)",
                descricaoGeral: "Monólito vulcânico vivo composto de magma resfriado e basalto impenetrável.",
                sinaisDeAtaque: "Erupção de lava em cone frontal e arremesso de rochas incandescentes a longa distância.",
                fraquezas: "Resfriamento das juntas basálticas através de dano de gelo concentrado reduz drasticamente sua defesa.",
                dropsConhecidos: "Coração de Basalto, Magma Solidificado, Rédeas do Basalto Vivente",
                fraseBestiario: "A montanha que caminha não pode ser contida com muros. Apenas o frio profundo desacelera seus passos."
            ),
        };

        public static FichaDeLoreBestiario Obter(int criaturaId)
        {
            for (int i = 0; i < Fichas.Length; i++)
            {
                if (Fichas[i].CriaturaId == criaturaId)
                {
                    return Fichas[i];
                }
            }
            return null;
        }
    }
}
