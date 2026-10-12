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
- **Fusão/infusão** (decidido por Gabriel em 2026-10-11, a validar pelo grupo): só doadores do mesmo tipo **e** raridade, +2% fixo do valor-base por doador, sem retorno decrescente, **com custo em coroas**. O item sacrificado é destruído. Valores iniciais e fluxo entregues na [Infusão de equipamentos](#infusão-de-equipamentos-174); carteira em [economia.md](economia.md).
- Itens negociáveis, inclusive os fundidos (fomentar a economia)
- **Inventário de jogo, não de formulário** (decidido por Gabriel em 2026-10-12): bolsa em grade de casas **1×1** para todos os tipos, paperdoll, tooltip, clique direito e arrastar e soltar, com a base pronta para inventário grande. Veja [Interface do inventário](#interface-do-inventário-560).

## Em aberto

- [x] Cinco raridades confirmadas para #166: comum, incomum, raro, épico e lendário. Grau continua um eixo separado (veja Equipamentos abaixo).
- [ ] Quantas skills um item pode ter? A raridade muda isso?
- [x] Infusão usa o atributo marcado e +2% do valor-base; substitui a proposta antiga de herança e retorno decrescente (#174).
- [x] Custo inicial configurável por raridade na #174, sujeito a balanceamento (veja Infusão abaixo).
- [ ] Ajustes à tabela de raridades: pendente de Gabriel (a tabela do board foi adotada "com ajustes" ainda não informados; segue como proposta).

## Regras

### Infusão de equipamentos (#174)

I → Equipamentos → **Infundir equipamentos** abre a bancada na janela do inventário. Escolher um receptor (pode estar equipado) e um doador da bolsa; **Ver prévia** mostra nomes/identidades, atributo antes/depois, slots e custo. Só **Confirmar infusão e consumir doador** executa a operação. Cancelar ou trocar a seleção não consome nada. A prévia mostra o resultado mesmo sem saldo; nesse caso a confirmação fica desabilitada.

O doador precisa ter o mesmo tipo **e** raridade, mas pode ter outro grau, região ou família. Uma confirmação consome um doador e um slot. O atributo escolhido na geração recebe **2% do seu valor-base**, preservando frações, sem juros compostos: base 20 recebe +0,4 por doador e chega a 24 após dez infusões. Os demais atributos, nível, raridade e sorteios permanecem. O limite segue `RegrasEquipamentos.LimiteInfusoes`.

O valor-base infusível é gravado na geração. Para exemplar anterior sem esse campo e sem infusões, usa-se o atributo atual na primeira confirmação; exemplar já infundido sem base válida é recusado. Peças sem metadados precisam ser equipadas uma vez pelo fluxo legado antes de aparecerem na bancada. A ação de favorito grava a proteção no exemplar; doador favorito ou equipado não pode ser consumido. O receptor pode continuar favorito.

**Coroas:** custo inicial por doador Comum/Incomum/Raro/Épico/Lendário: **10/20/40/80/160**, configurável em `Itens:Infusao:CustosPorRaridade` (`ConfigInfusao`). São valores iniciais de balanceamento, não custo derivado dos atributos do doador. A carteira começa em zero por personagem. Em editor/build de desenvolvimento contra servidor Development, **Receber coroas de teste** acrescenta 1000 coroas; essa rota não é registrada em produção.

**Autoridade e persistência:** conta dona, exemplares, posição, proteção, limite e saldo são conferidos pelo servidor. A prévia não altera o estado. A confirmação compara a assinatura da prévia com o estado atual e grava cobrança, consumo e ganho na mesma transação; falha não cobra nem perde item. Doador usado por outra requisição desaparece uma única vez. Alterações relevantes nas peças/custo ou reinício do servidor exigem nova prévia. Após erro de conexão, reabrir a bancada consulta o resultado persistido. Bônus de receptor equipado são obtidos pelo contrato já existente, sem soma cumulativa. Detalhes técnicos no [ADR 0032](../decisoes/0032-infusao-e-cobranca-atomicas.md).

API autenticada sob `/api/personagens/{personagemId}/equipamentos/infusao`, sempre POST: `/estado`, `/previa` e `/confirmar` (`PedidoInfusao`: receptor, doador e confirmação na última), `/favorito` (`PedidoFavorito`) e `/coroas-teste` (só Development). As respostas são `EquipamentosResponse` com saldo, modo da bancada e prévia opcional. Identidade igual é 400; personagem/exemplar alheio ou ausente é 404; incompatibilidade, favorito, doador equipado, teto, saldo insuficiente e prévia desatualizada são 409. A UI traduz os motivos e bloqueia pedidos durante a espera.

Transferência de skills, promoção de raridade e obtenção normal de coroas continuam nas tarefas específicas; a #174 entrega reforço de atributo.

### Equipamentos (#166, confirmação do usuário em 2026-10-11)

Modelo completo do livro: **11 posições** (capacete, peito, luvas, calça, botas, cinto, dois anéis, dois brincos e adorno), mais **dois conjuntos de armas**, cada um com mão principal e secundária. Só o conjunto ativo concede atributos. Armaduras e acessórios dos dois lados somam; misturar famílias é livre.

Cada exemplar tem identidade própria, modelo, região, tipo, raridade, grau, nível da peça, requisito de nível do personagem, atributos fixos, atributos sorteados e índice do atributo infusível. O servidor sorteia e grava uma vez; vestir, tirar, trocar conjunto e reconectar não sorteiam novamente. O nível da peça não é o requisito de nível do personagem.

As cinco raridades e a tabela do livro (níveis máximos, slots de skills e infusões) têm representação em `RegrasEquipamentos`. Graus inicial/médio/forte usam os fatores 1/2/4 já escritos. A estrutura confirmada é a da seção Anatomia do equipamento do [livro](../brainstorm/figjam.md). Skills utilizáveis, XP da peça, promoção e infusão são as tarefas #167, #331 e #174; esta entrega não executa essas operações.

**Geração para teste:** `ConfigEquipamentos` mantém a quantidade e os valores dos atributos aleatórios fora do sorteio. O padrão provisório usa dois atributos distintos, sem repetir os fixos, dentre os cinco atributos básicos; valores de referência 6 (3 para Espírito), retirados dos exemplos de peças do livro. Não é uma tabela final de balanceamento, de chances ou de atributos elementais. É configurável em `Itens:Equipamentos`. O catálogo confirmado da #330 contém as 72 peças do livro, além do Martelo Ressonante e dos dois equipamentos legados de teste; veja Catálogo abaixo. Espada e escudo de teste continuam compatíveis, sem inventar um bônus fixo para eles.

**Troca autoritativa e persistente:** o servidor confere a conta dona, a identidade do exemplar, o tipo da posição e o nível exigido. A peça anterior volta ao inventário. Uma arma de duas mãos retira a mão secundária do mesmo conjunto; equipar escudo/foco com ela é recusado. Se todas as peças retiradas não couberem na bolsa, nada muda (a primeira peça deslocada ocupa a casa que o item novo deixou; as outras, a primeira casa livre). Peças equipadas não ocupam espaço do inventário. Transação e índice único por personagem/posição impedem duplicação em pedidos concorrentes.

**Bônus:** Combate consulta o total atual através de `Contratos/Itens/IBonusEquipamentos`; não referencia a implementação de Itens. Força, Destreza, Intelecto, Vitalidade e Espírito entram nos cálculos já existentes de dano e recursos, sem acumular o mesmo bônus a cada troca. Defesa e demais status têm representação e apresentação, mas não criam fórmulas novas de mitigação, bloqueio ou efeitos nesta tarefa.

**No jogo:** abrir o inventário com **I** ([Interface do inventário](#interface-do-inventário-560)). O paperdoll mostra os encaixes e o conjunto ativo; o tooltip de cada item mostra atributos fixos/sorteados, atributo infusível, raridade, nível e requisito; clique direito e arrastar e soltar equipam e desequipam; os botões **Conj. 1** e **Conj. 2** trocam o conjunto de armas. Em editor/build de desenvolvimento, **Obter equipamento de teste** pede uma peça ao servidor. Essa rota só existe em ambiente `Development`; não altera o baú original; o loot dos mobs tem tabela própria ([Loot dos inimigos](#loot-dos-inimigos-493)).

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

Soltar um item da bolsa fora da janela do inventário pede confirmação (**Confirmar descarte** / **Cancelar**) e só a confirmação envia o pedido ([Interface do inventário](#interface-do-inventário-560); antes da #560 era o botão **Descartar** na linha do item). O item some e o contador x/capacidade cai.

`POST /api/personagens/{personagemId}/inventario/descartar` (`RotasItens.Descartar`), corpo `PedidoDescartar` (`InstanciaId`); responde 200 `EquipamentosResponse` com o estado atualizado.

- **Destruição definitiva:** o exemplar é apagado; não vira loot no chão nem vai para recompra (decisão assumida em 2026-10-12; venda é a #173).
- **Erros:** personagem ausente ou de outra conta é 404 `PersonagemNaoEncontrado`; exemplar inexistente, de outra conta ou já descartado é 404 `ItemNaoEncontrado`; exemplar equipado é 409 `ItemEquipado` (desequipar antes). Sem token é 401.
- **Idempotência:** repetir o descarte de um exemplar já descartado devolve 404 `ItemNaoEncontrado` e não altera nada (decisão assumida: não dá para distinguir "já descartado" de "alheio" sem vazar a existência de itens de outras contas).
- **Concorrência:** a rota roda em transação Serializable, como equipar. Dois descartes do mesmo exemplar: um vale (200), o outro recebe 404. Descartar junto com equipar: um dos dois vence em série (equipou antes: descarte 409; descartou antes: equipar 404); nunca há peça duplicada nem equipada e apagada.

### Interface do inventário (#560)

Decidido por Gabriel em 2026-10-12. Substitui a lista com dropdown e botões das abas "Itens" e "Equipamentos".

**Por que 1×1:** inventários com tamanho por tipo (Diablo II, Path of Exile) freiam o acúmulo pela falta de espaço. Aqui o centro do jogo é juntar doadores do mesmo tipo e raridade para infundir, e cada peça é única (não empilha). Tamanho por tipo puniria justamente esse comportamento e dificultaria ordenar, filtrar e mover em lote. A bolsa guarda os itens valiosos do jogador; o conforto vem das ferramentas (abaixo), não da escassez de espaço.

**Janela (tecla I):** uma janela só. À esquerda o **paperdoll**: as posições de `PosicaoEquipamento` em volta da silhueta, as armas dos dois conjuntos embaixo e o botão de trocar conjunto. À direita a **bolsa**: grade de 8 colunas, abas no topo e contador x/capacidade. Embaixo, o resumo dos bônus. O painel de infusão continua o atual, aberto pelo botão **Bancada**; vira arrastar numa tarefa própria.

**Casa:** ícone de silhueta por `TipoEquipamento`, tingido e com borda na cor da raridade (sem ícone ainda, a sigla do tipo). Item recém-chegado (baú, loot) tem selo **novo** até o mouse passar por cima. Encaixe vazio mostra a silhueta apagada do tipo.

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
- A **capacidade vem do servidor** (abas e casas por aba, em configuração), não fica fixa no client nem espalhada nas regras. Expandir a bolsa é liberar abas (#564).
- A **ordem é salva no servidor**: mover para uma casa ocupada troca os dois na mesma transação, com índice único por personagem, contêiner, aba e casa. Tudo que entra na bolsa (baú, loot, desequipar, peça deslocada) vai para a primeira casa livre. A migração numera os itens existentes pela ordem atual.

**Entregue na #560 (decisões do dono, 2026-10-12):**

- **Contrato:** `EntradaInventario` ganha `Conteiner` (`ConteinerItem`: Bolsa; Banco e BauDoGrupo reservados, o servidor só aceita Bolsa), `Aba` e `Casa` (`-1` com o item equipado). `CapacidadeBolsa` (`Abas`, `CasasPorAba`, `Total`, `Contem`) vem em `EquipamentosResponse`, `InventarioResponse` e `AbrirBauResponse`; o servidor a decide em `Bolsa.CapacidadeDe` (hoje 1 aba x 40 casas, constantes em `RegrasItens`). O client desenha o que receber.
- **Mover:** `POST /api/personagens/{id}/inventario/mover` (`RotasItens.Mover`), corpo `PedidoMover` (`InstanciaId`, `Aba`, `Casa`), responde 200 `EquipamentosResponse`. Casa ocupada: os dois trocam na mesma transação Serializable. Aba ou casa fora da capacidade: 400 `CasaInvalida`; exemplar inexistente ou alheio: 404 `ItemNaoEncontrado`; equipado: 409 `ItemEquipado`. Mover para a própria casa é um 200 sem efeito.
- **Desequipar para casa ocupada** é o mesmo que equipar o ocupante naquele encaixe: troca se ele servir (tipo, nível, duas mãos, espaço), senão a recusa do equipar (400 `PosicaoInvalida`, 409 `NivelInsuficiente`, `ArmaDeDuasMaos`, `InventarioCheio`) e nada muda.
- **Migração:** a migração só cria as colunas e o índice único (`Personagem, Conteiner, Aba, Casa`, filtrado em casa não nula). Linhas antigas sem casa são numeradas pelo servidor na primeira leitura ou operação, pela ordem do `Id` e na primeira casa livre (`Bolsa.Normalizar`), e a numeração é gravada na primeira operação que grava. Evita SQL à mão na migração e cobre também linhas criadas por outro caminho.
- **Encaixe automático** com os dois encaixes do par ocupados substitui o 1. Equipar já vestido em outro encaixe do mesmo tipo (arrastar anel 1 para o encaixe do anel 2) reequipa.
- **Esc durante o arraste:** o Esc é do Core e fecha a janela inteira, o que também cancela o arraste (o item não se move). Soltar sobre a janela fora de casas e encaixes cancela sem fechar.
- **Selo "novo":** o client compara os itens com os da abertura anterior (mesmo personagem, na sessão) e com os ganhos do baú; o selo sai quando o mouse passa por cima. Na primeira abertura da sessão nada é "novo".
- **Tooltip:** comparação só para peça da bolsa, contra o encaixe automático (com os dois encaixes de anel ou brinco cheios, contra o 1); encaixe vazio mostra tudo como ganho. Aparece depois de 0,3 s e some ao arrastar. Cabe na tela e não cobre o painel de bônus (testa direita/esquerda e embaixo/em cima). ▲ e ▼ têm sinal e valor no texto, então a cor não é a única pista.
- **Descartar:** o descarte (solta fora da janela) pede a confirmação numa faixa da própria janela; só o **Confirmar descarte** envia.
- **Ícone:** ponto único `PainelInventario.IconeDoTipo` (`IconesEquipamento.Carregar`, silhueta branca tingida pela raridade no USS); sem sprite, a sigla do tipo.
- **Dados de equipamento no client:** `JsonUtility` troca `"Equipamento": null` por um objeto vazio (tipo `Capacete`); use sempre `RegrasEquipamentos.DadosDe(entrada)`, nunca `entrada.Equipamento != null` (causa do 400 `PosicaoInvalida` da #585).
- **Painel de infusão:** continua o atual, aberto por **Bancada**.
- **Fora do ambiente de teste do agente:** a conferência visual da janela (capturas 1280x720 a 2560x1440, texto 20 e máximo) fica como pendência de Unity.

**Na sequência:** destacar doadores compatíveis (#561), ordenar (#562), favoritar, lixo e ações em lote (#563), busca e filtros (#334), expansão em abas (#564), filtro de loot (#565), Ctrl+clique entre contêineres (#566).

### Catálogo de modelos-base (#330, confirmado pelo usuário em 2026-10-11)

As quatro famílias **Vigia/Bastião, Predador/Ruptor, Aurora/Tempestade e Batedor/Olho Longo** têm variantes A (atributo principal +6) e B (principal +4 e especialização), cada uma nas nove categorias de peça. São 72 modelos-base, sem contar armas. Os nomes, valores fixos e defesas por categoria vêm da seção 12 do [livro](../brainstorm/figjam.md#12--equipamentos-por-função-e-peça). As constantes/tabelas de balanceamento ficam em `CatalogoEquipamentos`.

Bastião acrescenta +8 à defesa-base da peça, Ruptor e Olho Longo acrescentam +10 de Vigor, Tempestade acrescenta +3 de Espírito. Defesa e especialização de defesa são somadas em um único atributo fixo. As peças de adorno chamadas Foco continuam na posição Adorno: o nome não as transforma em arma secundária.

Os IDs 101–109 do Vigia permanecem; Bastião usa 110–118, Predador 119–127, Ruptor 128–136, Aurora 137–145, Tempestade 146–154, Batedor 155–163 e Olho Longo 164–172. Cada faixa segue capacete, peito, luvas, calça, botas, cinto, anel, brinco e adorno. O catálogo devolve objetos novos; alterar um exemplar não altera modelos ou outros exemplares. Exemplares já persistidos conservam seus valores, sem regeneração nem migração.

O gerador aplica o fator Inicial/Médio/Forte **1/2/4 uma única vez** aos valores-base fixos e aleatórios; requisitos de personagem são 1/10/20. Raridade e nível da peça permanecem independentes do grau. Família e variante são gravadas junto aos metadados e apresentadas no inventário; itens antigos sem esses campos continuam válidos.

**No jogo:** I → Equipamentos → escolher o grau → Obter equipamento de teste (editor/build de desenvolvimento, servidor Development). O servidor sorteia uniformemente entre os 72 modelos e o martelo já existente, grava o exemplar e devolve a bolsa atualizada. O parâmetro opcional `?grau=0|1|2` da rota `/bau-teste` escolhe o grau; omitido mantém Inicial, inválido retorna 400 sem criar item. Bolsa cheia, autenticação, propriedade e transação seguem as regras anteriores. Peças médias/fortes exigem nível 10/20 para equipar.

O catálogo inicial permanece na região Vale; variações regionais, pools de skills (#340/#167) não são definidos aqui; a distribuição no loot dos mobs é a de [Loot dos inimigos](#loot-dos-inimigos-493). O baú original conserva a tabela dele.

### Inventário e baú de teste: definido (provisório, 2026-10-11)

Ficha aprovada em 2026-10-11. É um **recorte de teste** para ter o fluxo inventário + baú de ponta a ponta; **não** substitui as decisões de [Definido](#definido) nem responde as de [Em aberto](#em-aberto) (raridades, skills nos itens e fusão continuam como estão e ficam fora deste recorte).

- Catálogo fixo de **4 itens** de teste (2026-10-11), com ids estáveis: 1 Espada de teste, 2 Escudo de teste, 3 Poção de teste, 4 Moeda de teste.
- O inventário de cada personagem tem **40 espaços** (2026-10-11). Cada entrada ocupa um espaço; itens repetidos ocupam espaços separados (sem pilha).
- Abrir o baú dá **1 a 3 itens** (2026-10-11), sorteados **com reposição** e **uniformes** entre os 4 do catálogo. O sorteio é do servidor.
- O servidor só grava se **todos** os itens sorteados couberem nos 40 espaços. Se não couberem, responde `InventarioCheio` e **não grava nada** (nem parte dos itens).
- Itens deste recorte não têm raridade, skills nem fusão.
- Fora deste recorte: sorteio com raridade, drop de mobs, persistência definitiva, UI, prefab de baú, mundo.

### Proposta do board (2026-10-11, não validada)

Proposta do board em 2026-10-11: as partes ainda **não validadas pelo grupo** do [Livro de ideias](../brainstorm/figjam.md#asterra--livro-de-ideias-do-jogo) não substituem o [Definido](#definido). As confirmações de equipamentos e catálogo acima prevalecem sobre as propostas originais. Resumo:

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
- **[Mobs](mobs.md):** o evento de morte do inimigo, com quem participou e onde ele caiu (loot no chão).
- **[Mundo](mundo.md):** a posição do personagem, para conferir a distância da coleta, e o aviso de quem entrou na área.

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

### Loot dos inimigos (#493)

Matar um inimigo dá **peças reais** do catálogo de 72 (armaduras e acessórios), não mais os itens de teste. Os valores são **proposta a calibrar**, todos em `ConfigLoot` (shared); as contas puras ficam em `TabelaDeLoot`.

- **Quantidade:** continua a de `RegrasLoot` (50% nenhum, 40% um, 10% dois por participante). Cada item vira peça com `ConfigLoot.ChanceDeEquipamento` (hoje 100%); poção e moeda de teste **saíram da tabela** (seguem no baú de teste).
- **Qual peça:** uniforme entre as 72, sem armas (martelo e itens legados não caem).
- **Raridade pela origem do inimigo** (pesos relativos, Comum/Incomum/Raro/Épico/Lendário):

| Origem | Quando | Pesos |
|---|---|---|
| Comum | comum sem variante | 700 / 250 / 45 / 5 / 0 |
| Fortalecida (elite) | comum fortalecida | 400 / 400 / 170 / 28 / 2 |
| Chefe raro | categoria chefe raro | 100 / 400 / 380 / 100 / 20 |
| Chefe | categoria chefe (dungeon) | 0 / 300 / 450 / 200 / 50 |

- **Grau e requisito pelo nível do inimigo** (`InimigoDerrotado.Nivel`; 0 vale o da ficha da criatura): 1-9 Inicial (equipa no nível 1), 10-19 Médio (nível 10), 20+ Forte (nível 20). Os fatores 1/2/4 de atributos são os de [Equipamentos](#equipamentos-166-confirmação-do-usuário-em-2026-10-11).
- **Individual:** cada participante faz o próprio sorteio (peça, raridade e grau); a queda guarda a raridade e o grau até a coleta, e o exemplar é gerado ao entrar no inventário.
- **Aviso e chão:** `LootNoChao` traz `Raridade` e `Grau`, e o nome vem do catálogo de equipamentos. O aviso do HUD mostra nome e raridade (veja abaixo).
- **Meta de ritmo (X = 3 min):** jogador solo, 4 mobs/min, níveis 1 a 10: em média uma peça **melhor** que a equipada a cada 3 minutos, e no máximo 20 minutos sem nenhuma. "Melhor" é maior `TabelaDeLoot.Pontuacao` (soma dos atributos, +10% por degrau de raridade) na posição onde a peça cabe. Hoje a simulação mede ~1,4 min de média e ~15 min na pior espera (posições quase cheias perto do nível 10). O teste `RitmoDeLootTestes` falha se a tabela, a quantidade ou o ritmo saírem da meta.
- **Fora desta entrega:** `MultiplicadorDeRecompensa` não muda a quantidade (a variante já pesa na raridade); loot de chefe usa a mesma quantidade do comum, até o grupo definir a tabela do boss; skills nas peças (#340/#167); atributos elementais.

### Loot no chão (#64, contrato da #367)

Regras na [fatia jogável, seção 5](fatia-jogavel.md#5-loot-no-chão): loot **individual**, cada participante sorteia o próprio e só ele o vê. O servidor sorteia, guarda o dono e valida a coleta. Este é o contrato; o servidor (#368) e o client (#369) vêm depois. Tempo real, faixa 7000–7999:

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 7000 | `LootApareceu` | servidor → o dono | `Loot` (`LootNoChao`: `LootId`, `ItemId`, `Nome`, `Raridade`, `Grau`, `X`, `Z`, `ExpiraEmSegundos`) |
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
- **Qual item cai:** peças do catálogo de equipamentos, com raridade e grau (veja [Loot dos inimigos](#loot-dos-inimigos-493)). Sem skills.
- **Inventário cheio:** o item fica no chão até expirar; os que couberem entram.
- **Posição:** `X` e `Z` de onde o inimigo caiu. O servidor não conhece a altura do terreno; o client assenta o item no chão.
- **Quem volta ao jogo** recebe de novo `LootApareceu` dos itens que ainda estão no prazo, com o tempo que falta.
- **Dono do loot (#332), decidido em 2026-10-11 (/decida):** vale a [fatia](fatia-jogavel.md#5-loot-no-chão) (loot individual para quem participou); a proposta do livro (maior dano) **não** é adotada agora e fica registrada em [social.md](social.md). Deste contrato só vale "o dono". Coleta com F e recurso garantido ao primeiro golpe seguem como melhoria separada, sem mexer no dono.

**No servidor (#368):**

- Quem **participou** é quem o evento de morte de Mobs lista (`InimigoDerrotado.Participantes`: causou dano ao inimigo). Cada um tem um sorteio próprio; a variante e o nível do inimigo mudam a raridade e o grau (veja [Loot dos inimigos](#loot-dos-inimigos-493)).
- O chão fica **em memória**: se o servidor reinicia, o que estava no chão some (o inventário não, ele é do banco).
- A coleta confere, nesta ordem: o item existe e é do jogador (senão `LootNaoEncontrado`), está ao alcance com a posição que o Mundo aceitou (senão `LongeDemais`) e cabe no inventário (senão `InventarioCheio`). O item só sai do chão depois de gravado.
- Dois pedidos do mesmo item ao mesmo tempo: um coleta, o outro recebe `LootNaoEncontrado`.
- O item some até 1 s depois do prazo (`ConfigLootServidor.IntervaloDaVarreduraSegundos`); a coleta confere o prazo na hora.
- Se o banco falhar na coleta, o item volta para o chão e o jogador não recebe resposta: basta apertar F de novo.

**No client (#369):**

- As mensagens de loot são assinadas desde o início do jogo (não só com o mundo montado), para não perder o `LootApareceu` que chega logo ao voltar.
- Cada item do jogador no chão vira um marcador dourado girando, assentado no chão, que aparece a até 12 m (`RaioDeVisaoMetros`). O client tira o item quando o prazo vence, sem esperar o `LootSumiu`.
- Com algum item a até 3 m, aparece a dica **"F: pegar"**; F manda `ColetarLoot` com "tudo por perto". Com janela ou menu do Esc aberto, nem dica nem coleta.
- Avisos no HUD: "Você pegou <item> (<raridade>)." ao coletar (ex.: "Você pegou Elmo do Vigia (Raro)."); inventário cheio (amarelo), longe demais e nada para pegar viram texto, sem mostrar o código.

## Ícones de equipamento

Silhuetas brancas com alpha (128 px), um por `TipoEquipamento`, para a UI tingir pela raridade. Para achar o ícone, use `IconesEquipamento.Carregar(tipo)` (`Resources/Itens/Icones/<TipoEquipamento>`). Origem e crédito em [licencas.md](../arte/licencas.md).
