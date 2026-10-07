# Microstrap for Roblox

A lightweight browser extension that gives the Roblox website an orange-to-red Microstrap visual theme while retaining Roblox navigation, content, and account behavior.

## Install (Chrome, Edge, or another Chromium browser)

1. Open the browser's extensions page (`chrome://extensions` or `edge://extensions`).
2. Enable **Developer mode**.
3. Choose **Load unpacked** and select this `MicrostrapExtension` directory.
4. Open or refresh `https://www.roblox.com/home` while signed in.

The extension only runs on `www.roblox.com`; settings are stored locally by the browser extension storage API. The popup and the Microstrap control in Roblox's top bar can toggle the orange-to-red theme, compact game cards, hide promotional panels, and choose both gradient colors. The Roblox home UI was inspected directly; the existing top bar, account controls, recommended game tiles, and settings navigation are retained.
