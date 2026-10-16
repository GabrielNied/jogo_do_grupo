// Prepara o Husky configurando os hooks para rodar tanto no Windows (PowerShell/cmd)
// quanto no Git Bash, Linux e macOS sem depender de sh no PATH global do Windows.
//
// Chamado pelo script "prepare" do package.json (ao rodar npm install).

import { existsSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import husky from "husky";
import { encontrarDiretoriosGit, raizDoGit } from "./localizar-sh.mjs";

/**
 * Ajusta os arquivos gerados pelo Husky em .husky/_ para suportar Windows PowerShell.
 */
export function ajustarShimsHusky(pastaHusky = ".husky") {
  const pastaInterna = join(pastaHusky, "_");
  if (!existsSync(pastaInterna)) return;

  const arquivos = readdirSync(pastaInterna);
  for (const arquivo of arquivos) {
    if (arquivo === ".gitignore") continue;
    const caminho = join(pastaInterna, arquivo);
    let conteudo = readFileSync(caminho, "utf8");

    // No Windows, Git for Windows traduz #!/bin/sh diretamente para sh.exe,
    // enquanto #!/usr/bin/env sh depende de sh estar no PATH do Windows e falha no PowerShell.
    if (conteudo.startsWith("#!/usr/bin/env sh")) {
      conteudo = "#!/bin/sh" + conteudo.slice("#!/usr/bin/env sh".length);
    }

    if (arquivo === "h") {
      // Injeta localização dinâmica do sh e utilitários POSIX se não estiverem no PATH
      const raizDetectada = raizDoGit();
      const candidatos = [];
      if (raizDetectada) candidatos.push(raizDetectada.replaceAll("\\", "/"));
      for (const padrao of ["C:/Program Files/Git", "C:/Program Files (x86)/Git", "$LOCALAPPDATA/Programs/Git"]) {
        if (!candidatos.includes(padrao)) candidatos.push(padrao);
      }

      const trechoPath = `
# Garante utilitários POSIX do Git for Windows no PATH (Windows/PowerShell)
if ! command -v sh >/dev/null 2>&1; then
  for _git_dir in ${candidatos.map((c) => `"${c}"`).join(" ")}; do
    if [ -d "$_git_dir/usr/bin" ]; then
      export PATH="$_git_dir/usr/bin:$_git_dir/bin:$PATH"
      break
    fi
  done
fi
`;
      if (!conteudo.includes("Garante utilitários POSIX")) {
        conteudo = conteudo.replace('export PATH="node_modules/.bin:$PATH"', `${trechoPath}\nexport PATH="node_modules/.bin:$PATH"`);
      }
    }

    writeFileSync(caminho, conteudo, { mode: 0o755 });
  }
}

export function prepararHusky(pastaHusky = ".husky") {
  const retorno = husky(pastaHusky);
  if (retorno && retorno.length > 0) {
    console.error(retorno);
  }
  ajustarShimsHusky(pastaHusky);
}

// Execução direta via CLI (npm run prepare)
if (process.argv[1]?.endsWith("preparar-husky.mjs")) {
  prepararHusky();
}
