// ChargeGate Interactive Demo & Theme Engine

let currentThreshold = 80;
let isTravelMode = false;

// Theme Engine with System Adaptation
const THEME_STORAGE_KEY = 'chargegate-theme';
let webTheme = localStorage.getItem(THEME_STORAGE_KEY) || 'auto';

function initTheme() {
  applyWebTheme(webTheme);

  // Real-time system theme change watcher
  const darkQuery = window.matchMedia('(prefers-color-scheme: dark)');
  if (darkQuery.addEventListener) {
    darkQuery.addEventListener('change', () => {
      if (webTheme === 'auto') {
        applyWebTheme('auto');
      }
    });
  }
}

function applyWebTheme(mode) {
  webTheme = mode;
  const iconEl = document.getElementById('theme-icon');
  
  if (mode === 'light') {
    document.documentElement.setAttribute('data-theme', 'light');
    if (iconEl) iconEl.textContent = '☀️';
  } else if (mode === 'dark') {
    document.documentElement.setAttribute('data-theme', 'dark');
    if (iconEl) iconEl.textContent = '🌙';
  } else {
    // auto: adapt to system preferences
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    document.documentElement.setAttribute('data-theme', prefersDark ? 'dark' : 'light');
    if (iconEl) iconEl.textContent = '🌓';
  }
}

function cycleWebTheme() {
  const next = webTheme === 'auto' ? 'dark' : webTheme === 'dark' ? 'light' : 'auto';
  localStorage.setItem(THEME_STORAGE_KEY, next);
  applyWebTheme(next);
}

// Auto-run theme initialization
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initTheme);
} else {
  initTheme();
}

function applyDemoPreset(percentage, label) {
  currentThreshold = percentage;

  // Update preset cards
  [80, 85, 100].forEach(p => {
    const card = document.getElementById(`preset-card-${p}`);
    const badge = document.getElementById(`badge-${p}`);
    const btn = document.getElementById(`btn-${p}`);

    if (p === percentage) {
      card.classList.add('active');
      badge.style.display = 'inline-block';
      btn.textContent = 'Active';
      btn.style.background = '#10b981';
      btn.style.color = '#032115';
    } else {
      card.classList.remove('active');
      badge.style.display = 'none';
      btn.textContent = 'Apply';
      btn.style.background = 'rgba(255, 255, 255, 0.08)';
      btn.style.color = '#f9fafb';
    }
  });

  // Update UI Elements
  const percentEl = document.getElementById('demo-percent');
  const progressFill = document.getElementById('demo-progress-fill');
  const activeTitle = document.getElementById('demo-active-title');
  const statusDesc = document.getElementById('demo-status-desc');
  const feedback = document.getElementById('demo-feedback');
  const telemetryRate = document.getElementById('demo-telemetry-rate');

  // Simulated live percentage (e.g. 79% for 80%, 84% for 85%, 100% for 100%)
  const simPercent = percentage === 100 ? 100 : percentage - 1;
  percentEl.textContent = simPercent;
  progressFill.style.width = `${simPercent}%`;
  activeTitle.textContent = `Active Threshold: ${label}`;

  if (percentage === 100) {
    statusDesc.textContent = 'Fully Charged (100%) • AC Connected';
    telemetryRate.textContent = 'Trickle Idle (0.2 W)';
  } else {
    statusDesc.textContent = `Plugged in • Charge gated at ${percentage}%`;
    telemetryRate.textContent = 'AC Passthrough (0.0 W)';
  }

  feedback.textContent = `Applied ${percentage}% limit in BIOS!`;
  setTimeout(() => {
    feedback.textContent = 'Ready';
  }, 2500);

  // If manual preset applied while travel mode was on, disable travel mode
  if (isTravelMode && percentage < 100) {
    isTravelMode = false;
    updateTravelButton();
  }
}

function toggleDemoTravel() {
  isTravelMode = !isTravelMode;
  updateTravelButton();

  if (isTravelMode) {
    applyDemoPreset(100, 'Full Capacity (100%)');
    document.getElementById('demo-feedback').textContent = 'Travel Mode Active (Auto-reverts to 80% on unplug)';
  } else {
    applyDemoPreset(80, 'Battery Health (80%)');
    document.getElementById('demo-feedback').textContent = 'Travel Mode Canceled. Restored 80% limit.';
  }
}

