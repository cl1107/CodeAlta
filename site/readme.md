---
title: Home
layout: simple
og_type: website
---

<section class="text-center py-5 codealta-hero">
  <div class="container">
    <pre class="codealta-ascii-logo mx-auto" aria-label="CodeAlta"><span class="logo-code">   ██████                  ██           </span><span class="logo-alta">     ██       ██    ██</span>
<span class="logo-code">  ██░░░░██                ░██           </span><span class="logo-alta">    ████     ░██   ░██</span>
<span class="logo-code"> ██    ░░    ██████       ░██   █████   </span><span class="logo-alta">   ██░░██    ░██  ██████   ██████</span>
<span class="logo-code">░██         ██░░░░██   ██████  ██░░░██  </span><span class="logo-alta">  ██  ░░██   ░██ ░░░██░   ░░░░░░██</span>
<span class="logo-code">░██        ░██   ░██  ██░░░██ ░███████  </span><span class="logo-alta"> ██████████  ░██   ░██     ███████</span>
<span class="logo-code">░░██    ██ ░██   ░██ ░██  ░██ ░██░░░░   </span><span class="logo-alta">░██░░░░░░██  ░██   ░██    ██░░░░██</span>
<span class="logo-code"> ░░██████  ░░██████  ░░██████ ░░██████  </span><span class="logo-alta">░██     ░██  ███   ░░██  ░░████████</span>
<span class="logo-code">  ░░░░░░    ░░░░░░    ░░░░░░   ░░░░░░   </span><span class="logo-alta">░░      ░░  ░░░     ░░    ░░░░░░░░</span></pre>
    <p class="lead mt-4 mb-4">
      A keyboard-first, terminal AI coding workspace for managing projects, model providers, threads, plugins, and delegated agents.
    </p>
    <div class="d-flex justify-content-center gap-3 mt-4 flex-wrap">
      <a href="{{site.basepath}}/docs/getting-started/" class="btn btn-primary btn-lg"><i class="bi bi-rocket-takeoff"></i> Get started</a>
      <a href="{{site.basepath}}/docs/model-providers/" class="btn btn-outline-secondary btn-lg"><i class="bi bi-cpu"></i> Configure providers</a>
      <a href="https://github.com/CodeAlta/CodeAlta" class="btn btn-info btn-lg"><i class="bi bi-github"></i> GitHub</a>
    </div>
    <div class="mt-4 text-start mx-auto" style="max-width: 48rem;">
      <pre class="language-shell-session"><code>dotnet tool install -g CodeAlta
alta</code></pre>
      <p class="text-center text-secondary mt-2" style="font-size: 0.85rem;">The NuGet package is <a href="https://www.nuget.org/packages/CodeAlta/" class="text-secondary">CodeAlta</a>; the installed command is <code>alta</code>. Requires <a href="https://dotnet.microsoft.com/en-us/download/dotnet/10.0" class="text-secondary">.NET 10</a>.</p>
    </div>
  </div>
</section>

<section class="container my-5 codealta-workflow-preview">
  <div class="workflow-preview-panel">
    <div class="workflow-preview-copy">
      <p class="text-uppercase text-secondary fw-semibold mb-2">Terminal workflow preview</p>
      <h2 class="display-6 mb-3">One terminal surface for prompts, tools, files, and agent state.</h2>
      <p class="lead mb-0">A short usage video will be embedded here to show first launch, provider setup, prompt sending, tool calls, and timeline cards.</p>
    </div>
    <div class="terminal-demo-placeholder" role="img" aria-label="CodeAlta terminal demo placeholder">
      <div class="demo-titlebar"><span></span><span></span><span></span><strong>alta</strong></div>
      <pre><code>Projects  ▸ CodeAlta
Threads   ▸ Fix parser test

provider: Codex · gpt-5.5 · reasoning high · ctx 18%

&gt; Use the smallest safe change and run the focused test.

assistant  Planning the change…
tool       read_file src/CodeAlta/...
result     1 file modified · +12 -3</code></pre>
    </div>
    <!-- Replace the placeholder above with a video element when the demo capture is available. -->
  </div>
</section>

