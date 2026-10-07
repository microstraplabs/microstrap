(() => {
  const defaults = {
    enabled: true,
    compactCards: false,
    hidePromotions: false,
    accentStart: "#ffad35",
    accentEnd: "#f13b38",
  };
  let settings = { ...defaults };

  const toRgb = (hex) => {
    const value = /^#[0-9a-f]{6}$/i.test(hex) ? hex.slice(1) : "ff6f2f";
    return `${parseInt(value.slice(0, 2), 16)}, ${parseInt(value.slice(2, 4), 16)}, ${parseInt(value.slice(4, 6), 16)}`;
  };

  const applySettings = () => {
    const { enabled, compactCards, hidePromotions, accentStart, accentEnd } = settings;
    document.body?.classList.toggle("microstrap-theme-enabled", enabled);
    document.body?.classList.toggle("microstrap-compact-cards", enabled && compactCards);
    document.body?.classList.toggle("microstrap-hide-promotions", enabled && hidePromotions);
    document.documentElement.style.setProperty("--microstrap-accent-start", accentStart);
    document.documentElement.style.setProperty("--microstrap-accent-end", accentEnd);
    document.documentElement.style.setProperty("--microstrap-accent-rgb", toRgb(accentStart));
  };

  const openOrClosePanel = (button) => {
    const open = document.querySelector("[data-microstrap-panel]");
    if (open) {
      open.remove();
      document.querySelectorAll("[data-microstrap-settings] button").forEach((item) => item.setAttribute("aria-expanded", "false"));
    } else {
      document.body.append(createSettingsPanel());
      document.querySelectorAll("[data-microstrap-settings] button").forEach((item) => item.setAttribute("aria-expanded", String(item === button)));
    }
  };

  const mountSettings = () => {
    const header = document.querySelector("#right-navigation-header .rbx-navbar-right-nav")
      || document.querySelector("#header .rbx-navbar-right-nav");
    if (header && !header.querySelector("[data-microstrap-settings]")) {
      const item = document.createElement("li");
      item.dataset.microstrapSettings = "true";
      item.innerHTML = `<button type="button" class="microstrap-settings-trigger" aria-expanded="false" title="Microstrap appearance settings"><span aria-hidden="true">✦</span><span class="microstrap-settings-label">Microstrap</span></button>`;
      const trigger = item.querySelector("button");
      trigger.addEventListener("click", () => openOrClosePanel(trigger));
      header.prepend(item);
    }
  };

  const createSettingsPanel = () => {
    const panel = document.createElement("section");
    panel.dataset.microstrapPanel = "true";
    panel.setAttribute("aria-label", "Microstrap appearance settings");
    panel.innerHTML = `
      <div class="microstrap-panel-header"><div><span class="microstrap-panel-brand">Microstrap</span><span class="microstrap-panel-caption">Roblox appearance</span></div><button type="button" data-close aria-label="Close settings">×</button></div>
      <label class="microstrap-toggle-row"><span><b>Orange → red theme</b><small>Recolor Roblox controls and navigation</small></span><input type="checkbox" data-setting="enabled"></label>
      <label class="microstrap-toggle-row"><span><b>Compact game cards</b><small>Tighter card corners</small></span><input type="checkbox" data-setting="compactCards"></label>
      <label class="microstrap-toggle-row"><span><b>Hide promotions</b><small>Hide promotional and upsell panels</small></span><input type="checkbox" data-setting="hidePromotions"></label>
      <div class="microstrap-colors"><label>Orange<input type="color" data-setting="accentStart"></label><label>Red<input type="color" data-setting="accentEnd"></label></div>
      <div class="microstrap-panel-footer">Settings are saved in this browser.</div>`;

    panel.querySelector("[data-close]").addEventListener("click", () => {
      panel.remove();
      document.querySelectorAll("[data-microstrap-settings] button").forEach((item) => item.setAttribute("aria-expanded", "false"));
    });
    panel.querySelectorAll("[data-setting]").forEach((input) => {
      const key = input.dataset.setting;
      input.type === "checkbox" ? (input.checked = Boolean(settings[key])) : (input.value = settings[key]);
      input.addEventListener("change", () => {
        settings[key] = input.type === "checkbox" ? input.checked : input.value;
        chrome.storage.local.set({ microstrapSettings: settings });
        applySettings();
      });
    });
    return panel;
  };

  chrome.storage.local.get({ microstrapSettings: defaults }, (stored) => {
    settings = { ...defaults, ...stored.microstrapSettings };
    applySettings();
    mountSettings();
    new MutationObserver(() => {
      applySettings();
      mountSettings();
    }).observe(document.documentElement, { childList: true, subtree: true });
  });

  chrome.storage.onChanged.addListener((changes, area) => {
    if (area === "local" && changes.microstrapSettings) {
      settings = { ...defaults, ...changes.microstrapSettings.newValue };
      applySettings();
    }
  });
})();