function updateTravelButton() {
  const travelBtn = document.getElementById('demo-travel-btn');
  if (isTravelMode) {
    travelBtn.textContent = 'Active (Cancel)';
    travelBtn.classList.add('active');
  } else {
    travelBtn.textContent = 'Top Up';
    travelBtn.classList.remove('active');
  }
}

// ----------------------------------------------------
// Live Mockup Telemetry Graph Simulation
// ----------------------------------------------------
let demoGraphMode = 'Watts';
let demoDataWatts = [];
let demoDataPercent = [];
const MAX_DEMO_POINTS = 30;

function initDemoGraphData() {
  demoDataWatts = [];
  demoDataPercent = [];
  for (let i = 0; i < MAX_DEMO_POINTS; i++) {
    // Generate realistic historical watt curve with charge and drain transitions
    const t = i / MAX_DEMO_POINTS;
    const w = 18 * Math.sin(t * 3.5) + (Math.random() * 4 - 2);
    demoDataWatts.push(Math.round(w * 10) / 10);
    demoDataPercent.push(Math.round(72 + (i / MAX_DEMO_POINTS) * 7));
  }
}

function setDemoGraphMode(mode) {
  demoGraphMode = mode;
  const tabWatts = document.getElementById('demo-tab-watts');
  const tabPercent = document.getElementById('demo-tab-percent');
  const sub = document.getElementById('demo-graph-sub');

  if (mode === 'Watts') {
    tabWatts?.classList.add('active');
    tabPercent?.classList.remove('active');
    if (sub) sub.textContent = 'Live Power Flow (Watts)';
  } else {
    tabPercent?.classList.add('active');
    tabWatts?.classList.remove('active');
    if (sub) sub.textContent = 'Battery Level (% over time)';
  }
  renderDemoChart();
}

