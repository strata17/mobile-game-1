// Reveal — scratch-away puzzle
// Uncover >=85% of a board by dragging to scratch away foil tiles, while avoiding
// hidden bombs. Endless levels with scaling difficulty. Ad hooks: interstitial
// (every 3rd game over), rewarded continue, rewarded hint.

// ---------- Config ----------
const WIN_RATIO = 0.70;         // fraction of safe tiles to clear a level
const MAX_HEARTS = 3;           // mistakes allowed per level before it fails
const REVEAL_MS = 240;          // per-tile cover dissolve duration
const STAR_TIMES = [30, 60];    // <=30s -> 3 stars, <=60s -> 2 stars, else 1
const HINT_COST = 50;           // coins to spend on a hint instead of watching an ad
const BONUS_COIN = 25;          // coins from a surprise bonus tile
const LEVEL_COIN = 15;          // coins for clearing a level
const CHEST_EVERY = 5;          // a chapter chest every N levels
const DAILY_BASE = 40;          // base daily-reward coins (+streak bonus)
const ENDOW_TILES = 4;          // head-start tiles pre-revealed (endowed progress effect)
const NUDGE_REMAINING = 5;      // show "almost there" when this many safe tiles remain
const GLOW_AT = 0.7;            // progress fraction where the goal-gradient glow kicks in

const SCENES = [
  { emoji: "🐱", bg: ["#ff9a9e", "#fecfef"], deco: "#ffffff" },
  { emoji: "🚀", bg: ["#4facfe", "#00f2fe"], deco: "#ffffff" },
  { emoji: "🌈", bg: ["#a18cd1", "#fbc2eb"], deco: "#fff6b7" },
  { emoji: "🍩", bg: ["#f6d365", "#fda085"], deco: "#ffffff" },
  { emoji: "🦊", bg: ["#f77062", "#fe5196"], deco: "#ffe29a" },
  { emoji: "🐳", bg: ["#2af598", "#009efd"], deco: "#ffffff" },
  { emoji: "🎈", bg: ["#ff6a88", "#ff99ac"], deco: "#ffffff" },
  { emoji: "🌻", bg: ["#fceabb", "#f8b500"], deco: "#ffffff" },
  { emoji: "🍉", bg: ["#43e97b", "#38f9d7"], deco: "#ffffff" },
  { emoji: "🐢", bg: ["#0ba360", "#3cba92"], deco: "#f0fff4" },
  { emoji: "💎", bg: ["#a1c4fd", "#c2e9fb"], deco: "#ffffff" },
  { emoji: "🦄", bg: ["#fbc2eb", "#a6c1ee"], deco: "#ffffff" },
];

// ---------- DOM ----------
const canvas = document.getElementById("board");
const ctx = canvas.getContext("2d");
const boardWrap = document.getElementById("boardWrap");
const flashLayer = document.getElementById("flashLayer");
const tutorialHint = document.getElementById("tutorialHint");

const hudLevel = document.getElementById("hudLevel");
const hudScore = document.getElementById("hudScore");
const hudBest = document.getElementById("hudBest");
const progressFill = document.getElementById("progressFill");
const progressPct = document.getElementById("progressPct");

const menuScreen = document.getElementById("menuScreen");
const levelCompleteScreen = document.getElementById("levelCompleteScreen");
const gameOverScreen = document.getElementById("gameOverScreen");
const settingsScreen = document.getElementById("settingsScreen");
const adScreen = document.getElementById("adScreen");

const startBtn = document.getElementById("startBtn");
const howtoBtn = document.getElementById("howtoBtn");
const nextLevelBtn = document.getElementById("nextLevelBtn");
const continueBtn = document.getElementById("continueBtn");
const restartBtn = document.getElementById("restartBtn");
const hintBtn = document.getElementById("hintBtn");
const settingsBtn = document.getElementById("settingsBtn");
const soundBtn = document.getElementById("soundBtn");
const closeSettingsBtn = document.getElementById("closeSettingsBtn");
const soundToggle = document.getElementById("soundToggle");
const hapticsToggle = document.getElementById("hapticsToggle");

const clearedLevelEl = document.getElementById("clearedLevel");
const levelPointsEl = document.getElementById("levelPoints");
const finalScoreEl = document.getElementById("finalScore");
const adTimerEl = document.getElementById("adTimer");
const starEls = [...document.querySelectorAll("#starRow .star")];

const hudCoins = document.getElementById("hudCoins");
const coinPill = document.getElementById("coinPill");
const heartsRow = document.getElementById("heartsRow");
const goalNudge = document.getElementById("goalNudge");
const streakBadge = document.getElementById("streakBadge");
const streakDays = document.getElementById("streakDays");
const collectionRow = document.getElementById("collectionRow");
const collectionCount = document.getElementById("collectionCount");
const levelCoinsEl = document.getElementById("levelCoins");
const unlockNote = document.getElementById("unlockNote");
const nearMissEl = document.getElementById("nearMiss");
const resetBtn = document.getElementById("resetBtn");

const journeyFill = document.getElementById("journeyFill");
const chapterLabel = document.getElementById("chapterLabel");
const chestHint = document.getElementById("chestHint");
const dailyBtn = document.getElementById("dailyBtn");
const missionsList = document.getElementById("missionsList");
const chestBadge = document.getElementById("chestBadge");
const lcTitle = document.getElementById("lcTitle");

