/**
 * SpireScratch — editor application logic.
 * Communicates with the game via WebSocket (ws://localhost:7878).
 * Falls back to File System Access API / download when the game is not running.
 */

let workspace = null;
let scriptsDirHandle = null;

// ─────────────────────── Script list cache ───────────────────────

/** @type {{ fileName: string, name: string|null, enabled: boolean|null, error: string|null }[]} */
let cachedScripts = [];
let activeFileName = null; // currently loaded script's file name

async function refreshScriptList() {
  if (wsReady) {
    try {
      const resp = await wsSend('list');
      cachedScripts = resp.data?.files ?? [];
      renderScriptList();
      return;
    } catch (_) {}
  }
  // FSA fallback: build list from directory handle
  if (scriptsDirHandle) {
    try {
      const entries = [];
      for await (const entry of scriptsDirHandle.values()) {
        if (entry.kind === 'file' && entry.name.endsWith('.json'))
          entries.push({ fileName: entry.name, name: null, enabled: null, error: null });
      }
      cachedScripts = entries;
      renderScriptList();
    } catch (_) {}
  }
}

function renderScriptList() {
  const list = document.getElementById('scriptList');
  if (!list) return;
  list.innerHTML = '';

  if (cachedScripts.length === 0) {
    const empty = document.createElement('div');
    empty.className = 'script-empty';
    empty.id = 'scriptListEmpty';
    empty.textContent = wsReady ? t('ui.noScripts') : t('ui.connectForList');
    list.appendChild(empty);
    return;
  }

  for (const s of cachedScripts) {
    const item = document.createElement('div');
    const classes = ['script-item'];
    if (s.error)            classes.push('has-error');
    if (s.enabled === false) classes.push('disabled');
    if (s.fileName === activeFileName) classes.push('active');
    item.className = classes.join(' ');
    item.title = s.error ?? s.fileName;

    const dot = document.createElement('span');
    dot.className = 'script-status-dot';
    dot.style.background = s.error ? '#e74c3c' : (s.enabled === false ? '#555' : '#2ecc71');

    const nameEl = document.createElement('div');
    nameEl.className = 's-name';
    nameEl.appendChild(dot);
    nameEl.appendChild(document.createTextNode(s.name ?? s.fileName.replace('.json', '')));

    const fileEl = document.createElement('div');
    fileEl.className = 's-file';
    fileEl.textContent = s.fileName;

    item.appendChild(nameEl);
    item.appendChild(fileEl);
    item.addEventListener('click', () => loadScriptByName(s));
    list.appendChild(item);
  }
}

async function loadScriptByName(s) {
  if (wsReady) {
    try {
      const r = await wsSend('load', { fileName: s.fileName });
      loadScript(r.data.content, s.fileName);
      setActiveItem(s.fileName);
    } catch (e) { setStatus(t('status.loadFailed', e.message), true); }
    return;
  }
  if (scriptsDirHandle) {
    try {
      const fh = await scriptsDirHandle.getFileHandle(s.fileName);
      const file = await fh.getFile();
      loadScript(await file.text(), s.fileName);
      setActiveItem(s.fileName);
    } catch (e) { setStatus(t('status.loadFailed', e.message), true); }
  }
}

function setActiveItem(fileName) {
  activeFileName = fileName;
  document.querySelectorAll('.script-item').forEach(el => {
    const fileEl = el.querySelector('.s-file');
    el.classList.toggle('active', fileEl?.textContent === fileName);
  });
}

// ─────────────────────── WebSocket bridge ───────────────────────

const WS_URL = 'ws://localhost:7878';
let ws = null;
let wsReady = false;
const pendingCalls = new Map(); // id → { resolve, reject }
let callIdCounter = 0;
let reconnectTimer = null;

function wsConnect() {
  if (ws && (ws.readyState === WebSocket.CONNECTING || ws.readyState === WebSocket.OPEN)) return;

  ws = new WebSocket(WS_URL);

  ws.onopen = () => {
    wsReady = true;
    clearTimeout(reconnectTimer);
    updateConnectionBadge(true);
    refreshScriptList();
  };

  ws.onclose = () => {
    wsReady = false;
    ws = null;
    updateConnectionBadge(false);
    renderScriptList(); // show "connect game" hint
    reconnectTimer = setTimeout(wsConnect, 3000);
  };

  ws.onerror = () => {
    wsReady = false;
    updateConnectionBadge(false);
  };

  ws.onmessage = (evt) => {
    try {
      const msg = JSON.parse(evt.data);
      if (msg.id !== undefined) {
        const pending = pendingCalls.get(msg.id);
        if (pending) {
          pendingCalls.delete(msg.id);
          if (msg.type === 'error') pending.reject(new Error(msg.data?.message ?? 'Bridge error'));
          else pending.resolve(msg);
          return;
        }
      }
      handlePush(msg);
    } catch (_) {}
  };
}

