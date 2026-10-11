# Itens

**Responsável:**
**Status:** em definição · **prioridade atual do grupo** (2026-10-10: "vamos focar no sistema de item")
**Faixa de IDs de mensagem:** 7000–7999

## Resumo

Inventário, equipamentos, raridades e sistema de fusão.

## Definido

- Equipamentos e raridades
- **Os itens carregam as skills, sorteadas aleatoriamente** (D3 da [validação de 2026-10-10](../brainstorm/validacao.md)), e a combinação de itens equipados define o estilo de jogo (D2)
- Estilo looter: muito loot é parte do loop principal (D5)
- Os itens equipados definem o papel no grupo: tank, healer ou DPS (veja [combate.md](combate.md))
- ~~Sistema de fusão: limite de fusões por raridade, item sacrificado é destruído, só funde itens do mesmo tipo ou tier, herança parcial de atributos, retorno decrescente, custo crescente em ouro (tira moeda do jogo), teto de atributo por tier~~ (substituído em 2026-10-11 pela decisão abaixo)
- **Fusão/infusão** (decidido por Gabriel em 2026-10-11, a validar pelo grupo): vale a infusão do board ([Proposta do board](#proposta-do-board-2026-10-11-não-validada)): só doadores do mesmo tipo **e** raridade, +2% fixo do valor-base por doador, sem retorno decrescente, **com custo em coroas** (mantém a saída de moeda). O item sacrificado continua destruído. O custo exato em coroas é valor a propor (dono: agente economia, PR #360); veja [economia.md](economia.md).
- Itens negociáveis, inclusive os fundidos (fomentar a economia)

## Em aberto

- [ ] Quantas raridades/tiers?
- [ ] Quantas skills um item pode ter? A raridade muda isso?
- [ ] Herança de atributos na fusão: porcentagem ou escolha do jogador?
- [ ] Custo exato em coroas da infusão: a propor (agente economia).
- [ ] Ajustes à tabela de raridades: pendente de Gabriel (a tabela do board foi adotada "com ajustes" ainda não informados; segue como proposta).

## Regras

### Inventário e baú de teste: definido (provisório, 2026-10-11)

Ficha aprovada em 2026-10-11. É um **recorte de teste** para ter o fluxo inventário + baú de ponta a ponta; **não** substitui as decisões de [Definido](#definido) nem responde as de [Em aberto](#em-aberto) (raridades, skills nos itens e fusão continuam como estão e ficam fora deste recorte).

- Catálogo fixo de **4 itens** de teste (2026-10-11), com ids estáveis: 1 Espada de teste, 2 Escudo de teste, 3 Poção de teste, 4 Moeda de teste.
- O inventário de cada personagem tem **40 espaços** (2026-10-11). Cada entrada ocupa um espaço; itens repetidos ocupam espaços separados (sem pilha).
- Abrir o baú dá **1 a 3 itens** (2026-10-11), sorteados **com reposição** e **uniformes** entre os 4 do catálogo. O sorteio é do servidor.
- O servidor só grava se **todos** os itens sorteados couberem nos 40 espaços. Se não couberem, responde `InventarioCheio` e **não grava nada** (nem parte dos itens).
- Itens deste recorte não têm raridade, skills nem fusão.
- Fora deste recorte: sorteio com raridade, drop de mobs, persistência definitiva, UI, prefab de baú, mundo.

### Proposta do board (2026-10-11, não validada)

Proposta do board em 2026-10-11, **não validada pelo grupo**: nomes e números do [Livro de ideias](../brainstorm/figjam.md#asterra--livro-de-ideias-do-jogo) (seções 03 e 12) são propostas e não substituem o [Definido](#definido). Resumo, sem copiar o texto:

- **Anatomia do equipamento:** nome, tipo, região de origem, raridade, nível, atributo fixo (tipo + região), atributos aleatórios (o tipo limita o conjunto), um atributo sorteado como **infusível**, skills sorteadas (gravadas no item) e infusões usadas.
- **Cinco raridades** (responde "quantas raridades/skills" em Em aberto, se aprovado):

| Raridade | Skills (níveis que liberam) | Nível máx. | Infusões |
|---|---|---|---|
| Comum (cinza) | 1 (1) | 10 | 10 |
| Incomum (verde) | 2 (1, 5) | 15 | 15 |
| Raro (azul) | 3 (1, 5, 10) | 20 | 20 |
| Épico (roxo) | 4 (1, 5, 10, 15) | 25 | 25 |
| Lendário (dourado) | 5 (1, 5, 10, 15, 20) | 30 | 30 |

- **Infusão:** sacrifica doadores do mesmo tipo e raridade (região livre); cada doador ocupa 1 slot e soma 2% do valor-base do atributo infusível (sem recalcular sobre o infundido). Prévia antes/depois e confirmação; favorito fica protegido.
- **Fusão de skills (infusão especial):** em vez de reforçar atributo, o doador compatível transfere uma skill que **substitui** uma do receptor; gasta 1 slot de infusão e não soma atributo. A skill herda o requisito de nível do slot; sem skill repetida na peça.
- **Promoção:** exige nível máximo e infusões cheias + componentes na forja (couro reforçado, lingote da região, essência de promoção). Sobe 1 raridade, nível volta a 1, contador de infusões zera; ganhos e skills já liberadas permanecem; escolhe 1 de 2 skills sorteadas para o novo slot.
- **Loot e inventário:** F coleta, segurar F escolhe; dono do loot = quem mais causou dano ao mob; recurso fica com quem acertou primeiro (reserva liberada após 15 s sem interação); inventário grande sem peso, com busca e favoritos. Transmog: a peça obtida registra a aparência, sem mudar atributos.
- **Equipamento por função e peça:** 11 posições (capacete, peito, luvas, calça, botas, cinto, 2 anéis, 2 brincos, adorno); 4 famílias (Vigia/Bastião tanque, Predador/Ruptor dano físico, Aurora/Tempestade mago, Batedor/Olho Longo arqueiro), cada uma com variação A (atributo principal) e B (especialização); 72 modelos-base. Graus Inicial/Médio/Forte multiplicam os valores-base por 1/2/4 (não as skills); grau é um eixo e raridade é outro. Misturar famílias é livre. A tabela de valores por peça fica no board.
- **Base:** fusão/infusão de equipamento com sacrifício de itens, de jogos de loot. Igual: o item doador é destruído. Diferente: skills nos itens e transferência de skill por substituição.

## Depende de

- **Core:** conta, login e o token (JWT) que as rotas exigem.
- **[Personagem](personagem.md):** o `Id` do personagem, que vai na rota. O inventário pertence ao personagem.

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
| `PersonagemNaoEncontrado` | 404 | não existe personagem com esse id, ou ele é de outra conta (mesma resposta nos dois casos) |
| `NaoAutenticado` (Core) | 401 | sem token, inválido ou expirado |

Nem o Core nem Personagem tinham um código para personagem inexistente em 2026-10-11, por isso `PersonagemNaoEncontrado` nasce aqui; se o grupo preferir, ele pode migrar para Personagem. Erros genéricos (requisição inválida, erro interno) usam os códigos do Core (`CodigosErro`).

- Por serem HTTP, essas rotas não usam IDs da faixa 7000–7999.

### Loot no chão (#64, contrato da #367)

Regras na [fatia jogável, seção 5](fatia-jogavel.md#5-loot-no-chão): loot **individual**, cada participante sorteia o próprio e só ele o vê. O servidor sorteia, guarda o dono e valida a coleta. Este é o contrato; o servidor (#368) e o client (#369) vêm depois. Tempo real, faixa 7000–7999:

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 7000 | `LootApareceu` | servidor → o dono | `Loot` (`LootNoChao`: `LootId`, `ItemId`, `Nome`, `X`, `Z`, `ExpiraEmSegundos`) |
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
- **Qual item cai:** uniforme no catálogo de teste, com reposição (igual ao baú). Sem raridade nem skills.
- **Inventário cheio:** o item fica no chão até expirar; os que couberem entram.
- **Posição:** `X` e `Z` de onde o inimigo caiu. O servidor não conhece a altura do terreno; o client assenta o item no chão.
- **Quem volta ao jogo** recebe de novo `LootApareceu` dos itens que ainda estão no prazo, com o tempo que falta.
- A proposta do livro para o dono do loot (maior dano, #332) continua em aberto e não muda este contrato: ele só fala de "o dono".
