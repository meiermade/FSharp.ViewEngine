(() => {
  const parameter = { mode: "fveAppMode", frame: "fveAppFrame", dock: "fveAppDock" };
  const appMode = () => new URL(window.location.href).searchParams.get(parameter.mode) === "app";

  const cleanUrl = () => {
    const url = new URL(window.location.href);
    url.searchParams.delete(parameter.mode);
    url.searchParams.delete(parameter.frame);
    url.searchParams.delete(parameter.dock);
    return `${url.pathname}${url.search}${url.hash}`;
  };

  const appUrl = (href, frameId) => {
    const url = new URL(href, window.location.origin);
    if (url.origin !== window.location.origin || !url.protocol.startsWith("http")) return url.href;
    url.searchParams.set(parameter.mode, "app");
    url.searchParams.set(parameter.frame, frameId);
    const dock = new URL(window.location.href).searchParams.get(parameter.dock);
    if (dock === "top" || dock === "bottom") url.searchParams.set(parameter.dock, dock);
    return `${url.pathname}${url.search}${url.hash}`;
  };

  const request = (href) => {
    if (window.fsharpDocsNavigation) window.fsharpDocsNavigation.request(href, "push");
    else window.location.assign(href);
  };

  const icon = (path) => `<svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="${path}" clip-rule="evenodd"/></svg>`;
  const previousIcon = icon("M11.78 5.22a.75.75 0 0 1 0 1.06L8.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06l-4.25-4.25a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0Z");
  const nextIcon = icon("M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 1 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z");
  const dockIcon = (destination) => destination === "top"
    ? icon("M10.53 4.47a.75.75 0 0 0-1.06 0l-4.25 4.25a.75.75 0 0 0 1.06 1.06L9.25 6.81V15a.75.75 0 0 0 1.5 0V6.81l2.97 2.97a.75.75 0 1 0 1.06-1.06l-4.25-4.25Z")
    : icon("M9.47 15.53a.75.75 0 0 0 1.06 0l4.25-4.25a.75.75 0 1 0-1.06-1.06l-2.97 2.97V5a.75.75 0 0 0-1.5 0v8.19l-2.97-2.97a.75.75 0 1 0-1.06 1.06l4.25 4.25Z");

  const reviewLink = (source, label, graphic, frameId) => {
    if (!source) return null;
    const link = document.createElement("a");
    link.href = appUrl(source.getAttribute("href"), frameId);
    link.title = `${label}: ${source.textContent.trim()}`;
    link.setAttribute("aria-label", link.title);
    link.innerHTML = graphic;
    return link;
  };

  const directionControl = (source, label, graphic, frameId) => {
    const link = reviewLink(source, label, graphic, frameId);
    if (link) return link;
    const unavailable = document.createElement("span");
    unavailable.setAttribute("aria-disabled", "true");
    unavailable.setAttribute("aria-label", `No ${label.toLowerCase()} workflow step`);
    unavailable.innerHTML = graphic;
    return unavailable;
  };

  const controlsFor = (frame, frameId) => {
    const authored = frame.closest("[data-fve-fixture]")?.querySelector(":scope > [data-fve-app-mode-navigation]") ?? frame.querySelector(":scope > [data-fve-app-mode-navigation]");
    const controls = document.createElement("nav");
    controls.id = "fve-app-mode-controls";
    controls.className = "fve-components fve-theme-sky fve-density-compact fve-control-small";
    controls.setAttribute("data-fve-app-mode-controls", "true");
    controls.setAttribute("aria-label", "App mode controls");
    controls.dataset.fveAppDock = new URL(window.location.href).searchParams.get(parameter.dock) === "top" ? "top" : "bottom";
    const synchronizeColorMode = () => {
      // The dock uses the shared Components theme in the opposite resolved mode
      // so it remains distinct over either product appearance.
      controls.dataset.fveColorMode = document.documentElement.classList.contains("dark") ? "light" : "dark";
    };
    synchronizeColorMode();
    window.addEventListener("fsharpdocs:colormode", synchronizeColorMode);
    controls.fveAppModeColorModeCleanup = () => window.removeEventListener("fsharpdocs:colormode", synchronizeColorMode);

    const previous = authored?.querySelector("[data-fve-app-mode-previous]");
    const next = authored?.querySelector("[data-fve-app-mode-next]");
    const hasWorkflow = previous || next;
    if (hasWorkflow) {
      controls.append(directionControl(previous, "Previous", previousIcon, frameId));
    }

    const stateSelect = authored?.querySelector("[data-fve-app-mode-state-select]");
    const states = Array.from(authored?.querySelectorAll("[data-fve-app-mode-state]") ?? []);
    if (stateSelect && states.length) {
      stateSelect.addEventListener("click", (event) => {
        const option = event.target.closest?.('[role="option"][data-fve-option-value]');
        if (!option || !stateSelect.contains(option)) return;
        const destination = states.find((state) => state.getAttribute("href") === option.dataset.fveOptionValue);
        if (!destination) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        request(appUrl(destination.getAttribute("href"), frameId));
      }, true);
      controls.append(stateSelect);
    }

    if (hasWorkflow) {
      controls.append(directionControl(next, "Next", nextIcon, frameId));
    }

    if (hasWorkflow) {
      const divider = document.createElement("span");
      divider.setAttribute("data-fve-app-mode-divider", "true");
      divider.setAttribute("aria-hidden", "true");
      controls.append(divider);
    }

    // The Docs color-mode picker already owns persistence, system preference,
    // and the document-wide change event. Move that one control into App mode
    // rather than creating an independent theme preference.
    const colorMode = document.getElementById("spec-color-mode-trigger")?.closest(".spec-color-mode");
    if (colorMode?.parentNode) {
      colorMode.setAttribute("data-fve-app-mode-theme-menu", "true");
      const placeholder = document.createComment("fve-app-mode-color-mode");
      colorMode.parentNode.insertBefore(placeholder, colorMode);
      controls.append(colorMode);
      controls.fveAppModeColorModeRestore = () => {
        colorMode.removeAttribute("data-fve-app-mode-theme-menu");
        if (placeholder.parentNode) placeholder.replaceWith(colorMode);
      };
    }

    const dock = document.createElement("button");
    dock.type = "button";
    const refreshDock = () => {
      const destination = controls.dataset.fveAppDock === "top" ? "bottom" : "top";
      dock.setAttribute("aria-label", `Move App mode controls to ${destination}`);
      dock.title = dock.getAttribute("aria-label");
      dock.innerHTML = dockIcon(destination);
    };
    dock.addEventListener("click", () => {
      controls.dataset.fveAppDock = controls.dataset.fveAppDock === "top" ? "bottom" : "top";
      const url = new URL(window.location.href);
      url.searchParams.set(parameter.dock, controls.dataset.fveAppDock);
      window.history.replaceState(null, "", `${url.pathname}${url.search}${url.hash}`);
      refreshDock();
    });
    refreshDock();
    controls.append(dock);

    const exit = document.createElement("a");
    exit.href = cleanUrl();
    exit.setAttribute("aria-label", "Exit App mode");
    exit.title = "Exit App mode";
    exit.innerHTML = "×";
    controls.append(exit);
    return controls;
  };

  const leave = () => {
    document.getElementById("fve-app-mode-root")?.remove();
    const controls = document.getElementById("fve-app-mode-controls");
    controls?.fveAppModeColorModeCleanup?.();
    controls?.fveAppModeColorModeRestore?.();
    controls?.remove();
    for (const sibling of Array.from(document.body.children)) {
      if (sibling.dataset.fveAppModeHidden !== "true") continue;
      delete sibling.dataset.fveAppModeHidden;
      sibling.removeAttribute("aria-hidden");
      sibling.inert = false;
    }
    delete document.documentElement.dataset.fveAppModeReady;
  };

  const enter = () => {
    if (!appMode() || document.getElementById("fve-app-mode-root")) return;
    const frameId = new URL(window.location.href).searchParams.get(parameter.frame);
    const frame = frameId && document.querySelector(`[data-fve-app-mode-frame-id="${CSS.escape(frameId)}"]`);
    const content = frame?.querySelector(":scope > [data-fve-app-mode-content]");
    if (!frame || !content) {
      window.location.replace(cleanUrl());
      return;
    }

    const root = document.createElement("div");
    root.id = "fve-app-mode-root";
    root.setAttribute("data-fve-app-mode-root", "true");
    root.dataset.fveAppModeSurface = frame.dataset.fveAppModeSurface;
    root.dataset.fveAppModeFrame = frameId;
    root.setAttribute("role", "main");
    root.setAttribute("aria-label", frame.dataset.fveAppModeLabel || "App mode preview");
    const browserFixture = frame.dataset.fveAppModeSurface === "browser"
      ? content.querySelector(":scope > .spec-browser-frame > :not(.spec-browser-toolbar)")
      : null;
    if (browserFixture) {
      // Keep the product surface intact so its background and layout own the viewport.
      root.append(browserFixture);
    } else {
      while (content.firstChild) root.append(content.firstChild);
    }
    // The Docs document remains mounted for normal history navigation, but its
    // original siblings must neither receive focus nor be exposed beside App mode.
    for (const sibling of Array.from(document.body.children)) {
      sibling.dataset.fveAppModeHidden = "true";
      sibling.setAttribute("aria-hidden", "true");
      sibling.inert = true;
    }
    document.body.append(root, controlsFor(frame, frameId));
    document.documentElement.dataset.fveAppModeReady = "true";
  };

  document.addEventListener("click", (event) => {
    const launch = event.target.closest?.("[data-fve-app-mode-launch]");
    if (launch) {
      const frame = launch.closest("[data-fve-app-mode-frame]");
      if (!frame) return;
      event.preventDefault();
      request(appUrl(window.location.href, frame.dataset.fveAppModeFrameId));
      return;
    }

    if (!appMode() || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    const link = event.target.closest?.("#fve-app-mode-root a[href]");
    if (!link || link.closest("[data-fve-app-mode-navigation]")) return;
    const href = link.getAttribute("href");
    const target = new URL(href, window.location.origin);
    if (target.origin !== window.location.origin || !target.protocol.startsWith("http")) return;
    const destination = appUrl(href, document.getElementById("fve-app-mode-root")?.dataset.fveAppModeFrame);
    if (window.fsharpDocsNavigation) {
      // The navigation adapter must inspect the original event before it is cancelled.
      if (window.fsharpDocsNavigation.navigate(event, destination)) event.stopImmediatePropagation();
    } else if (!event.defaultPrevented && (!link.target || link.target === "_self") && !link.hasAttribute("download")) {
      event.preventDefault();
      event.stopImmediatePropagation();
      window.location.assign(destination);
    }
  }, true);

  // Native GET filters replace the action's query string. Preserve only the
  // viewer envelope in submitted data, leaving validation and navigation native.
  document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!appMode() || event.defaultPrevented || !(form instanceof HTMLFormElement) || !form.closest("#fve-app-mode-root")) return;
    const submitter = event.submitter;
    const method = submitter?.hasAttribute("formmethod") ? submitter.formMethod : form.method;
    const target = submitter?.hasAttribute("formtarget") ? submitter.formTarget : form.target;
    const action = new URL(submitter?.hasAttribute("formaction") ? submitter.formAction : form.action);
    if (method.toLowerCase() !== "get" || (target && target !== "_self") || action.origin !== location.origin || !action.protocol.startsWith("http")) return;
    const envelope = new URL(appUrl(action.href, document.getElementById("fve-app-mode-root").dataset.fveAppModeFrame), location.origin);
    const preserve = ({ formData }) => {
      for (const key of Object.values(parameter)) {
        if (envelope.searchParams.has(key)) formData.set(key, envelope.searchParams.get(key));
      }
    };
    form.addEventListener("formdata", preserve, { once: true });
    // An application may cancel submission later in this same event dispatch.
    setTimeout(() => form.removeEventListener("formdata", preserve), 0);
  });

  document.addEventListener("datastar-fetch", (event) => {
    if (event.detail?.el !== document.body || event.detail.type !== "finished") return;
    queueMicrotask(() => {
      if (!document.getElementById("fve-app-mode-root") && !appMode()) return;
      leave();
      enter();
    });
  });

  document.addEventListener("DOMContentLoaded", enter);
  window.fsharpViewEngineAppMode = { appUrl, cleanUrl, enter, leave };
})();
