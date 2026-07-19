// Reveal — scratch-away puzzle MVP
// Core loop: uncover >=85% of a grid without hitting a bomb cell. Endless levels, single life per run.

const CANVAS_SIZE = 360;
const WIN_RATIO = 0.85;
const REVEAL_ICONS = ["🍕", "🎁", "🐱", "🌟", "💎", "🍩", "🚀", "🌈", "🎈", "🦊", "🍉", "⚽"];
const PALETTES = [
  ["#ff6b6b", "#ffd93d"],
  ["#4ecdc4", "#1a936f"],
  ["#a78bfa", "#f472b6"],
  ["#38bdf8", "#818cf8"],
  ["#fb923c", "#f43f5e"],
  ["#34d399", "#facc15"],
];

const canvas = document.getElementById("board");
const ctx = canvas.getContext("2d");

const hudLevel = document.getElementById("hudLevel");
const hudScore = document.getElementById("hudScore");
const hudBest = document.getElementById("hudBest");
const progressFill = document.getElementById("progressFill");
const hintBtn = document.getElementById("hintBtn");

const menuScreen = document.getElementById("menuScreen");
const levelCompleteScreen = document.getElementById("levelCompleteScreen");
const gameOverScreen = document.getElementById("gameOverScreen");
const adScreen = document.getElementById("adScreen");

const startBtn = document.getElementById("startBtn");
const nextLevelBtn = document.getElementById("nextLevelBtn");
const continueBtn = document.getElementById("continueBtn");
const restartBtn = document.getElementById("restartBtn");

const clearedLevelEl = document.getElementById("clearedLevel");
const levelPointsEl = document.getElementById("levelPoints");
const finalScoreEl = document.getElementById("finalScore");
const adTimerEl = document.getElementById("adTimer");

let state = {
  level: 1,
  score: 0,
  best: Number(localStorage.getItem("reveal.best") || 0),
  gridSize: 8,
  cellSize: CANVAS_SIZE / 8,
  revealed: [],
  bomb: [],
  revealedCount: 0,
  nonBombTotal: 0,
  usedContinueThisRun: false,
  gameOverCount: Number(localStorage.getItem("reveal.gameOverCount") || 0),
  playing: false,
  icon: "🍕",
  palette: PALETTES[0],
};

hudBest.textContent = state.best;

function gridSizeForLevel(level) {
  return Math.min(8 + Math.floor((level - 1) / 3), 12);
}

function bombDensityForLevel(level) {
  return Math.min(0.05 + (level - 1) * 0.015, 0.25);
}

function buildLevel(level) {
  const gridSize = gridSizeForLevel(level);
  const cellSize = CANVAS_SIZE / gridSize;
  const bombDensity = bombDensityForLevel(level);

  const revealed = Array.from({ length: gridSize }, () => Array(gridSize).fill(false));
  const bomb = Array.from({ length: gridSize }, () => Array(gridSize).fill(false));

  let bombCount = 0;
  for (let r = 0; r < gridSize; r++) {
    for (let c = 0; c < gridSize; c++) {
      if (Math.random() < bombDensity) {
        bomb[r][c] = true;
        bombCount++;
      }
    }
  }

  state.gridSize = gridSize;
  state.cellSize = cellSize;
  state.revealed = revealed;
  state.bomb = bomb;
  state.revealedCount = 0;
  state.nonBombTotal = gridSize * gridSize - bombCount;
  state.icon = REVEAL_ICONS[Math.floor(Math.random() * REVEAL_ICONS.length)];
  state.palette = PALETTES[Math.floor(Math.random() * PALETTES.length)];

  drawBase();
  render();
  updateHud();
}

function drawBase() {
  const g = ctx.createLinearGradient(0, 0, CANVAS_SIZE, CANVAS_SIZE);
  g.addColorStop(0, state.palette[0]);
  g.addColorStop(1, state.palette[1]);
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, CANVAS_SIZE, CANVAS_SIZE);

  ctx.font = "180px serif";
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillText(state.icon, CANVAS_SIZE / 2, CANVAS_SIZE / 2 + 10);

  // cache the base image so we don't have to redraw it every frame
  state.baseImage = ctx.getImageData(0, 0, CANVAS_SIZE, CANVAS_SIZE);
}

function render() {
  ctx.putImageData(state.baseImage, 0, 0);

  const { gridSize, cellSize, revealed, bomb } = state;
  for (let r = 0; r < gridSize; r++) {
    for (let c = 0; c < gridSize; c++) {
      if (revealed[r][c]) {
        if (bomb[r][c]) {
          ctx.fillStyle = "#e11d48";
          ctx.fillRect(c * cellSize, r * cellSize, cellSize, cellSize);
          ctx.font = `${cellSize * 0.6}px serif`;
          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          ctx.fillText("💣", c * cellSize + cellSize / 2, r * cellSize + cellSize / 2);
        }
        continue;
      }
      ctx.fillStyle = ((r + c) % 2 === 0) ? "#b8bcc4" : "#a7abb4";
      ctx.fillRect(c * cellSize, r * cellSize, cellSize, cellSize);
    }
  }

  // grid lines for scratch-card feel
  ctx.strokeStyle = "rgba(0,0,0,0.08)";
  ctx.lineWidth = 1;
  for (let i = 0; i <= gridSize; i++) {
    ctx.beginPath();
    ctx.moveTo(i * cellSize, 0);
    ctx.lineTo(i * cellSize, CANVAS_SIZE);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(0, i * cellSize);
    ctx.lineTo(CANVAS_SIZE, i * cellSize);
    ctx.stroke();
  }
}

