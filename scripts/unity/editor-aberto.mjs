// Diz se o editor do Unity está com um projeto aberto.
// Ignora Hub, licença, Package Manager e workers: eles não travam o projeto.
//
// Uso: npm run unity:editor
// Sai 0 se o editor está fechado (dá para importar). Sai 1 se está aberto.

import { execFileSync } from "node:child_process";
import { pathToFileURL } from "node:url";

export function editorDoProjetoAberto() {
  if (process.platform !== "win32") return [];
  const saida = execFileSync(
    "powershell",
    [
      "-NoProfile",
      "-Command",
      "Get-CimInstance Win32_Process -Filter \"Name = 'Unity.exe'\" | ForEach-Object { $_.ProcessId.ToString() + '|' + $_.ExecutablePath }",
    ],
    { encoding: "utf8" },
  );
  return saida
    .split(/\r?\n/)
    .map((linha) => linha.trim())
    .filter((linha) => /\\Editor\\Unity\.exe$/i.test(linha.split("|")[1] ?? ""));
}

const direto = process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href;
if (direto) {
  const abertos = editorDoProjetoAberto();
  if (abertos.length === 0) {
    console.log("Editor do Unity fechado.");
    process.exit(0);
  }
  console.error("Editor do Unity aberto. Feche a janela do projeto antes de importar:");
  for (const linha of abertos) console.error(`  ${linha.replace("|", " ")}`);
  process.exit(1);
}
