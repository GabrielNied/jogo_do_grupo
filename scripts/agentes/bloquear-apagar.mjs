// Hook PreToolUse (Bash/PowerShell) do projeto: aplica a regra 0 do AGENTS.md.
// A IA nunca apaga o que as pessoas criaram; comandos que apagam arquivos, descartam
// trabalho ou reescrevem histórico são bloqueados (código 2, a mensagem vai para a IA).
// Se algo precisar mesmo sumir, a IA explica e a pessoa roda o comando.

import { readFileSync } from "node:fs";

const entrada = JSON.parse(readFileSync(0, "utf8") || "{}");
const comando = entrada.tool_input?.command ?? "";

// Início de um comando: começo da linha ou depois de ; & | ( ou quebra de linha.
const ini = String.raw`(?:^|[;&|(\n]\s*)`;

const proibidos = [
  [new RegExp(`${ini}(?:sudo\\s+)?rm(?:dir)?\\s`), "rm/rmdir"],
  [new RegExp(`${ini}(?:del|erase|rd)\\s`, "i"), "del/rd"],
  [/\bRemove-Item\b|\bri\s+-/i, "Remove-Item"],
  [/\bgit\s+reset\s+.*--hard\b/, "git reset --hard"],
  [/\bgit\s+clean\b/, "git clean"],
  [/\bgit\s+checkout\s+(?:\S+\s+)?--\s/, "git checkout -- <arquivo>"],
  [/\bgit\s+restore\b(?!.*--staged)/, "git restore (sem --staged)"],
  [/\bgit\s+stash\s+(?:drop|clear)\b/, "git stash drop/clear"],
  [/\bgit\s+push\b.*(?:\s--force(?:-with-lease)?\b|\s-f\b|\s\+\S)/, "git push --force"],
  [/\bgit\s+push\b.*(?:\s--delete\b|\s-d\b|\s:\S)/, "apagar branch remota"],
  [/\bgit\s+branch\s+.*(?:-D\b|-d\b|--delete\b)/, "apagar branch"],
  [/\bgit\s+worktree\s+remove\b/, "git worktree remove"],
  [/\bgh\s+(?:issue|pr)\s+(?:close|delete)\b/, "fechar/apagar issue ou PR"],
  [/\bgh\s+(?:repo|release|project)\s+delete\b/, "apagar no GitHub"],
];

for (const [padrao, nome] of proibidos) {
  if (padrao.test(comando)) {
    console.error(
      `Bloqueado pela regra 0 do AGENTS.md (${nome}): a IA nunca apaga o que as pessoas criaram ` +
        "nem descarta trabalho. Ajuste ou marque como obsoleto. Se algo precisar mesmo ser apagado, " +
        "explique à pessoa e deixe que ela rode o comando.",
    );
    process.exit(2);
  }
}
