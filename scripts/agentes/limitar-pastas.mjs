// Hook PreToolUse (Edit/Write) dos agentes: bloqueia edição fora das pastas do agente.
// Uso no frontmatter do agente: node scripts/agentes/limitar-pastas.mjs <pasta/> [<pasta/> ...]
// Recebe o JSON do hook no stdin. Sai com código 2 para bloquear (a mensagem vai para o agente).
// Arquivos fora do repositório (ex.: rascunhos temporários) são liberados.

import { readFileSync } from "node:fs";
import { isAbsolute, relative, resolve } from "node:path";

// --sem-yaml-unity: também bloqueia cenas, prefabs, assets e .meta (regra 9 do AGENTS.md).
const semYamlUnity = process.argv.includes("--sem-yaml-unity");
const permitidas = process.argv
  .slice(2)
  .filter((a) => !a.startsWith("--"))
  .map((p) => p.replace(/\\/g, "/"));
const entrada = JSON.parse(readFileSync(0, "utf8") || "{}");
const arquivo = entrada.tool_input?.file_path ?? entrada.tool_input?.notebook_path;
if (!arquivo) process.exit(0);

const raiz = process.env.CLAUDE_PROJECT_DIR || entrada.cwd || process.cwd();
const absoluto = isAbsolute(arquivo) ? arquivo : resolve(raiz, arquivo);
const caminho = relative(raiz, absoluto).replace(/\\/g, "/");

if (caminho.startsWith("../") || isAbsolute(caminho)) process.exit(0);
if (semYamlUnity && /\.(unity|prefab|asset|meta|mat|controller|anim|overrideController)$/i.test(caminho)) {
  console.error(
    `Bloqueado: "${caminho}" é um arquivo do editor do Unity (cena, prefab, asset ou .meta). ` +
      "Não se escreve o YAML do Unity à mão: crie ou altere pelo próprio Unity, com um script de editor " +
      "(npm run unity:executar -- <Classe.Metodo>) ou o Unity CLI no editor aberto.",
  );
  process.exit(2);
}
if (permitidas.some((p) => caminho === p.replace(/\/$/, "") || caminho.startsWith(p))) process.exit(0);

console.error(
  `Bloqueado: "${caminho}" está fora das pastas deste agente (${permitidas.join(", ")}). ` +
    "Se a tarefa precisa mexer aí, pare e descreva a mudança no seu relatório para o agente " +
    "responsável (veja docs/agentes.md).",
);
process.exit(2);