// ---------- Audio ----------
const Sound = {
  ctx: null,
  enabled: localStorage.getItem("reveal.sound") !== "off",
  init() {
    if (!this.ctx) {
      try { this.ctx = new (window.AudioContext || window.webkitAudioContext)(); }
      catch (e) { /* no audio */ }
    }
    if (this.ctx && this.ctx.state === "suspended") this.ctx.resume();
  },
  tone(freq, dur, type = "sine", gain = 0.06, when = 0) {
    if (!this.enabled || !this.ctx) return;
    const t = this.ctx.currentTime + when;
    const o = this.ctx.createOscillator();
    const g = this.ctx.createGain();
    o.type = type;
    o.frequency.setValueAtTime(freq, t);
    g.gain.setValueAtTime(gain, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(g).connect(this.ctx.destination);
    o.start(t); o.stop(t + dur);
  },
  noise(dur = 0.3, gain = 0.25) {
    if (!this.enabled || !this.ctx) return;
    const t = this.ctx.currentTime;
    const n = Math.floor(this.ctx.sampleRate * dur);
    const buf = this.ctx.createBuffer(1, n, this.ctx.sampleRate);
    const d = buf.getChannelData(0);
    for (let i = 0; i < n; i++) d[i] = (Math.random() * 2 - 1) * (1 - i / n);
    const src = this.ctx.createBufferSource();
    const g = this.ctx.createGain();
    src.buffer = buf;
    g.gain.setValueAtTime(gain, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    src.connect(g).connect(this.ctx.destination);
    src.start(t);
  },
  reveal(combo) { this.tone(420 + Math.min(combo, 18) * 34, 0.05, "triangle", 0.05); },
  tap() { this.tone(280, 0.05, "square", 0.04); },
  bomb() { this.noise(0.35, 0.3); this.tone(160, 0.5, "sawtooth", 0.12); this.tone(70, 0.6, "sine", 0.14); },
  win() { [523, 659, 784, 1047].forEach((f, i) => this.tone(f, 0.22, "triangle", 0.07, i * 0.09)); },
  bonus() { [880, 1175, 1568].forEach((f, i) => this.tone(f, 0.1, "triangle", 0.06, i * 0.05)); },
  coin() { this.tone(1319, 0.09, "triangle", 0.05); },
};

function haptic(ms) {
  if (state.haptics && navigator.vibrate) navigator.vibrate(ms);
}

// ---------- State ----------
const state = {
  level: Number(localStorage.getItem("reveal.level") || 1),
  score: 0,
  best: Number(localStorage.getItem("reveal.best") || 0),
  coins: Number(localStorage.getItem("reveal.coins") || 0),
  gridSize: 8,
  cellSize: 0,
  boardPx: 360,
  revealed: [],
  revealAt: [],
  bomb: [],
  bonus: [],
  hearts: MAX_HEARTS,
  revealedCount: 0,
  nonBombTotal: 0,
  usedContinueThisRun: false,
  gameOverCount: Number(localStorage.getItem("reveal.gameOverCount") || 0),
  playing: false,
  levelStart: 0,
  scene: SCENES[0],
  haptics: localStorage.getItem("reveal.haptics") !== "off",
  hasScratchedEver: localStorage.getItem("reveal.tutorialDone") === "1",
  collection: safeParse(localStorage.getItem("reveal.collection"), []),
  streak: 0,
  missions: [],
  _lastScore: 0,
};

// ---------- Daily missions ----------
const MISSION_POOL = [
  { id: "clear3", type: "clearLevels", goal: 3, text: "Clear {goal} levels", reward: 40 },
  { id: "clear6", type: "clearLevels", goal: 6, text: "Clear {goal} levels", reward: 80 },
  { id: "bonus4", type: "findBonus", goal: 4, text: "Find {goal} bonus tiles", reward: 50 },
  { id: "coins200", type: "earnCoins", goal: 200, text: "Earn {goal} coins", reward: 40 },
  { id: "perfect2", type: "perfectClears", goal: 2, text: "Clear {goal} levels without losing a heart", reward: 90 },
  { id: "chest1", type: "chests", goal: 1, text: "Open {goal} chapter chest", reward: 60 },
];

function today() { return new Date().toDateString(); }

function loadMissions() {
  const raw = safeParse(localStorage.getItem("reveal.missions"), null);
  if (raw && raw.date === today() && Array.isArray(raw.list)) {
    state.missions = raw.list;
    return;
  }
  const pool = [...MISSION_POOL];
  shuffle(pool);
  state.missions = pool.slice(0, 2).map((m) => ({
    id: m.id, type: m.type, goal: m.goal, reward: m.reward,
    text: m.text.replace("{goal}", m.goal), progress: 0, claimed: false,
  }));
  saveMissions();
}
function saveMissions() {
  localStorage.setItem("reveal.missions", JSON.stringify({ date: today(), list: state.missions }));
}
function progressMission(type, n = 1) {
  let changed = false;
  state.missions.forEach((m) => {
    if (m.type === type && !m.claimed && m.progress < m.goal) {
      m.progress = Math.min(m.goal, m.progress + n);
      changed = true;
    }
  });
  if (changed) saveMissions();
}
function claimMission(m) {
  if (m.claimed || m.progress < m.goal) return;
  m.claimed = true;
  saveMissions();
  addCoins(m.reward);
  Sound.bonus();
  renderMissions();
}
function renderMissions() {
  missionsList.innerHTML = "";
  state.missions.forEach((m) => {
    const done = m.progress >= m.goal;
    const row = document.createElement("div");
    row.className = "mission " + (m.claimed ? "claimed" : done ? "ready" : "");
    const pct = Math.min(100, Math.round((m.progress / m.goal) * 100));
    const rewardLabel = m.claimed ? "✓ Claimed" : done ? `Claim +${m.reward} 🪙` : `${m.progress}/${m.goal}`;
    row.innerHTML =
      `<div class="mission-text">${m.text}</div>` +
      `<div class="mission-bar"><div style="width:${pct}%"></div></div>` +
      `<div class="mission-reward">${rewardLabel}</div>`;
    if (done && !m.claimed) {
      row.querySelector(".mission-reward").addEventListener("click", () => claimMission(m));
    }
    missionsList.appendChild(row);
  });
}

// ---------- Daily reward ----------
function dailyAvailable() { return localStorage.getItem("reveal.dailyClaim") !== today(); }
function dailyAmount() { return DAILY_BASE + Math.min(state.streak, 7) * 10; }
function renderDaily() {
  if (dailyAvailable()) {
    dailyBtn.textContent = `🎁 Daily reward · +${dailyAmount()} 🪙`;
    dailyBtn.classList.remove("hidden", "claimed");
    dailyBtn.disabled = false;
  } else {
    dailyBtn.textContent = "🎁 Daily reward claimed — come back tomorrow";
    dailyBtn.classList.remove("hidden");
    dailyBtn.classList.add("claimed");
    dailyBtn.disabled = true;
  }
}
dailyBtn.addEventListener("click", () => {
  if (!dailyAvailable()) return;
  Sound.init();
  const amt = dailyAmount();
  localStorage.setItem("reveal.dailyClaim", today());
  addCoins(amt);
  Sound.bonus();
  renderDaily();
});

// ---------- Journey / chapters ----------
function chapterOf(level) { return Math.floor((level - 1) / CHEST_EVERY) + 1; }
function renderJourney() {
  const level = Number(localStorage.getItem("reveal.level") || 1);
  chapterLabel.textContent = `Chapter ${chapterOf(level)} · Level ${level}`;
  const into = (level - 1) % CHEST_EVERY;
  const toChest = CHEST_EVERY - into;
  chestHint.textContent = toChest === 1 ? "🎁 next level!" : `🎁 in ${toChest}`;
  journeyFill.style.width = (into / CHEST_EVERY) * 100 + "%";
}

function safeParse(s, fallback) { try { return s ? JSON.parse(s) : fallback; } catch (e) { return fallback; } }

hudBest.textContent = state.best;
hudCoins.textContent = state.coins;

function renderHearts() {
  let s = "";
  for (let i = 0; i < MAX_HEARTS; i++) s += i < state.hearts ? "❤️" : "🖤";
  heartsRow.textContent = s;
}

function addCoins(n) {
  state.coins += n;
  localStorage.setItem("reveal.coins", String(state.coins));
  hudCoins.textContent = state.coins;
  coinPill.classList.remove("bump");
  void coinPill.offsetWidth;
  coinPill.classList.add("bump");
  if (typeof refreshHintButton === "function") refreshHintButton();
}

// ---------- Canvas / DPI ----------
let dpr = 1;
let hiddenCanvas = document.createElement("canvas");
let hiddenCtx = hiddenCanvas.getContext("2d");
let coverTile = null;

function makeCoverTile() {
  const s = 96;
  const c = document.createElement("canvas");
  c.width = c.height = s;
  const g = c.getContext("2d");
  const grad = g.createLinearGradient(0, 0, s, s);
  grad.addColorStop(0, "#c9ccd6");
  grad.addColorStop(0.45, "#9fa3b2");
  grad.addColorStop(0.55, "#babecb");
  grad.addColorStop(1, "#8a8e9d");
  g.fillStyle = grad;
  g.fillRect(0, 0, s, s);
  // diagonal sheen
  g.globalAlpha = 0.25;
  g.strokeStyle = "#ffffff";
  g.lineWidth = 6;
  g.beginPath(); g.moveTo(-10, 30); g.lineTo(40, -10); g.stroke();
  g.globalAlpha = 0.12;
  g.beginPath(); g.moveTo(50, s + 10); g.lineTo(s + 10, 50); g.stroke();
  // subtle speckle
  g.globalAlpha = 0.08;
  g.fillStyle = "#ffffff";
  for (let i = 0; i < 18; i++) g.fillRect(Math.random() * s, Math.random() * s, 2, 2);
  g.globalAlpha = 1;
  coverTile = c;
}
makeCoverTile();

function sizeBoard() {
  const avail = Math.min(boardWrap.parentElement.clientWidth, 440);
  state.boardPx = Math.max(280, Math.floor(avail));
  dpr = Math.min(window.devicePixelRatio || 1, 3);

  canvas.style.width = state.boardPx + "px";
  canvas.style.height = state.boardPx + "px";
  canvas.width = Math.floor(state.boardPx * dpr);
  canvas.height = Math.floor(state.boardPx * dpr);

  hiddenCanvas.width = canvas.width;
  hiddenCanvas.height = canvas.height;

  state.cellSize = state.boardPx / state.gridSize;
  composeHidden();
}

// ---------- Level generation ----------
// Grow the board slowly so early levels stay familiar (8x8 for the first 5).
function gridSizeForLevel(level) { return Math.min(8 + Math.floor((level - 1) / 5), 12); }
// Very gentle ramp: 1 bomb for levels 1-5, then +1 every 5 levels. Bombs are
// visible and avoidable, so this is the whole difficulty dial — keep it slow.
function bombCountForLevel(level, cells) {
  const bombs = 1 + Math.floor((level - 1) / 5);
  return Math.min(bombs, Math.floor(cells * 0.18));
}

function buildLevel(level) {
  const gridSize = gridSizeForLevel(level);
  const revealed = Array.from({ length: gridSize }, () => Array(gridSize).fill(false));
  const revealAt = Array.from({ length: gridSize }, () => Array(gridSize).fill(0));
  const bomb = Array.from({ length: gridSize }, () => Array(gridSize).fill(false));
  const bonus = Array.from({ length: gridSize }, () => Array(gridSize).fill(false));

  const target = bombCountForLevel(level, gridSize * gridSize);
  let bombCount = 0;
  while (bombCount < target) {
    const r = Math.floor(Math.random() * gridSize);
    const c = Math.floor(Math.random() * gridSize);
    if (!bomb[r][c]) { bomb[r][c] = true; bombCount++; }
  }

  state.gridSize = gridSize;
  state.cellSize = state.boardPx / gridSize;
  state.revealed = revealed;
  state.revealAt = revealAt;
  state.bomb = bomb;
  state.bonus = bonus;
  state.revealedCount = 0;
  state.nonBombTotal = gridSize * gridSize - bombCount;
  state.scene = SCENES[Math.floor(Math.random() * SCENES.length)];
  state.levelStart = performance.now();
  // score is per-level attempt; each level (and each retry) starts fresh.
  state.score = 0;
  state._lastScore = 0;
  // one ad-continue is available per level attempt.
  state.usedContinueThisRun = false;
  // fresh hearts for the new board
  state.hearts = MAX_HEARTS;
  renderHearts();

  // Variable-ratio reward (Skinner): 1-3 hidden bonus tiles that pay coins when
  // scratched — unpredictable payoffs on otherwise-ordinary tiles.
  const safeCells = [];
  for (let r = 0; r < gridSize; r++)
    for (let c = 0; c < gridSize; c++)
      if (!bomb[r][c]) safeCells.push([r, c]);
  shuffle(safeCells);
  const bonusCount = 1 + Math.floor(Math.random() * 3);
  for (let i = 0; i < bonusCount && i < safeCells.length; i++) {
    const [r, c] = safeCells[i];
    bonus[r][c] = true;
  }

  // Endowed progress effect (Nunes & Drèze 2006): pre-reveal a few safe,
  // non-bonus corner-ish tiles so the player starts already partway to the goal.
  const endowPool = safeCells.filter(([r, c]) => !bonus[r][c]);
  endowPool.sort((a, b) => cornerDist(a, gridSize) - cornerDist(b, gridSize));
  for (let i = 0; i < ENDOW_TILES && i < endowPool.length; i++) {
    const [r, c] = endowPool[i];
    revealed[r][c] = true;
    revealAt[r][c] = performance.now() - REVEAL_MS; // already fully dissolved
    state.revealedCount++;
  }

  composeHidden();
  updateHud();
}

function shuffle(arr) {
  for (let i = arr.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [arr[i], arr[j]] = [arr[j], arr[i]];
  }
}

// distance to the nearest corner, so endowed tiles cluster at the edges
function cornerDist([r, c], n) {
  return Math.min(r, n - 1 - r) + Math.min(c, n - 1 - c);
}

function composeHidden() {
  const g = hiddenCtx;
  const P = state.boardPx;
  g.setTransform(dpr, 0, 0, dpr, 0, 0);
  g.clearRect(0, 0, P, P);

  const grad = g.createLinearGradient(0, 0, P, P);
  grad.addColorStop(0, state.scene.bg[0]);
  grad.addColorStop(1, state.scene.bg[1]);
  g.fillStyle = grad;
  g.fillRect(0, 0, P, P);

  // soft bokeh
  g.save();
  for (let i = 0; i < 7; i++) {
    g.globalAlpha = 0.12 + Math.random() * 0.12;
    g.fillStyle = state.scene.deco;
    const r = P * (0.05 + Math.random() * 0.12);
    g.beginPath();
    g.arc(Math.random() * P, Math.random() * P, r, 0, Math.PI * 2);
    g.fill();
  }
  g.restore();

  // subject
  g.textAlign = "center";
  g.textBaseline = "middle";
  g.font = `${Math.floor(P * 0.5)}px serif`;
  g.fillText(state.scene.emoji, P / 2, P / 2 + P * 0.02);

  // vignette
  const vg = g.createRadialGradient(P / 2, P / 2, P * 0.3, P / 2, P / 2, P * 0.72);
  vg.addColorStop(0, "rgba(0,0,0,0)");
  vg.addColorStop(1, "rgba(0,0,0,0.28)");
  g.fillStyle = vg;
  g.fillRect(0, 0, P, P);
}

// ---------- Particles / shake ----------
let particles = [];
let shake = 0;

function burst(x, y, color, count, spread) {
  for (let i = 0; i < count; i++) {
    const a = Math.random() * Math.PI * 2;
    const sp = spread * (0.3 + Math.random() * 0.7);
    particles.push({
      x, y,
      vx: Math.cos(a) * sp,
      vy: Math.sin(a) * sp - spread * 0.3,
      life: 1, decay: 0.02 + Math.random() * 0.02,
      size: 2 + Math.random() * 4,
      color, grav: 0.25,
    });
  }
}

// floating text popups ("+25 🪙", etc.) drawn in the render loop
let popups = [];
function popText(x, y, text, color) {
  popups.push({ x, y, text, color, life: 1 });
}

function confetti() {
  const colors = ["#ff5470", "#ffcb47", "#38e07b", "#4facfe", "#a18cd1", "#ffffff"];
  for (let i = 0; i < 90; i++) {
    particles.push({
      x: Math.random() * state.boardPx,
      y: -10,
      vx: (Math.random() - 0.5) * 3,
      vy: 2 + Math.random() * 4,
      life: 1, decay: 0.006 + Math.random() * 0.006,
      size: 4 + Math.random() * 5,
      color: colors[Math.floor(Math.random() * colors.length)],
      grav: 0.08, spin: (Math.random() - 0.5) * 0.4, rot: Math.random() * 7,
    });
  }
}

// ---------- HUD ----------
function updateHud() {
  hudLevel.textContent = state.level;
  hudScore.textContent = state.score;
  hudBest.textContent = state.best;
  const threshold = Math.ceil(state.nonBombTotal * WIN_RATIO);
  const frac = threshold === 0 ? 0 : state.revealedCount / threshold;
  const pct = Math.min(100, Math.round(frac * 100));
  progressFill.style.width = pct + "%";
  progressPct.textContent = pct + "%";

  // Goal-gradient effect: light up the bar as the finish line nears
  const near = frac >= GLOW_AT && frac < 1;
  progressFill.classList.toggle("near-goal", near);
  progressPct.classList.toggle("near-goal", near);

  // Zeigarnik / goal-gradient nudge: surface exactly how few tiles remain
  const remaining = Math.max(0, threshold - state.revealedCount);
  if (state.playing && remaining > 0 && remaining <= NUDGE_REMAINING) {
    goalNudge.textContent = `🔥 Almost! ${remaining} to go`;
    goalNudge.classList.remove("hidden");
  } else {
    goalNudge.classList.add("hidden");
  }
}

function bumpScore() {
  hudScore.classList.remove("bump");
  void hudScore.offsetWidth;
  hudScore.classList.add("bump");
}

// ---------- Reveal logic ----------
function cellCenter(r, c) {
  return { x: (c + 0.5) * state.cellSize, y: (r + 0.5) * state.cellSize };
}

// returns true if this reveal ended the stroke (bomb or win)
function revealCell(r, c, combo) {
  if (!state.playing) return false; // ignore reveals once the level has ended (prevents double-completion)
  if (r < 0 || c < 0 || r >= state.gridSize || c >= state.gridSize) return false;
  if (state.revealed[r][c]) return false;
  state.revealed[r][c] = true;
  state.revealAt[r][c] = performance.now();

  const { x, y } = cellCenter(r, c);

  if (state.bomb[r][c]) {
    // Hitting a bomb costs a heart, not the whole run. The cell stays revealed
    // (shown as a spent 💣) so it can't be hit again. Only losing all hearts
    // fails the level — a single slip near the finish no longer means redo.
    state.hearts--;
    renderHearts();
    if (state.hearts <= 0) {
      triggerGameOver(r, c); // handles the dramatic particles/shake/flash/sound
    } else {
      burst(x, y, "#ff5470", 26, state.cellSize * 0.16);
      burst(x, y, "#ffcb47", 12, state.cellSize * 0.12);
      shake = 16;
      Sound.bomb();
      haptic([50, 30, 80]);
      flashLayer.className = "flash-bomb";
      setTimeout(() => { flashLayer.className = ""; }, 400);
      popText(x, y - state.cellSize * 0.2, "-1 ❤️", "#ff5470");
    }
    return true;
  }

  state.revealedCount++;
  const gain = 10 + Math.min(combo, 12) * 2;
  state.score += gain;
  burst(x, y, state.scene.bg[0], 6, state.cellSize * 0.06);
  Sound.reveal(combo);

  // Variable-ratio surprise payout
  if (state.bonus[r][c]) {
    addCoins(BONUS_COIN);
    progressMission("findBonus", 1);
    progressMission("earnCoins", BONUS_COIN);
    popText(x, y - state.cellSize * 0.2, "+" + BONUS_COIN + " 🪙", "#ffcb47");
    burst(x, y, "#ffcb47", 16, state.cellSize * 0.12);
    Sound.bonus();
    haptic(20);
  }

  if (state.revealedCount >= Math.ceil(state.nonBombTotal * WIN_RATIO)) {
    triggerLevelComplete();
    return true;
  }
  return false;
}

let comboCount = 0;
let lastX = null, lastY = null;

function toBoard(clientX, clientY) {
  const rect = canvas.getBoundingClientRect();
  return {
    x: (clientX - rect.left) * (state.boardPx / rect.width),
    y: (clientY - rect.top) * (state.boardPx / rect.height),
  };
}

// Precise single-cell scratch trail. We interpolate between successive pointer
// samples so a fast drag reveals a continuous 1-cell-wide path — and can't skip
// over a bomb that lies on that path. Bombs are visible, so steering around them
// is the skill; dragging onto one detonates it.
function scratchAt(clientX, clientY) {
  if (!state.playing) return;
  const { x, y } = toBoard(clientX, clientY);

  if (!state.hasScratchedEver) {
    state.hasScratchedEver = true;
    localStorage.setItem("reveal.tutorialDone", "1");
    tutorialHint.classList.add("hidden");
  }

  if (lastX === null) { lastX = x; lastY = y; }
  const dist = Math.hypot(x - lastX, y - lastY);
  const steps = Math.max(1, Math.ceil(dist / (state.cellSize * 0.35)));

  let stopped = false;
  for (let i = 1; i <= steps && !stopped; i++) {
    const px = lastX + (x - lastX) * (i / steps);
    const py = lastY + (y - lastY) * (i / steps);
    const col = Math.floor(px / state.cellSize);
    const row = Math.floor(py / state.cellSize);
    if (row < 0 || col < 0 || row >= state.gridSize || col >= state.gridSize) continue;
    if (state.revealed[row][col]) continue;
    comboCount++;
    stopped = revealCell(row, col, comboCount);
  }
  lastX = x; lastY = y;

  updateHud();
  if (state.score !== state._lastScore) { bumpScore(); state._lastScore = state.score; }
}

// ---------- Input ----------
let dragging = false;
canvas.addEventListener("pointerdown", (e) => {
  Sound.init();
  dragging = true;
  comboCount = 0;
  lastX = lastY = null;
  scratchAt(e.clientX, e.clientY);
});
canvas.addEventListener("pointermove", (e) => { if (dragging) scratchAt(e.clientX, e.clientY); });
window.addEventListener("pointerup", () => { dragging = false; comboCount = 0; lastX = lastY = null; });
canvas.addEventListener("contextmenu", (e) => e.preventDefault());

// ---------- Render loop ----------
let lastFrame = performance.now();
function frame(now) {
  requestAnimationFrame(frame);
  const dt = Math.min((now - lastFrame) / 16.67, 3);
  lastFrame = now;

  // shake offset
  let sx = 0, sy = 0;
  if (shake > 0.2) {
    sx = (Math.random() - 0.5) * shake;
    sy = (Math.random() - 0.5) * shake;
    shake *= 0.86;
  } else shake = 0;

  ctx.setTransform(dpr, 0, 0, dpr, sx * dpr, sy * dpr);
  ctx.clearRect(-20, -20, state.boardPx + 40, state.boardPx + 40);

  // hidden image
  ctx.drawImage(hiddenCanvas, 0, 0, state.boardPx, state.boardPx);

  const cs = state.cellSize;
  const inset = Math.max(0.5, cs * 0.03);

  for (let r = 0; r < state.gridSize; r++) {
    for (let c = 0; c < state.gridSize; c++) {
      const x = c * cs, y = r * cs;
      const revealed = state.revealed[r][c];
      let coverAlpha = 1, scale = 1;

      if (revealed) {
        const t = (now - state.revealAt[r][c]) / REVEAL_MS;
        if (t >= 1) {
          // fully revealed: draw bomb marker if applicable, else nothing (base shows)
          if (state.bomb[r][c]) drawBomb(x, y, cs);
          continue;
        }
        coverAlpha = 1 - t;
        scale = 1 - 0.25 * t;
      }

      const sz = (cs - inset * 2) * scale;
      const ox = x + (cs - sz) / 2, oy = y + (cs - sz) / 2;
      ctx.globalAlpha = coverAlpha;
      ctx.drawImage(coverTile, ox, oy, sz, sz);
      ctx.globalAlpha = 1;

      // visible bomb warning so the player can scratch around it
      if (!revealed && state.bomb[r][c]) {
        const pulse = 0.7 + 0.3 * Math.sin(now / 260);
        ctx.globalAlpha = 0.5 * pulse;
        ctx.fillStyle = "#ff2b3d";
        ctx.fillRect(ox, oy, sz, sz);
        ctx.globalAlpha = 1;
        ctx.font = `${cs * 0.55}px serif`;
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText("⚠️", x + cs / 2, y + cs / 2 + cs * 0.02);
      }

      if (revealed) {
        // white pop flash as it dissolves
        ctx.globalAlpha = coverAlpha * 0.6;
        ctx.fillStyle = "#ffffff";
        ctx.fillRect(ox, oy, sz, sz);
        ctx.globalAlpha = 1;
      }
    }
  }

  // particles
  for (let i = particles.length - 1; i >= 0; i--) {
    const p = particles[i];
    p.vy += p.grav * dt;
    p.x += p.vx * dt;
    p.y += p.vy * dt;
    p.life -= p.decay * dt;
    if (p.spin !== undefined) p.rot += p.spin * dt;
    if (p.life <= 0 || p.y > state.boardPx + 20) { particles.splice(i, 1); continue; }
    ctx.globalAlpha = Math.max(0, p.life);
    ctx.fillStyle = p.color;
    if (p.spin !== undefined) {
      ctx.save();
      ctx.translate(p.x, p.y);
      ctx.rotate(p.rot);
      ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 0.6);
      ctx.restore();
    } else {
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
      ctx.fill();
    }
  }
  ctx.globalAlpha = 1;

  // floating reward text
  for (let i = popups.length - 1; i >= 0; i--) {
    const p = popups[i];
    p.life -= 0.012 * dt;
    p.y -= 0.6 * dt;
    if (p.life <= 0) { popups.splice(i, 1); continue; }
    ctx.globalAlpha = Math.min(1, p.life * 1.5);
    ctx.font = "800 20px -apple-system, sans-serif";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.lineWidth = 3;
    ctx.strokeStyle = "rgba(0,0,0,0.5)";
    ctx.strokeText(p.text, p.x, p.y);
    ctx.fillStyle = p.color;
    ctx.fillText(p.text, p.x, p.y);
  }
  ctx.globalAlpha = 1;

  // combo badge near cursor
  if (dragging && comboCount >= 4) {
    // drawn subtly at top center to avoid finger occlusion
    ctx.font = "800 22px -apple-system, sans-serif";
    ctx.textAlign = "center";
    ctx.fillStyle = "rgba(255,203,71,0.95)";
    ctx.fillText("COMBO x" + Math.min(comboCount, 99), state.boardPx / 2, 30);
  }
}
requestAnimationFrame(frame);

function drawBomb(x, y, cs) {
  ctx.fillStyle = "rgba(255,84,112,0.9)";
  ctx.fillRect(x, y, cs, cs);
  ctx.font = `${cs * 0.6}px serif`;
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillText("💣", x + cs / 2, y + cs / 2);
}

// ---------- Flow ----------
function show(el) { el.classList.remove("hidden"); }
function hide(el) { el.classList.add("hidden"); }

function startRun() {
  Sound.init();
  Sound.tap();
  // resume the player's saved progress (levels persist; deaths don't reset them)
  state.level = Number(localStorage.getItem("reveal.level") || 1);
  hide(menuScreen); hide(gameOverScreen); hide(levelCompleteScreen);
  state.playing = true;
  particles = [];
  popups = [];
  buildLevel(state.level);
  refreshHintButton();
  maybeShowTutorial();
}

function maybeShowTutorial() {
  if (!state.hasScratchedEver) tutorialHint.classList.remove("hidden");
  else tutorialHint.classList.add("hidden");
}

function triggerLevelComplete() {
  state.playing = false;
  goalNudge.classList.add("hidden");
  const isChest = state.level % CHEST_EVERY === 0;      // chapter milestone
  const perfect = state.hearts === MAX_HEARTS;          // cleared without a hit

  const bonus = 50 * state.level;
  state.score += bonus;
  if (state.score > state.best) { state.best = state.score; localStorage.setItem("reveal.best", String(state.best)); }

  // reward coins (peak-end rule). Chapter chests pay a jackpot.
  const coinReward = isChest ? (80 + 15 * state.level) : (LEVEL_COIN + 3 * state.level);
  addCoins(coinReward);

  // daily-mission progress
  progressMission("clearLevels", 1);
  progressMission("earnCoins", coinReward);
  if (perfect) progressMission("perfectClears", 1);
  if (isChest) progressMission("chests", 1);

  confetti();
  if (isChest) setTimeout(confetti, 260);
  flashLayer.className = "flash-win";
  Sound.win();
  haptic([30, 40, 60]);

  const secs = (performance.now() - state.levelStart) / 1000;
  const stars = secs <= STAR_TIMES[0] ? 3 : secs <= STAR_TIMES[1] ? 2 : 1;

  // Collection / completionism: record this level's picture; a chest guarantees
  // a brand-new one even if this level's picture was already owned.
  let newlyFound = !state.collection.includes(state.scene.emoji);
  if (newlyFound) state.collection.push(state.scene.emoji);
  if (isChest && !newlyFound) {
    const locked = SCENES.map((s) => s.emoji).filter((e) => !state.collection.includes(e));
    if (locked.length) { state.collection.push(locked[Math.floor(Math.random() * locked.length)]); newlyFound = true; }
  }
  if (newlyFound) localStorage.setItem("reveal.collection", JSON.stringify(state.collection));

  // chest vs normal presentation
  chestBadge.classList.toggle("hidden", !isChest);
  lcTitle.textContent = isChest ? `Chapter ${chapterOf(state.level)} Cleared! 🎁` : "Board Cleared!";

  if (newlyFound) {
    const shown = state.collection[state.collection.length - 1];
    unlockNote.textContent = `✨ New picture unlocked! ${shown}  (${state.collection.length}/${SCENES.length})`;
    unlockNote.classList.remove("hidden");
  } else {
    unlockNote.classList.add("hidden");
  }

  clearedLevelEl.textContent = state.level;
  levelPointsEl.textContent = bonus;
  levelCoinsEl.textContent = coinReward;
  hudScore.textContent = state.score;
  updateHud();

  starEls.forEach((s, i) => {
    s.classList.remove("lit");
    if (i < stars) setTimeout(() => s.classList.add("lit"), 120 + i * 120);
  });

  setTimeout(() => { show(levelCompleteScreen); flashLayer.className = ""; }, 620);
}

function goToNextLevel() {
  Sound.tap();
  hide(levelCompleteScreen);
  state.level++;
  localStorage.setItem("reveal.level", String(state.level)); // progress persists
  state.playing = true;
  particles = [];
  popups = [];
  buildLevel(state.level);
  refreshHintButton();
}

function triggerGameOver(r, c) {
  state.playing = false;
  goalNudge.classList.add("hidden");
  const { x, y } = cellCenter(r, c);
  burst(x, y, "#ff5470", 40, state.cellSize * 0.18);
  burst(x, y, "#ffcb47", 20, state.cellSize * 0.14);
  shake = 22;
  flashLayer.className = "flash-bomb";
  Sound.bomb();
  haptic([60, 30, 120]);

  if (state.score > state.best) { state.best = state.score; localStorage.setItem("reveal.best", String(state.best)); }

  // Near-miss framing (Reid 1986): tell the player how close they were, to
  // motivate a continue/retry. Only emphasize when it was genuinely close.
  const threshold = Math.ceil(state.nonBombTotal * WIN_RATIO);
  const pctThere = threshold === 0 ? 0 : Math.min(99, Math.round((state.revealedCount / threshold) * 100));
  updateHud();

  setTimeout(() => {
    flashLayer.className = "";
    nearMissEl.textContent = pctThere >= 50 ? `😤 So close — you were ${pctThere}% of the way there!` : "";
    finalScoreEl.textContent = state.score;
    continueBtn.disabled = state.usedContinueThisRun;
    continueBtn.innerHTML = state.usedContinueThisRun
      ? "No continues left"
      : `<span class="reward-tag">AD</span> Refill ❤️ &amp; keep going`;
    restartBtn.textContent = `Retry level ${state.level}`;
    show(gameOverScreen);
  }, 700);
}

function playMockAd(durationMs, onDone) {
  show(adScreen);
  const start = performance.now();
  (function tick() {
    const elapsed = performance.now() - start;
    if (elapsed >= durationMs) { hide(adScreen); onDone(); return; }
    adTimerEl.textContent = `Ad · ${Math.ceil((durationMs - elapsed) / 1000)}s`;
    requestAnimationFrame(tick);
  })();
}

function maybeInterstitial(onDone) {
  state.gameOverCount++;
  localStorage.setItem("reveal.gameOverCount", String(state.gameOverCount));
  if (state.gameOverCount % 3 === 0) playMockAd(2000, onDone);
  else onDone();
}

// ---------- Buttons ----------
startBtn.addEventListener("click", startRun);
howtoBtn.addEventListener("click", () => {
  Sound.tap();
  state.hasScratchedEver = false;
  localStorage.removeItem("reveal.tutorialDone");
  startRun();
});
nextLevelBtn.addEventListener("click", goToNextLevel);

continueBtn.addEventListener("click", () => {
  if (state.usedContinueThisRun) return;
  Sound.tap();
  playMockAd(2000, () => {
    state.usedContinueThisRun = true;
    hide(gameOverScreen);
    // refill hearts and resume the same board (bombs already hit stay revealed)
    state.hearts = MAX_HEARTS;
    renderHearts();
    state.playing = true;
    updateHud();
  });
});

restartBtn.addEventListener("click", () => {
  Sound.tap();
  hide(gameOverScreen);
  // retry the SAME level with a fresh board — progress is never lost
  maybeInterstitial(() => {
    state.playing = true;
    particles = [];
    popups = [];
    buildLevel(state.level);
    refreshHintButton();
  });
});

function refreshHintButton() {
  // Autonomy (Self-Determination Theory): let players spend earned coins
  // instead of always being forced to watch an ad.
  if (state.coins >= HINT_COST) {
    hintBtn.innerHTML = `Reveal a safe tile · ${HINT_COST} 🪙`;
  } else {
    hintBtn.innerHTML = `<span class="reward-tag">AD</span> Reveal a safe tile`;
  }
}

function doHintReveal() {
  const safe = [];
  for (let r = 0; r < state.gridSize; r++)
    for (let c = 0; c < state.gridSize; c++)
      if (!state.revealed[r][c] && !state.bomb[r][c]) safe.push([r, c]);
  if (!safe.length) return;
  const [r, c] = safe[Math.floor(Math.random() * safe.length)];
  revealCell(r, c, 1);
  updateHud();
  bumpScore();
}

hintBtn.addEventListener("click", () => {
  if (!state.playing) return;
  Sound.init();
  if (state.coins >= HINT_COST) {
    addCoins(-HINT_COST);
    Sound.coin();
    doHintReveal();
    refreshHintButton();
  } else {
    playMockAd(1500, () => { doHintReveal(); refreshHintButton(); });
  }
});

settingsBtn.addEventListener("click", () => { Sound.tap(); syncToggles(); show(settingsScreen); });
closeSettingsBtn.addEventListener("click", () => { Sound.tap(); hide(settingsScreen); });

function setSound(on) {
  Sound.enabled = on;
  localStorage.setItem("reveal.sound", on ? "on" : "off");
  soundBtn.textContent = on ? "🔊" : "🔇";
}
function setHaptics(on) {
  state.haptics = on;
  localStorage.setItem("reveal.haptics", on ? "on" : "off");
}
function syncToggles() {
  soundToggle.dataset.on = String(Sound.enabled);
  hapticsToggle.dataset.on = String(state.haptics);
}
soundToggle.addEventListener("click", () => { setSound(!Sound.enabled); syncToggles(); if (Sound.enabled) { Sound.init(); Sound.tap(); } });
hapticsToggle.addEventListener("click", () => { setHaptics(!state.haptics); syncToggles(); haptic(30); });
soundBtn.addEventListener("click", () => { setSound(!Sound.enabled); if (Sound.enabled) { Sound.init(); Sound.tap(); } });

// ---------- Streak / collection / reset ----------
function computeStreak() {
  const today = new Date().toDateString();
  const last = localStorage.getItem("reveal.lastPlayed");
  let streak = Number(localStorage.getItem("reveal.streak") || 0);
  if (last !== today) {
    const yesterday = new Date(Date.now() - 86400000).toDateString();
    streak = last === yesterday ? streak + 1 : 1;
    localStorage.setItem("reveal.streak", String(streak));
    localStorage.setItem("reveal.lastPlayed", today);
  }
  state.streak = streak;
}

function renderMenu() {
  const savedLevel = Number(localStorage.getItem("reveal.level") || 1);
  startBtn.textContent = savedLevel > 1 ? `Continue · Level ${savedLevel}` : "Play";
  if (state.streak >= 1) {
    streakDays.textContent = state.streak;
    streakBadge.classList.remove("hidden");
  } else {
    streakBadge.classList.add("hidden");
  }
  collectionCount.textContent = `${state.collection.length}/${SCENES.length}`;
  collectionRow.innerHTML = "";
  SCENES.forEach((s) => {
    const div = document.createElement("div");
    const found = state.collection.includes(s.emoji);
    div.className = "collection-cell " + (found ? "found" : "locked");
    div.textContent = found ? s.emoji : "❓";
    collectionRow.appendChild(div);
  });
  renderJourney();
  renderDaily();
  renderMissions();
}

resetBtn.addEventListener("click", () => {
  if (!window.confirm("Reset all progress — coins, best score, streak and gallery?")) return;
  ["reveal.best", "reveal.coins", "reveal.collection", "reveal.streak", "reveal.lastPlayed",
   "reveal.gameOverCount", "reveal.level", "reveal.missions", "reveal.dailyClaim"]
    .forEach((k) => localStorage.removeItem(k));
  state.best = 0; state.coins = 0; state.collection = []; state.gameOverCount = 0; state.level = 1;
  hudBest.textContent = "0"; hudCoins.textContent = "0";
  computeStreak();
  loadMissions();
  renderMenu();
  refreshHintButton();
  hide(settingsScreen);
  show(menuScreen);
});

// ---------- Boot ----------
setSound(Sound.enabled);
computeStreak();
loadMissions();
renderMenu();
refreshHintButton();
window.addEventListener("resize", () => {
  const wasPlaying = state.playing;
  sizeBoard();
  updateHud();
  state.playing = wasPlaying;
});
sizeBoard();
buildLevel(state.level);
state.playing = false;
