// Hook PreToolUse (Bash/PowerShell e Figma) do projeto: aplica a regra 0 do AGENTS.md.
// A regra 0 hoje protege só o Figma (ADR 0026): a IA pode apagar arquivos, pastas, assets,
// docs e branches. O hook barra apenas:
//   - qualquer remoção no Figma feita por script (use_figma com .remove() ou .delete());
//   - o que não volta e é do grupo todo: push forçado e apagar repositório, release ou projeto.
// Código 2: a mensagem vai para a IA, que explica à pessoa e deixa ela rodar o comando.
// Worktrees e branches temporários de agentes saem pelo `npm run limpar-agentes` (ADR 0018).

import { readFileSync } from "node:fs";

const entrada = JSON.parse(readFileSync(0, "utf8") || "{}");
const comando = entrada.tool_input?.command ?? entrada.toolCall?.args?.CommandLine ?? "";
const nomeFerramenta = entrada.tool_name ?? "";
const ehAntigravity = entrada.toolCall?.name === "run_command";

function bloquear(mensagem) {
  if (ehAntigravity) {
    process.stdout.write(JSON.stringify({ decision: "deny", reason: mensagem }));
    process.exit(0);
  }

  console.error(mensagem);
  process.exit(2);
}

// Figma: o script do use_figma roda a API de plugin do Figma; remover nó, página ou
// componente é apagar o trabalho do grupo. Ler e criar continuam livres.
if (/figma/i.test(nomeFerramenta)) {
  const texto = JSON.stringify(entrada.tool_input ?? {});
  if (/\.(?:remove|delete)\s*\(/.test(texto) || /\bdelete\w*Node|\bremoveChild\b/.test(texto)) {
    bloquear(
      "Bloqueado pela regra 0 do AGENTS.md (apagar no Figma): a IA nunca apaga nada do Figma. " +
        "Crie ou edite o que a tarefa pede; se algo precisar sumir, explique à pessoa e deixe " +
        "que ela apague no Figma.",
    );
  }
  process.exit(0);
}

// Início de um comando: começo da linha ou depois de ; & | ( ou quebra de linha.
const ini = String.raw`(?:^|[;&|(\n]\s*)`;

const proibidos = [
  [
    new RegExp(`${ini}git\\s+push\\b.*(?:\\s--force(?:-with-lease)?\\b|\\s-f\\b|\\s\\+\\S)`),
    "push forçado",
  ],
  [
    new RegExp(`${ini}gh\\s+(?:repo|release|project)\\s+delete\\b`),
    "apagar repositório, release ou projeto",
  ],
];

for (const [padrao, nome] of proibidos) {
  if (padrao.test(comando)) {
    bloquear(
      `Bloqueado pela regra 0 do AGENTS.md (${nome}): é uma ação que não volta e afeta o grupo todo. ` +
        "Explique à pessoa o que quer fazer e por quê, e deixe que ela rode o comando.",
    );
  }
}