function wsSend(cmd, payload = {}) {
  return new Promise((resolve, reject) => {
    if (!wsReady || !ws) { reject(new Error('not_connected')); return; }
    const id = ++callIdCounter;
    pendingCalls.set(id, { resolve, reject });
    setTimeout(() => {
      if (pendingCalls.has(id)) {
        pendingCalls.delete(id);
        reject(new Error('timeout'));
      }
    }, 5000);
    ws.send(JSON.stringify({ cmd, id, ...payload }));
  });
}

function handlePush(msg) {
  if (msg.type === 'state') updateStateBadge(msg.data);
}

function updateConnectionBadge(connected) {
  const badge = document.getElementById('connBadge');
  if (!badge) return;
  badge.textContent = connected ? t('ui.connected') : t('ui.disconnected');
  badge.style.color = connected ? '#2ecc71' : '#e74c3c';
  badge.title = connected ? 'ws://localhost:7878' : t('ui.disconnectedHint');
}

function updateStateBadge(state) {
  if (!state) return;
  const el = document.getElementById('stateBadge');
  if (!el) return;
  if (state.hp !== undefined) {
    el.textContent = `HP ${state.hp}/${state.maxHp}  Floor ${state.floor}`;
    el.style.display = '';
  }
}

// ─────────────────────── Init ───────────────────────

const t = I18n.t;

function initEditor() {
  I18n.setLanguage(I18n.detectLanguage());
  applyUiTranslations();
  defineBlocks();

  workspace = Blockly.inject('blocklyDiv', {
    toolbox: buildToolbox(),
    grid: { spacing: 20, length: 3, colour: '#ccc', snap: true },
    zoom: { controls: true, wheel: true, startScale: 1.0, maxScale: 3, minScale: 0.3 },
    trashcan: true,
    move: { scrollbars: true, drag: true, wheel: true },
    theme: Blockly.Themes.Classic,
    renderer: 'zelos',
  });

  bindButtons();

  const langSelect = document.getElementById('langSelect');
  langSelect.value = I18n.lang();
  langSelect.addEventListener('change', () => switchLanguage(langSelect.value));

  setStatus(t('status.ready'));
  renderScriptList(); // show empty state before WS connects
  wsConnect();
}

function bindButtons() {
  document.getElementById('btnSave').addEventListener('click', onSave);
  document.getElementById('btnBindDir').addEventListener('click', onBindDir);
  document.getElementById('btnExport').addEventListener('click', onExport);
  document.getElementById('btnImport').addEventListener('click', onImport);
  document.getElementById('btnClear').addEventListener('click', onClear);
  document.getElementById('scriptPanelRefresh').addEventListener('click', refreshScriptList);
}

// ─────────────────────── Language switching ───────────────────────

function switchLanguage(lang) {
  const xml = workspace ? Blockly.Xml.workspaceToDom(workspace) : null;
  I18n.setLanguage(lang);
  applyUiTranslations();

  if (workspace) { workspace.dispose(); workspace = null; }

  for (const key of Object.keys(Blockly.Blocks)) {
    if (key.startsWith('sts_')) delete Blockly.Blocks[key];
  }
  defineBlocks();

  workspace = Blockly.inject('blocklyDiv', {
    toolbox: buildToolbox(),
    grid: { spacing: 20, length: 3, colour: '#ccc', snap: true },
    zoom: { controls: true, wheel: true, startScale: 1.0, maxScale: 3, minScale: 0.3 },
    trashcan: true,
    move: { scrollbars: true, drag: true, wheel: true },
    theme: Blockly.Themes.Classic,
    renderer: 'zelos',
  });

  if (xml) { try { Blockly.Xml.domToWorkspace(xml, workspace); } catch (_) {} }

  bindButtons();
  renderScriptList();
  setStatus(t('status.ready'));
}

