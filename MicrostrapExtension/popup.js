(() => {
  const defaults = {
    enabled: true,
    compactCards: false,
    hidePromotions: false,
    accentStart: "#ffad35",
    accentEnd: "#f13b38",
  };
  const fields = ["enabled", "compactCards", "hidePromotions", "accentStart", "accentEnd"];

  chrome.storage.local.get({ microstrapSettings: defaults }, ({ microstrapSettings }) => {
    const settings = { ...defaults, ...microstrapSettings };
    for (const key of fields) {
      const input = document.getElementById(key);
      input.type === "checkbox" ? (input.checked = Boolean(settings[key])) : (input.value = settings[key]);
      input.addEventListener("change", () => {
        settings[key] = input.type === "checkbox" ? input.checked : input.value;
        chrome.storage.local.set({ microstrapSettings: settings });
      });
    }
  });
})();
