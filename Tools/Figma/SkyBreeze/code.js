// Golden Mend - Sky Breeze screens.
// A local Figma plugin: builds the "Sky Breeze" page with a frosted-glass button kit (glass buttons, the Level
// button in Normal / Hard / Very Hard tones, glass icon buttons) and every game screen as editable layers.
// Run it again at any time: it clears and rebuilds only what it made (the "Glass Kit" frame and frames named "SB ...").
//
// Figma desktop: Plugins > Development > Import plugin from manifest... > pick manifest.json, then run it.

const PAGE_NAME = "Sky Breeze";
const W = 540;
const H = 960;

// ---------------------------------------------------------------- palette and helpers

const P = {
  skyTop: 0xC9F0FF, skyBottom: 0x8EC5FF, ink: 0x2B4A8B, title: 0x2350B5, pale: 0xE6EEFB, pale2: 0xDCE7F8,
  dash: 0x9DB8E8, gold: 0xFFD54A, goldDeep: 0xF5A400, orangeTop: 0xFFC24D, orange: 0xFF9418,
  glazeTop: 0xFF7EB3, glazeBottom: 0x7A5CFF, scrim: 0x1C3A78
};

function hex(h) { return { r: ((h >> 16) & 255) / 255, g: ((h >> 8) & 255) / 255, b: (h & 255) / 255 }; }
function rgba(h, a) { const c = hex(h); return { r: c.r, g: c.g, b: c.b, a: a }; }
function solid(h, o) { return [{ type: "SOLID", color: hex(h), opacity: o === undefined ? 1 : o }]; }
function vgrad(stops) {
  return [{ type: "GRADIENT_LINEAR", gradientTransform: [[0, 1, 0], [-1, 0, 1]],
    gradientStops: stops.map(function (s) { return { position: s[0], color: rgba(s[1], s[2] === undefined ? 1 : s[2]) }; }) }];
}
function mix(a, b, t) {
  const A = hex(a), B = hex(b);
  const c = function (x, y) { return Math.round((x + (y - x) * t) * 255); };
  return (c(A.r, B.r) << 16) | (c(A.g, B.g) << 8) | c(A.b, B.b);
}
function font(style) { return { family: "Fredoka", style: style }; }
function shadow(h, a, y, r, spread) {
  return { type: "DROP_SHADOW", color: rgba(h, a), offset: { x: 0, y: y }, radius: r, spread: spread || 0, visible: true, blendMode: "NORMAL" };
}
function inner(h, a, y, r) {
  return { type: "INNER_SHADOW", color: rgba(h, a), offset: { x: 0, y: y }, radius: r, spread: 0, visible: true, blendMode: "NORMAL" };
}
function blur(r) { return { type: "BACKGROUND_BLUR", radius: r, visible: true }; }

function rect(parent, x, y, w, h, fills, opts) {
  opts = opts || {};
  const r = figma.createRectangle();
  r.name = opts.name || "Rect";
  r.resize(w, h); r.x = x; r.y = y;
  r.fills = fills;
  if (opts.radius !== undefined) r.cornerRadius = opts.radius;
  if (opts.stroke !== undefined) { r.strokes = solid(opts.stroke, opts.strokeOpacity); r.strokeWeight = opts.strokeWeight || 2; if (opts.dash) r.dashPattern = opts.dash; }
  if (opts.effects) r.effects = opts.effects;
  parent.appendChild(r);
  return r;
}
function ellipse(parent, x, y, w, h, fills, opts) {
  opts = opts || {};
  const e = figma.createEllipse();
  e.name = opts.name || "Ellipse";
  e.resize(w, h); e.x = x; e.y = y; e.fills = fills;
  if (opts.effects) e.effects = opts.effects;
  if (opts.stroke !== undefined) { e.strokes = solid(opts.stroke, opts.strokeOpacity); e.strokeWeight = opts.strokeWeight || 2; }
  parent.appendChild(e);
  return e;
}
function text(parent, s, x, y, w, style, size, color, opts) {
  opts = opts || {};
  const t = figma.createText();
  t.fontName = font(style);
  t.characters = s;
  t.fontSize = size;
  t.fills = solid(color, opts.opacity);
  t.textAlignHorizontal = opts.align || "CENTER";
  t.textAutoResize = "HEIGHT";
  t.resize(w, t.height);
  t.x = x; t.y = y;
  if (opts.tracking) t.letterSpacing = { unit: "PERCENT", value: opts.tracking };
  if (opts.lineHeight) t.lineHeight = { unit: "PERCENT", value: opts.lineHeight };
  if (opts.shadow !== undefined) t.effects = [shadow(opts.shadow, opts.shadowAlpha || 0.45, opts.shadowY || 2, opts.shadowBlur === undefined ? 2 : opts.shadowBlur)];
  t.name = opts.name || s.slice(0, 28);
  parent.appendChild(t);
  return t;
}
function group(parent, x, y, w, h, name) {
  const f = figma.createFrame();
  f.name = name; f.resize(w, h); f.x = x; f.y = y; f.fills = []; f.clipsContent = false;
  parent.appendChild(f);
  return f;
}
function stretch(node) { node.constraints = { horizontal: "STRETCH", vertical: "STRETCH" }; }