<section class="container my-5 codealta-principles">
  <div class="principles-intro mx-auto text-center">
    <p class="text-uppercase text-secondary fw-semibold mb-2">CodeAlta principles</p>
    <h2 class="display-6 mb-3">Efficient. Transparent. Keyboard-first. Thread-oriented. Provider-agnostic. Native .NET. Error-aware. Pluggable.</h2>
    <p class="lead mb-0">A compact design manifesto for a terminal workspace that stays practical while it grows.</p>
  </div>
  <div class="principle-flow mt-5">
    <article class="principle-feature" style="--accent: #f472ff; --accent-2: #38bdf8;">
      <div class="principle-copy">
        <span class="principle-kicker">Efficient interface</span>
        <h3><i class="bi bi-arrows-collapse"></i> Using terminal space efficiently</h3>
      </div>
      <div class="principle-shot" role="img" aria-label="Screenshot placeholder for the compact workspace timeline">
        <div class="shot-topline"><span></span><span></span><span></span><strong>Main workspace</strong></div>
        <div class="shot-body shot-body--workspace">
          <span class="rail"></span><span class="timeline wide"></span><span class="timeline"></span><span class="chip"></span><span class="chip alt"></span><span class="footer"></span>
        </div>
      </div>
    </article>
    <article class="principle-feature" style="--accent: #60a5fa; --accent-2: #c084fc;">
      <div class="principle-copy">
        <span class="principle-kicker">Transparent execution</span>
        <h3><i class="bi bi-eye"></i> Keeping execution inspectable</h3>
      </div>
      <div class="principle-shot" role="img" aria-label="Screenshot placeholder for expanded system prompt and execution details">
        <div class="shot-topline"><span></span><span></span><span></span><strong>Execution trace</strong></div>
        <div class="shot-body"><span class="diff plus"></span><span class="diff"></span><span class="diff plus short"></span><span class="stats"></span></div>
      </div>
    </article>
    <article class="principle-feature" style="--accent: #34d399; --accent-2: #facc15;">
      <div class="principle-copy">
        <span class="principle-kicker">Keyboard-first workflow</span>
        <h3><i class="bi bi-keyboard"></i> Working keyboard-first</h3>
      </div>
      <div class="principle-shot" role="img" aria-label="Screenshot placeholder for command discovery and shortcuts">
        <div class="shot-topline"><span></span><span></span><span></span><strong>Command help</strong></div>
        <div class="shot-body shot-body--commands"><kbd>/providers</kbd><kbd>/threads</kbd><kbd>Ctrl+K</kbd><kbd>Alt+↑</kbd></div>
      </div>
    </article>
    <article class="principle-feature" style="--accent: #22d3ee; --accent-2: #a78bfa;">
      <div class="principle-copy">
        <span class="principle-kicker">Thread-oriented workspace</span>
        <h3><i class="bi bi-diagram-3"></i> Coordinating multiple agents in durable threads</h3>
      </div>
      <div class="principle-shot" role="img" aria-label="Screenshot placeholder for project threads and delegated child agents">
        <div class="shot-topline"><span></span><span></span><span></span><strong>Agents &amp; threads</strong></div>
        <div class="shot-body shot-body--tree"><span></span><span></span><span></span><span></span><span></span></div>
      </div>
    </article>
    <article class="principle-feature" style="--accent: #fb923c; --accent-2: #38bdf8;">
      <div class="principle-copy">
        <span class="principle-kicker">Provider-agnostic runtime</span>
        <h3><i class="bi bi-cpu"></i> Switching between multiple providers and models, local and remote</h3>
      </div>
      <div class="principle-shot" role="img" aria-label="Screenshot placeholder for provider and model selection">
        <div class="shot-topline"><span></span><span></span><span></span><strong>Providers</strong></div>
        <div class="shot-body shot-body--providers"><span>Codex</span><span>OpenAI</span><span>Anthropic</span><span>Gemini</span></div>
      </div>
    </article>
    <article class="principle-feature" style="--accent: #818cf8; --accent-2: #2dd4bf;">
      <div class="principle-copy">
        <span class="principle-kicker">Native .NET foundation</span>
        <h3><i class="bi bi-braces-asterisk"></i> Staying native to C# and .NET</h3>
      </div>
      <div class="principle-shot" role="img" aria-label="Screenshot placeholder for the native .NET foundation">
        <div class="shot-topline"><span></span><span></span><span></span><strong>.NET TUI</strong></div>
        <div class="shot-body shot-body--stack"><span>CodeAlta</span><span>XenoAtom.Terminal.UI</span><span>.NET</span></div>
      </div>
    </article>
    <article class="principle-feature" style="--accent: #f43f5e; --accent-2: #fbbf24;">
      <div class="principle-copy">
        <span class="principle-kicker">Actionable errors</span>
        <h3><i class="bi bi-life-preserver"></i> Turning failures into repair paths</h3>
      </div>
      <div class="principle-shot" role="img" aria-label="Screenshot placeholder for validation and recovery UI">
        <div class="shot-topline"><span></span><span></span><span></span><strong>Recovery</strong></div>
        <div class="shot-body shot-body--error"><span>!</span><span></span><span></span><span class="fix-button">Fix</span></div>
      </div>
    </article>
    <article class="principle-feature" style="--accent: #a3e635; --accent-2: #06b6d4;">
      <div class="principle-copy">
        <span class="principle-kicker">Plugin support</span>
        <h3><i class="bi bi-puzzle"></i> Adding trusted local plugins</h3>
      </div>
      <div class="principle-shot" role="img" aria-label="Screenshot placeholder for plugin management">
        <div class="shot-topline"><span></span><span></span><span></span><strong>Plugins</strong></div>
        <div class="shot-body shot-body--plugins"><span>plugin.cs</span><span>commands</span><span>tools</span></div>
      </div>
    </article>
  </div>
  <div class="principles-cta text-center mt-5">
    <a href="{{site.basepath}}/docs/principles/" class="btn btn-outline-primary btn-lg">Read the full principles manifesto</a>
  </div>
