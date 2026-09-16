const loading = import('https://cdn.jsdelivr.net/npm/@tailwindplus/elements@1.0.22')
void loading.catch(() => undefined)

window.fsharpDocsTailwindElements = {
  startedAt: document.readyState,
  loading,
}