// The frosted-glass surface: tinted translucent body, background blur, bright rim, top gloss.
function glassBody(parent, w, h, r, top, bottom, alpha, glow) {
  const b = figma.createRectangle();
  b.name = "Glass body"; b.resize(w, h); b.cornerRadius = r;
  b.fills = vgrad([[0, top, alpha], [1, bottom, alpha]]);
  b.strokes = vgrad([[0, 0xFFFFFF, 0.95], [0.55, 0xFFFFFF, 0.25], [1, 0xFFFFFF, 0.55]]);
  b.strokeWeight = 2; b.strokeAlign = "INSIDE";
  b.effects = [shadow(glow, 0.36, 10, 22, -2), inner(0xFFFFFF, 0.7, 3, 2), inner(bottom, 0.35, -5, 8), blur(24)];
  parent.appendChild(b);
  stretch(b);
  const g = figma.createRectangle();
  g.name = "Gloss"; g.resize(w - 16, Math.round(h * 0.46)); g.x = 8; g.y = 5;
  g.topLeftRadius = g.topRightRadius = Math.max(4, r - 6);
  g.bottomLeftRadius = g.bottomRightRadius = Math.round(h * 0.25);
  g.fills = vgrad([[0, 0xFFFFFF, 0.62], [1, 0xFFFFFF, 0.04]]);
  parent.appendChild(g);
  g.constraints = { horizontal: "STRETCH", vertical: "MIN" };
  return b;
}

// A white frosted panel (cards, popups, trays).
function glassPanel(parent, x, y, w, h, r, alpha, name) {
  const f = group(parent, x, y, w, h, name || "Glass panel");
  const b = rect(f, 0, 0, w, h, vgrad([[0, 0xFFFFFF, alpha], [1, 0xF2F7FF, alpha * 0.85]]), { radius: r, name: "Panel" });
  b.strokes = vgrad([[0, 0xFFFFFF, 1], [1, 0xFFFFFF, 0.35]]); b.strokeWeight = 2; b.strokeAlign = "INSIDE";
  b.effects = [shadow(0x3D6FC4, 0.22, 14, 30, -4), inner(0xFFFFFF, 0.8, 2, 1), blur(30)];
  return f;
}

// ---------------------------------------------------------------- icons
// Line icons from Lucide (ISC licence, lucide.dev), drawn as editable vectors.

const ICONS = {
  settings: '<path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/>',
  hint: '<path d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"/><path d="M9 18h6"/><path d="M10 22h4"/>',
  pull: '<path d="m5 12 7-7 7 7"/><path d="M12 19V5"/>',
  back: '<path d="m12 19-7-7 7-7"/><path d="M19 12H5"/>',
  close: '<path d="M18 6 6 18"/><path d="m6 6 12 12"/>',
  pause: '<rect x="6" y="4" width="4" height="16" rx="1"/><rect x="14" y="4" width="4" height="16" rx="1"/>',
  sound: '<path d="M11 4.702a.705.705 0 0 0-1.203-.498L6.413 7.587A1.4 1.4 0 0 1 5.416 8H3a1 1 0 0 0-1 1v6a1 1 0 0 0 1 1h2.416a1.4 1.4 0 0 1 .997.413l3.383 3.384A.705.705 0 0 0 11 19.298z"/><path d="M16 9a5 5 0 0 1 0 6"/><path d="M19.364 18.364a9 9 0 0 0 0-12.728"/>',
  music: '<path d="M9 18V5l12-2v13"/><circle cx="6" cy="18" r="3"/><circle cx="18" cy="16" r="3"/>',
  haptics: '<path d="m2 8 2 2-2 2 2 2-2 2"/><path d="m22 8-2 2 2 2-2 2 2 2"/><rect width="8" height="14" x="8" y="5" rx="1"/>',
  lock: '<rect width="18" height="11" x="3" y="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>',
  play: '<path d="M6 3 20 12 6 21Z"/>'
};

function svgIcon(parent, name, x, y, size, color, weight) {
  const c = hex(color);
  const css = "rgb(" + Math.round(c.r * 255) + "," + Math.round(c.g * 255) + "," + Math.round(c.b * 255) + ")";
  const filled = name === "pause" || name === "play";
  const svg = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="' + (filled ? css : "none") +
    '" stroke="' + css + '" stroke-width="' + (weight || 2.4) + '" stroke-linecap="round" stroke-linejoin="round">' + ICONS[name] + "</svg>";
  const n = figma.createNodeFromSvg(svg);
  n.name = "icon/" + name;
  n.resize(size, size);
  n.x = x; n.y = y;
  parent.appendChild(n);
  return n;
}

// ---------------------------------------------------------------- kit (components)

const kit = {};

// Button labels fill the button and centre vertically, so an instance can change size or font size and stay centred.
// Labels with a fixed y (the Level button's caption and number) keep their own height instead.
function makeLabel(parent, s, w, h, size, color, shade, style, y) {
  const t = text(parent, s, 0, 0, w, style || "Bold", size, color, shade === null ? {} : { shadow: shade });
  if (y === undefined) {
    t.textAutoResize = "NONE";
    t.resize(w, h);
    t.y = 0;
    t.textAlignVertical = "CENTER";
    t.constraints = { horizontal: "STRETCH", vertical: "STRETCH" };
  } else {
    t.y = y;
    t.constraints = { horizontal: "STRETCH", vertical: "CENTER" };
  }
  return t;
}

