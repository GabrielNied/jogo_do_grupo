# Economia

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 9000–9999

## Resumo

Moedas, mercado, comércio entre jogadores, saídas de recursos e taxas.

## Definido

- Moeda principal: **coroas** (decidido por Gabriel em 2026-10-11, a validar pelo grupo); as outras moedas e como se ganha seguem em aberto
- Mercado: casa de leilão e comércio entre jogadores. **Leilão com ordens de compra e venda**, não preço fixo (decidido por Gabriel em 2026-10-11, a validar pelo grupo; backlog #176). Vem depois de itens, loja de NPC (#173) e troca direta.
- Profissões dependem umas das outras
- Drops, crafting e fusão alimentam o mercado
- Saídas de recursos: custo de fusão, reparo, taxas
- Taxas e impostos: leilão, guilda, território
- **Decisões do Gabriel em 2026-10-16** (quiz de lacunas de MMO; números a propor):
  - **Entrada de coroas:** criaturas largam poucas (mais nos chefes), a loja de NPC compra itens por um valor baixo, e missões, diárias e semanais pagam valor fixo ([missões](missoes.md)).
  - **Saídas:** fusão, reparo da [durabilidade](itens.md#definido), taxas, correio e criação de guilda.
  - **Entre jogadores**, além do leilão: troca direta (os dois lados confirmam e o servidor troca de uma vez), correio (itens e coroas para quem está offline, com taxa), troca do loot da luta ([itens](itens.md#definido)) e banco pessoal na cidade, compartilhado entre os personagens da conta.
  - **Imposto de território:** só quando existirem os territórios de guilda ([pvp](pvp.md#definido)).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições; números a propor pelo agente economia, em constante nomeada):
  - **Sem moeda premium**, nunca: sem dinheiro real no jogo.
  - **Taxa do leilão:** porcentagem **só na venda** (proposta: 5%); anunciar é grátis.
  - **Barracas de jogador, ligadas ao leilão:** o leilão continua sendo o único livro de ordens, e as barracas trazem vida à cidade sem dividir o mercado.
    - **Barraca presencial:** o personagem monta a barraca numa praça da cidade e fica ali, visível e livre da desconexão por inatividade. As ofertas dele entram no leilão com o selo "na barraca de <nome>", e quem compra ali em pessoa paga taxa menor (proposta: 3%).
    - **Vendedor offline:** a pessoa contrata um manequim com a aparência do personagem, que fica na praça com ela deslogada e cobra uma **diária em coroas** (outra saída de moeda). As ofertas também entram no leilão.
    - Vem depois do leilão ([#176](https://github.com/GabrielNied/jogo_do_grupo/issues/176)).
  - **Redistribuir a árvore** custa coroas, escalando com o nível ([talentos](talentos.md#em-aberto)).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3):
  - **Baú compartilhado:** só **banco pessoal entre personagens da conta** (já definido) e **banco de guilda** ([#338](https://github.com/GabrielNied/jogo_do_grupo/issues/338) refocada); **sem baú de party**.
  - **Banco de guilda:** todos **depositam**; só **líder e oficial retiram** (membro e recruta só depositam).
  - **Leilão (#176):** abre **interagindo com o NPC leiloeiro** na cidade (não de qualquer lugar).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 4):
  - **Reparo de durabilidade:** sink **leve** (referência New World): reparo barato, desgaste lento; quebrado tira atributos mas é raro numa sessão típica da fatia. Números finos em constante nomeada (agente economia, [#173](https://github.com/GabrielNied/jogo_do_grupo/issues/173)).
  - **Loja de NPC (#173):** o vendedor **compra qualquer item** do inventário por preço **baixo fixo por tier** (sink de lixo); também vende. Preços em `ConfigEconomia`.
  - **Uma loja por categoria** (decidido por Gabriel em 2026-10-18): armeiro (armas), armadureiro (armaduras), taverneiro (comida e bebida) e merceeiro (consumíveis e materiais básicos) substituem o vendedor geral; cada um com catálogo próprio, e todos compram qualquer item pelo preço por tier ([npcs](npcs.md#elenco-da-brasa-mansa), [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066)).

## Em aberto

- [x] Haverá moeda premium? Decidido em 2026-10-16: não.
- [x] Lojas de jogadores? Decidido em 2026-10-16: barraca presencial e vendedor offline, espelhados no leilão (veja Definido).
- [x] Carteira e custo inicial da infusão entregues na #174; valores e configuração em [Itens — Infusão](itens.md#infusão-de-equipamentos-174).
- [x] Desde 2026-10-12 a fusão cobra coroas **por nível** (+1 a +10) e, de +6 em diante, materiais de dungeon e de raid; custos iniciais em [Itens — Fusão](itens.md#fusão) (#616, #617). O "ouro" do [documento mestre](../brainstorm/documento-mestre.md) são as coroas. Itens não são desmontados (DEC-005): a economia não conta com materiais vindos de desmontagem.
- [x] Custo em coroas do reparo e das taxas: política **leve** decidida em 2026-10-16 (quiz 4); valores e loja de NPC entregues na [#173](https://github.com/GabrielNied/jogo_do_grupo/issues/173) em `ConfigEconomia`.

## Regras

### Loja de NPC, Venda de Itens e Reparo de Durabilidade (#173, #1066)

Entregue na [#173](https://github.com/GabrielNied/jogo_do_grupo/issues/173) e expandido na [#1066](https://github.com/GabrielNied/jogo_do_grupo/issues/1066):
- **Carteira de coroas:** persistida na tabela `SaldosCoroas` por personagem. Contrato `ICarteiraCoroas` permite consulta, crédito e débito atômico.
- **Lojas de NPC por categoria (#1066):**
  - Quatro lojas especializadas substituem o vendedor geral:
    - **Armeiro (Gareth):** armas (`ConfigEconomia.CatalogoArmeiro`).
    - **Armadureiro (Bruna):** armaduras e itens defensivos (`ConfigEconomia.CatalogoArmadureiro`).
    - **Taverneiro (Barnabé):** comidas e bebidas (`ConfigEconomia.CatalogoTaverneiro`).
    - **Merceeiro (Tomas):** poções, consumíveis e materiais básicos (`ConfigEconomia.CatalogoMerceeiro`).
  - Todas compram qualquer item do inventário pelo preço fixo por tier (`ConfigEconomia.PrecoVendaPorTier`: D=2, C=5, B=10, A=25, S=60, S+=100 coroas).
  - Itens equipados ou marcados como favoritos não podem ser vendidos.
  - A tentativa de comprar um item fora do catálogo daquela loja é recusada pelo servidor (`CodigosErroEconomia.ItemNaoAVenda`).
  - Preço da compra é autoritativo pelo servidor (o client envia apenas o ID e a quantidade).
  - Proximidade com o NPC autoritativo de cada loja (`CatalogoNpcs`) é exigida tanto em tempo real quanto via REST (`CodigosErroEconomia.LongeDoNpc`).
- **Durabilidade e Reparo:**
  - Equipamentos possuem `DurabilidadeMaxima` (100) e `DurabilidadeAtual`.
  - Item com durabilidade zero é considerado `Quebrado` e deixa de fornecer atributos ou poderes (arma quebrada não concede ataque).
  - Ao cair em combate (`PersonagemCaiu`), os equipamentos vestidos sofrem desgaste leve (`ConfigEconomia.DesgasteAoCair = 5`).
  - O reparo no ferreiro custa `ConfigEconomia.CustoReparoPorPontoPerdido = 1` coroa por ponto de durabilidade restaurado.
- **Interface e interação:**
  - Compra e venda: interagir (**E**) com o comerciante correspondente na cidade (`ConfigNpcEconomia`, marcadores e corpos no client). Abre a `JanelaVendedor` com o título e catálogo da loja (`economia.armeiro`, `economia.armadureiro`, `economia.taverneiro`, `economia.merceeiro` ou `economia.vendedor`), sem tecla na barra de atalhos.
  - Reparo: interagir com o **ferreiro** (`JanelaFerreiro`, `economia.ferreiro`). O inventário continua sendo a forma de escolher o item a reparar.
  - O servidor recusa compra, venda, catálogo e reparo se o personagem estiver longe do NPC correspondente (`CodigosErroEconomia.LongeDoNpc`), pela posição aceita pelo Mundo.
- **Mensageria e Rotas:**
  - Faixa de IDs de tempo real: `9000–9999` (`EconomiaMessageIds`).
  - Mensagens: `ConsultarCatalogoLoja`, `RespostaCatalogoLoja`, `ComprarItemDaLoja` (agora com `CategoriaLoja` e `NpcId`).
  - Rotas HTTP REST em `RotasEconomia`: `/api/economia/saldo`, `/api/economia/loja/catalogo` (aceita `?categoria=...`), `/api/economia/loja/comprar`, `/api/economia/loja/vender`, `/api/economia/itens/reparar`.

### Carteira de coroas para infusão (#174)

Saldo persistente inteiro por personagem, inicialmente zero. `Contratos/Economia/ICarteiraCoroas` publica consulta, crédito e débito; operações de escrita exigem a transação do chamador e valores positivos. Débito confere saldo no banco, sem permitir negativo; crédito verifica overflow. A exclusão do personagem remove sua carteira. A infusão usa esse contrato na mesma transação dos equipamentos, conforme o [ADR 0032](../decisoes/0032-infusao-e-cobranca-atomicas.md).

O crédito de teste é acessível pela bancada de Itens somente em Development. Ganho normal de coroas, loja e demais saídas seguem na #173.

### Mercado (leilão por ordens, #176)

Feito em 2026-10-18. O leilão é **um livro de ordens só** (a barraca de jogador e o vendedor offline, [#905](https://github.com/GabrielNied/jogo_do_grupo/issues/905), vão espelhar nele). Abre com **E no NPC leiloeiro** da cidade (marcador em `ConfigMercado.LeiloeiroX/Z`; o servidor recusa a mais de 4 m). Constantes em `ConfigMercado` (shared); a regra vive em `ServicoDoMercado` (servidor).

| Constante | Valor | Observação |
|---|---|---|
| `TaxaVendaPercentual` | 5 | **só na venda**; **anunciar é grátis** ([decisão de 2026-10-16](#definido); a ficha original previa 1% ao abrir, que saiu) |
| `ExpiracaoDias` | 7 | a ordem vence; coroas e item voltam |
| `PrecoMinimo` / `PrecoMaximo` | 2 / 1.000.000.000 | por unidade; com o mínimo, a taxa arredondada para cima ainda deixa 1 coroa ao vendedor |
| `QuantidadeMaximaDaCompra` | 99 | unidades por ordem de compra |
| `OrdensAbertasMaximas` | 20 | por personagem (compra e venda juntas) |

- **Ordem de venda:** um exemplar da bolsa e o preço. O item sai da bolsa e **fica guardado na ordem** enquanto ela está aberta (cópia do exemplar, com os mesmos atributos; só entra o que a bolsa negocia: equipado e favorito não entram). **Ordem de compra:** um modelo, a quantidade e o **preço máximo por unidade**; as coroas de todas as unidades ficam **guardadas na ordem** (debitadas ao abrir).
- **Casamento automático pelo melhor preço:** a ordem nova casa com a melhor ordem oposta (a venda mais barata, ou a compra que paga mais; **empate: a mais antiga**), pelo **preço de quem já estava no livro**, e continua casando até acabar a quantidade ou os preços deixarem de se cruzar. O que sobra fica aberto. **Nunca casa com ordem do mesmo personagem.** Quem compra pagando menos que o máximo recebe a diferença de volta na hora.
- **Compra direta:** além do pedido, dá para comprar já uma ordem de venda específica (o equipamento tem atributos próprios), pelo preço dela.
- **Taxa:** `ceil(preço × 5%)` por venda, **uma vez só**, tirada do vendedor e **destruída** (saída de moeda). O comprador paga o preço cheio.
- **Bolsa cheia do comprador:** na compra direta a operação inteira é desfeita (`InventarioCheio`); no casamento automático, a ordem de quem está com a bolsa cheia é **pulada** e segue aberta.
- **Devolução (decisão de design, enquanto o correio [#212](https://github.com/GabrielNied/jogo_do_grupo/issues/212) não existe):** ao **cancelar** ou **vencer**, as coroas de uma compra voltam direto à carteira e o item de uma venda **volta direto à bolsa**. Se a bolsa está cheia, a venda fica cancelada ou vencida **com o item ainda guardado** ("aguardando devolução") e é entregue **no próximo login** ou no botão "Receber itens devolvidos". Nada se perde e nada se entrega duas vezes. **Para mudar para o correio:** trocar só `ServicoDoMercado.TentarDevolverAsync` (hoje põe na bolsa) por um envio de correio; o resto não muda.
- **Expiração:** uma varredura (`VarreduraDoMercado`, a cada 60 s) vence as ordens passadas da validade, e uma ordem vencida **nunca casa**, mesmo antes da varredura. O jogador online recebe um aviso quando a ordem vence ou casa.
- **Servidor autoritativo e atômico:** cada operação roda numa transação Serializable, uma de cada vez no processo; item e moeda nunca duplicam. Apagar um personagem apaga as ordens dele (e o que elas guardam), como apaga o inventário.
- **Contrato** (`shared/.../Features/Economia/MensagensDoMercado.cs`, faixa 9100–9199): `ConsultarMercado` 9100, `OferecerVenda` 9101, `OferecerCompra` 9102, `ComprarOrdem` 9103, `CancelarOrdem` 9104, `ReceberDevolucoes` 9105 (client → servidor); `EstadoDoMercado` 9110 (o livro, as minhas ordens, a bolsa e a lista de modelos, depois de cada pedido) e `AvisoDoMercado` 9111 (servidor → client). Recusas vêm em `EconomiaRecusada` com os códigos novos de `CodigosErroEconomia` (`preco_invalido`, `limite_de_ordens`, `ordem_indisponivel`...).
- **Na tela:** `JanelaLeilao` com três abas: **Comprar** (busca por nome, ordens à venda, pedidos abertos, formulário de pedido), **Vender** (escolher o item da bolsa e o preço, mostrando quanto o vendedor recebe) e **Minhas ordens** (cancelar, receber devoluções). As ordens de quem possui barraca ativa na praça ganham o selo `[na barraca de <nome>]`.
- **Testes:** casamento pelo melhor preço e por antiguidade, taxa uma vez, custódia e devolução, bolsa cheia, expiração, e concorrência (vários compradores para uma venda; vendas e compras ao mesmo tempo conservam itens e coroas) em `MercadoTestes`, `MercadoExpiracaoTestes` e `MercadoTempoRealTestes`; contas puras em `ConfigMercadoTestes`.

### Barracas de Jogador e Vendedor Offline (#905)

Entregue na [#905](https://github.com/GabrielNied/jogo_do_grupo/issues/905), conforme decisão de 2026-10-16:

| Constante | Valor | Observação |
|---|---|---|
| `TaxaVendaBarracaPercentual` | 3 | taxa presencial reduzida (no leilão geral é 5%) |
| `DiariaVendedorOfflineCoroas` | 50 | saída de coroas por diária (24 h) do manequim offline |
| `RaioPracaMetros` | 20 m | raio ao redor da praça central da cidade (`0, -10`) |
| `RaioInteracaoBarracaMetros` | 4 m | proximidade para interagir com a barraca |

- **Espelhamento no livro único do leilão:** o mercado continua tendo um único livro de ordens. Quando o vendedor tem uma barraca ativa, suas ofertas à venda no leilão recebem o selo `[na barraca de <nome>]`.
- **Taxa presencial reduzida (3%):** quem vai até a praça da cidade e compra diretamente na barraca (presencial ou do manequim) paga taxa reduzida de **3%** (arredondada para cima), beneficiando tanto quem compra quanto quem vende.
- **Barraca presencial:** montada na praça da cidade (`ConfigBarraca.DentroDaPraca`). Enquanto com a barraca ativa, o personagem fica visível e livre de desconexão por inatividade. Ao desconectar, a barraca presencial é desmontada.
- **Vendedor offline (manequim):** o jogador contrata um manequim com sua aparência por N diárias mediante cobrança atômica em coroas (`ICarteiraCoroas`). Permanece na praça atendendo compradores mesmo com a pessoa deslogada. A varredura periódica desativa manequins cujo prazo expirou.
- **Concorrência e atomicidade:** a compra presencial consome a ordem diretamente no `ServicoDoMercado` sob transação Serializable e trava de escrita (`Trava`), garantindo que uma ordem seja vendida exatamente uma vez, sem duplicação de itens ou saldo.
- **Mensagens (faixa 9120–9127):** `MontarBarraca`, `DesmontarBarraca`, `ContratarVendedorOffline`, `EncerrarVendedorOffline`, `ConsultarBarraca`, `DadosDaBarraca`, `BarracasNaArea` e `ComprarDaBarraca`.
- **Interface:** `JanelaBarraca` (F8) para gestão de barraca própria e exploração das barracas da praça; selo integrado na `JanelaLeilao`.

### Troca direta (#175)

Feita em 2026-10-18; regras, contrato e testes em [social](social.md#troca-direta-issue-175). A troca de dono do item e das coroas roda no servidor numa transação só (`ExecutorDaTroca`), e Social avisa Economia (`CoroasMovimentadas`) para o client ver o saldo novo.

### Banco de guilda (#338)

Entregue na [#338](https://github.com/GabrielNied/jogo_do_grupo/issues/338), conforme a decisão de 2026-10-16 (quiz de definições 3): baú compartilhado da guilda onde **todos os membros depositam** (líder, oficial, membro, recruta) e **apenas líder e oficial retiram** itens e coroas. Não há baú de party.

| Constante | Valor | Observação |
|---|---|---|
| `CasasPorAba` | 40 | quantidade de espaços em cada aba |
| `AbasDisponiveis` | 2 | abas disponíveis no cofre da guilda (total de 80 casas) |
| `CapacidadeMaxima` | 80 | limite máximo de itens armazenados |
| `BancoGuildaX` / `BancoGuildaZ` | 3 / -14 | localização do Cofre da Guilda em Brasa Mansa |
| `RaioInteracaoMetros` | 4 m | proximidade para interagir com o cofre |

- **Permissões por Cargo:**
  - `RegrasGuilda.PodeDepositarNoBanco(cargo)`: permitido para todos os membros da guilda.
  - `RegrasGuilda.PodeRetirarDoBanco(cargo)`: permitido exclusivamente para `CargoDaGuilda.Lider` e `CargoDaGuilda.Oficial`. Membro e recruta recebem recusa com `CodigosErroEconomia.SemPermissaoBancoGuilda`.
- **Atomicidade e Concorrência:**
  - Depósito e retirada operam sob transação Serializable isolada.
  - O item transita diretamente entre a bolsa do jogador e a tabela `ItensBancoGuilda` via `ICustodiaDeItens`: se a bolsa estiver cheia na tentativa de retirada, o servidor executa rollback e o item permanece preservado no cofre sem se perder.
  - Depósito e retirada de coroas são integrados à carteira (`ICarteiraCoroas`) e sincronizados com a tabela `SaldosBancoGuilda`.
- **Mensagens (faixa 9220–9229 em `EconomiaMessageIds`):**
  - `ConsultarBancoGuilda` (9220), `EstadoDoBancoGuilda` (9221), `DepositarItemBancoGuilda` (9222), `RetirarItemBancoGuilda` (9223), `DepositarCoroasBancoGuilda` (9224), `RetirarCoroasBancoGuilda` (9225) e `AvisoBancoGuilda` (9226).
- **Interface e Interação:**
  - Acessado pelo marcador "Cofre da Guilda" na cidade (`MarcadorNpcEconomia.BancoGuilda`). Abre a `JanelaBancoGuilda` exibindo saldo do cofre, lista de itens com autor do depósito e controles de retirada bloqueados para membros sem permissão.


### Proposta do board (2026-10-11, não validada)

Proposta do board em 2026-10-11, **não validada pelo grupo** (seções 07, 08 e 03 do [Livro de ideias](../brainstorm/figjam.md#asterra--livro-de-ideias-do-jogo)):

- **Cidade pronta (Brasa Mansa):** serviços já construídos: forja, cozinha, banco, mercado, vendedores. Construção de bases é "em breve", fora do MVP. NPCs e mapa: veja as docs de mundo e npcs.
- **Banco pessoal:** armazenamento próprio na cidade; item depositado não fornece skills (só o equipado entra na preparação).
- ~~**Baú compartilhado:** grupo ou guilda compra acesso~~ Substituído em 2026-10-16 (quiz 3): só **guilda** (party saiu); banco pessoal por conta continua em Definido.
- **Mercado:** ~~listagem simples a preço fixo, compra direta~~ (substituído em 2026-10-11 pelo leilão de ordens, veja Definido); filtros; o equipamento mantém atributos, raridade, skills e infusões. Troca direta mostra os dois lados antes de confirmar.
- **Saídas de moeda:** o board não define custos; vale o Definido (custo em coroas na infusão, reparo, taxas).
- **Expansão futura:** contratos de coleta (depósito do contratante; vencido, devolve) e Caravana da Forja (carga fora do inventário, rota segura ou contestada com +50% e aviso de PvP). Vêm depois da troca direta e do leilão.

## Depende de

- [itens](itens.md) para inventário e equipamentos

## Expõe

- `ICarteiraCoroas` em `server/src/JogoDoGrupo.Server/Contratos/Economia/`
- Catálogo e compra/venda de NPC em `RotasEconomia` e `EconomiaMessageIds`
