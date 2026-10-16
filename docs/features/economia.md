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
  - **Redistribuir a árvore** custa coroas, escalando com o nível ([talentos](talentos.md#pendente)).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3):
  - **Baú compartilhado:** só **banco pessoal entre personagens da conta** (já definido) e **banco de guilda** ([#338](https://github.com/GabrielNied/jogo_do_grupo/issues/338) refocada); **sem baú de party**.
  - **Banco de guilda:** todos **depositam**; só **líder e oficial retiram** (membro e recruta só depositam).
  - **Leilão (#176):** abre **interagindo com o NPC leiloeiro** na cidade (não de qualquer lugar).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 4):
  - **Reparo de durabilidade:** sink **leve** (referência New World): reparo barato, desgaste lento; quebrado tira atributos mas é raro numa sessão típica da fatia. Números finos em constante nomeada (agente economia, [#173](https://github.com/GabrielNied/jogo_do_grupo/issues/173)).
  - **Loja de NPC (#173):** o vendedor **compra qualquer item** do inventário por preço **baixo fixo por tier** (sink de lixo); também vende. Preços em `ConfigEconomia`.

## Em aberto

- [x] Haverá moeda premium? Decidido em 2026-10-16: não.
- [x] Lojas de jogadores? Decidido em 2026-10-16: barraca presencial e vendedor offline, espelhados no leilão (veja Definido).
- [x] Carteira e custo inicial da infusão entregues na #174; valores e configuração em [Itens — Infusão](itens.md#infusão-de-equipamentos-174).
- [x] Desde 2026-10-12 a fusão cobra coroas **por nível** (+1 a +10) e, de +6 em diante, materiais de dungeon e de raid; custos iniciais em [Itens — Fusão](itens.md#fusão) (#616, #617). O "ouro" do [documento mestre](../brainstorm/documento-mestre.md) são as coroas. Itens não são desmontados (DEC-005): a economia não conta com materiais vindos de desmontagem.
- [x] Custo em coroas do reparo e das taxas: política **leve** decidida em 2026-10-16 (quiz 4); valores a propor pelo agente economia em constante ([#173](https://github.com/GabrielNied/jogo_do_grupo/issues/173)).

## Regras

### Carteira de coroas para infusão (#174)

Saldo persistente inteiro por personagem, inicialmente zero. `Contratos/Economia/ICarteiraCoroas` publica consulta, crédito e débito; operações de escrita exigem a transação do chamador e valores positivos. Débito confere saldo no banco, sem permitir negativo; crédito verifica overflow. A exclusão do personagem remove sua carteira. A infusão usa esse contrato na mesma transação dos equipamentos, conforme o [ADR 0032](../decisoes/0032-infusao-e-cobranca-atomicas.md).

O crédito de teste é acessível pela bancada de Itens somente em Development. Ganho normal de coroas, loja e demais saídas seguem na #173.

### Proposta do board (2026-10-11, não validada)

Proposta do board em 2026-10-11, **não validada pelo grupo** (seções 07, 08 e 03 do [Livro de ideias](../brainstorm/figjam.md#asterra--livro-de-ideias-do-jogo)):

- **Cidade pronta (Brasa Mansa):** serviços já construídos: forja, cozinha, banco, mercado, vendedores. Construção de bases é "em breve", fora do MVP. NPCs e mapa: veja as docs de mundo e npcs.
- **Banco pessoal:** armazenamento próprio na cidade; item depositado não fornece skills (só o equipado entra na preparação).
- ~~**Baú compartilhado:** grupo ou guilda compra acesso~~ Substituído em 2026-10-16 (quiz 3): só **guilda** (party saiu); banco pessoal por conta continua em Definido.
- **Mercado:** ~~listagem simples a preço fixo, compra direta~~ (substituído em 2026-10-11 pelo leilão de ordens, veja Definido); filtros; o equipamento mantém atributos, raridade, skills e infusões. Troca direta mostra os dois lados antes de confirmar.
- **Saídas de moeda:** o board não define custos; vale o Definido (custo em coroas na infusão, reparo, taxas).
- **Expansão futura:** contratos de coleta (depósito do contratante; vencido, devolve) e Caravana da Forja (carga fora do inventário, rota segura ou contestada com +50% e aviso de PvP). Vêm depois da troca direta e do leilão.

## Depende de

## Expõe