function buildKit(page) {
  const frame = figma.createFrame();
  frame.name = "Glass Kit"; frame.x = 0; frame.y = 0; frame.resize(2420, 700); frame.cornerRadius = 32;
  frame.fills = vgrad([[0, P.skyTop], [1, P.skyBottom]]);
  page.appendChild(frame);
  [[120, 260, 260], [700, 120, 320], [1500, 300, 300], [2050, 60, 260]].forEach(function (c) {
    ellipse(frame, c[0], c[1], c[2] * 1.6, c[2], solid(0xFFFFFF, 0.55), { name: "Cloud" });
  });
  text(frame, "Sky Breeze - Glass kit", 48, 36, 1400, "Bold", 48, P.title, { align: "LEFT" });
  text(frame, "Frosted glass: tinted translucent body, background blur, bright rim, top gloss. The Level button replaces Play; its tone shows difficulty.", 48, 100, 2300, "Medium", 22, P.ink, { align: "LEFT" });

  const styles = [
    ["Primary", P.orangeTop, P.orange, 0.88, 0xC46A00, 0xFFFFFF, 0xB35A00],
    ["Secondary", 0xFFFFFF, P.pale, 0.55, 0x5B8FD9, P.title, null],
    ["Confirm", 0x7BE38E, 0x2FB34E, 0.85, 0x1E7A36, 0xFFFFFF, 0x1E7A36],
    ["Danger", 0xFF8A8A, 0xE5484D, 0.85, 0xA3262B, 0xFFFFFF, 0x9C1F24],
    ["Sky", 0x7FD3FF, 0x3F8CFF, 0.85, 0x1F5FD0, 0xFFFFFF, 0x1C4FB0]
  ];
  const buttons = styles.map(function (s) {
    const c = figma.createComponent();
    c.name = "Style=" + s[0]; c.resize(340, 104); c.fills = []; c.clipsContent = false;
    glassBody(c, 340, 104, 52, s[1], s[2], s[3], s[4]);
    const t = makeLabel(c, "BUTTON", 340, 104, 40, s[5], s[6]);
    t.name = "Label";
    return c;
  });
  kit.button = figma.combineAsVariants(buttons, frame);
  kit.button.name = "Glass Button";
  layoutRow(kit.button, 48, 170, 28);

  // Level button: a compact pill with a thick light rim and a glossy glass body, "Level 17" on one line. Difficulty
  // changes the body colour and adds a tab on the top edge; every variant has the same size so layouts never shift.
  const tones = [
    ["Normal", 0xFFC24D, 0xFF9418, 0xB35A00, 0xFFF1CC, null, 0, 0],
    ["Hard", 0xFF6B7A, 0xE0244A, 0x8E0F28, 0xFFD6DC, "Hard", 0xC81E3E, 0x8E0F28],
    ["Very Hard", 0x9F7BFF, 0x5A2BD6, 0x2A0D74, 0xE6DCFF, "Very Hard", 0x5B2BD6, 0x2A0D74]
  ];
  const BW = 320, BH = 104, TOP = 26;
  const levels = tones.map(function (t) {
    const c = figma.createComponent();
    c.name = "Difficulty=" + t[0]; c.resize(BW, BH + TOP); c.fills = []; c.clipsContent = false;
    const rim = rect(c, 0, TOP, BW, BH, vgrad([[0, 0xFFFFFF, 1], [1, t[4], 1]]), { radius: BH / 2, name: "Rim" });
    rim.effects = [shadow(t[3], 0.45, 8, 0), shadow(t[2], 0.35, 14, 24, -4)];
    const body = group(c, 6, TOP + 6, BW - 12, BH - 12, "Body");
    glassBody(body, BW - 12, BH - 12, (BH - 12) / 2, t[1], t[2], 0.94, t[2]);
    const label = text(c, "Level 17", 0, 0, BW, "Bold", 46, 0xFFFFFF, { shadow: t[3], shadowAlpha: 0.75, shadowY: 3, shadowBlur: 0 });
    label.name = "Label";
    label.y = TOP + Math.round((BH - label.height) / 2) + 1;
    if (t[5]) {
      const tab = figma.createFrame();
      tab.name = "Difficulty tab"; tab.layoutMode = "HORIZONTAL";
      tab.primaryAxisSizingMode = "AUTO"; tab.counterAxisSizingMode = "AUTO";
      tab.paddingLeft = tab.paddingRight = 20; tab.paddingTop = 4; tab.paddingBottom = 5;
      tab.cornerRadius = 14;
      tab.fills = vgrad([[0, t[6]], [1, t[7]]]);
      tab.strokes = solid(0xFFFFFF); tab.strokeWeight = 3; tab.strokeAlign = "OUTSIDE";
      tab.effects = [shadow(t[7], 0.4, 3, 0)];
      const tt = figma.createText();
      tt.fontName = font("Bold"); tt.characters = t[5]; tt.fontSize = 22; tt.fills = solid(0xFFFFFF);
      tt.letterSpacing = { unit: "PERCENT", value: 2 };
      tab.appendChild(tt);
      c.appendChild(tab);
      tab.x = Math.round((BW - tab.width) / 2); tab.y = 6;
      tab.constraints = { horizontal: "CENTER", vertical: "MIN" };
    }
    return c;
  });
  kit.level = figma.combineAsVariants(levels, frame);
  kit.level.name = "Level Button";
  layoutRow(kit.level, 48, 340, 32);

  kit.icon = iconComponent(frame, "Glass Icon", 0xFFFFFF, P.pale, 0.6, 0x5B8FD9, P.title, null, 1480, 380);
  kit.iconAccent = iconComponent(frame, "Glass Icon Accent", 0xFF8FC0, 0xF0457E, 0.88, 0xB0124F, 0xFFFFFF, 0x9E1B50, 1590, 380);
  kit.iconClose = iconComponent(frame, "Glass Icon Close", 0xFF8A8A, 0xE5484D, 0.88, 0xA3262B, 0xFFFFFF, 0x9C1F24, 1700, 380);
  ["settings", "hint", "pause", "back", "sound", "music", "haptics"].forEach(function (n, i) {
    icon(frame, n, 1480 + (i % 4) * 110, 490 + Math.floor(i / 4) * 0, 84);
  });

  text(frame, "Level button: Normal = sunny orange, no tab   |   Hard = ruby body + Hard tab   |   Very Hard = deep violet body + Very Hard tab", 48, 600, 2300, "SemiBold", 22, P.ink, { align: "LEFT" });
  return frame;
}