function applyUiTranslations() {
  document.title = 'SpireScratch — STS2';
  setText('headerTitle',      t('ui.title'));
  setText('labelName',        t('ui.name'));
  setText('scriptPanelTitle', t('ui.scriptPanel'));
  const nameInput = document.getElementById('scriptName');
  if (nameInput) nameInput.placeholder = t('ui.namePlaceholder');
  setText('btnSave',    t('ui.save'));
  setText('btnBindDir', t('ui.bindFolder'));
  setText('btnExport',  t('ui.exportXml'));
  setText('btnImport',  t('ui.importXml'));
  setText('btnClear',   t('ui.clear'));
  setText('footerHint', t('ui.footer'));
  updateConnectionBadge(wsReady);
}

function setText(id, text) {
  const el = document.getElementById(id);
  if (el) el.textContent = text;
}

function buildToolbox() {
  return {
    kind: 'categoryToolbox',
    contents: [
      { kind: 'category', name: t('cat.trigger'), colour: '45', contents: [
          { kind: 'block', type: 'sts_trigger' }]},
      { kind: 'category', name: t('cat.conditions'), colour: '210', contents: [
          { kind: 'block', type: 'sts_hp_condition' },
          { kind: 'block', type: 'sts_floor_condition' },
          { kind: 'block', type: 'sts_power_condition' },
          { kind: 'block', type: 'sts_var_condition' },
          { kind: 'sep', gap: '20' },
          { kind: 'block', type: 'sts_logic_and' },
          { kind: 'block', type: 'sts_logic_or' },
          { kind: 'block', type: 'sts_logic_not' }]},
      { kind: 'category', name: t('cat.control'), colour: '120', contents: [
          { kind: 'block', type: 'sts_if' },
          { kind: 'block', type: 'sts_foreach_enemy' },
          { kind: 'block', type: 'sts_repeat', fields: { COUNT: 3 } }]},
      { kind: 'category', name: t('cat.actions'), colour: '330', contents: [
          { kind: 'block', type: 'sts_apply_power', fields: { POWER_ID: 'Strength', AMOUNT: 1 } },
          { kind: 'block', type: 'sts_add_card',    fields: { CARD_ID: 'Strike' } },
          { kind: 'block', type: 'sts_use_potion',  fields: { POTION_ID: 'FirePotion' } },
          { kind: 'block', type: 'sts_save_slot' }]},
      { kind: 'category', name: t('cat.variables'), colour: '60', contents: [
          { kind: 'block', type: 'sts_set_var',  fields: { VAR_NAME: 'counter', VALUE: 0 } },
          { kind: 'block', type: 'sts_incr_var', fields: { VAR_NAME: 'counter', VALUE: 1 } }]},
    ],
  };
}

// ─────────────────────── Save ───────────────────────

async function onSave() {
  const json = generateScript();
  if (!json) { setStatus(t('status.noTrigger'), true); return; }

  const name     = document.getElementById('scriptName').value.trim() || 'untitled';
  const fileName = sanitizeFileName(name) + '.json';

  // Overwrite check
  if (!await confirmOverwrite(fileName)) return;

  // 1. WebSocket → game writes file directly
  if (wsReady) {
    try {
      await wsSend('save', { fileName, content: json });
      setActiveItem(fileName);
      await refreshScriptList(); // auto-refresh panel
      setStatus(t('status.savedWs', fileName));
      return;
    } catch (e) {
      console.warn('WS save failed, falling back:', e);
    }
  }

  // 2. File System Access API
  if (scriptsDirHandle) {
    try {
      const fh = await scriptsDirHandle.getFileHandle(fileName, { create: true });
      const w  = await fh.createWritable();
      await w.write(json);
      await w.close();
      setActiveItem(fileName);
      await refreshScriptList();
      setStatus(t('status.saved', fileName));
      return;
    } catch (e) {
      console.warn('FSA write failed, falling back:', e);
    }
  }

  // 3. Download fallback
  downloadFile(fileName, json);
  setStatus(t('status.downloaded', fileName));
}

/**
 * Returns true if the save should proceed.
 * Prompts when a file with the same name already exists.
 */
