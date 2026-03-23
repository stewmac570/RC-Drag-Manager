✅ Spec 1 — FINAL (LOCKED)

Live Feed Refresh Behaviour

Page updates automatically ✅

No user interaction required ✅

Refresh interval: every 30 seconds ✅

Method: data refresh only (no full page reload) ✅

Behaviour rules

Every 30 seconds:

call GET /api/live

update UI with latest data

If nothing changed:

page stays visually the same

No flicker / no full reload

🧱 What this means technically (simple version)



Website will:



setInterval(() => {

&#x20; fetch('/api/live')

&#x20;   .then(res => res.json())

&#x20;   .then(data => updateUI(data));

}, 30000);

🚨 Important (small but key)



We should also track:



👉 last update timestamp or version



So later we can:



skip redraw if nothing changed

make it feel faster without hammering UI



(Not required for v1, but worth noting)