function layoutRow(set, x, y, gap) {
  set.x = x; set.y = y;
  set.layoutMode = "HORIZONTAL"; set.itemSpacing = gap;
  set.paddingLeft = set.paddingRight = set.paddingTop = set.paddingBottom = 24;
  set.primaryAxisSizingMode = "AUTO"; set.counterAxisSizingMode = "AUTO";
  set.fills = [];
}

function iconComponent(parent, name, top, bottom, alpha, glow, ink, shade, x, y) {
  const c = figma.createComponent();
  c.name = name; c.resize(84, 84); c.fills = []; c.clipsContent = false;
  glassBody(c, 84, 84, 26, top, bottom, alpha, glow);
  parent.appendChild(c);
  c.x = x; c.y = y;
  return c;
}

// ---------------------------------------------------------------- instances

function setText(node, name, value) {
  const t = node.findOne(function (n) { return n.type === "TEXT" && n.name === name; });
  if (t) t.characters = value;
}
function button(parent, style, label, x, y, w, h, size) {
  const variant = kit.button.children.filter(function (c) { return c.name === "Style=" + style; })[0];
  const i = variant.createInstance();
  parent.appendChild(i);
  i.x = x; i.y = y;
  if (w) i.resize(w, h);
  setText(i, "Label", label);
  if (size) { const t = i.findOne(function (n) { return n.type === "TEXT" && n.name === "Label"; }); t.fontSize = size; }
  i.name = "Button " + label;
  return i;
}
// A buy button with a price pill on the right. Detached from the kit, since a pill can't be added inside an instance.
function offerButton(parent, style, label, x, y, w, h, size) {
  const f = button(parent, style, label, x, y, w, h, size).detachInstance();
  f.name = "Offer " + label;
  const t = f.findOne(function (n) { return n.type === "TEXT"; });
  t.textAlignHorizontal = "LEFT";
  t.x = 36;
  t.resize(w - 36, t.height);
  return f;
}

function levelButton(parent, difficulty, number, x, y) {
  const variant = kit.level.children.filter(function (c) { return c.name === "Difficulty=" + difficulty; })[0];
  const i = variant.createInstance();
  parent.appendChild(i);
  i.x = x; i.y = y;
  setText(i, "Label", "Level " + number);
  i.name = "Level button " + number + " (" + difficulty + ")";
  return i;
}
// A glass icon button: an instance of the glass kit's icon base with a line icon laid on top, both in a small group.
function icon(parent, name, x, y, size, which) {
  size = size || 84;
  const f = group(parent, x, y, size, size, "Icon button " + name);
  const base = (which || kit.icon).createInstance();
  f.appendChild(base);
  base.x = 0; base.y = 0;
  base.resize(size, size);
  const ink = which && which !== kit.icon ? 0xFFFFFF : P.title;
  const glyph = Math.round(size * 0.46);
  svgIcon(f, name, Math.round((size - glyph) / 2), Math.round((size - glyph) / 2), glyph, ink, 2.6);
  return f;
}

// ---------------------------------------------------------------- shared screen parts

function screen(board, x, y, name, caption) {
  text(board, caption, x, y - 52, W, "Bold", 28, P.title, { align: "LEFT" });
  const s = figma.createFrame();
  s.name = "SB " + name; s.resize(W, H); s.x = x; s.y = y; s.cornerRadius = 36; s.clipsContent = true;
  s.fills = vgrad([[0, P.skyTop], [1, P.skyBottom]]);
  ellipse(s, -140, 120, 420, 220, solid(0xFFFFFF, 0.5), { name: "Cloud" });
  ellipse(s, 300, 360, 380, 200, solid(0xFFFFFF, 0.45), { name: "Cloud" });
  ellipse(s, -100, 700, 460, 240, solid(0xFFFFFF, 0.4), { name: "Cloud" });
  ellipse(s, 330, 820, 360, 200, solid(0xFFFFFF, 0.35), { name: "Cloud" });
  board.appendChild(s);
  return s;
}

function coinPill(parent, x, y, amount) {
  const f = group(parent, x, y, 160, 56, "Coins");
  const b = rect(f, 0, 0, 160, 56, solid(0xFFFFFF, 0.55), { radius: 28, name: "Pill" });
  b.strokes = solid(0xFFFFFF, 0.9); b.strokeWeight = 2; b.effects = [shadow(0x3D6FC4, 0.18, 6, 14), blur(20)];
  ellipse(f, 8, 8, 40, 40, vgrad([[0, 0xFFE27A], [1, P.goldDeep]]), { name: "Coin", stroke: 0xE08A00, strokeWeight: 2 });
  text(f, String(amount), 54, 9, 96, "Bold", 28, P.ink);
  return f;
}

function star(parent, x, y, size, on) {
  const s = figma.createStar();
  s.pointCount = 5; s.innerRadius = 0.5; s.resize(size, size); s.x = x; s.y = y;
  s.fills = on ? vgrad([[0, 0xFFE27A], [1, P.goldDeep]]) : solid(0xB9CCEB);
  s.strokes = solid(on ? 0xE08A00 : 0x9DB2D8); s.strokeWeight = 2; s.strokeJoin = "ROUND";
  s.cornerRadius = size * 0.08;
  s.effects = [shadow(on ? 0xC46A00 : 0x7E95C2, 0.35, 3, 0)];
  s.name = on ? "Star" : "Star empty";
  parent.appendChild(s);
  return s;
}
function stars(parent, cx, y, size, earned) {
  for (let i = 0; i < 3; i++) {
    const big = i === 1 ? size * 1.25 : size;
    star(parent, cx + (i - 1) * size * 1.15 - big / 2, y - (i === 1 ? size * 0.2 : 0), big, i < earned);
  }
}

