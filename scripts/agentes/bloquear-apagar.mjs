// Hook PreToolUse (Bash/PowerShell) do projeto: aplica a regra 0 do AGENTS.md.
// A IA nunca apaga o que as pessoas criaram; comandos que apagam arquivos, descartam
// trabalho ou reescrevem histórico são bloqueados (código 2, a mensagem vai para a IA).
// Se algo precisar mesmo sumir, a IA explica e a pessoa roda o comando.
// Worktrees e branches temporários de agentes saem pelo `npm run limpar-agentes`, que só
// remove o que já está na main (ADR 0018); por isso os comandos crus continuam bloqueados.

import { readFileSync } from "node:fs";

const entrada = JSON.parse(readFileSync(0, "utf8") || "{}");
const comando = entrada.tool_input?.command ?? entrada.toolCall?.args?.CommandLine ?? "";
const nomeFerramentaCodex = entrada.tool_name ?? "";
const ehAntigravity = entrada.toolCall?.name === "run_command";

function bloquear(mensagem) {
  if (ehAntigravity) {
    process.stdout.write(JSON.stringify({ decision: "deny", reason: mensagem }));
    process.exit(0);
  }

  console.error(mensagem);
  process.exit(2);
}

// No Codex, apply_patch chega ao hook como uma única string em `command`.
// Detectamos somente a operação explícita de remover arquivo; remover linhas
// dentro de um arquivo continua sendo uma edição permitida pela regra 0.
if (nomeFerramentaCodex === "apply_patch" && /^\*\*\* Delete File:/m.test(comando)) {
  bloquear(
    "Bloqueado pela regra 0 do AGENTS.md (apagar arquivo): a IA nunca apaga o que " +
      "as pessoas criaram. Ajuste ou marque como obsoleto. Se algo precisar mesmo " +
      "ser apagado, explique à pessoa e deixe que ela rode o comando.",
  );
}
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

const ehLimpezaDeAgente = new Set(["apagar branch remota", "apagar branch", "git worktree remove"]);

for (const [padrao, nome] of proibidos) {
  if (padrao.test(comando)) {
    const dica = ehLimpezaDeAgente.has(nome)
      ? " Para worktrees e branches temporários de agentes, use `npm run limpar-agentes -- --aplicar`: " +
        "ele só remove o que já entrou na main."
      : "";
    bloquear(
      `Bloqueado pela regra 0 do AGENTS.md (${nome}): a IA nunca apaga o que as pessoas criaram ` +
        "nem descarta trabalho. Ajuste ou marque como obsoleto. Se algo precisar mesmo ser apagado, " +
        "explique à pessoa e deixe que ela rode o comando." +
        dica,
    );
  }
}