async function confirmOverwrite(fileName) {
  // Check WS cache
  if (wsReady && cachedScripts.some(s => s.fileName === fileName)) {
    return confirm(t('status.confirmOverwrite', fileName));
  }
  // Check FSA directory
  if (scriptsDirHandle) {
    try {
      await scriptsDirHandle.getFileHandle(fileName); // throws NotFoundError if absent
      return confirm(t('status.confirmOverwrite', fileName));
    } catch (_) {}
  }
  return true;
}

// ─────────────────────── Bind Directory ───────────────────────

async function onBindDir() {
  try {
    scriptsDirHandle = await window.showDirectoryPicker({ mode: 'readwrite' });
    document.getElementById('dirLabel').textContent = t('status.dirBoundLabel', scriptsDirHandle.name);
    setStatus(t('status.dirBound', scriptsDirHandle.name));
    await refreshScriptList();
  } catch (e) {
    if (e.name !== 'AbortError') setStatus(t('status.bindFailed', e.message), true);
  }
}

// ─────────────────────── Export / Import XML ───────────────────────

function onExport() {
  const xml  = Blockly.Xml.workspaceToDom(workspace);
  const text = Blockly.Xml.domToText(xml);
  const name = (document.getElementById('scriptName').value.trim() || 'workspace') + '.xml';
  downloadFile(name, text);
  setStatus(t('status.exported'));
}

function onImport() {
  const input = document.createElement('input');
  input.type = 'file';
  input.accept = '.xml';
  input.onchange = async (e) => {
    const file = e.target.files[0];
    if (!file) return;
    try {
      workspace.clear();
      const dom = Blockly.utils.xml.textToDom(await file.text());
      Blockly.Xml.domToWorkspace(dom, workspace);
      setStatus(t('status.imported', file.name));
    } catch (err) {
      setStatus(t('status.importError', err.message), true);
    }
  };
  input.click();
}

function onClear() {
  if (confirm(t('status.confirmClear'))) {
    workspace.clear();
    activeFileName = null;
    document.querySelectorAll('.script-item.active').forEach(el => el.classList.remove('active'));
    setStatus(t('status.cleared'));
  }
}

// ─────────────────────── Generate JSON ───────────────────────

function generateScript() {
  const trigger = workspace.getTopBlocks(true).find(b => b.type === 'sts_trigger');
  if (!trigger) return null;
  const rawCode = StsGen.blockToCode(trigger);
  if (!rawCode) return null;
  try {
    const parsed = JSON.parse(rawCode);
    const name   = document.getElementById('scriptName').value.trim() || 'Untitled Script';
    return JSON.stringify({
      name,
      trigger:       parsed.trigger,
      rootCondition: parsed.rootCondition || null,
      rootAction:    parsed.rootAction    || null,
      enabled:       true,
      _blocklyXml:   getWorkspaceXml(),
    }, null, 2);
  } catch (e) {
    console.error('JSON parse error:', e, rawCode);
    return null;
  }
}

// ─────────────────────── Load Script JSON ───────────────────────

function loadScript(jsonText, fileName) {
  try {
    const script = JSON.parse(jsonText);
    document.getElementById('scriptName').value = script.name || fileName.replace('.json', '');
    if (script._blocklyXml) {
      workspace.clear();
      Blockly.Xml.domToWorkspace(Blockly.utils.xml.textToDom(script._blocklyXml), workspace);
      setStatus(t('status.loaded', fileName));
    } else {
      setStatus(t('status.loadedJson', fileName));
    }
  } catch (e) {
    setStatus(t('status.parseError', e.message), true);
  }
}

// ─────────────────────── Utilities ───────────────────────

function getWorkspaceXml() {
  return Blockly.Xml.domToText(Blockly.Xml.workspaceToDom(workspace));
}

function setStatus(msg, isError) {
  const el = document.getElementById('status');
  el.textContent = msg;
  el.style.color = isError ? '#e74c3c' : '#7f8c8d';
}

function sanitizeFileName(name) {
  return name.replace(/[^a-zA-Z0-9_\-\u4e00-\u9fff]/g, '_').substring(0, 64);
}

function downloadFile(name, content) {
  const blob = new Blob([content], { type: 'application/json' });
  const url  = URL.createObjectURL(blob);
  const a    = Object.assign(document.createElement('a'), { href: url, download: name });
  a.click();
  URL.revokeObjectURL(url);
}

document.addEventListener('DOMContentLoaded', initEditor);