// Vessel drawing. "teaser" = the home showcase: pale empty cells with the in-cell animation states frozen (light
// wave, gold glint, hop) and the gold-framed anchor. "restored" = full glaze with a gold outer border.
function vessel(parent, cx, cy, cell, shape, mode, opts) {
  opts = opts || {};
  const rows = shape.length;
  let cols = 0; shape.forEach(function (r) { cols = Math.max(cols, r.length); });
  const g = group(parent, cx - cols * cell / 2, cy - rows * cell / 2, cols * cell, rows * cell, mode === "restored" ? "Vessel restored" : "Vessel (animated cells)");
  const lit = opts.lit || [];
  const top = opts.top || P.glazeTop, bottom = opts.bottom || P.glazeBottom;
  if (mode === "restored") {
    const pad = Math.max(3, cell * 0.08);
    for (let r = 0; r < rows; r++) for (let c = 0; c < shape[r].length; c++) {
      if (shape[r][c] === ".") continue;
      rect(g, c * cell - pad, r * cell - pad, cell + 2 * pad, cell + 2 * pad, solid(P.gold), { name: "Gold border" });
    }
  }
  for (let r = 0; r < rows; r++) for (let c = 0; c < shape[r].length; c++) {
    const ch = shape[r][c];
    if (ch === ".") continue;
    const key = r + "," + c;
    const t0 = r / rows, t1 = (r + 1) / rows;
    const glaze = vgrad([[0, mix(top, bottom, t0)], [1, mix(top, bottom, t1)]]);
    if (mode === "restored") { rect(g, c * cell, r * cell, cell + 0.5, cell + 0.5, glaze, { name: "Glaze" }); continue; }
    if (ch === "A" || (opts.placed && opts.placed.indexOf(key) >= 0)) {
      if (ch === "A") rect(g, c * cell - 3, r * cell - 3, cell + 6, cell + 6, solid(P.gold), { radius: 5, name: "Anchor - pulse" });
      rect(g, c * cell + 1, r * cell + 1, cell - 2, cell - 2, glaze, { radius: 4, name: ch === "A" ? "Anchor glaze" : "Placed shard" });
      continue;
    }
    const inset = cell * 0.06;
    let x = c * cell + inset, y = r * cell + inset, w = cell - 2 * inset;
    let fill = solid(0xFFFFFF, 0.5), name = "Cell";
    if (lit.indexOf(key) >= 0) { fill = vgrad([[0, 0xFFFFFF, 0.95], [1, 0xCFE8FF, 0.95]]); name = "Cell - light wave"; }
    if (key === opts.glint) { fill = vgrad([[0, 0xFFE27A], [1, 0xF5B020]]); name = "Cell - gold glint"; }
    if (key === opts.hop) { x -= cell * 0.06; y -= cell * 0.1; w += cell * 0.12; name = "Cell - hop"; }
    rect(g, x, y, w, w, fill, { radius: cell * 0.14, stroke: P.dash, strokeOpacity: 0.9, strokeWeight: 1.5, dash: [4, 3], name: name });
  }
  return g;
}

function scrim(s) {
  const r = rect(s, 0, 0, W, H, solid(P.scrim, 0.38), { name: "Scrim" });
  r.effects = [blur(8)];
  return r;
}

// A popup: white frosted card, sky-glass title ribbon, red glass close button clear of the ribbon.
function popup(s, w, h, y, title, closable) {
  const x = (W - w) / 2;
  const card = glassPanel(s, x, y, w, h, 40, 0.86, "Popup " + title);
  const ribbon = group(card, 70, -40, w - 140, 84, "Title ribbon");
  glassBody(ribbon, w - 140, 84, 42, 0x7FD3FF, 0x3F8CFF, 0.92, 0x1F5FD0);
  text(ribbon, title, 0, 14, w - 140, "Bold", 40, 0xFFFFFF, { shadow: 0x1C4FB0 });
  if (closable) icon(card, "close", w - 52, -30, 72, kit.iconClose);
  return card;
}

const BRUSH = ["XXXX", "XXXX", "XXXX", "XXXX", "XXXX", "XAX."];
const STORAGE = [".XXXX.", "XXXXXX", "XXXXXX", "XXXXXX", "..A..."];
const OFFERING = ["XXXXXX", "XXXXXX", "XXXXXX", "XXXXXX", ".XA..."];
const SAKE = ["XXX", "XXX", "XAX"];
const TEA = ["XXXXX", ".XXX.", ".XAX."];