</section>
<style>
.codealta-ascii-logo {
  display: block;
  width: max-content;
  max-width: 100%;
  overflow-x: auto;
  padding: 1rem 1.25rem;
  margin-bottom: 0;
  border-radius: 1rem;
  background: radial-gradient(circle at 20% 10%, rgba(0, 209, 255, 0.14), transparent 28%), linear-gradient(135deg, rgba(11, 18, 32, 0.52), rgba(17, 27, 48, 0.32));
  border: 1px solid rgba(255, 255, 255, 0.10);
  box-shadow: 0 1.25rem 3rem rgba(0, 0, 0, 0.28);
  color: rgba(234, 242, 255, 0.92);
  font-family: "Cascadia Mono", "SFMono-Regular", Consolas, "Liberation Mono", Menlo, monospace;
  font-size: clamp(0.42rem, 1.23vw, 1rem);
  line-height: 1.05;
  text-align: left;
  white-space: pre;
}
.logo-code { color: rgba(234, 242, 255, 0.86); }
.logo-alta {
  color: transparent;
  background-image: linear-gradient(115deg, rgba(0, 209, 255, 0.82), #7ae8ff, #4f46e5, #a855f7, #ffffff, #00d1ff);
  background-size: 240% 240%;
  background-position: var(--alta-logo-shift, 0%) 50%;
  -webkit-background-clip: text;
  background-clip: text;
  filter: drop-shadow(0 0 0.4rem rgba(0, 209, 255, 0.26));
}
.codealta-workflow-preview {
  position: relative;
}
.workflow-preview-panel {
  display: grid;
  grid-template-columns: minmax(0, .82fr) minmax(22rem, 1.18fr);
  align-items: center;
  gap: clamp(1.5rem, 4vw, 3rem);
  padding: clamp(1.5rem, 4vw, 3rem);
  border: 1px solid rgba(255, 255, 255, .10);
  border-radius: 2rem;
  background:
    radial-gradient(circle at 10% 15%, rgba(0, 209, 255, .16), transparent 32%),
    radial-gradient(circle at 92% 84%, rgba(168, 85, 247, .16), transparent 30%),
    linear-gradient(135deg, rgba(255,255,255,.065), rgba(255,255,255,.02));
  box-shadow: 0 1.75rem 4rem rgba(0, 0, 0, .24);
}
.workflow-preview-copy {
  max-width: 32rem;
}
.workflow-preview-copy h2 {
  font-size: clamp(1.6rem, 2.6vw, 2.45rem);
}
.terminal-demo-placeholder {
  border-radius: 1.3rem;
  border: 1px solid rgba(255,255,255,.13);
  background: #07111f;
  box-shadow: inset 0 0 0 1px rgba(255,255,255,.035), 0 1.25rem 3rem rgba(0,0,0,.28);
  overflow: hidden;
}
.demo-titlebar {
  display: flex;
  align-items: center;
  gap: .45rem;
  padding: .7rem .9rem;
  background: linear-gradient(90deg, rgba(255,255,255,.09), rgba(255,255,255,.035));
  color: rgba(255,255,255,.72);
  font-family: "Cascadia Mono", Consolas, monospace;
  font-size: .85rem;
}
.demo-titlebar span { width: .7rem; height: .7rem; border-radius: 50%; display: inline-block; }
.demo-titlebar span:nth-child(1) { background: #ff5f56; }
.demo-titlebar span:nth-child(2) { background: #ffbd2e; }
.demo-titlebar span:nth-child(3) { background: #27c93f; margin-right: .45rem; }
.terminal-demo-placeholder pre {
  margin: 0;
  padding: clamp(1rem, 2.3vw, 1.5rem);
  color: #d9e6ff;
  background: transparent;
  white-space: pre-wrap;
  font-size: clamp(.82rem, 1.05vw, .98rem);
}
.codealta-principles {
  position: relative;
}
.principles-intro {
  max-width: 68rem;
  position: relative;
}
.principles-intro::after {
  content: "";
  display: block;
  width: min(36rem, 82%);
  height: 1px;
  margin: 1.75rem auto 0;
  background: linear-gradient(90deg, transparent, rgba(0, 209, 255, .55), rgba(168, 85, 247, .55), transparent);
}
.principle-flow {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  align-items: stretch;
  gap: clamp(1.1rem, 2.1vw, 1.75rem);
}
.principle-feature {
  isolation: isolate;
  position: relative;
  display: grid;
  grid-template-columns: minmax(0, .82fr) minmax(12rem, 1.18fr);
  align-items: center;
  gap: clamp(.9rem, 1.8vw, 1.4rem);
  min-height: 14.5rem;
  padding: clamp(1rem, 2vw, 1.5rem);
  border: 1px solid rgba(255, 255, 255, .10);
  border-radius: 1.65rem;
  background:
    linear-gradient(135deg, rgba(255,255,255,.075), rgba(255,255,255,.025) 38%, rgba(255,255,255,.055)),
    rgba(7, 17, 31, .70);
  box-shadow: 0 1.35rem 3rem rgba(0, 0, 0, .20);
  overflow: hidden;
}
.principle-feature::before,
.principle-feature::after {
  content: "";
  position: absolute;
  z-index: -1;
  border-radius: 999px;
  filter: blur(8px);
  opacity: .20;
}
.principle-feature::before {
  width: 15rem;
  height: 15rem;
  inset: -7rem auto auto -5rem;
  background: radial-gradient(circle, var(--accent), transparent 68%);
}
.principle-feature::after {
  width: 12rem;
  height: 12rem;
  right: -5rem;
  bottom: -6rem;
  background: radial-gradient(circle, var(--accent-2), transparent 68%);
}
.principle-copy {
  position: relative;
  z-index: 1;
}
.principle-kicker {
  display: inline-flex;
  align-items: center;
  gap: .4rem;
  margin-bottom: .55rem;
  color: rgba(234, 242, 255, .66);
  font-size: .72rem;
  font-weight: 700;
  letter-spacing: .12em;
  text-transform: uppercase;
}
.principle-copy h3 {
  margin: 0;
  color: rgba(248, 252, 255, .96);
  font-size: clamp(1.08rem, 1.25vw, 1.42rem);
  line-height: 1.14;
}
.principle-copy h3 i {
  display: inline-grid;
  place-items: center;
  width: 2.05rem;
  height: 2.05rem;
  margin-right: .48rem;
  border-radius: .78rem;
  color: white;
  background: linear-gradient(135deg, var(--accent), var(--accent-2));
  box-shadow: 0 .6rem 1.35rem color-mix(in srgb, var(--accent) 25%, transparent);
  font-size: 1rem;
  vertical-align: .08em;
}
.principle-shot {
  border: 1px solid rgba(255, 255, 255, .13);
  border-radius: 1.1rem;
  background: linear-gradient(180deg, rgba(11, 18, 32, .96), rgba(3, 10, 19, .96));
  box-shadow: inset 0 0 0 1px rgba(255,255,255,.035), 0 .85rem 2rem rgba(0,0,0,.22);
  overflow: hidden;
}
.shot-topline {
  display: flex;
  align-items: center;
  gap: .38rem;
  padding: .55rem .7rem;
  color: rgba(234, 242, 255, .64);
  background: linear-gradient(90deg, rgba(255,255,255,.085), rgba(255,255,255,.025));
  font-family: "Cascadia Mono", Consolas, monospace;
  font-size: .68rem;
}
.shot-topline span {
  width: .5rem;
  height: .5rem;
  border-radius: 50%;
  background: var(--accent);
}
.shot-topline span:nth-child(2) { background: var(--accent-2); }
.shot-topline span:nth-child(3) { background: rgba(255,255,255,.32); margin-right: .25rem; }
.shot-body {
  min-height: 8.6rem;
  padding: .85rem;
  font-family: "Cascadia Mono", Consolas, monospace;
  font-size: .82rem;
}
.shot-body--workspace {
  display: grid;
  grid-template-columns: 3.2rem 1fr 1fr;
  gap: .5rem;
}
.shot-body--workspace .rail { grid-row: 1 / 5; border-radius: .65rem; background: linear-gradient(180deg, rgba(255,255,255,.16), rgba(255,255,255,.04)); }
.shot-body--workspace .timeline,
.shot-body--workspace .chip,
.shot-body--workspace .footer,
.shot-body .diff,
.shot-body .stats {
  display: block;
  border-radius: .5rem;
  background: rgba(255,255,255,.10);
}
.shot-body--workspace .timeline { height: 1.75rem; grid-column: 2 / 4; }
.shot-body--workspace .timeline.wide { background: linear-gradient(90deg, color-mix(in srgb, var(--accent) 34%, transparent), rgba(255,255,255,.08)); }
.shot-body--workspace .chip { height: 1.35rem; background: color-mix(in srgb, var(--accent) 30%, rgba(255,255,255,.08)); }
.shot-body--workspace .chip.alt { background: color-mix(in srgb, var(--accent-2) 32%, rgba(255,255,255,.08)); }
.shot-body--workspace .footer { height: 1.45rem; grid-column: 2 / 4; }
.shot-body .diff { height: 1rem; margin-bottom: .55rem; }
.shot-body .diff.plus { background: color-mix(in srgb, var(--accent) 32%, rgba(255,255,255,.08)); }
.shot-body .diff.short { width: 68%; }
.shot-body .stats { height: 2.65rem; margin-top: .85rem; background: linear-gradient(135deg, color-mix(in srgb, var(--accent-2) 25%, transparent), rgba(255,255,255,.075)); }
.shot-body--commands {
  display: flex;
  flex-wrap: wrap;
  align-content: center;
  gap: .55rem;
}
.shot-body--commands kbd,
.shot-body--providers span,
.shot-body--stack span,
.shot-body--plugins span,
.shot-body--error .fix-button {
  border: 1px solid rgba(255,255,255,.12);
  border-radius: .7rem;
  padding: .42rem .58rem;
  color: rgba(245, 250, 255, .9);
  background: linear-gradient(135deg, color-mix(in srgb, var(--accent) 25%, transparent), rgba(255,255,255,.06));
  box-shadow: inset 0 0 0 1px rgba(255,255,255,.025);
}
.shot-body--tree {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-content: center;
  gap: .55rem;
}
.shot-body--tree span {
  display: block;
  height: 1.55rem;
  border-left: 3px solid var(--accent);
  border-radius: .45rem;
  background: rgba(255,255,255,.08);
}
.shot-body--tree span:first-child { grid-column: 1 / 3; width: 72%; }
.shot-body--tree span:nth-child(3),
.shot-body--tree span:nth-child(5) { border-left-color: var(--accent-2); transform: translateX(.75rem); }
.shot-body--providers,
.shot-body--stack,
.shot-body--plugins {
  display: grid;
  align-content: center;
  gap: .55rem;
}
.shot-body--providers { grid-template-columns: 1fr 1fr; }
.shot-body--stack span:nth-child(2) { margin-left: .85rem; }
.shot-body--stack span:nth-child(3) { margin-left: 1.7rem; }
.shot-body--error {
  display: grid;
  grid-template-columns: 2rem 1fr;
  align-content: center;
  gap: .6rem;
}
.shot-body--error span:first-child {
  display: grid;
  place-items: center;
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  color: #08111f;
  font-weight: 800;
  background: linear-gradient(135deg, var(--accent), var(--accent-2));
}
.shot-body--error span:not(:first-child):not(.fix-button) {
  display: block;
  height: 1rem;
  border-radius: .55rem;
  background: rgba(255,255,255,.10);
}
.shot-body--error .fix-button { justify-self: start; grid-column: 2; }
.principles-cta .btn {
  border-radius: 999px;
}
@media (max-width: 1199.98px) {
  .principle-flow {
    grid-template-columns: 1fr;
  }
}
@media (max-width: 991.98px) {
  .workflow-preview-panel,
  .principle-feature {
    grid-template-columns: 1fr;
  }
  .workflow-preview-copy {
    max-width: none;
  }
}
@media (prefers-reduced-motion: reduce) {
  .logo-alta { background-position: 50% 50%; }
}
</style>

<script>
(function () {
  "use strict";
  var alta = document.querySelector(".logo-alta");
  if (!alta || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
  var start;
  function tick(timestamp) {
    if (start === undefined) start = timestamp;
    var phase = ((timestamp - start) / 5200) % 1;
    document.documentElement.style.setProperty("--alta-logo-shift", (phase * 100).toFixed(2) + "%");
    window.requestAnimationFrame(tick);
  }
  window.requestAnimationFrame(tick);
})();
</script>
