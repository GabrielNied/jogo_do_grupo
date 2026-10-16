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

## Em aberto

- [ ] Haverá moeda premium?
- [ ] Lojas de jogadores?
- [x] Carteira e custo inicial da infusão entregues na #174; valores e configuração em [Itens — Infusão](itens.md#infusão-de-equipamentos-174).
- [x] Desde 2026-10-12 a fusão cobra coroas **por nível** (+1 a +10) e, de +6 em diante, materiais de dungeon e de raid; custos iniciais em [Itens — Fusão](itens.md#fusão) (#616, #617). O "ouro" do [documento mestre](../brainstorm/documento-mestre.md) são as coroas. Itens não são desmontados (DEC-005): a economia não conta com materiais vindos de desmontagem.
- [ ] Custo em coroas do reparo e das taxas: a propor (agente economia).

## Regras

### Carteira de coroas para infusão (#174)

Saldo persistente inteiro por personagem, inicialmente zero. `Contratos/Economia/ICarteiraCoroas` publica consulta, crédito e débito; operações de escrita exigem a transação do chamador e valores positivos. Débito confere saldo no banco, sem permitir negativo; crédito verifica overflow. A exclusão do personagem remove sua carteira. A infusão usa esse contrato na mesma transação dos equipamentos, conforme o [ADR 0032](../decisoes/0032-infusao-e-cobranca-atomicas.md).

O crédito de teste é acessível pela bancada de Itens somente em Development. Ganho normal de coroas, loja e demais saídas seguem na #173.

### Proposta do board (2026-10-11, não validada)

Proposta do board em 2026-10-11, **não validada pelo grupo** (seções 07, 08 e 03 do [Livro de ideias](../brainstorm/figjam.md#asterra--livro-de-ideias-do-jogo)):

- **Cidade pronta (Brasa Mansa):** serviços já construídos: forja, cozinha, banco, mercado, vendedores. Construção de bases é "em breve", fora do MVP. NPCs e mapa: veja as docs de mundo e npcs.
- **Banco pessoal:** armazenamento próprio na cidade; item depositado não fornece skills (só o equipado entra na preparação).
- **Baú compartilhado:** grupo ou guilda compra acesso; permissões separadas de depositar e retirar (proposta).
- **Mercado:** ~~listagem simples a preço fixo, compra direta~~ (substituído em 2026-10-11 pelo leilão de ordens, veja Definido); filtros; o equipamento mantém atributos, raridade, skills e infusões. Troca direta mostra os dois lados antes de confirmar.
- **Saídas de moeda:** o board não define custos; vale o Definido (custo em coroas na infusão, reparo, taxas).
- **Expansão futura:** contratos de coleta (depósito do contratante; vencido, devolve) e Caravana da Forja (carga fora do inventário, rota segura ou contestada com +50% e aviso de PvP). Vêm depois da troca direta e do leilão.

## Depende de

## Expõe
