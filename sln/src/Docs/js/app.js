import "./fve-imports.js";

// The extension guide owns this dependency; FVE components do not use it.
const loadTailwindElements = () => {
  if (!document.querySelector('[data-tailwind-elements-preview]')) return;
  observer.disconnect();
  void import('@tailwindplus/elements');
};
const observer = new MutationObserver(loadTailwindElements);
observer.observe(document.body, { childList: true, subtree: true });
loadTailwindElements();
