# Itens

**Responsável:**
**Status:** em definição · **prioridade alta de implementação** (2026-10-12: modelo de itens v2, épico [#619](https://github.com/GabrielNied/jogo_do_grupo/issues/619))
**Faixa de IDs de mensagem:** 7000–7999

## Resumo

Inventário, equipamentos, tier calculado pelos atributos e fusão determinística até +10.

## Definido

- ~~Equipamentos e raridades~~ (substituído em 2026-10-12): equipamentos com **tier D a S+ calculado pelos atributos** ([Modelo de itens v2](#modelo-de-itens-v2-documento-mestre-2026-10-12))
- **Os itens carregam as skills, sorteadas aleatoriamente** (D3 da [validação de 2026-10-10](../brainstorm/validacao.md)), e a combinação de itens equipados define o estilo de jogo (D2)
- Estilo looter: muito loot é parte do loop principal (D5)
- Os itens equipados definem o papel no grupo: tank, healer ou DPS (veja [combate.md](combate.md))
- ~~Sistema de fusão: limite de fusões por raridade, item sacrificado é destruído, só funde itens do mesmo tipo ou tier, herança parcial de atributos, retorno decrescente, custo crescente em ouro (tira moeda do jogo), teto de atributo por tier~~ (substituído em 2026-10-11 pela decisão abaixo)
- ~~**Fusão/infusão** (decidido por Gabriel em 2026-10-11, a validar pelo grupo): só doadores do mesmo tipo **e** raridade, +2% fixo do valor-base por doador, sem retorno decrescente, **com custo em coroas**. O item sacrificado é destruído.~~ Substituído em 2026-10-12 pela fusão v2 (abaixo); a fusão v2 entrou na [#616](https://github.com/GabrielNied/jogo_do_grupo/issues/616) ([Fusão](#fusão)) e a infusão saiu do código.
- **Modelo de itens v2** (decidido por Gabriel em 2026-10-12, a partir do [documento mestre](../brainstorm/documento-mestre.md); **prioridade alta**): tier pelos atributos, fusão que escolhe um atributo e anda metade da diferença, sem falha, até **+10**, custos em coroas e materiais de dungeon e raid, sem desmontagem. Regras e invariantes em [Modelo de itens v2](#modelo-de-itens-v2-documento-mestre-2026-10-12).
- Itens negociáveis, inclusive os fundidos (fomentar a economia)
- **Inventário de jogo, não de formulário** (decidido por Gabriel em 2026-10-12): bolsa em grade de casas **1×1** para todos os tipos, paperdoll, tooltip, clique direito e arrastar e soltar, com a base pronta para inventário grande. Veja [Interface do inventário](#interface-do-inventário-560).

## Em aberto

- [x] ~~Cinco raridades confirmadas para #166: comum, incomum, raro, épico e lendário.~~ Substituído em 2026-10-12: tier D a S+ calculado pelos atributos. Grau continua um eixo separado (veja Equipamentos abaixo).
- [x] ~~Quantas skills um item pode ter?~~ Decidido em 2026-10-14: a **arma** sorteia **3 ativas** no exemplar a partir do pool do **modelo-base** ([skills](skills.md#definido), SKILL-004); outras peças não somam ativa na barra (poderes #629). Catálogo amplo: [#340](https://github.com/GabrielNied/jogo_do_grupo/issues/340) / [#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167).
- [x] ~~Infusão usa o atributo marcado e +2% do valor-base (#174).~~ Substituído em 2026-10-12 pela fusão v2 (#616).
- [x] ~~Custo inicial configurável por raridade na #174.~~ Substituído em 2026-10-12: custo por nível de fusão (#616).
- [x] ~~Ajustes à tabela de raridades~~: sem objeto desde 2026-10-12 (a raridade saiu).
- [ ] Pendências do modelo v2: veja [Pendente](#pendente).

## Regras

### Modelo de itens v2 (documento mestre, 2026-10-12)

Decidido por Gabriel em 2026-10-12 a partir do [documento mestre](../brainstorm/documento-mestre.md) (seções 7 a 9 e 18). **Prioridade alta de implementação**, épico [#619](https://github.com/GabrielNied/jogo_do_grupo/issues/619): tier [#615](https://github.com/GabrielNied/jogo_do_grupo/issues/615), fusão [#616](https://github.com/GabrielNied/jogo_do_grupo/issues/616), materiais [#617](https://github.com/GabrielNied/jogo_do_grupo/issues/617) e a decisão [#618](https://github.com/GabrielNied/jogo_do_grupo/issues/618). O **tier** (#615), a **fusão** (#616) e os [atributos próprios dos equipamentos](#entregue-na-623-atributos-próprios-dos-equipamentos) (#623) já estão no jogo; os materiais de dungeon e raid (#617) ainda não, então a fusão jogável vai até +5.

Status usados aqui: **Definido** (aprovado), **Proposta** (valor inicial em constante, que quem implementa pode calibrar e registrar aqui), **Pendente** (precisa de decisão do Gabriel) e **Depreciado** (substituído, mantido para histórico). "Ouro" no documento mestre são as **coroas** do jogo.

#### Invariantes

Não mudam por decisão de quem implementa: mudar exige aprovação do Gabriel e atualização desta seção (exceção às [Decisões de design](../agentes.md#decisões-de-design)).

- **DEC-001:** atributos de equipamento não concedem pontos de árvore nem são convertidos nos atributos do personagem; a interação é só pelas fórmulas de combate e pelas skills. **Definido** por Gabriel em 2026-10-12 na especificação de atributos ([personagem](personagem.md#atributos-do-personagem), [#618](https://github.com/GabrielNied/jogo_do_grupo/issues/618)): trocar de equipamento não muda a [árvore](talentos.md), e os pontos da árvore vêm só de subir de nível. O item também usa os nomes Força, Vigor, Agilidade, Intelecto e Espírito, mas como atributos dele, com conversão própria para o combate ([Atributos dos equipamentos](#atributos-dos-equipamentos)). Implementado na [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623) ([Entregue na #623](#entregue-na-623-atributos-próprios-dos-equipamentos)).
- **DEC-002:** a fusão não tem chance de falha. O equilíbrio vem de custo, materiais e limites.
- **DEC-003:** uma fusão altera um único atributo, escolhido pelo jogador; o resto do item principal, inclusive as skills e os poderes, fica igual.
- **DEC-004:** o tier atual vem só dos valores dos atributos e é recalculado quando eles mudam.
- **DEC-005:** sem desmontagem. Destinos do item: equipado, vendido, consumido como material de fusão ou descartado (sem recompensa nem material; incluído por Gabriel em 2026-10-12).
- A fórmula da fusão (metade da diferença), o limite **+10**, a estrutura de custo (coroas; dungeon até +8; raid em +9 e +10), a operação atômica e o servidor como autoridade.

#### Exemplar

| Conceito (documento mestre) | Hoje (`Equipamento`) | Modelo v2 |
|---|---|---|
| Identidade da instância, item-base | `InstanciaId`, `ModeloId` | Iguais |
| Tipo e família de arma | `Tipo` | **Categoria de peça** = `Tipo` (Espada, Peito, Capacete, Anel...); "família de arma" do documento mestre vale para todas as peças |
| Atributos próprios | `Atributos` (fixos e sorteados, fracionários) | Inteiros numa **escala interna de 0 a 100** por atributo ([Tier](#tier)); quais atributos cada peça pode ter vem do modelo-base. Lista e conversão em [Atributos dos equipamentos](#atributos-dos-equipamentos) |
| Skills | ainda não existem ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167)) | Quantidade definida pelo **modelo-base**, não por raridade; gravadas no exemplar; a fusão nunca as muda |
| Nível de fusão | `InfusoesUsadas` (limite 10 a 30 pela raridade) | `NivelFusao`, de +0 a +10 (feito na #616; `InfusoesUsadas` ficou só como leitura do legado) |
| Tier atual | não existe | Calculado pelos atributos ([Tier](#tier)), `RegrasTier.TierDe` (feito na #615) |
| Tier de origem | `Raridade` (sorteada) | `TierOrigem`, gravado na geração (feito na #615); `Raridade` fica só como legado |
| Posse e equipamento | posição, contêiner, aba e casa | Iguais |
| Grau, região, família de conjunto, requisito de nível, favorito | existem | Continuam |
| Atributo infusível (`IndiceInfusivel`, `ValorBaseInfusivel`) | existem | **Removidos na #616**: qualquer atributo elegível nos dois itens pode ser escolhido |
| Nível da peça (`Nivel`) | existe, sem XP ainda | **Fixo em 1** até existir progressão própria; não controla tier, qualidade nem skills |

- **ITEM-001:** o servidor gera e grava o exemplar uma vez; vestir, tirar e reconectar não sorteiam de novo (já vale desde a #166).
- **ITEM-002:** destinos permitidos: equipar, vender ([#173](https://github.com/GabrielNied/jogo_do_grupo/issues/173)), consumir como material de fusão e descartar ([Descartar item](#descartar-item-495)). Qualquer destino novo exige decisão explícita.
- **ITEM-004:** o descarte apaga o item sem recompensa e sem gerar material. Item com investimento (nível de fusão acima de +0) ou de tier A ou acima pede uma **segunda confirmação** (feito na [#616](https://github.com/GabrielNied/jogo_do_grupo/issues/616); regra em `RegrasFusao.DescarteExigeSegundaConfirmacao`, pergunta no client).
- **ITEM-003:** material favorito ou equipado não é consumido (vem da #174).

#### Atributos dos equipamentos

Decidido por Gabriel em 2026-10-12 (opção B da [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623), que implementa). Fonte única da lista de atributos de item e de como eles chegam ao combate. Os atributos do personagem estão em [personagem](personagem.md#atributos-do-personagem), e a pipeline que junta as fontes é do [Combate](combate.md) ([#624](https://github.com/GabrielNied/jogo_do_grupo/issues/624)).

**Definido:**

- **ATRIB-001 (lista, v3 — Gabriel 2026-10-13, [#709](https://github.com/GabrielNied/jogo_do_grupo/issues/709)):** um item **não** usa os nomes dos cinco atributos do personagem. Stats de combate do exemplar:
  - **dano físico**, **dano mágico**, **vida** (vida máxima);
  - **dano da arma**, físico ou mágico, só em arma;
  - **defesa física** e **defesa mágica**;
  - regeneração de vida, stamina e mana (não stamina nem mana máximas);
  - os demais [secundários](personagem.md#atributos-secundários).

  Cada um é inteiro de 0 a 100 (TIER-006). O modelo-base declara quais tem (TIER-007). A versão 3 dos exemplares (`RegrasTier.VersaoAtual`) migra Força→dano físico, Intelecto→dano mágico, Vigor→vida, Agilidade→regeneração de stamina, Espírito→regeneração de mana (`MigracaoDeAtributosV3`).
- **ATRIB-002 (do item, não do personagem):** DEC-001 inalterado: nada do item soma nos cinco do personagem nem na árvore.
- **ATRIB-003 (conversão linear por categoria):** `combate = no0 + (no100 - no0) × V / 100` em `ConfigAtributosItem`. Números de teste até a [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627).
- **ATRIB-004 (arma e dano flat):** dano-base = dano da arma + dano físico ou mágico flat das peças (conforme o tipo da arma). A Força ou o Intelecto **do personagem** aumentam em %; estrutura em [combate](combate.md#dano-com-arma-e-atributos-do-equipamento-623).
- **ATRIB-005 (na tela):** tooltip com nome de combate (ex.: "Dano físico: +12"). Janela do personagem separa linhas do equipamento (FR-010 de [talentos](talentos.md#requisitos)).
- **ATRIB-006:** tier e fusão usam o valor de 0 a 100, nunca o convertido. Os poderes e os bônus de set não são atributos e não entram no tier (PODER-005).

**Para onde vai cada atributo** (proposta; quem implementa a #623 e a #624 pode ajustar e registrar aqui):

| Atributo do item | Vira no combate | Forma |
|---|---|---|
| Dano da arma | Parte do dano-base do golpe (físico ou mágico) | Valor fixo |
| Dano físico / mágico | Soma flat ao dano-base (tipo da arma) | Valor fixo |
| Vida | Vida máxima | Valor fixo |
| Defesa física / mágica | Defesa física / mágica | Valor fixo |
| Regenerações | Regeneração contínua | Por segundo |
| Secundários | O próprio secundário | Unidade definida pelo Combate |

**Elegíveis por categoria (v3):** principal da família = **vida** (Vigia/Bastião), **dano físico** (Predador/Ruptor), **dano mágico** (Aurora/Tempestade) ou **regeneração de stamina** (Batedor/Olho Longo). Variante B: defesa mágica (Bastião), regeneração de stamina (Ruptor/Olho Longo) ou regeneração de mana (Tempestade). Matriz em `ConfigAtributosItem`.

**Migração:** versão 2 (#623) e versão 3 (#709) em `MigracaoDeTier` / `MigracaoDeAtributosV3`. Histórico da v2 em [Entregue na #623](#entregue-na-623-atributos-próprios-dos-equipamentos).

#### Entregue na #623: atributos próprios dos equipamentos

Opção B do DEC-001: o equipamento tem atributos próprios e eles entram nas fórmulas de combate como a fonte "equipamento". O personagem continua com os cinco dele (base + pontos + efeitos).

- **Lista e elegíveis:** `StatusItem` ganhou Agilidade, dano da arma, defesa física e mágica, as três regenerações, chance e dano crítico, velocidade de ataque física e mágica e penetração física e mágica. Os ordinais antigos não mudam (o exemplar guarda o número); Vigor passou a ser o atributo (antes era o recurso). A matriz por categoria é a tabela acima, em `ConfigAtributosItem.Elegiveis`. Atributo inelegível que sobrou num exemplar migrado fica no item sem efeito.
- **Conversão (ATRIB-003):** `ConfigAtributosItem` guarda o valor no 0 e no 100 de cada par (atributo, categoria), com padrão por atributo e exceções por categoria; valor fora de 0 a 100 é limitado. Padrões (valores de teste até a #627): dano da arma 10 a 30 (martelo 14 a 44, arco 8 a 28); Força e Intelecto 0 a +10% (espada, arco e cajado +20%, martelo +25%); Vigor 0 a +150 de vida (peito 250); Agilidade e Espírito 0 a +30 de stamina e mana; defesa física e mágica 0 a 40 (peito 60, capacete físico 50); regenerações vida 0 a 2, stamina 0 a 5, mana 0 a 3 por segundo; chance crítica 0 a 10% (anel 15%); dano crítico 0 a +50% (brinco 75%); velocidade de ataque 0 a 20%; penetração 0 a 15%.
- **Quem lê:** `EquipamentosParaCombate` (Itens) lê as peças ativas numa transação e serve as duas portas. `RegrasAtributosDeItem.Resumir` soma só peças ativas (o conjunto de armas ativo e as de fora da bolsa), o dano da arma só vem da arma principal do conjunto, e os secundários vão pela pipeline com a origem sendo o nome da peça (ordem 30).
- **Arma e dano (ATRIB-004):** o dano-base é o da arma, ou 20 sem arma (`ParcelasDeDano.DanoSemArma`). A Força (arma física) ou o Intelecto (cajado) do personagem soma `PercentualDeDanoPorPonto` (10%) por ponto, e o % do item é outra parcela: `dano = base × (1 + %personagem + %item) × multiplicador do golpe`. O servidor lê as parcelas na mesma ida ao banco do início do ataque.
- **Recursos:** vida, stamina e mana máximas somam o Vigor, a Agilidade e o Espírito dos itens já convertidos. A regeneração de stamina do equipamento entra pelo secundário, e a previsão local do client (`ProntidaoLocal`) lê esse secundário em vez da base fixa quando consulta os recursos.
- **Defesas:** chegam ao contrato e à janela, mas ainda não reduzem o dano recebido (fórmula final na #627).
- **Migração (versão 1 e 0 para 2):** Destreza → Agilidade; Vitalidade e Vida → Vigor; Vigor antigo (recurso) → Agilidade; Energia → Espírito; Defesa → defesa física; RecuperacaoVigor → regeneração de stamina; Força, Intelecto, Espírito, Potência de fogo e gelo e Alcance ficam. Valores que caem no mesmo destino somam, arredondam e são limitados à faixa. `TierOrigem` é preservado para exemplares já na escala 0 a 100 e calculado na hora para os da escala antiga (TIER-004). Roda ao ler (`MigracaoDeTier`) e o `MigradorDeEquipamentos` regrava na subida; sem migração do EF.
- **Catálogo:** os modelos de armadura declaram o principal da família, a especialização da variante B (sem repetir) e o núcleo da categoria; valor 0 e `Fixo` ("veio do modelo"). Ids 173 a 177 são armas de teste (espada, arco, cajado, escudo e foco "Ressonante"), só pelo botão de teste: o loot ainda não solta arma.
- **Gerador:** os extras saem de `StatusAleatorios` (padrão: Força, Vigor, Agilidade, Intelecto, Espírito, defesas, regenerações, crítico, velocidades e penetrações), só os elegíveis que o modelo ainda não tem; se houver menos candidatos que `QuantidadeAleatorios`, sai o que houver.
- **Na tela:** o tooltip mostra o valor e o efeito convertido; o resumo do inventário e a janela do personagem listam as linhas do equipamento em seção própria ("Do equipamento, não soma nos atributos").

#### Tier

Implementada na [#615](https://github.com/GabrielNied/jogo_do_grupo/issues/615) ([Entregue na #615](#entregue-na-615-tier-calculado-pelos-atributos)).

**Definido:**

- **TIER-001:** `D < C < B < A < S`, mais a condição `S+`.
- **TIER-002:** o tier depende só dos valores dos atributos; não depende de raridade, etiqueta do drop, nível de fusão nem skills.
- **TIER-003:** é recalculado sempre que um valor muda (fusão, migração).
- **TIER-004:** o tier de origem é gravado na geração, nunca muda e não limita a evolução: um item D pode chegar a S por fusões.
- **TIER-005:** S+ é um item S com todos os atributos elegíveis no máximo da faixa válida. É condição verificada no estado do item, nunca um tier sorteado.

**Cálculo** (decidido por Gabriel em 2026-10-12; os números são valores iniciais de balanceamento, em `ConfigTier`):

- **TIER-006 (escala e faixa):** cada atributo de item é um inteiro numa escala interna de **0 a 100**, com mínimo e máximo **absolutos por atributo e categoria de peça**. 100 é o máximo interno, não "100% de crítico" nem "100 de vida": a conversão para o combate é outra regra, de cada atributo. O grau não muda a faixa; proposta: ele só escolhe em que parte da faixa a geração sorteia.
- **TIER-007 (elegíveis):** cada modelo-base declara quais atributos são elegíveis para geração, tier e S+ (ex.: peito com defesa física e Vigor; anel com chance crítica e penetração; lista em [Atributos dos equipamentos](#atributos-dos-equipamentos)). Atributo que o modelo não tem **não conta como zero**: simplesmente não entra.
- **TIER-008 (qualidade):** `Qi = (Vi - Vmin) / (Vmax - Vmin) × 100` para cada atributo elegível; a qualidade do item é a média. Exemplo: 90%, 85% e 65% dão 80%, tier S.

| Tier | Qualidade média |
|---|---|
| D | abaixo de 35% |
| C | de 35% a menos de 50% |
| B | de 50% a menos de 65% |
| A | de 65% a menos de 80% |
| S | de 80% a 100% |
| S+ | S com **todos** os elegíveis exatamente no máximo (média de 100% por arredondamento não basta) |

- **Loot:** a origem do inimigo deixa de pesar a raridade e passa a melhorar o sorteio dos valores (fortalecido = melhor de 2 rolagens por atributo, chefe raro 3, chefe 4), calibrado pela meta de ritmo de [Loot dos inimigos](#loot-dos-inimigos-493).
- **Migração:** os valores antigos (escala do catálogo, com fator de grau) viram a escala 0 a 100 pela regra de [Entregue na #615](#entregue-na-615-tier-calculado-pelos-atributos).

##### Entregue na #615 (tier calculado pelos atributos)

Decisões de quem implementou (propostas, ajustáveis em `ConfigTier` e `ConfigLoot`; as invariantes TIER-001 a TIER-008 não mudaram).

- **Genérico sobre `StatusItem`:** `RegrasTier` (cálculo, S+, sorteio do valor) não cita nenhum atributo. Tudo o que depende de qual atributo é vem de `ConfigTier.Faixas` (`TabelaDeFaixas`): faixa padrão por `StatusItem`, faixa própria por categoria de peça e atributo inelegível numa categoria. **Elegível = ter faixa para a categoria.** Desde a [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623) a matriz de [elegíveis por categoria](#atributos-dos-equipamentos) (`ConfigAtributosItem.Elegiveis`) define as faixas, todas de 0 a 100; os atributos legados (Destreza, Vitalidade, Vida, Energia, Defesa, RecuperacaoVigor, Potência de fogo e gelo, Alcance) não têm faixa. A [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627) rebalanceia os números.
- **API para a fusão (#616):** `ConfigTier.Maximo(tipo, status)` (o teto de FUSAO-015, `null` se o atributo não é elegível), `ConfigTier.Faixa(tipo, status)`, `ConfigTier.Elegiveis(tipo)` e `RegrasTier.Calcular(equipamento)` (tier, qualidade média e quantos atributos entraram) para recalcular depois de mudar um valor.
- **Tier atual é calculado, não gravado:** `RegrasTier.TierDe(equipamento)` olha só `Atributos` (TIER-002 e TIER-003). Só entram os atributos do exemplar que são elegíveis na categoria dele; o que o modelo não tem não conta como zero (TIER-007) e um atributo legado sem faixa (PotenciaFogo, Vida...) é ignorado. Exemplar sem nenhum atributo elegível é tier D. S+ é verificado no estado: S com todos os elegíveis em `valor >= máximo` (um atributo a 1 do máximo é S).
- **Tier de origem:** `Equipamento.TierOrigem` é gravado na geração e na migração e nunca mais muda. `Equipamento.VersaoDosAtributos` marca a escala: 0 é a escala antiga, 1 é a de 0 a 100 com `TierOrigem` válido, e 2 (`RegrasTier.VersaoAtual`) é a dos atributos próprios da #623.
- **Geração (`GeradorEquipamentos`):** os atributos do modelo-base do catálogo mais `QuantidadeAleatorios` (2) extras sorteados entre `StatusAleatorios` (os cinco básicos, só os elegíveis na categoria e que o modelo ainda não tem). Cada atributo recebe um **inteiro dentro da faixa**. O **grau** escolhe a janela da faixa em que se sorteia (`ConfigTier`: Inicial 0 a 60%, Médio 20 a 80%, Forte 40 a 100% da amplitude); a **origem do inimigo** dá rolagens ao atributo e vale a melhor (`ConfigLoot`: comum 1, fortalecido 2, chefe raro 3, chefe 4). Os valores fixos do catálogo (+6, defesas por categoria) não são mais valores: o catálogo só diz **quais** atributos o modelo tem (`Fixo` agora quer dizer "veio do modelo"). S+ não sai do sorteio na prática (exigiria todos os atributos exatamente no máximo): vem da evolução por fusão.
- **Migração dos exemplares existentes:** `novo = round(valor_antigo × 100 / referência)`, limitado à faixa, com a referência de `ConfigTier.ReferenciaLegada` (Força, Destreza, Intelecto e Vitalidade 30; Espírito 15; Vigor 50; Defesa 190; atributo sem faixa: 100, ou seja, o valor fica como estava). A referência é 1,25 vez o que o grau Forte dava no catálogo, então um valor Inicial vira ~20, Médio ~40 e Forte ~80 (o grau decide onde a peça antiga cai, como decide a janela da geração). `TierOrigem` = tier calculado no momento. **Sem mudança de esquema:** o exemplar é um JSON na coluna `EquipamentoJson`, então não há migração do EF; a conversão (`MigracaoDeTier`) roda ao **ler** o exemplar (determinística, então vale na hora, mesmo antes de gravar) e o `MigradorDeEquipamentos` grava os antigos de volta na subida do servidor, o que fixa o `TierOrigem` antes de qualquer ajuste de limiar.
- **Combate (desde a #623):** o bônus das peças não soma mais nos atributos do personagem. `RegrasEquipamentos.Somar` e a conversão provisória `ParaEscalaLegada` saíram; a conversão linear por atributo e categoria (ATRIB-003) está em `ConfigAtributosItem`.
- **`RaridadeItem` é legado:** `Equipamento.Raridade` continua no exemplar só como histórico (a fusão da #616 também não a lê); peças novas nascem `Comum` e nada decide por ela. Saíram `LootNoChao.Raridade`, os pesos de raridade e a promoção por sorteio.
- **Pontuação do loot** (`TabelaDeLoot.Pontuacao`, só para dizer qual peça é "melhor"): soma dos valores × (1 + `BonusDePontuacaoPorTier` × degrau do tier atual), com 10% por degrau de D a S+. Antes o degrau era o da raridade.
- **Na tela:** bolsa, paperdoll, tooltip e avisos de loot seguem [Visual de tier](#visual-de-tier) (UI implementada; aura no mundo ainda não).

##### Visual de tier

Decidido por Gabriel em 2026-10-13 ([#670](https://github.com/GabrielNied/jogo_do_grupo/issues/670)). A leitura visual reforça a ordem **S+ > S > A > B > C > D** sem mudar o cálculo (TIER-001 a TIER-008). São **duas camadas**: cores Lumora na interface e aura/shader no mundo 3D quando existir mesh da peça.

**Interface (implementado):** a casa da bolsa e do paperdoll mostram a **letra do tier** no canto e a cor dele na borda e no ícone, via tokens Lumora em `EstiloItens.uss` (D cinza, C verde, B turquesa, A rosa, S ouro, S+ ouro claro com borda grossa). O tooltip traz "Tier A" (e "Tier B (origem D)" quando o tier atual difere do de origem), o nome na cor do tier e cada atributo como `valor/máximo` com o efeito convertido ("Força 72/100 · +14,4% de dano físico"). O aviso diz "Você pegou Elmo do Vigia (tier B)."; a **cor** no aviso depende do HUD do Core (os avisos só têm as cores de informação, aviso e erro e não aceitam texto colorido).

**Mundo 3D (pendente):** brilho ao redor da peça no chão e, depois, no personagem equipado — no estilo de drops raros em MMORPGs clássicos (outline, glow ou rim light pulsante em URP). **Complementa** a letra na UI; não a substitui. A paleta da aura **pode divergir** das cores da bolsa, desde que a ordem B < A < S < S+ seja óbvia à distância.

| Tier | Aura no mundo | Intensidade (referência) |
|---|---|---|
| D | nenhuma | — |
| C | nenhuma | — |
| B | azul | entrada no efeito “raro” |
| A | roxo | mais forte que B |
| S | laranja | mais forte que A |
| S+ | vermelho escuro | máximo; leitura de peça perfeita |

**Estado atual no client:** ainda não há modelo 3D por equipamento; o loot no chão usa um marcador genérico com cor única (`ControleLoot`, [#369](https://github.com/GabrielNied/jogo_do_grupo/issues/369)). A aura entra quando houver visual de item no mundo (encaixe e pose no avatar: [#664](https://github.com/GabrielNied/jogo_do_grupo/issues/664) e arte). Cores e intensidade da aura ficam em constantes ou config de balanceamento visual no client, não no shared.

#### Fusão

Implementada na [#616](https://github.com/GabrielNied/jogo_do_grupo/issues/616); substituiu a infusão da #174. Prévia, confirmação e atomicidade seguem o [ADR 0032](../decisoes/0032-infusao-e-cobranca-atomicas.md).

**Definido:**

- **FUSAO-001:** o item principal é o resultado; o material é consumido.
- **FUSAO-002:** o material é compatível: mesma **categoria de peça** (`TipoEquipamento`), em armas, armaduras e acessórios (espada com espada, peito com peito, anel com anel). Raridade legada, grau, região e família de conjunto (Vigia, Predador...) não restringem. Se uma categoria tiver modelos com estruturas de atributos diferentes, a compatibilidade também pode exigir o mesmo modelo-base ou grupo de atributos, declarado nos dados.
- **FUSAO-003:** o jogador escolhe um atributo por operação, elegível nos dois itens; a fusão não acrescenta atributo novo. A regra é a mesma em todas as categorias; muda só o catálogo de atributos, as faixas e os efeitos.
- **FUSAO-004:** só o atributo escolhido muda. Os outros atributos, as skills, o tipo, o grau e o tier de origem ficam iguais.
- **FUSAO-005:** `novo = atual + (material - atual) / 2`. O valor anda metade da diferença em direção ao do material e pode diminuir.
- **FUSAO-006:** sem chance de falha.
- **FUSAO-007:** cada fusão sobe 1 nível, de +0 a **+10**. Item em +10 é recusado.
- **FUSAO-008:** o tier atual é recalculado; o de origem é preservado.
- **FUSAO-009 (custo por nível):**

| Evolução | Coroas | Material especial |
|---|---:|---|
| +0 → +1 | 100 | nenhum |
| +1 → +2 | 200 | nenhum |
| +2 → +3 | 350 | nenhum |
| +3 → +4 | 550 | nenhum |
| +4 → +5 | 800 | nenhum |
| +5 → +6 | 1.200 | 1 de dungeon |
| +6 → +7 | 1.700 | 2 de dungeon |
| +7 → +8 | 2.400 | 3 de dungeon |
| +8 → +9 | 3.400 | 1 de raid |
| +9 → +10 | 4.800 | 2 de raid |

  Valores iniciais de teste de Gabriel (2026-10-12), em `ConfigFusao`, não balanceamento final: 15.500 coroas de +0 a +10, além da peça compatível consumida em cada fusão. Material de raid nunca é exigido antes de +8 → +9. Os materiais são a [#617](https://github.com/GabrielNied/jogo_do_grupo/issues/617); até ela, o servidor recusa de +5 → +6 em diante com `MateriaisInsuficientes` (a prévia ainda mostra o que seria exigido).
- **FUSAO-010 (prévia):** principal, material, compatibilidade, nível atual e resultante, atributo escolhido, valor atual, do material e resultante, variação, custo em coroas, materiais exigidos, aviso de que o material some e tier antes e depois. A prévia não muda nada.
- **FUSAO-011:** só a confirmação explícita aplica. O servidor confere tudo de novo e grava cobrança, consumo, novo valor, nível e tier na mesma transação: tudo ou nada.
- **FUSAO-012:** recusa material alheio, inexistente, incompatível, equipado ou favorito; item em +10; coroas ou materiais insuficientes. Nunca cobra sem aplicar, aplica sem consumir, duplica item ou moeda em pedidos concorrentes, nem usa o mesmo material em duas fusões.
- **FUSAO-013:** confirmação repetida ou após queda de rede não aplica duas vezes; reabrir a bancada mostra o resultado gravado.
- **FUSAO-014 (arredondamento, decidido por Gabriel em 2026-10-12):** o resultado da fórmula vai para o **inteiro mais próximo**; empate exato (,5) vai **na direção do material** (o mesmo que `atual + sinal(d) × teto(|d| / 2)`, com `d = material - atual`). Quando os valores diferem, anda pelo menos 1 e nunca passa do valor do material. Se um dia houver atributo decimal, a regra se adapta à precisão dele.

| Principal | Material | Resultado |
|---:|---:|---:|
| 20 | 40 | 30 |
| 40 | 20 | 30 |
| 20 | 25 | 23 (22,5 vai para o material) |
| 20 | 21 | 21 |
| 21 | 20 | 20 |
| 20 | 20 | 20 |

- **FUSAO-015 (teto, decidido por Gabriel em 2026-10-12):** material de grau maior pode levar o atributo até o **máximo absoluto** do atributo naquela categoria de peça (TIER-006). Nem o grau do principal nem o tier atual são teto.

- **Migração (feita na #616):** `InfusoesUsadas` vira `NivelFusao` (no máximo 10); valores fracionários são arredondados; os textos "infusão" passam a "fusão". Detalhes em [Entregue na #616](#entregue-na-616-fusão-v2).

##### Entregue na #616 (fusão v2)

Decisões de quem implementou (propostas, ajustáveis em `ConfigFusao`; FUSAO-001 a FUSAO-015 não mudaram).

- **Regra pura e genérica (`RegrasFusao`, shared):** nenhuma regra cita um atributo. **Elegível = o atributo existe nos dois itens e tem faixa na categoria** (`ConfigTier.Faixa`); a [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623) só troca a lista da tabela. `Resultado` é `atual + sinal(d) × teto(|d| / 2)` sobre os valores arredondados, limitado à faixa (o máximo absoluto é o teto, FUSAO-015); a tabela de FUSAO-014 e as bordas estão nos testes. Compatível = mesma `TipoEquipamento` (FUSAO-002); o "mesmo modelo-base ou grupo de atributos" fica para quando uma categoria tiver estruturas diferentes (hoje nenhuma tem).
- **Custos em `ConfigFusao` (shared), por nível atual:** coroas 100, 200, 350, 550, 800, 1.200, 1.700, 2.400, 3.400, 4.800 (total 15.500); materiais de dungeon 1, 2 e 3 em +5→+6 a +7→+8 e de raid 1 e 2 em +8→+9 e +9→+10. Mudar um valor é mudar só esse arquivo.
- **Sem materiais especiais ainda:** a #617 ainda não implementou os saldos; o jogador nunca os tem. A prévia de +5→+6 em diante funciona e traz `MateriaisSuficientes = false` com o que seria exigido; a confirmação é recusada com `MateriaisInsuficientes` **antes** de cobrar. Quando a #617 entrar, credita e consome na mesma transação da fusão (veja [Materiais de fusão](#materiais-de-fusão-617)).
- **Rotas:** `/api/personagens/{personagemId}/equipamentos/fusao`, sempre POST: `/estado`, `/previa` e `/confirmar` (`PedidoFusao`: principal, material, atributo e, na confirmação, a assinatura), `/favorito` (`PedidoFavorito`) e `/coroas-teste` (só Development, 20.000 coroas, o bastante para testar os cinco níveis jogáveis). As respostas são `EquipamentosResponse` (saldo, `ModoFusao`, `Previa` opcional, aviso). Mesmo contrato de coroas da infusão: `ICarteiraCoroas`, em transação serializável (ADR 0032). A prévia assinada (HMAC) cobre o personagem, os dois exemplares como gravados, as posições, o atributo e o custo; mudar qualquer um pede outra prévia.
- **Erros:** material igual ao principal é 400 `MesmoItem`; personagem, principal ou material alheio, inexistente ou já consumido é 404; 409 para `MaterialIncompativel`, `MaterialEquipado`, `ItemFavorito` (do material), `NivelMaximo`, `AtributoInelegivel`, `MateriaisInsuficientes`, `CoroasInsuficientes`, `PreviaDesatualizada` e `ItemInvalido`. O principal pode estar equipado ou favorito. A prévia mostra o resultado mesmo sem saldo ou sem materiais (a confirmação fica desabilitada no client e é recusada no servidor).
- **Confirmação repetida ou concorrente:** a segunda recebe 404 (o material já foi consumido) e nada é cobrado de novo; dois principais disputando o mesmo material: um vale, o outro recebe 404. Falha ao gravar desfaz cobrança, consumo e valor (transação única).
- **Exemplar:** `NivelFusao` (0 a 10) substitui `InfusoesUsadas`, `IndiceInfusivel` e `ValorBaseInfusivel` (os dois últimos removidos). Tier de origem, grau, região, favorito e os outros atributos não mudam; o tier atual é sempre calculado.
- **Migração sem mudança de esquema** (o exemplar é JSON em `EquipamentoJson`, então não há migração do EF, como na #615): `MigracaoDeTier` converte ao **ler** `InfusoesUsadas` em `NivelFusao` (no máximo 10) e arredonda os valores fracionários da infusão para o inteiro mais próximo; o `MigradorDeEquipamentos` regrava, na subida, todo exemplar sem o campo `NivelFusao`. O tier de origem fica como estava.
- **Descarte (ITEM-004):** peça com `NivelFusao` acima de +0 ou tier atual A ou acima: o **Confirmar descarte** leva a uma segunda pergunta ("Descartar mesmo assim") que diz o motivo. A pergunta é do client; o servidor não muda (o descarte continua um destino aprovado e sem corpo extra).
- **Na tela:** I → **Bancada** abre a bancada de fusão (`PainelFusao`) na janela do inventário: peça principal (equipada ou da bolsa), material (da bolsa, mesma categoria, sem favorito), atributo (os elegíveis, com "valor → valor do material"). **Ver prévia** mostra o que FUSAO-010 pede; **Confirmar fusão e consumir material** só fica habilitado com saldo e materiais; trocar qualquer escolha apaga a prévia. Sem escolha possível, um texto diz por quê (sem material compatível, sem atributo em comum, peça em +10). O tooltip e a descrição mostram "Fusão +N/10" e o tier atual com a origem. Os motivos de recusa do servidor têm texto em português (`TextosEquipamento.MensagemDeErro`).
- **Fora desta entrega:** materiais de dungeon e raid ([#617](https://github.com/GabrielNied/jogo_do_grupo/issues/617)), destacar os compatíveis na bolsa ([#561](https://github.com/GabrielNied/jogo_do_grupo/issues/561)) e skills ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167); a fusão não toca nelas).

#### Materiais de fusão (#617)

Decidido em 2026-10-13 (`/decida`): representação e fluxo; quantidades continuam as de FUSAO-009 em `ConfigFusao` (**proposta** até calibrar).

- **MAT-001 (saldo, não item na bolsa):** dois contadores inteiros **por personagem** — material de **dungeon** e material de **raid** — persistidos como a carteira de coroas (`ICarteiraCoroas`, ADR 0032): mesma transação serializável da fusão, sem slot de inventário e sem empilhamento 1×1.
- **MAT-002 (IDs estáveis):** em `ConfigFusao`, `IdMaterialDungeon = "essencia-instancia"` e `IdMaterialRaid = "essencia-incursao"`. Nomes na UI (**proposta**): "Essência de instância" e "Essência de incursão".
- **MAT-003 (fontes):** o chefe da dungeon ([#66](https://github.com/GabrielNied/jogo_do_grupo/issues/66)) credita material de dungeon a cada participante (mesma regra de participação do loot da fatia). Material de raid só quando existir conteúdo de raid; até lá, rota **Development** espelhando `/coroas-teste` (ex.: crédito de teste na API de fusão) para validar +9 e +10.
- **MAT-004 (consumo):** na confirmação da fusão, debita coroas e o material exigido pelo nível atual (FUSAO-009); falta de qualquer um recusa com `MateriaisInsuficientes` ou `CoroasInsuficientes` sem alterar exemplar nem saldo parcial.
- **Implementação sugerida (`/trabalhe`):** contrato `ISaldosMateriaisFusao` em `Contratos/Itens/` (ou junto da carteira se Economia for dono do persist), tabela simples por personagem, exposição no `EquipamentosResponse` e na bancada (`PainelFusao`). Reaproveitar o padrão de `CarteiraCoroas`; não criar item empilhável na bolsa (evita conflito com DEC-005 e com a bolsa 1×1).

#### Poderes e sets

Decidido por Gabriel em 2026-10-12 ([especificação de poderes e sets](../brainstorm/poderes-e-sets.md)). Implementação: poderes [#629](https://github.com/GabrielNied/jogo_do_grupo/issues/629) e sets [#630](https://github.com/GabrielNied/jogo_do_grupo/issues/630), as duas bloqueadas pelas skills nas armas ([#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167), [#580](https://github.com/GabrielNied/jogo_do_grupo/issues/580)).

**Princípio:** os equipamentos modificam as skills e o estilo de combate, não só os números. A **arma** define as skills-base; as **outras peças** fortalecem, modificam ou conectam essas skills; os **poderes lendários** criam mecânicas de build; os **sets** dão sinergias pela combinação de peças; a **árvore** especializa o personagem pelas categorias e propriedades das skills ([talentos](talentos.md)). Duas builds com a mesma arma devem jogar de jeitos bem diferentes por causa do equipamento.

**Definido:**

- **PODER-001:** um equipamento lendário pode ter um poder que modifica o comportamento de uma ou mais skills da arma.
- **PODER-002:** categorias de poder: amplificação, transformação, gatilho, preparação e consumo, conversão, recarga e recursos, mobilidade, defesa reativa e sinergia de grupo. Preferir poderes que mudem decisões de combate a bônus só numéricos.
- **PODER-003:** o poder declara de forma explícita o que afeta. Não vale "afeta tudo da mesma arma ou categoria".
- **PODER-004:** a definição do poder traz: skills, categorias ou propriedades compatíveis; condição de ativação; efeito; alvos; duração e limites; interação com outros poderes; regra de acumulação; condição de encerramento; restrições.
- **PODER-005:** poderes e bônus de set **não entram no tier** (DEC-004); a fusão preserva os poderes do item principal (DEC-003).
- **PODER-006 (decidido em 2026-10-12):** o poder é **sorteado** de um catálogo compatível. Cada modelo-base declara quais categorias de poder pode receber; ao obter o item, o servidor sorteia o poder entre as opções válidas para aquele modelo e grava no exemplar, onde fica fixo. Modelo pode ter poder exclusivo quando isso fizer parte da identidade dele.
- **PODER-007 (decidido em 2026-10-12):** só recebe poder lendário o modelo que declara categorias de poder; nem todo equipamento tem poder. O item sem poder continua com atributos numéricos e pode fazer parte de um set. Cada item tem no máximo um poder. Os bônus de set são independentes dos poderes individuais.
- **SET-001:** set é um grupo de peças próprias com bônus pela quantidade **equipada**. Marcos: **2 peças**, bônus inicial que favorece uma mecânica ou estilo; **4 peças**, especialização que amplia ou transforma essa mecânica; **6 peças** só em sets que justifiquem, com efeito avançado que define uma combinação. A lista de sets, peças e bônus é definida set a set, na criação do conteúdo.
- **SET-002:** cada peça mantém seus atributos e poderes; o bônus de set independe do tier; misturar peças de sets diferentes é livre; nenhum set é obrigatório nem superior para qualquer build.
- **SET-003:** os bônus são recalculados quando o equipamento muda.
- **Técnico:** definição de poder e de set separada do exemplar, em dados; condições e efeitos explícitos, sem regra espalhada pelo código; o servidor valida e aplica (autoridade); sem ciclo infinito de ativação; acumulação, duração e remoção controladas; efeitos atualizados ao equipar e desequipar. Sem arquitetura de plugins, scripts ou eventos complexos antes de a necessidade aparecer.

**Foco de cada peça** (diretriz de design, não restrição: um poder fora do foco vale se a mecânica fizer sentido):

| Peça | Foco | Exemplos do que pode fazer |
|---|---|---|
| Capacete | especialização de skills | alcance, duração, número de alvos, interação com estados do inimigo |
| Peito | transformação e combos | ligar skills, uma prepara e outra consome, distribuição do dano |
| Luvas | ofensiva e sequências | combos, golpes seguidos ou carregados, críticos |
| Calça | sobrevivência e sustentação | mitigação, escudos, contra-ataque, luta longa |
| Botas | mobilidade e posicionamento | esquiva, investida, efeito ao atravessar inimigos |
| Adorno (o "amuleto" da especificação, decidido em 2026-10-12: mesmo encaixe, sem categoria nova; segue as regras gerais dos acessórios) | modificadores amplos de build | categorias e elementos, geração e consumo de recurso |
| Anel | gatilhos e condições | inimigo marcado ou debilitado, recarga sob condição |
| Cinto | recursos, sustentação e sobrevivência | vida, stamina, mana, regeneração, efeitos ligados ao uso de skills |
| Brinco | especialização e efeitos secundários | chance crítica, velocidade de ataque, penetração, modificadores condicionais |

**Exemplo** (ilustrativo, não é regra de balanceamento): arma com Investida Trovejante, Corte Relâmpago e Explosão Elétrica; o peito faz a investida marcar, as luvas fazem golpes seguidos fortalecerem a próxima skill, as botas fazem a esquiva fortalecer a investida e o anel faz acerto em marcado reduzir uma recarga. O jogador prepara, encadeia e fecha com a área.

**Lendário:** não é raridade sorteada (a raridade saiu); é o item cujo modelo-base declara categorias de poder (PODER-006 e PODER-007). ~~As famílias do catálogo são os primeiros sets~~ (substituído em 2026-10-12: os sets são definidos set a set no conteúdo, SET-001).

#### Pendente

Precisa de decisão do Gabriel. Até lá, vale a proposta indicada.

- ~~**DEC-001 x fatia:** [#618](https://github.com/GabrielNied/jogo_do_grupo/issues/618)~~ Decidido em 2026-10-12: DEC-001 vale (veja Invariantes).
- ~~**Grau e faixa**~~ decidido em 2026-10-12: até o máximo absoluto (FUSAO-015).
- ~~**Skills por item**~~ decidido em 2026-10-12: a quantidade vem do modelo-base. Armas têm seus conjuntos de skills; armaduras e acessórios modificam essas skills com poderes próprios; item especial pode ter exceção declarada nos dados. Sorteio e catálogo seguem na [#167](https://github.com/GabrielNied/jogo_do_grupo/issues/167) ([skills](skills.md)).
- ~~**Nível da peça e XP da peça**~~ decidido em 2026-10-12: fixo em 1 até existir progressão própria; não é regra central.
- ~~**Descarte**~~ decidido em 2026-10-12: destino válido (ITEM-002 e ITEM-004).
- ~~**Atributos de cada categoria de peça** e a conversão de cada um para o combate~~ decidido em 2026-10-12: [Atributos dos equipamentos](#atributos-dos-equipamentos). Os números da conversão seguem na [#627](https://github.com/GabrielNied/jogo_do_grupo/issues/627); a implementação é a [#623](https://github.com/GabrielNied/jogo_do_grupo/issues/623).
- ~~**Poderes e sets:** poder fixo ou sorteado, foco do Cinto e do Brinco, marcos dos sets, Adorno como amuleto, quem tem poder~~ decididos em 2026-10-12 (PODER-006, PODER-007, SET-001, tabela de foco). O conteúdo de cada set vem na criação dos sets ([#630](https://github.com/GabrielNied/jogo_do_grupo/issues/630)).
- **Materiais:** representação e fontes decididas em [Materiais de fusão](#materiais-de-fusão-617); quantidades e nomes de exibição seguem **proposta** em `ConfigFusao`. A raid ainda não tem issue de conteúdo.
- **Venda, negociação e vinculação**, e fontes de coroas ([#173](https://github.com/GabrielNied/jogo_do_grupo/issues/173)).

#### Depreciado em 2026-10-12

| Sai | Fica no lugar | Onde |
|---|---|---|
| Raridade sorteada e pesos por origem do inimigo | Tier calculado; a origem melhora os valores | #615 |
| Cor da raridade na UI, ordenar e filtrar por raridade | Cor, ordem e filtro por tier | #615, #496, #562, #565 |
| Infusão de +2%, atributo infusível, limite e custo por raridade | Fusão v2 | #616 |
| Doador do mesmo tipo e raridade | Material do mesmo tipo | #616, #561 |
| Promoção de raridade na forja (proposta do board) | Nada: o tier sobe pelos atributos | #331 fechada |
| Fusão de skills (proposta do board) | Nada: as skills do principal são preservadas | DEC-003 |
| Tabela de raridades do board (skills, nível máximo, infusões) | Skills pelo modelo-base; nível da peça fixo em 1 | #167 |

### Infusão de equipamentos (#174)

**Removida na #616.** A infusão de +2% (doador do mesmo tipo e raridade, atributo infusível, limite e custo por raridade) foi substituída pela [fusão v2](#fusão); o que ficou do código é só a leitura do legado (`InfusoesUsadas`, convertido em `NivelFusao` na migração). A carteira de coroas e o padrão de prévia assinada e transação única seguem valendo ([ADR 0032](../decisoes/0032-infusao-e-cobranca-atomicas.md)); o crédito de teste do Development vive em `fusao/coroas-teste`. Valores e rotas em [Entregue na #616](#entregue-na-616-fusão-v2).

### Equipamentos (#166, confirmação do usuário em 2026-10-11)

Modelo completo do livro: **11 posições** (capacete, peito, luvas, calça, botas, cinto, dois anéis, dois brincos e adorno), mais **dois conjuntos de armas**, cada um com mão principal e secundária. Só o conjunto ativo concede atributos. Armaduras e acessórios dos dois lados somam; misturar famílias é livre.

Cada exemplar tem identidade própria, modelo, região, tipo, tier de origem (a raridade é legado desde a #615), grau, nível da peça, requisito de nível do personagem, atributos (os do modelo e os sorteados) e nível de fusão. O servidor sorteia e grava uma vez; vestir, tirar, trocar conjunto e reconectar não sorteiam novamente. O nível da peça não é o requisito de nível do personagem.

As cinco raridades e a tabela do livro (níveis máximos e slots de skills; as infusões viraram o nível de fusão na #616) têm representação em `RegrasEquipamentos`. Graus inicial/médio/forte usam os fatores 1/2/4 já escritos. A estrutura confirmada é a da seção Anatomia do equipamento do [livro](../brainstorm/figjam.md). Skills utilizáveis e XP da peça são as tarefas #167 e #331; a infusão virou a fusão da #616; esta entrega não executa essas operações.

**Geração para teste:** `ConfigEquipamentos` mantém a quantidade e o conjunto de atributos extras fora do sorteio (desde a #615 os valores saem da faixa, veja [Entregue na #615](#entregue-na-615-tier-calculado-pelos-atributos)). O padrão provisório usa dois atributos extras distintos, sem repetir os do modelo, dentre os cinco atributos básicos. Os atributos do modelo vêm do catálogo e os extras só saem dos elegíveis na categoria ([Atributos dos equipamentos](#atributos-dos-equipamentos)). Não é uma tabela final de balanceamento, de chances ou de atributos elementais. É configurável em `Itens:Equipamentos`. O catálogo confirmado da #330 contém as 72 peças do livro, além do Martelo Ressonante e dos dois equipamentos legados de teste; veja Catálogo abaixo. Espada e escudo de teste continuam compatíveis, sem inventar um bônus fixo para eles.

**Limpeza de teste (#662):** em build DEBUG, o rodapé do inventário oferece “Limpar inventário”. A confirmação apaga definitivamente todos os exemplares da bolsa e dos encaixes, preserva o conjunto ativo e publica o visual vazio para a área. O servidor só expõe essa ação em ambiente Development, com `Desenvolvimento:PermitirComandos` ligado e, se configurada, a conta incluída em `Desenvolvimento:Contas` ([Desenvolvimento](desenvolvimento.md)).

**Troca autoritativa e persistente:** o servidor confere a conta dona, a identidade do exemplar, o tipo da posição e o nível exigido. A peça anterior volta ao inventário. Uma arma de duas mãos retira a mão secundária do mesmo conjunto; equipar escudo/foco com ela é recusado. Se todas as peças retiradas não couberem na bolsa, nada muda (a primeira peça deslocada ocupa a casa que o item novo deixou; as outras, a primeira casa livre). Peças equipadas não ocupam espaço do inventário. Transação e índice único por personagem/posição impedem duplicação em pedidos concorrentes.

**Bônus:** Combate consulta o que as peças ativas dão através de `Contratos/Itens/IAtributosDosEquipamentos` (dano da arma, % de Força e Intelecto, vida, stamina e mana máximas, defesas e as linhas para a tela, já convertidos) e da pipeline, como `IFonteDeModificadores` com `Fonte = Equipamento` (os nove secundários). Nada disso soma nos cinco atributos do personagem (DEC-001). Detalhes em [Entregue na #623](#entregue-na-623-atributos-próprios-dos-equipamentos).

**Aviso de mudança (#547):** Itens publica `Contratos/Itens/EquipamentoMudou(PersonagemId)` depois de gravar, ao equipar, desequipar (inclusive a troca numa casa ocupada), trocar o conjunto de armas, fundir uma peça vestida e limpar o inventário de teste. O evento não carrega valores: quem guarda atributos finais invalida a entrada e relê por `IAtributosDosEquipamentos`. Mover ou descartar item da bolsa não publica.

**No jogo:** abrir o inventário com **I** ([Interface do inventário](#interface-do-inventário-560)). O paperdoll mostra os encaixes e o conjunto ativo; o tooltip de cada item mostra o tier, os atributos com valor e máximo, o nível de fusão, nível e requisito; clique direito e arrastar e soltar equipam e desequipam; os botões **Conj. 1** e **Conj. 2** trocam o conjunto de armas. Em editor/build de desenvolvimento, **Obter equipamento de teste** pede uma peça ao servidor. Essa rota só existe em ambiente `Development`; não altera o baú original; o loot dos mobs tem tabela própria ([Loot dos inimigos](#loot-dos-inimigos-493)).

API autenticada, sob `/api/personagens/{personagemId}/equipamentos`:

| Método/sufixo | Intenção |
|---|---|
| GET | Consultar inventário, peças vestidas, conjunto ativo e totais |
| POST `/equipar` | `PedidoEquipar`: identidade do exemplar e posição |
| POST `/desequipar` | `PedidoDesequipar`: posição e, opcionalmente (`TemDestino`), a aba e a casa onde guardar; sem destino vai para a primeira casa livre |
| POST `/conjunto` | `PedidoConjunto`: conjunto 1 ou 2 |
| POST `/bau-teste` | Uma peça do catálogo de equipamentos de teste, só Development |

Operações devolvem `EquipamentosResponse`; conta alheia e personagem ausente têm o mesmo 404. Erros de posição/tipo/conjunto são 400; nível, incompatibilidade de duas mãos e inventário cheio são 409. A migração preserva os itens existentes; legados sem metadados recebem um único sorteio no primeiro equipamento bem-sucedido.

### Descartar item (#495)

Soltar um item da bolsa fora da janela do inventário pede confirmação (**Confirmar descarte** / **Cancelar**) e só a confirmação envia o pedido ([Interface do inventário](#interface-do-inventário-560); antes da #560 era o botão **Descartar** na linha do item). O item some e o contador x/capacidade cai. Desde 2026-10-12 o descarte é destino aprovado (ITEM-002); item com investimento (fusão acima de +0) ou de tier A ou acima pede uma segunda confirmação (ITEM-004, feito na #616).

`POST /api/personagens/{personagemId}/inventario/descartar` (`RotasItens.Descartar`), corpo `PedidoDescartar` (`InstanciaId`); responde 200 `EquipamentosResponse` com o estado atualizado.

- **Destruição definitiva:** o exemplar é apagado; não vira loot no chão nem vai para recompra (decisão assumida em 2026-10-12; venda é a #173).
- **Erros:** personagem ausente ou de outra conta é 404 `PersonagemNaoEncontrado`; exemplar inexistente, de outra conta ou já descartado é 404 `ItemNaoEncontrado`; exemplar equipado é 409 `ItemEquipado` (desequipar antes). Sem token é 401.
- **Idempotência:** repetir o descarte de um exemplar já descartado devolve 404 `ItemNaoEncontrado` e não altera nada (decisão assumida: não dá para distinguir "já descartado" de "alheio" sem vazar a existência de itens de outras contas).
- **Concorrência:** a rota roda em transação Serializable, como equipar. Dois descartes do mesmo exemplar: um vale (200), o outro recebe 404. Descartar junto com equipar: um dos dois vence em série (equipou antes: descarte 409; descartou antes: equipar 404); nunca há peça duplicada nem equipada e apagada.

### Interface do inventário (#560)

Decidido por Gabriel em 2026-10-12. Substitui a lista com dropdown e botões das abas "Itens" e "Equipamentos".

**Por que 1×1:** inventários com tamanho por tipo (Diablo II, Path of Exile) freiam o acúmulo pela falta de espaço. Aqui o centro do jogo é juntar material da mesma categoria para fundir, e cada peça é única (não empilha). Tamanho por tipo puniria justamente esse comportamento e dificultaria ordenar, filtrar e mover em lote. A bolsa guarda os itens valiosos do jogador; o conforto vem das ferramentas (abaixo), não da escassez de espaço.

**Janela (tecla I):** uma janela só. À esquerda o **paperdoll**: as posições de `PosicaoEquipamento` em volta da silhueta, as armas dos dois conjuntos embaixo e o botão de trocar conjunto. À direita a **bolsa**: grade de 8 colunas, faixa de abas no topo quando houver mais de uma aba (com 1 aba fica oculta, #666) e cabeçalho com contador x/capacidade e tooltip "Itens que não estão equipados". Embaixo, o resumo dos bônus. A bancada de fusão é aberta pelo botão **Bancada**; vira arrastar numa tarefa própria.

**Casa:** ícone de silhueta por `TipoEquipamento`, tingido e com borda na cor do tier, mais a letra do tier no canto (sem ícone ainda, a sigla do tipo). Item recém-chegado (baú, loot) tem selo **novo** até o mouse passar por cima. Encaixe vazio mostra a silhueta apagada do tipo.

| Ação | Na bolsa | No paperdoll |
|---|---|---|
| Mouse em cima (~0,3 s) | Tooltip completo e comparação com a peça equipada no mesmo encaixe (▲ verde / ▼ vermelho) | Tooltip completo |
| Clique direito | Equipa no encaixe automático | Desequipa para a primeira casa livre |
| Arrastar até um encaixe | Equipa; encaixes válidos acendem, inválidos ficam vermelhos | Troca entre os pares (anel 1 ↔ 2, brinco 1 ↔ 2) |
| Arrastar até uma casa | Move; casa ocupada troca os dois | Desequipa para aquela casa; se ocupada, troca quando o item da casa serve no encaixe, senão recusa |
| Soltar fora da janela | Confirmação de descarte ([Descartar item](#descartar-item-495)) | Nada (peça equipada não é descartada; desequipe antes) |
| Esc durante o arraste | Cancela | Cancela |

**Encaixe automático** (`RegrasEquipamentos`, no shared): anel e brinco vão para o primeiro encaixe livre do par (ambos ocupados: substitui o 1); arma vai para a arma do conjunto ativo; escudo e foco, para a secundária do conjunto ativo. As demais validações (nível, duas mãos, espaço) continuam no servidor.

**Esperando o servidor:** o item fica semitransparente e travado. Recusa devolve o item ao lugar e mostra a mensagem no HUD; sucesso aplica o estado devolvido pelo servidor.

**Modelo preparado para inventário grande:**

- Cada item fora do corpo tem **contêiner, aba e casa** (equipado: nulos). Por ora só existe o contêiner **Bolsa**, com 1 aba de 40 casas; o banco (#177) e o baú de grupo usam o mesmo modelo e a mesma grade.
- A **capacidade vem do servidor** (abas e casas por aba, em configuração), não fica fixa no client nem espalhada nas regras. Expandir a bolsa é liberar abas (#564). Com 1 aba, a faixa de abas fica oculta (#666); com mais de uma aba, exibe **Bolsa** na primeira e **Aba N** nas seguintes.
- A **ordem é salva no servidor**: mover para uma casa ocupada troca os dois na mesma transação, com índice único por personagem, contêiner, aba e casa. Tudo que entra na bolsa (baú, loot, desequipar, peça deslocada) vai para a primeira casa livre. A migração numera os itens existentes pela ordem atual.

**Entregue na #560 (decisões do dono, 2026-10-12):**

- **Contrato:** `EntradaInventario` ganha `Conteiner` (`ConteinerItem`: Bolsa; Banco e BauDoGrupo reservados, o servidor só aceita Bolsa), `Aba` e `Casa` (`-1` com o item equipado). `CapacidadeBolsa` (`Abas`, `CasasPorAba`, `Total`, `Contem`) vem em `EquipamentosResponse`, `InventarioResponse` e `AbrirBauResponse`; o servidor a decide em `Bolsa.CapacidadeDe` (hoje 1 aba x 40 casas, constantes em `RegrasItens`). O client desenha o que receber.
- **Mover:** `POST /api/personagens/{id}/inventario/mover` (`RotasItens.Mover`), corpo `PedidoMover` (`InstanciaId`, `Aba`, `Casa`), responde 200 `EquipamentosResponse`. Casa ocupada: os dois trocam na mesma transação Serializable. Aba ou casa fora da capacidade: 400 `CasaInvalida`; exemplar inexistente ou alheio: 404 `ItemNaoEncontrado`; equipado: 409 `ItemEquipado`. Mover para a própria casa é um 200 sem efeito.
- **Desequipar para casa ocupada** é o mesmo que equipar o ocupante naquele encaixe: troca se ele servir (tipo, nível, duas mãos, espaço), senão a recusa do equipar (400 `PosicaoInvalida`, 409 `NivelInsuficiente`, `ArmaDeDuasMaos`, `InventarioCheio`) e nada muda.
- **Migração:** a migração só cria as colunas e o índice único (`Personagem, Conteiner, Aba, Casa`, filtrado em casa não nula). Linhas antigas sem casa são numeradas pelo servidor na primeira leitura ou operação, pela ordem do `Id` e na primeira casa livre (`Bolsa.Normalizar`), e a numeração é gravada na primeira operação que grava. Evita SQL à mão na migração e cobre também linhas criadas por outro caminho.
- **Encaixe automático** com os dois encaixes do par ocupados substitui o 1. Equipar já vestido em outro encaixe do mesmo tipo (arrastar anel 1 para o encaixe do anel 2) reequipa.
- **Esc durante o arraste:** o Esc é do Core e fecha a janela inteira, o que também cancela o arraste (o item não se move). Soltar sobre a janela fora de casas e encaixes cancela sem fechar.
- **Selo "novo":** o client compara os itens com os da abertura anterior (mesmo personagem, na sessão) e com os ganhos do baú; o selo sai quando o mouse passa por cima. Na primeira abertura da sessão nada é "novo".
- **Tooltip:** a comparação só vale para peça da bolsa, contra o encaixe automático (com os dois encaixes de anel ou brinco cheios, contra o 1). O tooltip do item sob o mouse mostra as diferenças (▲ ganho e ▼ perda, com sinal e valor no texto, então a cor não é a única pista) e, ao lado, um segundo tooltip com a peça equipada, encabeçado por "Equipado"; encaixe vazio mostra tudo como ganho e não tem segundo tooltip. Aparece depois de 0,3 s e some ao arrastar. Os dois andam juntos como um bloco que cabe na tela e não cobre o painel de bônus (testa direita/esquerda e embaixo/em cima).
- **Motivo da recusa antes do clique (#496, 2026-10-13):** a regra de recusa de equipar é uma só, `RegrasEquipar.Recusa` (shared): encaixe que não aceita o tipo (`PosicaoInvalida`, 400), nível do personagem abaixo do exigido (`NivelInsuficiente`, 409), mão secundária com arma de duas mãos na principal (`ArmaDeDuasMaos`, 409); item que não é equipamento é `ItemNaoEquipavel` (400) e conjunto fora de 1 e 2 é `ConjuntoInvalido` (400). O servidor a usa ao equipar e o client a usa antes do clique, então a tela não promete o que o servidor recusa. Para isso `EquipamentosResponse.NivelDoPersonagem` (0 = não informado, a tela não recusa por nível) vem do servidor. No tooltip de peça da bolsa, uma linha vermelha diz "Requer nível N" ou "Retire a arma de duas mãos para equipar aqui" (a do encaixe que o clique direito usaria); ao arrastar, só os encaixes que aceitam a peça agora acendem verdes e os outros, vermelhos. Os códigos moram em `CodigosErroItens` (servidor e client usam as constantes).
- **Doadores na bolsa (#561, 2026-10-13):** passar o mouse num equipamento (da bolsa ou vestido) acende na bolsa os **doadores compatíveis** com borda dourada e o tooltip ganha "N doadores na bolsa" (ou "Nenhum doador na bolsa"). Doador compatível = o que a [fusão](#fusão) aceita como material daquela peça (`RegrasFusao.Doadores`, que usa `Validar`): outra peça da **bolsa** do **mesmo tipo** (`TipoEquipamento`; não há mais raridade), sem favorito, e a peça sob o mouse ainda fora de +10. Peça vestida nunca é doadora. O destaque só acende a aba aberta (o número do tooltip conta todas) e não acende durante um arraste.
- **Ordenar (#562, 2026-10-13):** o botão **Ordenar** do rodapé pede ao servidor (`POST .../inventario/ordenar`, `PedidoOrdenar { Aba }`, resposta `EquipamentosResponse`) para ordenar a aba aberta. Ordem: equipamentos primeiro, por **tipo** (a ordem de `TipoEquipamento`, que agrupa os doadores de cada peça), **tier** do maior para o menor (S+ ... D) e **nível** do maior para o menor; depois os outros itens por modelo; empate pela identidade (estável, ordenar de novo não muda nada). As peças ocupam as casas da aba a partir da primeira, sem buracos. **Favoritos ficam onde estão** (o campo `Favorito` do equipamento, da fusão): as outras peças se arrumam nas casas que sobram. Peça vestida não entra. Aba fora da capacidade: 400 `CasaInvalida`. Transação Serializable como o mover, então ordenações e movimentos simultâneos nunca deixam duas peças numa casa.
- **Favoritar, marcar como lixo e ações em lote (#563, 2026-10-14):**
  - **Trava / Favorito:** o campo `Favorito` no exemplar protege o item contra qualquer descarte (`Descartar` e `DescartarLote`, respondendo 409 `ItemFavorito`), impede que seja consumido como material de fusão (`RegrasFusao.Validar`) e fixa sua posição na ordenação da aba. No client, exibe selo dourado de favorito (★); atalho **Alt+clique** no slot alterna a proteção imediatamente.
  - **Marcar como Lixo:** o campo `Lixo` no exemplar (`Equipamento`) permite marcar peças para limpeza rápida. Rota `POST .../inventario/lixo` (`PedidoLixo { InstanciaId, Lixo, InstanciaIds }`). Itens favoritos não podem ser marcados como lixo (409 `ItemFavorito`). Marcar como favorito desmarca o lixo automaticamente. No client, exibe selo vermelho LIXO e atenuação visual.
  - **Seleção Múltipla:** **Ctrl+clique** nos slots seleciona e deseleciona múltiplos itens na bolsa. Com itens selecionados, o rodapé exibe botões contextuais: **Travar (N)**, **Marcar lixo (N)**, **Descartar (N)** (com diálogo de confirmação) e **Limpar seleção**.
  - **Descarte em Lote:** rota `POST .../inventario/descartar-lote` (`PedidoDescartarLote { InstanciaIds, ApenasLixo }`). Em transação Serializable, destrói atomicamente todos os itens selecionados ou todos os marcados como lixo na bolsa (`ApenasLixo = true`). Recusa com 409 se contiver item equipado (`ItemEquipado`) ou favorito (`ItemFavorito`). O botão **Descartar lixo (N)** aparece no rodapé sempre que houver peças de lixo na bolsa.
- **Filtro de loot por tier (#565, 2026-10-13):** o seletor **Loot no chão** (rodapé da janela: Tudo, C ou melhor, B, A, S, só S+) esconde as peças de tier abaixo do escolhido: o marcador some, a dica "F: pegar" não conta com elas e a tecla F as deixa no chão. A preferência mora **no client** (`PlayerPrefs`, deste computador; `FiltroDeLoot`): o servidor não a persiste, e o client a manda em cada pedido de "tudo por perto" (`ColetarLoot.TierMinimo`, padrão D = tudo), que o servidor aplica (peça abaixo fica no chão; item que não é equipamento não tem tier e sempre passa; pedir um item pelo `LootId` ignora o filtro, é escolha explícita). Para o chão saber o tier, **os valores da peça agora são sorteados quando o mob a larga** (antes eram na coleta): `LootNoChao` ganha `TemTier` e `Tier`, e é esse mesmo exemplar que entra no inventário (mesma origem e grau, mesmas faixas). O que ficou no chão pelo filtro expira no prazo de sempre.
- **Cabe na tela, sem rolagem** (decidido em 2026-10-12): a casa tem 40 a 64 px (unidades do painel, que escala por 1920×1080) e é o maior tamanho em que o paperdoll de 7 linhas e a bolsa de 8 colunas cabem na janela, medindo a moldura do Core e o resto do conteúdo; recalcula quando a geometria muda. A conta é `MedidasInventario`. Os botões do rodapé têm altura normal de botão.
- **Descartar:** o descarte (solta fora da janela) pede a confirmação numa faixa da própria janela; só o **Confirmar descarte** envia. Peça com fusão acima de +0 ou de tier A ou acima pergunta de novo, com o motivo, antes de enviar.
- **Ícone:** ponto único `PainelInventario.IconeDoTipo` (`IconesEquipamento.Carregar`, silhueta branca tingida pelo tier no USS); sem sprite, a sigla do tipo.
- **Dados de equipamento no client:** `JsonUtility` troca `"Equipamento": null` por um objeto vazio (tipo `Capacete`); use sempre `RegrasEquipamentos.DadosDe(entrada)`, nunca `entrada.Equipamento != null` (causa do 400 `PosicaoInvalida` da #585).
- **Bancada de fusão:** aberta por **Bancada** (`PainelFusao`); veja [Entregue na #616](#entregue-na-616-fusão-v2).
- **Expansão em abas (#564, 2026-10-14):** a bolsa suporta até 4 abas de 40 casas cada (`ConfigBolsa.AbasMaximas = 4`, total de até 160 espaços). O personagem começa com 1 aba (`RegrasItens.AbasBolsaInicial = 1`) e pode desbloquear novas abas com coroas pela rota `POST /api/personagens/{id}/inventario/expandir` (`RotasItens.Expandir`, sem corpo, resposta `EquipamentosResponse`). Custos progressivos: Aba 2 = 100 coroas, Aba 3 = 250 coroas, Aba 4 = 500 coroas (`ConfigBolsa.CustoPorAba`). A cobrança é atômica via `ICarteiraCoroas.DebitarAsync` em transação Serializable; sem saldo responde 409 `CoroasInsuficientes`, no limite de abas responde 400 `BolsaNoLimite`. A quantidade de abas é persistida em `EstadoEquipamentos.AbasBolsa`. Na UI (`PainelInventario`), a barra de abas mostra as abas desbloqueadas e, enquanto houver abas bloqueadas, um botão `+` com tooltip de custo; ao clicar, exibe confirmação na própria janela antes de enviar, e ao concluir seleciona a nova aba liberada.
- **Fora do ambiente de teste do agente:** a conferência visual da janela (capturas 1280x720 a 2560x1440, texto 20 e máximo) fica como pendência de Unity.

**Na sequência:** busca e filtros (#334), Ctrl+clique entre contêineres (#566).

### Catálogo de modelos-base (#330, confirmado pelo usuário em 2026-10-11)

As quatro famílias **Vigia/Bastião, Predador/Ruptor, Aurora/Tempestade e Batedor/Olho Longo** têm variantes A (atributo principal +6) e B (principal +4 e especialização), cada uma nas nove categorias de peça. São 72 modelos-base, sem contar armas. Os nomes, valores fixos e defesas por categoria vêm da seção 12 do [livro](../brainstorm/figjam.md#12--equipamentos-por-função-e-peça). As constantes/tabelas de balanceamento ficam em `CatalogoEquipamentos`. Na #623, o principal de cada família e a especialização passam a usar os nomes novos, como atributos do item ([Atributos dos equipamentos](#atributos-dos-equipamentos)).

Bastião acrescenta +8 à defesa-base da peça, Ruptor e Olho Longo acrescentam +10 de Vigor, Tempestade acrescenta +3 de Espírito. Defesa e especialização de defesa são somadas em um único atributo fixo. As peças de adorno chamadas Foco continuam na posição Adorno: o nome não as transforma em arma secundária.

Os IDs 101–109 do Vigia permanecem; Bastião usa 110–118, Predador 119–127, Ruptor 128–136, Aurora 137–145, Tempestade 146–154, Batedor 155–163 e Olho Longo 164–172. Cada faixa segue capacete, peito, luvas, calça, botas, cinto, anel, brinco e adorno. O catálogo devolve objetos novos; alterar um exemplar não altera modelos ou outros exemplares. Exemplares já persistidos conservam seus valores, sem regeneração nem migração.

Até a #615 o gerador aplicava o fator Inicial/Médio/Forte **1/2/4** aos valores-base; agora o grau escolhe a janela da faixa em que os valores são sorteados ([Entregue na #615](#entregue-na-615-tier-calculado-pelos-atributos)), e os valores do catálogo só dizem quais atributos o modelo tem (e dão a referência da migração dos exemplares antigos). Requisitos de personagem continuam 1/10/20. O nível da peça permanece independente do grau. Família e variante são gravadas junto aos metadados e apresentadas no inventário; itens antigos sem esses campos continuam válidos.

**No jogo:** I → Equipamentos → escolher o grau → Obter equipamento de teste (editor/build de desenvolvimento, servidor Development). O servidor sorteia uniformemente entre os 72 modelos e o martelo já existente, grava o exemplar e devolve a bolsa atualizada. O parâmetro opcional `?grau=0|1|2` da rota `/bau-teste` escolhe o grau; omitido mantém Inicial, inválido retorna 400 sem criar item. Bolsa cheia, autenticação, propriedade e transação seguem as regras anteriores. Peças médias/fortes exigem nível 10/20 para equipar.

O catálogo inicial permanece na região Vale; variações regionais, pools de skills (#340/#167) não são definidos aqui; a distribuição no loot dos mobs é a de [Loot dos inimigos](#loot-dos-inimigos-493). O baú original conserva a tabela dele.

### Inventário e baú de teste: definido (provisório, 2026-10-11)

Ficha aprovada em 2026-10-11. É um **recorte de teste** para ter o fluxo inventário + baú de ponta a ponta; **não** substitui as decisões de [Definido](#definido) nem responde as de [Em aberto](#em-aberto) (raridades, skills nos itens e fusão continuam como estão e ficam fora deste recorte).

- Catálogo fixo de **4 itens** de teste (2026-10-11), com ids estáveis: 1 Espada de teste, 2 Escudo de teste, 3 Poção de teste, 4 Moeda de teste.
- O inventário de cada personagem tem **40 espaços** (2026-10-11). Cada entrada ocupa um espaço; itens repetidos ocupam espaços separados (sem pilha).
- Abrir o baú dá **1 a 3 itens** (2026-10-11), sorteados **com reposição** e **uniformes** entre os 4 do catálogo. O sorteio é do servidor.
- O servidor só grava se **todos** os itens sorteados couberem nos 40 espaços. Se não couberem, responde `InventarioCheio` e **não grava nada** (nem parte dos itens).
- Itens deste recorte não têm raridade, skills nem fusão.
- **Posição e recarga do baú (#492, 2026-10-13):** `POST /bau` continua existindo, mas o servidor confere antes de sortear. (1) Posição: a que o Mundo aceitou (`IPosicoesDoMundo`), na área `cidade`, a até 4 m do baú; senão 409 `BauLongeDemais`. (2) Recarga por personagem de 5 s, reservada de forma atômica antes do sorteio (dois pedidos simultâneos dão um só sorteio), devolvida se nada foi aberto (bolsa cheia); senão 429 `BauEmRecarga`. O baú tem posição fixa em `ConfigBau` (shared): (2,5; -20,5), o nascimento da cidade mais 3,5 m à frente e 2,5 m à direita; o client o põe ali (antes calculava a partir de onde o jogador nascia). O alcance do client (3 m) é menor que o do servidor (4 m) para a posição atrasada pela rede não recusar quem o client via perto. A recarga fica em memória (reiniciar o servidor zera). Config `Itens:Bau:ExigirPosicao` e `Itens:Bau:RecargaSegundos` existem só para os testes que precisam de itens sem posição; os padrões são os de produção. No client, a dica "E: abrir o baú" vira "Baú recarregando (N s)" durante a espera.
- Fora deste recorte: sorteio com raridade, drop de mobs, persistência definitiva, UI, prefab de baú, mundo.

### Proposta do board (2026-10-11, não validada)

Proposta do board em 2026-10-11: as partes ainda **não validadas pelo grupo** do [Livro de ideias](../brainstorm/figjam.md#asterra--livro-de-ideias-do-jogo) não substituem o [Definido](#definido). As confirmações de equipamentos e catálogo acima prevalecem sobre as propostas originais. Resumo:

- **Anatomia do equipamento:** nome, tipo, região de origem, raridade, nível, atributo fixo (tipo + região), atributos aleatórios (o tipo limita o conjunto), um atributo sorteado como **infusível**, skills sorteadas (gravadas no item) e infusões usadas.
- ~~**Cinco raridades**~~ (depreciado em 2026-10-12: [tier pelos atributos](#tier)):

| Raridade | Skills (níveis que liberam) | Nível máx. | Infusões |
|---|---|---|---|
| Comum (cinza) | 1 (1) | 10 | 10 |
| Incomum (verde) | 2 (1, 5) | 15 | 15 |
| Raro (azul) | 3 (1, 5, 10) | 20 | 20 |
| Épico (roxo) | 4 (1, 5, 10, 15) | 25 | 25 |
| Lendário (dourado) | 5 (1, 5, 10, 15, 20) | 30 | 30 |

- ~~**Infusão:**~~ (depreciado em 2026-10-12: [fusão v2](#fusão)) sacrifica doadores do mesmo tipo e raridade (região livre); cada doador ocupa 1 slot e soma 2% do valor-base do atributo infusível (sem recalcular sobre o infundido). Prévia antes/depois e confirmação; favorito fica protegido.
- ~~**Fusão de skills (infusão especial):**~~ (depreciado em 2026-10-12: a fusão preserva as skills do principal, DEC-003) em vez de reforçar atributo, o doador compatível transfere uma skill que **substitui** uma do receptor; gasta 1 slot de infusão e não soma atributo. A skill herda o requisito de nível do slot; sem skill repetida na peça.
- ~~**Promoção:**~~ (depreciado em 2026-10-12: o tier sobe pelos atributos; #331 fechada) exige nível máximo e infusões cheias + componentes na forja (couro reforçado, lingote da região, essência de promoção). Sobe 1 raridade, nível volta a 1, contador de infusões zera; ganhos e skills já liberadas permanecem; escolhe 1 de 2 skills sorteadas para o novo slot.
- **Loot e inventário:** F coleta, segurar F escolhe; dono do loot = quem mais causou dano ao mob; recurso fica com quem acertou primeiro (reserva liberada após 15 s sem interação); inventário grande sem peso, com busca e favoritos. Transmog: a peça obtida registra a aparência, sem mudar atributos.
- **Equipamento por função e peça:** 11 posições (capacete, peito, luvas, calça, botas, cinto, 2 anéis, 2 brincos, adorno); 4 famílias (Vigia/Bastião tanque, Predador/Ruptor dano físico, Aurora/Tempestade mago, Batedor/Olho Longo arqueiro), cada uma com variação A (atributo principal) e B (especialização); 72 modelos-base. Graus Inicial/Médio/Forte multiplicam os valores-base por 1/2/4 (não as skills); grau é um eixo e raridade é outro. Misturar famílias é livre. A tabela de valores por peça fica no board.
- **Base:** fusão/infusão de equipamento com sacrifício de itens, de jogos de loot. Igual: o item doador é destruído. Diferente: skills nos itens e transferência de skill por substituição.

## Depende de

- **Core:** conta, login e o token (JWT) que as rotas exigem.
- **[Personagem](personagem.md):** o `Id` do personagem, que vai na rota. O inventário pertence ao personagem.
- **[Mobs](mobs.md):** o evento de morte do inimigo, com quem participou e onde ele caiu (loot no chão).
- **[Mundo](mundo.md):** a posição do personagem, para conferir a distância da coleta, e o aviso de quem entrou na área.

### Arma equipada no personagem (#497)

A arma que o personagem veste aparece no corpo dele, para ele e para quem está na mesma área: a do conjunto **ativo** na mão direita, a do conjunto **inativo** nas costas. Trocar de conjunto, equipar e desequipar mudam o que se vê.

- **Contrato:** `VisualEquipado` (`ItensMessageIds.VisualEquipado`, 7005), servidor → área, sempre o **estado inteiro** do personagem (`PersonagemId`, `ConjuntoAtivo` e as `Armas`, cada uma com posição, tipo e modelo-base); o client troca o que tinha. Lista vazia = sem arma. Regras puras em `RegrasVisualEquipado` (shared): qual encaixe (`EncaixeDoVisual`: `MaoDireita` ou `Costas`) e o nome do objeto (`EncaixeMaoDireita`, `EncaixeCostas`, criados pelo gerador do Personagem).
- **Quando sai:** depois de gravar um equipar, desequipar ou troca de conjunto (uma mensagem por mudança aceita, nada se a rota recusa), para os presentes na área do personagem, ele incluído; e ao entrar na área, o recém-chegado recebe o próprio visual e o de quem já estava com arma, e a área recebe o dele. `VisualEquipadoServico` (servidor).
- **Client:** `VisualEquipadoDoCliente` guarda o estado por personagem (a mensagem pode chegar antes do avatar); `ControleDoVisualEquipado` confere 4 vezes por segundo, acha o avatar (o jogador local, ou `Jogador_<id>` do Mundo) e `ApresentadorDoVisual` tira os modelos antigos e põe os novos no encaixe, achado pelo nome em qualquer profundidade do visual.
- **Decisões de design (2026-10-13):** (1) só a **arma principal** de cada conjunto tem modelo; mão secundária (escudo, foco) e armaduras não aparecem ainda (não há encaixe de mão esquerda no gerador). (2) Modelos **de primitivas em código**, um por tipo (espada, martelo, arco, cajado; `ModelosDeArma`), sem asset: o cabo na origem, apontando para +Y; o artista de itens os troca por modelo de verdade no mesmo ponto. (3) **Avatar remoto sem encaixe** (a cápsula do Mundo hoje): a Itens cria o `EncaixeMaoDireita`/`EncaixeCostas` no objeto raiz do avatar (`ApresentadorDoVisual.MaoDireitaNaRaiz`, `CostasNaRaiz`); quando o Mundo trocar a cápsula pelo modelo animado ([#498](https://github.com/GabrielNied/jogo_do_grupo/issues/498)) e ele tiver os encaixes do gerador, passam a valer sem mudar nada aqui. (4) Nas costas o modelo gira 40° e o meio da arma fica no encaixe. (5) O visual mostra o que o servidor aceitou; nada de previsão no client.
- **Conferir sem rodar o jogo:** `npm run unity:executar -- JogoDoGrupo.Client.Features.Itens.Editor.CapturaDoVisualEquipado.Capturar` grava o Valdren de frente, de lado e de costas em `client/prints/`.
- **Pendente:** ajuste fino da pose da arma na mão (o osso `Weapon.R` define a orientação; hoje o cabo está na palma e a lâmina sobe), clipes de guarda e modelo animado dos outros jogadores (#498).

## Expõe

API HTTP do recorte de teste (mesmo estilo de [Personagem](personagem.md#expõe); [ADR 0009](../decisoes/0009-api-http-para-conta-e-personagens.md)). Todas as chamadas exigem o token (JWT) do login. Os tipos ficam em [shared/Package/Runtime/Features/Itens/](../../shared/Package/Runtime/Features/Itens/); o código do contrato é a fonte dos nomes e tipos exatos. Contrato definido em 2026-10-11.

| Rota | O que faz | Envia | Recebe |
|---|---|---|---|
| `GET /api/personagens/{personagemId}/inventario` | Lista o inventário do personagem | nada | **200** `InventarioResponse`: lista de `EntradaInventario` (no máximo 40) |
| `POST /api/personagens/{personagemId}/bau` | Abre o baú: o servidor sorteia, grava e devolve | nada (sem corpo) | **200** `AbrirBauResponse`: `Ganhos` (1 a 3 entradas desta abertura) e `Inventario` (o inventário resultante, no mesmo formato do GET); recusado: o motivo (veja abaixo) |

- **`EntradaInventario`:** um espaço ocupado: `ItemId` (int, do catálogo) e `Nome` (texto exibido).
- **`CatalogoItens`:** os 4 ids fixos e `ObterNome`.
- **`RegrasItens`:** os limites (40 espaços, 1 a 3 itens por baú), só os números.
- **`RotasItens`:** os caminhos; `InventarioDe` e `BauDe` montam o caminho para um personagem.
- **`CodigosErroItens`:** os motivos de recusa, no campo `Codigo` do `ErroApi` do Core:

| Código | Status | Quando |
|---|---|---|
| `InventarioCheio` | 409 | os itens sorteados não cabem nos 40 espaços; nada é gravado |
| `BauLongeDemais` | 409 | fora da área do baú ou a mais de 4 m dele; nada é sorteado |
| `BauEmRecarga` | 429 | abriu há menos de 5 s; nada é sorteado |
| `PersonagemNaoEncontrado` | 404 | não existe personagem com esse id, ou ele é de outra conta (mesma resposta nos dois casos) |
| `NaoAutenticado` (Core) | 401 | sem token, inválido ou expirado |

Nem o Core nem Personagem tinham um código para personagem inexistente em 2026-10-11, por isso `PersonagemNaoEncontrado` nasce aqui; se o grupo preferir, ele pode migrar para Personagem. Erros genéricos (requisição inválida, erro interno) usam os códigos do Core (`CodigosErro`).

- Por serem HTTP, essas rotas não usam IDs da faixa 7000–7999.

### Loot dos inimigos (#493)

Matar um inimigo dá **peças reais** do catálogo de 72 (armaduras e acessórios), não mais os itens de teste. Os valores são **proposta a calibrar**, todos em `ConfigLoot` (shared); as contas puras ficam em `TabelaDeLoot`.

- **Quantidade:** continua a de `RegrasLoot` (50% nenhum, 40% um, 10% dois por participante). Cada item vira peça com `ConfigLoot.ChanceDeEquipamento` (hoje 100%); poção e moeda de teste **saíram da tabela** (seguem no baú de teste).
- **Qual peça:** uniforme entre as 72, sem armas (martelo e itens legados não caem).
- **Origem do inimigo melhora os valores** (desde a [#615](https://github.com/GabrielNied/jogo_do_grupo/issues/615); antes eram pesos de raridade): a queda guarda a origem e, quando a peça entra no inventário, cada atributo é sorteado dentro da janela do grau e vale a **melhor de N rolagens**. O tier é só a consequência dos valores ([Tier](#tier)). Valores em `ConfigLoot`:

| Origem | Quando | Rolagens por atributo |
|---|---|---|
| Comum | comum sem variante | 1 |
| Fortalecida (elite) | comum fortalecida | 2 |
| Chefe raro | categoria chefe raro | 3 |
| Chefe | categoria chefe (dungeon) | 4 |

- **Grau e requisito pelo nível do inimigo** (`InimigoDerrotado.Nivel`; 0 vale o da ficha da criatura): 1-9 Inicial (equipa no nível 1), 10-19 Médio (nível 10), 20+ Forte (nível 20). Os fatores 1/2/4 de atributos são os de [Equipamentos](#equipamentos-166-confirmação-do-usuário-em-2026-10-11).
- **Individual:** cada participante faz o próprio sorteio (peça e grau); a queda guarda a origem e o grau até a coleta, e o exemplar (valores, tier de origem) é gerado ao entrar no inventário. Por isso o item no chão não tem tier: ele nasce na coleta.
- **Aviso e chão:** `LootNoChao` traz o `Grau` (a `Raridade` saiu na #615), e o nome vem do catálogo de equipamentos. O aviso do HUD mostra nome e tier (veja abaixo).
- **Meta de ritmo (X = 3 min):** jogador solo, 4 mobs/min, níveis 1 a 10: em média uma peça **melhor** que a equipada a cada 3 minutos, e no máximo 20 minutos sem nenhuma. "Melhor" é maior `TabelaDeLoot.Pontuacao` (soma dos valores dos atributos, +10% por degrau do tier atual) na posição onde a peça cabe. **A meta não mudou** com a #615: a simulação (200 corridas de nível 1 a 10) mede ~1,3 min de média e ~17 min na pior espera (posições quase cheias perto do nível 10; eram ~1,4 e ~15 min com a raridade), e a primeira peça melhor sai nos 2 primeiros minutos. O teste `RitmoDeLootTestes` falha se a tabela, a quantidade ou o ritmo saírem da meta.
- **Fora desta entrega:** `MultiplicadorDeRecompensa` não muda a quantidade (a variante já pesa nas rolagens); loot de chefe usa a mesma quantidade do comum, até o grupo definir a tabela do boss; skills nas peças (#340/#167); atributos elementais.

### Loot no chão (#64, contrato da #367)

Regras na [fatia jogável, seção 5](fatia-jogavel.md#5-loot-no-chão): loot **individual**, cada participante sorteia o próprio e só ele o vê. O servidor sorteia, guarda o dono e valida a coleta. Este é o contrato; o servidor (#368) e o client (#369) vêm depois. Tempo real, faixa 7000–7999:

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 7000 | `LootApareceu` | servidor → o dono | `Loot` (`LootNoChao`: `LootId`, `ItemId`, `Nome`, `Grau`, `X`, `Z`, `ExpiraEmSegundos`) |
| 7001 | `ColetarLoot` | client → servidor | `LootId` (0 = tudo o que é meu por perto) |
| 7002 | `LootColetado` | servidor → o dono | `LootId`, `Entrada` (`EntradaInventario`) |
| 7003 | `ColetaRecusada` | servidor → o dono | `LootId`, `Codigo` (`LootNaoEncontrado`, `LongeDemais`, `InventarioCheio`) |
| 7004 | `LootSumiu` | servidor → o dono | `LootId` (expirou) |

Valores em `RegrasLoot`, todos **proposta a calibrar**:

| Valor | Constante | Hoje |
|---|---|---|
| Itens por participante, por inimigo comum | `ChanceDeNenhumItem`, `ChanceDeUmItem`, `ChanceDeDoisItens` | 50% nenhum, 40% um, 10% dois |
| Tempo no chão, reservado ao dono | `ReservaSegundos` | 180 s |
| Distância para coletar | `RaioDeColetaMetros` | 3 m, no plano do chão |
| Distância em que o client mostra o item | `RaioDeVisaoMetros` | 12 m (não é regra do servidor) |

- **Tecla F:** o client envia `ColetarLoot` com `LootId` 0 e o servidor coleta tudo o que é do personagem dentro de 3 m; os "12 m" da fatia viram a distância em que o item aparece na tela. Com um `LootId`, coleta só aquele item.
- **Qual item cai:** peças do catálogo de equipamentos, com origem e grau (veja [Loot dos inimigos](#loot-dos-inimigos-493)). Sem skills.
- **Inventário cheio:** o item fica no chão até expirar; os que couberem entram.
- **Posição:** `X` e `Z` de onde o inimigo caiu. O servidor não conhece a altura do terreno; o client assenta o item no chão.
- **Quem volta ao jogo** recebe de novo `LootApareceu` dos itens que ainda estão no prazo, com o tempo que falta.
- **Dono do loot (#332), decidido em 2026-10-11 (/decida):** vale a [fatia](fatia-jogavel.md#5-loot-no-chão) (loot individual para quem participou); a proposta do livro (maior dano) **não** é adotada agora e fica registrada em [social.md](social.md). Deste contrato só vale "o dono". Coleta com F e recurso garantido ao primeiro golpe seguem como melhoria separada, sem mexer no dono.

**No servidor (#368):**

- Quem **participou** é quem o evento de morte de Mobs lista (`InimigoDerrotado.Participantes`: causou dano ao inimigo). Cada um tem um sorteio próprio; a variante e o nível do inimigo mudam as rolagens dos valores e o grau (veja [Loot dos inimigos](#loot-dos-inimigos-493)).
- O chão fica **em memória**: se o servidor reinicia, o que estava no chão some (o inventário não, ele é do banco).
- A coleta confere, nesta ordem: o item existe e é do jogador (senão `LootNaoEncontrado`), está ao alcance com a posição que o Mundo aceitou (senão `LongeDemais`) e cabe no inventário (senão `InventarioCheio`). O item só sai do chão depois de gravado.
- Dois pedidos do mesmo item ao mesmo tempo: um coleta, o outro recebe `LootNaoEncontrado`.
- O item some até 1 s depois do prazo (`ConfigLootServidor.IntervaloDaVarreduraSegundos`); a coleta confere o prazo na hora.
- Se o banco falhar na coleta, o item volta para o chão e o jogador não recebe resposta: basta apertar F de novo.

**No client (#369):**

- Cada item do jogador no chão vira um marcador dourado girando, assentado no chão, que aparece a até 12 m (`RaioDeVisaoMetros`). O client tira o item quando o prazo vence, sem esperar o `LootSumiu`.
- Com algum item a até 3 m, aparece a dica **"F: pegar — <item> (tier <letra>)"** (com múltiplos itens no alcance, "F: pegar (<N> itens)"); F manda `ColetarLoot` com "tudo por perto". Com janela ou menu do Esc aberto, nem dica nem coleta.
- Avisos no HUD: "Você pegou <item> (tier <letra>)." ao coletar (ex.: "Você pegou Elmo do Vigia (tier B)."; a cor do tier no aviso depende do HUD do Core); inventário cheio (amarelo), longe demais e nada para pegar viram texto, sem mostrar o código.

## Ícones de equipamento

Silhuetas brancas com alpha (128 px), um por `TipoEquipamento`, para a UI tingir pelo tier. Para achar o ícone, use `IconesEquipamento.Carregar(tipo)` (`Resources/Itens/Icones/<TipoEquipamento>`). Origem e crédito em [licencas.md](../arte/licencas.md).