function updateHud() {
  hudLevel.textContent = state.level;
  hudScore.textContent = state.score;
  hudBest.textContent = state.best;
  const pct = state.nonBombTotal === 0 ? 0 : Math.min(100, (state.revealedCount / state.nonBombTotal) * 100);
  progressFill.style.width = pct + "%";
}

function revealCell(r, c) {
  if (r < 0 || c < 0 || r >= state.gridSize || c >= state.gridSize) return;
  if (state.revealed[r][c]) return;
  state.revealed[r][c] = true;

  if (state.bomb[r][c]) {
    triggerGameOver();
    return true; // stop brush from continuing to reveal more cells this stroke
  }

  state.revealedCount++;
  state.score += 10;

  if (state.revealedCount >= Math.ceil(state.nonBombTotal * WIN_RATIO)) {
    triggerLevelComplete();
    return true;
  }
  return false;
}

function revealBrush(clientX, clientY) {
  if (!state.playing) return;
  const rect = canvas.getBoundingClientRect();
  const x = (clientX - rect.left) * (CANVAS_SIZE / rect.width);
  const y = (clientY - rect.top) * (CANVAS_SIZE / rect.height);
  const col = Math.floor(x / state.cellSize);
  const row = Math.floor(y / state.cellSize);

  let stopped = false;
  for (let dr = -1; dr <= 1 && !stopped; dr++) {
    for (let dc = -1; dc <= 1 && !stopped; dc++) {
      stopped = revealCell(row + dr, col + dc);
    }
  }
  render();
  updateHud();
}

// --- input ---
let dragging = false;

canvas.addEventListener("pointerdown", (e) => {
  dragging = true;
  revealBrush(e.clientX, e.clientY);
});
canvas.addEventListener("pointermove", (e) => {
  if (dragging) revealBrush(e.clientX, e.clientY);
});
window.addEventListener("pointerup", () => { dragging = false; });

// --- flow control ---

function showOverlay(el) { el.classList.remove("hidden"); }
function hideOverlay(el) { el.classList.add("hidden"); }

function startRun() {
  state.level = 1;
  state.score = 0;
  state.usedContinueThisRun = false;
  hideOverlay(menuScreen);
  hideOverlay(gameOverScreen);
  hideOverlay(levelCompleteScreen);
  state.playing = true;
  buildLevel(state.level);
}

function triggerLevelComplete() {
  state.playing = false;
  const bonus = 50 * state.level;
  state.score += bonus;
  clearedLevelEl.textContent = state.level;
  levelPointsEl.textContent = bonus;
  updateHud();
  showOverlay(levelCompleteScreen);
}

function goToNextLevel() {
  hideOverlay(levelCompleteScreen);
  state.level++;
  state.playing = true;
  buildLevel(state.level);
}

function triggerGameOver() {
  state.playing = false;
  if (state.score > state.best) {
    state.best = state.score;
    localStorage.setItem("reveal.best", String(state.best));
  }
  render();
  updateHud();
  finalScoreEl.textContent = state.score;
  continueBtn.disabled = state.usedContinueThisRun;
  continueBtn.textContent = state.usedContinueThisRun ? "No Continues Left" : "▶ Watch Ad to Continue";
  showOverlay(gameOverScreen);
}

function playMockAd(durationMs, onDone) {
  showOverlay(adScreen);
  const start = performance.now();
  function tick() {
    const elapsed = performance.now() - start;
    if (elapsed >= durationMs) {
      hideOverlay(adScreen);
      onDone();
      return;
    }
    const remaining = Math.ceil((durationMs - elapsed) / 1000);
    adTimerEl.textContent = `Ad playing… ${remaining}s`;
    requestAnimationFrame(tick);
  }
  tick();
}

function maybeShowInterstitial(onDone) {
  state.gameOverCount++;
  localStorage.setItem("reveal.gameOverCount", String(state.gameOverCount));
  if (state.gameOverCount % 3 === 0) {
    playMockAd(1500, onDone);
  } else {
    onDone();
  }
}

// Buttons
startBtn.addEventListener("click", startRun);

nextLevelBtn.addEventListener("click", goToNextLevel);

continueBtn.addEventListener("click", () => {
  if (state.usedContinueThisRun) return;
  playMockAd(1500, () => {
    state.usedContinueThisRun = true;
    hideOverlay(gameOverScreen);
    // clear bombs from the current board and keep progress, so the player can push on
    for (let r = 0; r < state.gridSize; r++) {
      for (let c = 0; c < state.gridSize; c++) state.bomb[r][c] = false;
    }
    state.playing = true;
    render();
    updateHud();
  });
});

restartBtn.addEventListener("click", () => {
  hideOverlay(gameOverScreen);
  maybeShowInterstitial(() => {
    showOverlay(menuScreen);
  });
});

hintBtn.addEventListener("click", () => {
  if (!state.playing) return;
  playMockAd(1000, () => {
    const candidates = [];
    for (let r = 0; r < state.gridSize; r++) {
      for (let c = 0; c < state.gridSize; c++) {
        if (!state.revealed[r][c] && !state.bomb[r][c]) candidates.push([r, c]);
      }
    }
    if (candidates.length === 0) return;
    const [r, c] = candidates[Math.floor(Math.random() * candidates.length)];
    const stopped = revealCell(r, c);
    render();
    updateHud();
    if (!stopped && state.revealedCount >= Math.ceil(state.nonBombTotal * WIN_RATIO)) {
      triggerLevelComplete();
    }
  });
});

// initial paint so the board isn't blank behind the menu
buildLevel(1);
state.playing = false;