// Gameplay board (used for the gameplay screen and behind the in-game popups).
function gameplay(s, placed) {
  icon(s, "pause", 24, 30, 76);
  const plate = glassPanel(s, 118, 30, 230, 76, 38, 0.6, "Level plate");
  text(plate, "LEVEL 17", 0, 6, 230, "Bold", 30, P.title);
  text(plate, "Brush Pot", 0, 42, 230, "Medium", 20, P.ink);
  icon(s, "pull", 362, 26, 76, kit.iconAccent);
  const price = group(s, 356, 106, 88, 34, "Pull price");
  rect(price, 0, 0, 88, 34, solid(0xFFFFFF, 0.7), { radius: 17 });
  ellipse(price, 6, 5, 24, 24, vgrad([[0, 0xFFE27A], [1, P.goldDeep]]));
  text(price, "90", 30, 2, 52, "Bold", 22, P.ink);
  icon(s, "hint", 448, 26, 76);
  const badge = ellipse(s, 498, 20, 34, 34, solid(0xFF5B6E), { name: "Hint badge", stroke: 0xFFFFFF, strokeWeight: 3 });
  text(s, "2", 498, 23, 34, "Bold", 20, 0xFFFFFF);

  const card = glassPanel(s, 30, 160, 480, 360, 36, 0.6, "Vessel card");
  vessel(card, 240, 180, 44, BRUSH, "play", { placed: placed || ["4,0", "4,1", "5,2"] });

  text(s, "WORKBENCH", 0, 534, W, "SemiBold", 18, P.ink, { tracking: 14, opacity: 0.8 });
  const tray = glassPanel(s, 50, 562, 440, 96, 28, 0.6, "Workbench tray");
  for (let i = 0; i < 5; i++) {
    const x = 18 + i * 82;
    rect(tray, x, 14, 70, 68, solid(P.pale, 0.9), { radius: 16, stroke: 0xFFFFFF, strokeWeight: 2, name: "Slot" });
    if (i < 2) rect(tray, x + 4, 18, 62, 60, vgrad([[0, mix(P.glazeTop, P.glazeBottom, 0.2 + i * 0.4)], [1, mix(P.glazeTop, P.glazeBottom, 0.4 + i * 0.4)]]), { radius: 12, name: "Shard" });
  }

  const rack = glassPanel(s, 40, 676, 460, 262, 30, 0.5, "Rack");
  const pile = ["oooooo", "o#oo#o", "oooooo", "..oo.."];
  for (let r = 0; r < pile.length; r++) for (let c = 0; c < 6; c++) {
    const ch = pile[r][c];
    if (ch !== "o") continue;
    const x = 16 + c * 72, y = 14 + r * 60;
    const open = r === 0;
    const t = ((r * 6 + c) * 37 % 100) / 100;
    rect(rack, x, y, 66, 56, vgrad([[0, mix(P.glazeTop, P.glazeBottom, t)], [1, mix(P.glazeTop, P.glazeBottom, Math.min(1, t + 0.25))]]),
      { radius: 12, stroke: open ? P.gold : 0xFFFFFF, strokeWeight: open ? 3 : 1, strokeOpacity: open ? 1 : 0.5, name: open ? "Shard open" : "Shard buried" });
    if (!open) { rect(rack, x, y, 66, 56, solid(0x1C3A78, 0.28), { radius: 12, name: "Buried shade" }); svgIcon(rack, "lock", x + 42, y + 32, 18, 0xFFFFFF, 2.6); }
  }
}

// ---------------------------------------------------------------- screens

function home(board, x, y, caption, level, name, difficulty, shape, cell, opts) {
  const s = screen(board, x, y, "Home - " + difficulty, caption);
  coinPill(s, 24, 30, 260);
  icon(s, "settings", 432, 26, 84);
  text(s, "Golden Mend", 0, 110, W, "Bold", 64, P.title, { shadow: 0xFFFFFF, shadowAlpha: 0.9, shadowY: 4, shadowBlur: 0 });
  const chip = glassPanel(s, 160, 196, 220, 48, 24, 0.6, "Name chip");
  text(chip, name.toUpperCase(), 0, 9, 220, "SemiBold", 22, P.ink, { tracking: 8 });
  const card = glassPanel(s, 40, 270, 460, 420, 40, 0.55, "Showcase card");
  vessel(card, 230, 210, cell, shape, "teaser", opts);
  levelButton(s, difficulty, level, 110, 706);
  button(s, "Secondary", "MUSEUM", 150, 862, 240, 72, 28);
  return s;
}

function museum(board, x, y) {
  const s = screen(board, x, y, "Museum", "03 Museum");
  icon(s, "back", 24, 30, 84);
  text(s, "Museum", 0, 38, W, "Bold", 60, P.title, { shadow: 0xFFFFFF, shadowAlpha: 0.9, shadowY: 4, shadowBlur: 0 });
  const items = [
    ["Tea Cup", ["XXX", "XAX"], 3, 0x6FA8FF, 0x3B5BDB],
    ["Rice Bowl", ["XXXX", "XAXX"], 3, 0xFFB3C7, 0xE86A92],
    ["Sake Cup", SAKE, 3, 0x9BE3FF, 0x2F7DD1],
    ["Tea Bowl", TEA, 2, 0xFFC27A, 0xC2622B],
    ["Ikebana Vase", [".XX.", "XXXX", "XXXX", ".XX.", ".AX."], 3, 0xB7F0C2, 0x2F9E6B],
    ["Brush Pot", BRUSH, -1, 0, 0]
  ];
  items.forEach(function (it, i) {
    const col = i % 2, row = Math.floor(i / 2);
    const cx = 32 + col * 246, cy = 150 + row * 268;
    const card = glassPanel(s, cx, cy, 230, 252, 30, it[2] < 0 ? 0.75 : 0.6, "Card " + it[0]);
    if (it[2] < 0) { card.children[0].strokes = solid(P.gold); card.children[0].strokeWeight = 4; }
    const niche = rect(card, 14, 14, 202, 130, solid(P.pale, 0.7), { radius: 20, name: "Niche" });
    if (it[2] >= 0) {
      vessel(card, 115, 79, 112 / Math.max(it[1].length, Math.max.apply(null, it[1].map(function (r) { return r.length; }))), it[1], "restored", { top: it[3], bottom: it[4] });
      text(card, it[0], 0, 152, 230, "Bold", 26, P.ink);
      stars(card, 115, 196, 30, it[2]);
    } else {
      vessel(card, 115, 79, 20, it[1], "teaser", { lit: ["0,1", "1,0"], glint: "2,3" });
      text(card, it[0], 0, 150, 230, "Bold", 26, P.ink);
      button(card, "Primary", "LEVEL 17", 25, 192, 180, 50, 22);
    }
  });
  return s;
}