function renderDemoChart() {
  const canvas = document.getElementById('demo-telemetry-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  const w = canvas.width;
  const h = canvas.height;
  ctx.clearRect(0, 0, w, h);

  const padLeft = 36;
  const padRight = 12;
  const padTop = 14;
  const padBottom = 18;
  const plotW = w - padLeft - padRight;
  const plotH = h - padTop - padBottom;

  ctx.font = '9px Segoe UI, sans-serif';
  ctx.fillStyle = 'rgba(160, 160, 160, 0.7)';

  if (demoGraphMode === 'Watts') {
    // Dynamic Y Range
    const maxW = 40;
    const minW = -25;
    const range = maxW - minW;
    const zeroY = padTop + ((maxW - 0) / range) * plotH;

    // 0 W line
    ctx.strokeStyle = 'rgba(16, 185, 129, 0.4)';
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(padLeft, zeroY);
    ctx.lineTo(padLeft + plotW, zeroY);
    ctx.stroke();
    ctx.setLineDash([]);
    ctx.fillText('0W', padLeft - 22, zeroY + 3);

    // +30W and -20W labels
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.05)';
    const topY = padTop + ((maxW - 30) / range) * plotH;
    ctx.beginPath();
    ctx.moveTo(padLeft, topY);
    ctx.lineTo(padLeft + plotW, topY);
    ctx.stroke();
    ctx.fillText('+30W', padLeft - 28, topY + 3);

    const botY = padTop + ((maxW - (-20)) / range) * plotH;
    ctx.beginPath();
    ctx.moveTo(padLeft, botY);
    ctx.lineTo(padLeft + plotW, botY);
    ctx.stroke();
    ctx.fillText('-20W', padLeft - 25, botY + 3);

    // Points
    const pts = demoDataWatts.map((val, idx) => {
      const px = padLeft + (idx / (demoDataWatts.length - 1)) * plotW;
      const py = padTop + ((maxW - val) / range) * plotH;
      return { x: px, y: Math.min(Math.max(py, padTop), padTop + plotH), val };
    });

    // Fill under curve
    const grad = ctx.createLinearGradient(0, padTop, 0, padTop + plotH);
    grad.addColorStop(0, 'rgba(16, 185, 129, 0.28)');
    grad.addColorStop(0.5, 'rgba(16, 185, 129, 0.05)');
    grad.addColorStop(1, 'rgba(245, 158, 11, 0.18)');

    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.moveTo(pts[0].x, zeroY);
    pts.forEach(p => ctx.lineTo(p.x, p.y));
    ctx.lineTo(pts[pts.length - 1].x, zeroY);
    ctx.closePath();
    ctx.fill();

    // Curve Line
    ctx.lineWidth = 1.8;
    for (let i = 0; i < pts.length - 1; i++) {
      ctx.strokeStyle = pts[i + 1].val >= 0 ? '#10b981' : '#f59e0b';
      ctx.beginPath();
      ctx.moveTo(pts[i].x, pts[i].y);
      ctx.lineTo(pts[i + 1].x, pts[i + 1].y);
      ctx.stroke();
    }

    // Live Dot
    const last = pts[pts.length - 1];
    ctx.fillStyle = last.val >= 0 ? '#10b981' : '#f59e0b';
    ctx.beginPath();
    ctx.arc(last.x, last.y, 3, 0, Math.PI * 2);
    ctx.fill();

  } else {
    // Percentage mode
    [100, 75, 50].forEach(p => {
      const gy = padTop + ((100 - p) / 100) * plotH;
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.05)';
      ctx.beginPath();
      ctx.moveTo(padLeft, gy);
      ctx.lineTo(padLeft + plotW, gy);
      ctx.stroke();
      ctx.fillText(`${p}%`, padLeft - 24, gy + 3);
    });

    // Threshold line (80%)
    const thY = padTop + ((100 - currentThreshold) / 100) * plotH;
    ctx.strokeStyle = 'rgba(16, 185, 129, 0.7)';
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(padLeft, thY);
    ctx.lineTo(padLeft + plotW, thY);
    ctx.stroke();
    ctx.setLineDash([]);
    ctx.fillStyle = '#10b981';
    ctx.fillText(`${currentThreshold}% Gate`, padLeft + plotW - 48, thY - 4);

    // Percentage curve
    const pts = demoDataPercent.map((val, idx) => {
      const px = padLeft + (idx / (demoDataPercent.length - 1)) * plotW;
      const py = padTop + ((100 - val) / 100) * plotH;
      return { x: px, y: py };
    });

    const grad = ctx.createLinearGradient(0, padTop, 0, padTop + plotH);
    grad.addColorStop(0, 'rgba(16, 185, 129, 0.3)');
    grad.addColorStop(1, 'rgba(16, 185, 129, 0.02)');
    ctx.fillStyle = grad;
    ctx.beginPath();
    ctx.moveTo(pts[0].x, padTop + plotH);
    pts.forEach(p => ctx.lineTo(p.x, p.y));
    ctx.lineTo(pts[pts.length - 1].x, padTop + plotH);
    ctx.closePath();
    ctx.fill();

    ctx.strokeStyle = '#10b981';
    ctx.lineWidth = 1.8;
    ctx.beginPath();
    ctx.moveTo(pts[0].x, pts[0].y);
    pts.forEach(p => ctx.lineTo(p.x, p.y));
    ctx.stroke();

    const last = pts[pts.length - 1];
    ctx.fillStyle = '#10b981';
    ctx.beginPath();
    ctx.arc(last.x, last.y, 3, 0, Math.PI * 2);
    ctx.fill();
  }

  // Time markers
  ctx.fillStyle = 'rgba(160, 160, 160, 0.6)';
  ctx.fillText('10m ago', padLeft, padTop + plotH + 12);
  ctx.fillText('5m ago', padLeft + plotW / 2 - 12, padTop + plotH + 12);
  ctx.fillText('Now', padLeft + plotW - 18, padTop + plotH + 12);
}

// Live simulation tick
initDemoGraphData();
if (typeof window !== 'undefined') {
  window.addEventListener('load', () => {
    renderDemoChart();
    setInterval(() => {
      // Simulate live data flow
      if (demoDataWatts.length > 0) {
        const last = demoDataWatts[demoDataWatts.length - 1];
        const next = Math.max(-28, Math.min(45, last + (Math.random() * 6 - 3)));
        demoDataWatts.shift();
        demoDataWatts.push(Math.round(next * 10) / 10);

        const flowEl = document.getElementById('demo-graph-flow');
        if (flowEl) {
          flowEl.textContent = next >= 0 ? `+${next.toFixed(1)} W` : `${next.toFixed(1)} W`;
          flowEl.style.color = next >= 0 ? '#10b981' : '#f59e0b';
        }
        renderDemoChart();
      }
    }, 2800);
  });
}
