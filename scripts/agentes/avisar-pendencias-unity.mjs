// Hook de início de sessão (Claude Code): numa máquina com Unity, avisa o agente das
// pendências de Unity abertas, deixadas por agentes que trabalharam sem Unity (ADR 0017).
// Sem Unity, sem gh ou sem rede, não diz nada e sai 0: nunca atrapalha a sessão.

import { execFileSync } from "node:child_process";
import { LABEL_PENDENCIA } from "../git/pendencia-unity.mjs";
import { temUnityNaMaquina } from "../unity/sem-unity.mjs";

if (!temUnityNaMaquina()) process.exit(0);

let pendencias = [];
try {
  const saida = execFileSync(
    "gh",
    ["api", `repos/{owner}/{repo}/issues?state=open&labels=${LABEL_PENDENCIA}&per_page=20`],
    { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"], timeout: 8000 },
  );
  pendencias = JSON.parse(saida).filter((issue) => !issue.pull_request);
} catch {
  process.exit(0);
}
if (pendencias.length === 0) process.exit(0);

console.log(`Esta máquina tem Unity e há ${pendencias.length} pendência(s) de Unity deixadas por agentes sem Unity:`);
for (const issue of pendencias) {
  const andamento = issue.labels.some((l) => l.name === "em-andamento") ? " (em andamento)" : "";
  console.log(`- #${issue.number} ${issue.title}${andamento}`);
}
console.log(
  "Se a pessoa pedir, ou se for a tarefa desta sessão, puxe com `npm run tarefa -- puxar <N>` e siga os passos da issue. Não puxe sem ser pedido; só mencione que existem.",
);
