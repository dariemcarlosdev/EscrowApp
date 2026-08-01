// Theme locked to light while the dark theme is deferred. Set THEME_LOCK = null to
// restore user preference + re-enable the toggle. Dark tokens/code below stay intact.
var THEME_LOCK = 'light';

window.themeManager = {
  get: function() {
    try {
      var v = THEME_LOCK || localStorage.getItem('theme') || 'light';
      console && console.log && console.log('[themeManager] get ->', v);
      return v;
    } catch (e) { console && console.log && console.log('[themeManager] get error', e); return 'light'; }
  },
  set: function(t) {
    try {
      document.documentElement.setAttribute('data-theme', t);
      localStorage.setItem('theme', t);
      // Dispatch an event so any SPA-rendered code can react (force repaint if needed)
      try { window.dispatchEvent(new Event('theme:changed')); } catch(ex) { /* ignore */ }
      // Notify Blazor via JS -> .NET interop so server components can re-render if needed
      try { if (window.DotNet && window.DotNet.invokeMethodAsync) { window.DotNet.invokeMethodAsync('EscrowApp','ThemeChanged').catch(function(){}); } } catch(ex) { /* ignore */ }
      // Force stylesheet recalculation/repaint in case computed styles were cached by the browser
      try {
        var sheets = Array.prototype.slice.call(document.styleSheets || []);
        sheets.forEach(function(s){
          try{
            var node = s.ownerNode || s.owningElement;
            if(!node) return;
            var rel = (node.rel || '').toLowerCase();
            if(rel.indexOf('stylesheet') === -1) return;
            // avoid touching critical framework CSS if id present
            node.disabled = true;
            // re-enable on next frame
            window.requestAnimationFrame(function(){ node.disabled = false; });
          }catch(e){}
        });
      } catch(e) {}
      console && console.log && console.log('[themeManager] set ->', t);
    } catch (e) { console && console.log && console.log('[themeManager] set error', e); }
  },
  toggle: function() {
    try {
      if (THEME_LOCK) { this.set(THEME_LOCK); return THEME_LOCK; }
      var d = document.documentElement;
      var next = d.getAttribute('data-theme') === 'light' ? 'dark' : 'light';
      this.set(next);
      console && console.log && console.log('[themeManager] toggled ->', next);
      return next;
    } catch (e) { console && console.log && console.log('[themeManager] toggle error', e); return 'light'; }
  }
};

(function init() {
  try {
    var t = THEME_LOCK || localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', t);
    console && console.log && console.log('[themeManager] init ->', t);
    // expose ready flag
    window.__themeManagerReady = true;
  } catch (e) { console && console.log && console.log('[themeManager] init error', e); }
})();

// Attach a native click handler to the toggle button as a resilient fallback
function attachThemeToggle() {
  try {
    var btn = document.getElementById('theme-toggle-btn');
    if (!btn) return;
    // prevent double-attaching
    if (btn.__themeToggleAttached) return;
    btn.__themeToggleAttached = true;
    btn.addEventListener('click', function (ev) {
      try {
        var next = (window.themeManager && window.themeManager.toggle) ? window.themeManager.toggle() : (function(){ var d=document.documentElement; var nt=d.getAttribute('data-theme')==='light'?'dark':'light'; if(window.themeManager && window.themeManager.set){ window.themeManager.set(nt); } else { document.documentElement.setAttribute('data-theme', nt); localStorage.setItem('theme', nt); } return nt; })();
        btn.setAttribute('aria-pressed', next === 'light');
        var ic = btn.querySelector('.theme-icon'); if (ic) ic.textContent = next === 'light' ? '☀️' : '🌙';
        console && console.log && console.log('[themeManager] native attached toggle ->', next);
      } catch (e) { console && console.log && console.log('[themeManager] native handler error', e); }
    });
  } catch (e) { console && console.log && console.log('[themeManager] attach error', e); }
}

// try to attach immediately and also expose for manual attach
attachThemeToggle();
window.themeManager = window.themeManager || {};
window.themeManager.attachToggle = attachThemeToggle;

// Ensure theme is re-applied on SPA navigation (Blazor client-side routing)
(function navigationListener() {
  try {
    var origPush = history.pushState;
    history.pushState = function () { var res = origPush.apply(this, arguments); window.dispatchEvent(new Event('spa:navigation')); return res; };
    var origReplace = history.replaceState;
    history.replaceState = function () { var res = origReplace.apply(this, arguments); window.dispatchEvent(new Event('spa:navigation')); return res; };
    window.addEventListener('popstate', function () { window.dispatchEvent(new Event('spa:navigation')); });

    window.addEventListener('spa:navigation', function () {
      try {
        var t = THEME_LOCK || localStorage.getItem('theme') || 'light';
        document.documentElement.setAttribute('data-theme', t);
        // update toggle button aria/icon if present
        var btn = document.getElementById('theme-toggle-btn');
        if (btn) { btn.setAttribute('aria-pressed', t === 'light'); var ic = btn.querySelector('.theme-icon'); if (ic) ic.textContent = t === 'light' ? '☀️' : '🌙'; }
      } catch (e) { /* ignore */ }
    });
  } catch (e) { console && console.log && console.log('[themeManager] navigationListener error', e); }
})();
