// Push numa branch cujo PR já foi mergeado recria a branch que o GitHub apagou
// no squash, e o commit fica de fora da main.

export function mensagemPushMergeado(branch, numeroPr) {
  if (!numeroPr) return null;
  return `✖ A branch "${branch}" já teve o PR #${numeroPr} mergeado. Este push a recria e o commit fica de fora da main.
  Parta de origin/main e abra um PR novo (docs/licoes.md).`;
}
