// Front end, direction A "light line" (brief 051, Glyph, 2026-10-23): the one table of numbers the mockup page draws from and the spec quotes.
// All sizes are ref px at 1080p height (device px = ref x H / 1080). Every colour is a kit.uss token (the `src` of each is the kit.uss name);
// alphas are of those colours, never new colours. `bun testbed/hud-style/frontend-export.ts` writes this to briefs/051/lightline/frontend.a.json.
export const A = {
  schema: "suborned.frontend/2-lightline",
  refHeight: 1080,
  colors: {
    cream: "#f7f4ec", ink: "#17302f", inkDeep: "#0b1615", textDim: "#b0cdc7", chrome: "#cbc6c3", panelLine: "#d2e6e2", live: "#60e0ea", brass: "#99784d",
  },
  // alphas of cream: the whole language has four
  alpha: { idleWord: 0.74, idleRow: 0.9, focus: 1, disabled: 0.4, chevIdle: 0.5, chevDisabled: 0.25 },
  // type: Jost Regular (400) everywhere; Jost Medium (500, "strong") only for the title lockup, SUBORNED and the brief's title. Tabular digits for values.
  type: {
    word:      { font: "Jost-Regular", sizeRef: 22,   trackingEm: 0.22, upper: true,  note: "main menu items" },
    wordSmall: { font: "Jost-Regular", sizeRef: 18,   trackingEm: 0.20, upper: true,  note: "pause menu items, Continue on the brief" },
    action:    { font: "Jost-Regular", sizeRef: 15.5, trackingEm: 0.24, upper: true,  note: "Reset page, Back, Swap, Cancel" },
    title:     { font: "Jost-Regular", sizeRef: 32,   trackingEm: 0.30, upper: true,  note: "OPTIONS, CONTROLS, PAUSED (22 in the pause card)" },
    tab:       { font: "Jost-Regular", sizeRef: 15.5, trackingEm: 0.20, upper: true },
    group:     { font: "Jost-Regular", sizeRef: 12.5, trackingEm: 0.20, upper: true, color: "textDim" },
    rowLabel:  { font: "Jost-Regular", sizeRef: 17,   trackingEm: 0.02, upper: false },
    rowValue:  { font: "Jost-Regular-Tab", sizeRef: 17, trackingEm: 0.0, upper: false },
    keycap:    { font: "Jost-Regular-Tab", sizeRef: 14.5, trackingEm: 0.02, upper: false },
    keycapAlt: { font: "Jost-Regular-Tab", sizeRef: 12, trackingEm: 0.02, upper: false, color: "textDim" },
    build:     { font: "Jost-Regular-Tab", sizeRef: 12.5, color: "textDim", alpha: 0.85 },
  },
  shadow: { color: "inkDeep", alpha: 0.55, blurRef: 6, yRef: 1, note: "text shadow under words on the picture only (not on the sheet)" },
  marker: { shape: "square turned 45 degrees", sizeRef: 9, color: "live", gutterRef: 30, note: "centre sits gutterRef left of the label's left edge" },
  line: { wordHRef: 2, rowHRef: 2, color: "live", idleHairRef: 1, idleHair: "panelLine", idleHairAlpha: 0.16, tickWRef: 3, tickInsetRef: 7 },
  // menu scrims: the left one is stronger than the plate-era 0.62 because words sit straight on it (measured: see SPEC)
  scrims: {
    menu: [
      { edge: "left",   frac: 0.40, hold: 0.50, alpha: 0.66 },
      { edge: "top",    frac: 0.34, hold: 0,    alpha: 0.50 },
      { edge: "bottom", frac: 0.28, hold: 0,    alpha: 0.68 },
    ],
    panelVeil: { alpha: 0.52 },
    panel: [{ edge: "bottom", frac: 0.28, hold: 0, alpha: 0.68, note: "the loading screen's bottom scrim, so the build line stays readable under the sheet at 16:10" }],
    brief: [
      { edge: "right",  frac: 0.425, holdRef: 656, alpha: 0.92, note: "hold = column (568) + its right margin (64) + 24; the old 0.705 of the width left the column in the fade at 16:10 (measured 1.7:1 on the dossier heads at 2880x1864)" },
      { edge: "bottom", frac: 0.20, hold: 0, alpha: 0.50 },
    ],
  },
  menu: { leftRef: 72, bottomRef: 100, itemW: 300, itemH: 48, gapRef: 12, textInsetRef: 52, lineWRef: 232, lineYRef: 37 },
  sheet: { glassTint: 0.72, glassBlurRef: 18, wRef: 960, hRef: 780, fill: "ink", fillAlpha: 0.84, border: "chrome", borderAlpha: 0.34, borderRef: 1, radiusRef: 2, padX: 48,
    titleTop: 42, tabsTop: 116, tabH: 36, tabPadX: 14, tabGap: 22, tabsRuleY: 152, bodyTop: 172, footerRuleFromBottom: 92, footerItemH: 40,
    rowH: 44, groupH: 40, rowLabelX: 14, rowControlRight: 14, controlW: 300, colGap: 44, controlsRowH: 34 },
  controls: {
    chevRef: 14, chevHit: 28, choiceValueW: 190,
    slider: { railW: 220, railH: 2, railAlpha: 0.38, knobRef: 11, knobBorderRef: 1.5, knobFocusRef: 15, valueW: 54 },
    switch: { boxRef: 20, boxBorderRef: 1.5, boxBorderAlpha: 0.6, diamondRef: 9, wordW: 38 },
    keycap: { hRef: 28, minW: 48, padX: 12, altH: 22, borderRef: 1.5, borderAlpha: 0.85, altBorderAlpha: 0.35, fillAlpha: 0.45, radiusRef: 3 },
    clash: { badgeRef: 18, stripH: 56, stripBadgeRef: 26 },
  },
  pause: { wRef: 360, topFrac: 0.12, padX: 32, padTop: 30, padBottom: 24, fill: "ink", fillAlpha: 0.9, itemH: 44, titleRef: 22, gutterRef: 30, lineWRef: 266, endGapRef: 14 },
  brief: { colRightRef: 64, colWRef: 568, colTopRef: 84, imageFocus: [1, 0.5], continueBottomRef: 64, continueW: 232 },
  // timing, seconds. USS easings: ease-out-cubic, ease-in-cubic exist in Unity 6
  timing: {
    markerSlide: { s: 0.18, ease: "ease-out-cubic", css: "cubic-bezier(0.215, 0.61, 0.355, 1)" },
    lineDraw:    { s: 0.22, delay: 0.04, ease: "ease-out-cubic", css: "cubic-bezier(0.215, 0.61, 0.355, 1)", note: "left to right, from the label's left edge" },
    lineRetract: { s: 0.10, ease: "ease-in-cubic", css: "cubic-bezier(0.55, 0.055, 0.675, 0.19)", note: "the leaving item's line shrinks back to the left" },
    tickSlide:   { s: 0.16, ease: "ease-out-cubic", css: "cubic-bezier(0.215, 0.61, 0.355, 1)" },
    tabSlide:    { s: 0.18, ease: "ease-out-cubic", css: "cubic-bezier(0.215, 0.61, 0.355, 1)" },
    labelFade:   { s: 0.12, ease: "ease-out", css: "ease-out" },
    pressIn: 0.08, pressOut: 0.12, valueNudge: 0.10, switchFlip: 0.12,
    screenFade: 0.30, itemStagger: 0.05, itemRise: 6, itemFade: 0.40, markerAppearDelay: 0.30, pageSwitch: 0.12, listenBlink: 1.0,
  },
};
export const hexA = (hex, a) => { const n = parseInt(hex.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; };
