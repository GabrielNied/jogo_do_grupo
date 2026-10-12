// Chamado pelo .husky/pre-push. Sem gh, o push segue: o CI e a main protegida continuam.

import { execFileSync } from "node:child_process";
import { mensagemPushMergeado } from "./push-mergeado.mjs";

const branch = process.argv[2];
if (!branch || branch === "main") process.exit(0);

let numero = null;
try {
  const raw = execFileSync(
    "gh",
    ["pr", "list", "--state", "merged", "--head", branch, "--json", "number", "--limit", "5"],
    { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"], windowsHide: true },
  );
  numero = JSON.parse(raw || "[]")[0]?.number ?? null;
} catch {
  console.error(`ℹ Não conferi se "${branch}" já foi mergeada (gh indisponível). O push segue.`);
  process.exit(0);
}

const mensagem = mensagemPushMergeado(branch, numero);
if (mensagem) {
  console.error(mensagem);
  process.exit(1);
}
