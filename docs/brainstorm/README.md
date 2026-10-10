# Brainstorm

As ideias do grupo nascem no board do FigJam **"Jogo do Grupo - Definição de Features"**. O arquivo [figjam.md](figjam.md) é uma cópia do board em texto, para que todo mundo (e qualquer IA) leia as ideias sem acessar o Figma.

```
FigJam (ideias livres) ──export──► figjam.md ──pauta──► validacao.md ──decisão──► docs/features/<x>.md
```

- [figjam.md](figjam.md): cópia do board (gerada).
- [validacao.md](validacao.md): pauta de validação, com as perguntas que o grupo precisa decidir a partir das ideias do board.

- **`figjam.md` é gerado:** não edite à mão. Para mudar uma ideia, mude no FigJam e exporte de novo.
- **Ideias do brainstorm não são decisões.** Só viram regra quando o grupo valida e alguém leva para o `docs/features/<x>.md` da funcionalidade (em "Definido" ou "Em aberto").
- O histórico do git guarda as versões anteriores do `figjam.md`, então dá para ver o que mudou no board entre uma exportação e outra (`git log -p docs/brainstorm/figjam.md`).

## Atualizando o figjam.md

Quando o board mudar, rode na raiz do repo:

```
npm run figma:exportar
```

Depois abra um PR com o arquivo atualizado (`docs(core): atualiza export do brainstorm`).

### Token da Figma (uma vez)

O script usa a API REST da Figma com um token pessoal:

1. No Figma: **Settings > Security > Personal access tokens > Generate new token**.
2. Permissão: **File content → Read-only** (`file_content:read`). Nenhuma outra é necessária.
3. Na raiz do repo, crie um arquivo `.env` (já está no `.gitignore`, nunca vai para o git) com:
   ```
   FIGMA_TOKEN=figd_seu_token_aqui
   ```

Nunca cole o token em issues, PRs, chats de IA ou no código.

### Limites da Figma

O board está num time do plano **Starter** (gratuito). Os limites dependem do assento de quem roda o export no time:

| Assento no time do board | Limite para ler o arquivo |
|---|---|
| **Full** ou **Dev** | 10 leituras por minuto (na prática, sem limite para este uso) |
| View ou Collab | 6 leituras por mês |

Por isso o export é feito por quem tem assento Full (hoje, o responsável pelo core). Quem tem assento View/Collab lê o `figjam.md` do repo.

O conector de Figma das IAs (MCP) tem limite parecido no plano Starter, então **não use a IA para ler o board direto**: leia o `figjam.md`.

## Sem token?

Dá para atualizar sem API: no FigJam, exporte o board em PDF (menu principal > Export > PDF) e anexe o PDF na conversa com a IA ou numa issue. É manual, mas não gasta nenhuma cota.
