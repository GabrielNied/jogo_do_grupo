# Personagem

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 3000–3999

## Resumo

Raças, atributos e progressão de nível. Sem classes: o estilo de jogo vem dos itens equipados.

## Definido

- Raças com características únicas; cada raça dá uma **skill base** própria
- **Sem classes** (D2 da [validação de 2026-10-10](../brainstorm/validacao.md)): a combinação de itens define o estilo de jogo
- Progressão por nível (leveling) faz parte do loop principal (D5)
- ~~Classes diferenciadas~~ substituído em 2026-10-10 por "sem classes" (D2)

### Criação e seleção de personagem: definido (provisório)

Decidido pelo grupo em 2026-10-10. É **provisório**: serve para implementar a primeira versão e o grupo pode ajustar depois. Os detalhes estão em [Regras](#regras).

- A seleção de personagem exige estar **logado** (nome de usuário + senha). Conta e login são infraestrutura do Core, não desta funcionalidade.
- Até **4 personagens por conta**.
- Nome do personagem: **3 a 16 letras**, só letras de A a Z, sem acento, espaço ou número; **único** no servidor sem diferenciar maiúsculas.
- **Uma raça provisória: Humano**, até o grupo definir as raças definitivas.
- O personagem começa no **nível 1**.
- Fluxo de entrada: abertura → login (ou registro) → seleção de personagem → criação → volta para a seleção. "Entrar" ainda não leva a um mundo.
- Comunicação pela API HTTP do [ADR 0009](../decisoes/0009-api-http-para-conta-e-personagens.md) (veja [Expõe](#expõe)).
- O **servidor decide**: valida o nome, a unicidade, a raça e o limite de 4. O client só confere o formato do nome para dar retorno rápido.

## Em aberto

- [ ] Quais raças existem e qual a skill base de cada uma? (as raças definitivas substituem a raça provisória Humano)
- [ ] ~~Quais raças e classes existem?~~ classes não existem mais (D2)
- [ ] O que o nível dá ao personagem (atributos, espaços de skill, acesso a itens)?
- [ ] Nível máximo e curva de progressão
- [ ] Aparência e customização na criação (rosto, corpo, cores...)?
- [ ] Excluir personagem: pode? Tem confirmação (ex.: digitar o nome) ou tempo de espera antes de apagar de vez?
- [ ] Renomear personagem: pode? Tem custo ou limite?
- [ ] Aceitar letras acentuadas no nome (ç, ã, é...) no futuro? Hoje o grupo decidiu só A–Z (veja [Regras](#regras))

## Regras

**Base:** World of Warcraft (tela de seleção de personagem). **Igual:** a conta tem uma lista de personagens; a tela mostra a lista, um botão para criar e um para entrar; a criação pede nome e raça. **Diferente:** não se escolhe classe (D2); por enquanto só existe uma raça e não há customização de aparência; o limite é bem menor (4 por conta).

### Nome do personagem

| Regra | Valor | Quem confere |
|---|---|---|
| Tamanho | 3 a 16 caracteres | client (retorno rápido) e servidor (decide) |
| Caracteres | só letras de A a Z, maiúsculas ou minúsculas; nada de espaço, número, acento ou símbolo | client e servidor |
| Unicidade | único em todo o servidor, sem diferenciar maiúsculas ("Ana", "ana" e "ANA" são o mesmo nome) | só o servidor |

- O nome é guardado como a pessoa digitou (mantém as maiúsculas); só a comparação de unicidade ignora maiúsculas.
- O servidor não corrige o nome (não tira espaços, não troca letras): se não estiver no formato, recusa.
- Client e servidor usam **o mesmo validador do shared**, `RegrasPersonagem.ValidarNome`, para tamanho e caracteres. Assim os dois nunca discordam sobre o formato.
- Letras acentuadas não são aceitas (decisão do grupo). Aceitar no futuro é uma possível mudança, em [Em aberto](#em-aberto).
- A unicidade vale **mesmo com pedidos simultâneos** (duas pessoas criando "Ana" ao mesmo tempo): o servidor garante com um índice único no banco sobre o nome normalizado (em minúsculas). Se o índice recusar, a resposta é "nome em uso".

### Raça

- Única raça aceita por enquanto: **Humano**. Qualquer outro valor é recusado pelo servidor.
- Na tela de criação, Humano já vem escolhido (é a única opção).
- A skill base da raça ainda não existe (depende de [Skills](skills.md) e das raças definitivas).

### Limite por conta

- No máximo **4 personagens por conta**. O pedido de criar um 5º é recusado pelo servidor.
- O client pode desativar o botão Criar quando a conta já tem 4, mas quem garante o limite é o servidor.
- O limite vale **mesmo com pedidos simultâneos** (dois cliques em Confirmar, duas janelas abertas): o servidor conta os personagens da conta e cria o novo **dentro da mesma transação**, para nunca passar de 4.

### Personagem novo

- Começa no **nível 1**.
- Pertence à conta logada. O servidor descobre a conta pelo token (JWT), **nunca** por um campo enviado pelo client.

### Fluxo de entrada no jogo

Ordem das telas: abertura/carregamento → login (ou registro) → seleção de personagem ⇄ criação de personagem.

| Tela | Dona |
|---|---|
| Abertura/carregamento | Core |
| Login ou registro de conta | Core |
| Seleção de personagem | Personagem |
| Criação de personagem | Personagem |

1. **Abertura/carregamento:** tela inicial do jogo (Core).
2. **Login ou registro de conta:** nome de usuário + senha (Core). Ao logar, o client recebe o token (JWT) usado nas chamadas abaixo.
3. **Seleção de personagem:**
   - ao abrir, pede a lista de personagens da conta ao servidor (`GET`);
   - se o pedido falhar por **não autenticado** (401), volta para o login;
   - se falhar por **qualquer outro motivo** (ex.: servidor fora do ar, sem internet), mostra um erro genérico ("Não foi possível carregar os personagens.") com o botão **Tentar de novo**, que repete o pedido;
   - mostra cada personagem com **nome, raça e nível**, em **ordem de criação** (Id crescente: o mais antigo primeiro);
   - ao abrir a tela, vem selecionado o **primeiro personagem da lista**; se a lista estiver vazia, nenhum fica selecionado e só aparece o botão Criar;
   - botão **Criar**: abre a tela de criação;
   - botão **Entrar**: só funciona com um personagem selecionado. **Por enquanto não leva a um mundo** (ainda não existe): mostra a mensagem "O jogo ainda não foi implementado." e continua na seleção. Não chama o servidor.
4. **Criação de personagem:**
   - campo de **nome** e escolha de **raça** (só Humano);
   - enquanto a pessoa digita, o client confere o formato do nome com `RegrasPersonagem.ValidarNome` e avisa se estiver errado, sem chamar o servidor;
   - botão **Confirmar**: envia nome e raça ao servidor;
     - **deu certo:** volta para a seleção, com a lista atualizada e o personagem novo selecionado;
     - **foi recusado:** continua na criação e mostra o motivo (veja abaixo);
   - botão **Voltar**: volta para a seleção sem criar.

### Motivos de recusa na criação

O servidor recusa e diz o motivo, para o client mostrar uma mensagem clara:

| Motivo | Quando | Mensagem sugerida no client |
|---|---|---|
| Nome inválido | fora do formato (tamanho ou caracteres) | "O nome deve ter de 3 a 16 letras (A–Z, sem acento, espaço ou número)." |
| Nome em uso | já existe personagem com esse nome, sem diferenciar maiúsculas | "Esse nome já está em uso." |
| Raça inválida | raça diferente de Humano | "Raça inválida." |
| Limite atingido | a conta já tem 4 personagens | "Você já tem o máximo de 4 personagens." |
| Não autenticado | sem token ou token inválido/expirado | volta para o login (Core) |

O formato exato dessas respostas (códigos e campos) é definido no contrato do shared (veja [Expõe](#expõe)).

## Depende de

- **Core:** conta, login/registro e emissão do token (JWT). Personagem só lê qual é a conta a partir do token.
- **[Skills](skills.md):** a skill base de cada raça (quando as raças definitivas existirem).

## Expõe

API HTTP para listar e criar personagens (por que HTTP: [ADR 0009](../decisoes/0009-api-http-para-conta-e-personagens.md)). Todas as chamadas exigem o token (JWT) do login; sem ele, o servidor responde 401. Os tipos ficam em [shared/Package/Runtime/Features/Personagem/](../../shared/Package/Runtime/Features/Personagem/), usados pelo servidor e pelo client; o contrato completo entra pelo PR #14 e é a fonte dos nomes e tipos exatos.

| Rota | O que faz | Envia | Recebe |
|---|---|---|---|
| `GET /api/personagens` | Lista os personagens da conta logada, em ordem de criação | nada (a conta vem do token) | `ListaPersonagensResponse`: a lista de `PersonagemResumo` |
| `POST /api/personagens` | Cria um personagem na conta logada | `CriarPersonagemRequest`: Nome e Raca | deu certo: **201** com o `PersonagemResumo` do personagem criado (já com o Id); recusado: o motivo (veja [Motivos de recusa](#motivos-de-recusa-na-criação)) |

- **`PersonagemResumo`:** o que a seleção mostra de cada personagem: Id, Nome, Raca e Nivel.
- **`CriarPersonagemRequest`:** o que a criação envia: Nome e Raca.
- **`Raca`:** enum do shared (hoje só `Humano`). No JSON trafega como **número**, não como texto.
- **`RegrasPersonagem.ValidarNome`:** validador do formato do nome, usado pelo client e pelo servidor.
- **`ListaPersonagensResponse`:** a lista de personagens da conta (de 0 a 4).
- Por serem HTTP, essas rotas não usam IDs de mensagem da faixa 3000–3999.