function details(board, x, y) {
  const s = screen(board, x, y, "Vessel details", "04 Vessel details");
  museumBackdrop(s);
  scrim(s);
  const card = popup(s, 480, 760, 120, "Sake Cup", true);
  const niche = rect(card, 30, 70, 420, 250, solid(P.pale, 0.8), { radius: 26, name: "Niche" });
  vessel(card, 240, 195, 62, SAKE, "restored", { top: 0x9BE3FF, bottom: 0x2F7DD1 });
  text(card, "Hokusai, The Great Wave", 0, 334, 480, "Medium", 20, P.ink, { opacity: 0.75 });
  stars(card, 240, 380, 40, 3);
  const rows = [["Your best", "Workbench 1 - 1:14", P.ink], ["Best possible", "Workbench 1 - matched!", 0xE08A00], ["No-help best", "Workbench 2 - 1:35", P.ink], ["Times restored", "2", P.ink]];
  rows.forEach(function (r, i) {
    const ry = 452 + i * 52;
    text(card, r[0], 40, ry, 200, "Medium", 22, P.ink, { align: "LEFT", opacity: 0.8 });
    text(card, r[1], 200, ry, 240, "Bold", 22, r[2], { align: "RIGHT" });
    if (i < rows.length - 1) rect(card, 40, ry + 42, 400, 2, solid(P.dash, 0.4), { name: "Rule" });
  });
  button(card, "Primary", "REPLAY", 90, 664, 300, 84, 36);
  return s;
}

function museumBackdrop(s) {
  text(s, "Museum", 0, 38, W, "Bold", 60, P.title);
  for (let i = 0; i < 6; i++) glassPanel(s, 32 + (i % 2) * 246, 150 + Math.floor(i / 2) * 268, 230, 252, 30, 0.5, "Card");
}

function pause(board, x, y) {
  const s = screen(board, x, y, "Pause", "05 Pause");
  gameplay(s);
  scrim(s);
  const card = popup(s, 460, 560, 210, "Paused", true);
  button(card, "Confirm", "RESUME", 55, 76, 350, 100, 40);
  button(card, "Secondary", "RESTART", 55, 196, 350, 92, 36);
  button(card, "Secondary", "SETTINGS", 55, 306, 350, 92, 36);
  button(card, "Secondary", "HOME", 55, 416, 350, 92, 36);
  return s;
}

function levelComplete(board, x, y) {
  const s = screen(board, x, y, "Level complete", "06 Level complete");
  ellipse(s, 20, 150, 500, 500, [{ type: "GRADIENT_RADIAL", gradientTransform: [[1, 0, 0], [0, 1, 0]], gradientStops: [{ position: 0, color: rgba(0xFFF3B0, 0.85) }, { position: 1, color: rgba(0xFFF3B0, 0) }] }], { name: "Glow" });
  text(s, "Restored!", 0, 60, W, "Bold", 76, P.title, { shadow: 0xFFFFFF, shadowAlpha: 0.95, shadowY: 5, shadowBlur: 0 });
  stars(s, W / 2, 180, 84, 2);
  vessel(s, W / 2, 410, 46, BRUSH, "restored", { top: 0xFFB3C7, bottom: 0x7A5CFF });
  text(s, "Brush Pot joins your museum", 0, 570, W, "SemiBold", 26, P.ink);
  const reward = glassPanel(s, 190, 610, 160, 56, 28, 0.7, "Reward");
  ellipse(reward, 10, 8, 40, 40, vgrad([[0, 0xFFE27A], [1, P.goldDeep]]));
  text(reward, "+40", 56, 9, 92, "Bold", 30, P.ink);
  const stats = glassPanel(s, 50, 682, 440, 70, 24, 0.55, "Stats");
  text(stats, "Busiest workbench 2  -  best possible 2", 0, 8, 440, "SemiBold", 20, P.ink);
  text(stats, "Time 1:42", 0, 36, 440, "Medium", 18, P.ink, { opacity: 0.75 });
  levelButton(s, "Hard", 18, 110, 790);
  return s;
}

function benchFull(board, x, y) {
  const s = screen(board, x, y, "Workbench full", "07 Workbench full");
  gameplay(s);
  scrim(s);
  const card = popup(s, 480, 560, 220, "Bench full!", false);
  text(card, "No free slot for that shard.\nAdd two more and keep going?", 30, 70, 420, "SemiBold", 26, P.ink, { lineHeight: 130 });
  const cont = offerButton(card, "Confirm", "CONTINUE", 40, 170, 400, 110, 40);
  const pill = group(cont, 250, 28, 124, 54, "Price");
  rect(pill, 0, 0, 124, 54, solid(0x000000, 0.18), { radius: 27 });
  ellipse(pill, 8, 9, 36, 36, vgrad([[0, 0xFFE27A], [1, P.goldDeep]]));
  text(pill, "60", 46, 8, 70, "Bold", 30, 0xFFFFFF);
  text(card, "Adds 2 workbench slots - once per try", 0, 300, 480, "Medium", 20, P.ink, { opacity: 0.75 });
  button(card, "Secondary", "RETRY", 40, 360, 190, 86, 30);
  button(card, "Secondary", "HOME", 250, 360, 190, 86, 30);
  return s;
}

