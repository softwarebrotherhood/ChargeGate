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
