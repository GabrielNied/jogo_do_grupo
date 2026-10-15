// Baixa as imagens externas da biblioteca de referências das URLs oficiais do manifesto
// (elas não vão para o git: o repositório é público, ADR 0038). Pula o que já está no disco
// com as dimensões certas. Uso: npm run arte:baixar-referencias [-- --forcar]
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { lerImagem } from "./conferir-referencias.mjs";

const RAIZ = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const MANIFESTO = "docs/arte/referencias-visuais/manifesto-referencias.json";
const PAUSA_MS = 300;

const confere = (buf, e) => {
  const img = buf && lerImagem(buf);
  return img && img.format === e.format && img.width === e.width && img.height === e.height;
};

const forcar = process.argv.includes("--forcar");
const { imagens } = JSON.parse(fs.readFileSync(path.join(RAIZ, MANIFESTO), "utf8"));
const falhas = [];
let baixadas = 0;
let prontas = 0;
for (const e of imagens.filter((i) => i.category !== "assets-do-projeto")) {
  const destino = path.join(RAIZ, e.relative_path);
  if (!forcar && fs.existsSync(destino) && confere(fs.readFileSync(destino), e)) {
    prontas++;
    continue;
  }
  try {
    const resp = await fetch(e.source_url);
    if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
    const buf = Buffer.from(await resp.arrayBuffer());
    if (!confere(buf, e)) throw new Error("não é a imagem do manifesto (formato ou dimensões diferentes)");
    fs.mkdirSync(path.dirname(destino), { recursive: true });
    fs.writeFileSync(destino, buf);
    baixadas++;
  } catch (erro) {
    falhas.push(`${e.id}: ${erro.message} (${e.source_url})`);
  }
  await new Promise((r) => setTimeout(r, PAUSA_MS));
}
console.log(`✔ ${baixadas} baixadas, ${prontas} já estavam no disco.`);
if (falhas.length) {
  console.error(
    `✖ ${falhas.length} falharam (a fonte pode ter trocado a imagem; substitua pelo fluxo de instrucoes-agentes.md):\n- ${falhas.join("\n- ")}`
  );
  process.exit(1);
}