function hintOffer(board, x, y) {
  const s = screen(board, x, y, "Hint offer", "08 Hint offer");
  gameplay(s);
  scrim(s);
  const card = popup(s, 480, 560, 200, "Need a hint?", true);
  ellipse(card, 170, 66, 140, 140, [{ type: "GRADIENT_RADIAL", gradientTransform: [[1, 0, 0], [0, 1, 0]], gradientStops: [{ position: 0, color: rgba(0xFFF3B0, 1) }, { position: 1, color: rgba(0xFFF3B0, 0) }] }], { name: "Halo" });
  ellipse(card, 195, 91, 90, 90, vgrad([[0, 0xFFE27A], [1, P.goldDeep]]), { name: "Badge", stroke: 0xFFFFFF, strokeWeight: 4 });
  svgIcon(card, "hint", 214, 110, 52, 0xFFFFFF, 2.4);
  text(card, "Shows the piece that fits right now,\neven if it is buried.", 30, 200, 420, "SemiBold", 24, P.ink, { lineHeight: 130 });
  const buy = offerButton(card, "Primary", "GET HINT", 40, 290, 400, 104, 38);
  const pill = group(buy, 250, 25, 124, 54, "Price");
  rect(pill, 0, 0, 124, 54, solid(0x000000, 0.16), { radius: 27 });
  ellipse(pill, 8, 9, 36, 36, vgrad([[0, 0xFFE27A], [1, P.goldDeep]]));
  text(pill, "40", 46, 8, 70, "Bold", 30, 0xFFFFFF);
  button(card, "Sky", "WATCH AN AD", 40, 414, 400, 90, 32);
  return s;
}

function settings(board, x, y) {
  const s = screen(board, x, y, "Settings", "09 Settings");
  gameplay(s);
  scrim(s);
  const card = popup(s, 480, 600, 180, "Settings", true);
  [["sound", "Sound", true], ["music", "Music", true], ["haptics", "Haptics", false]].forEach(function (r, i) {
    const row = glassPanel(card, 30, 74 + i * 116, 420, 100, 26, 0.55, "Row " + r[1]);
    icon(row, r[0], 14, 12, 76);
    text(row, r[1], 106, 26, 180, "Bold", 34, P.ink, { align: "LEFT" });
    const track = rect(row, 296, 26, 104, 52, r[2] ? vgrad([[0, 0x7BE38E], [1, 0x2FB34E]]) : solid(0xB9CCEB), { radius: 26, name: "Switch", effects: [inner(0x000000, 0.12, 2, 4)] });
    ellipse(row, r[2] ? 350 : 300, 30, 44, 44, solid(0xFFFFFF), { name: "Knob", effects: [shadow(0x1C3A78, 0.3, 2, 4)] });
  });
  text(card, "Golden Mend 0.1.0", 0, 430, 480, "Medium", 18, P.ink, { opacity: 0.55 });
  button(card, "Primary", "DONE", 100, 476, 280, 92, 36);
  return s;
}

function gameplayScreen(board, x, y) {
  const s = screen(board, x, y, "Gameplay", "02 Gameplay");
  gameplay(s);
  return s;
}

// ---------------------------------------------------------------- run

async function main() {
  await Promise.all(["Bold", "SemiBold", "Medium"].map(function (st) { return figma.loadFontAsync(font(st)); }));

  let page = figma.root.children.filter(function (p) { return p.name === PAGE_NAME; })[0];
  if (!page) { page = figma.createPage(); page.name = PAGE_NAME; }
  await figma.setCurrentPageAsync(page);
  page.children.slice().forEach(function (n) { if (n.name === "Glass Kit" || n.name === "Sky Breeze screens") n.remove(); });

  buildKit(page);

  const board = figma.createFrame();
  board.name = "Sky Breeze screens"; board.x = 0; board.y = 780; board.resize(3640, 2340); board.cornerRadius = 32;
  board.fills = solid(0xF3F6FB);
  page.appendChild(board);
  text(board, "Sky Breeze - all screens", 48, 36, 2000, "Bold", 48, P.title, { align: "LEFT" });
  text(board, "The Level button replaces Play: orange = normal, ruby with a Hard tab = hard, deep violet with a Very Hard tab = very hard. No 'levels left' counts anywhere; the museum shows only reached vessels.", 48, 100, 3400, "Medium", 22, P.ink, { align: "LEFT" });

  const col = function (i) { return 48 + i * 600; };
  const row1 = 200, row2 = 1260;
  const jobs = [
    ["01 Home", function () { home(board, col(0), row1, "01 Home - normal level", 17, "Brush Pot", "Normal", BRUSH, 44, { lit: ["0,2", "1,1", "1,2", "2,0"], glint: "3,3", hop: "4,1" }); }],
    ["01b Home - hard", function () { home(board, col(1), row1, "01b Home - hard level", 18, "Storage Jar", "Hard", STORAGE, 52, { lit: ["0,2", "1,1", "1,2", "2,0"], glint: "2,4", hop: "3,1" }); }],
    ["01c Home - very hard", function () { home(board, col(2), row1, "01c Home - very hard level", 27, "Offering Bowl", "Very Hard", OFFERING, 52, { lit: ["0,0", "0,1", "1,0"], glint: "2,3", hop: "3,4" }); }],
    ["02 Gameplay", function () { gameplayScreen(board, col(3), row1); }],
    ["03 Museum", function () { museum(board, col(4), row1); }],
    ["04 Vessel details", function () { details(board, col(5), row1); }],
    ["05 Pause", function () { pause(board, col(0), row2); }],
    ["06 Level complete", function () { levelComplete(board, col(1), row2); }],
    ["07 Workbench full", function () { benchFull(board, col(2), row2); }],
    ["08 Hint offer", function () { hintOffer(board, col(3), row2); }],
    ["09 Settings", function () { settings(board, col(4), row2); }]
  ];
  const errors = [];
  jobs.forEach(function (job) {
    try { job[1](); } catch (e) { errors.push(job[0] + ": " + (e && e.message ? e.message : e)); }
  });

  figma.viewport.scrollAndZoomIntoView([board]);
  if (errors.length) {
    figma.closePlugin("Sky Breeze built with " + errors.length + " problem(s): " + errors.join(" | "));
  } else {
    figma.closePlugin("Sky Breeze: glass kit and " + jobs.length + " screens built.");
  }
}

main().catch(function (e) { figma.closePlugin("Sky Breeze failed: " + (e && e.message ? e.message : e)); });
