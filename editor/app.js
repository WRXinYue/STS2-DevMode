/**
 * SpireScratch — editor application logic.
 * Handles workspace setup, save/load, directory binding, and i18n.
 */

let workspace = null;
let scriptsDirHandle = null;

const t = I18n.t;

// ─────────────────────── Init ───────────────────────

function initEditor() {
  I18n.setLanguage(I18n.detectLanguage());

  applyUiTranslations();
  defineBlocks();

  const toolbox = buildToolbox();

  workspace = Blockly.inject('blocklyDiv', {
    toolbox: toolbox,
    grid: { spacing: 20, length: 3, colour: '#ccc', snap: true },
    zoom: { controls: true, wheel: true, startScale: 1.0, maxScale: 3, minScale: 0.3 },
    trashcan: true,
    move: { scrollbars: true, drag: true, wheel: true },
    theme: Blockly.Themes.Classic,
    renderer: 'zelos',
  });

  document.getElementById('btnSave').addEventListener('click', onSave);
  document.getElementById('btnLoad').addEventListener('click', onLoad);
  document.getElementById('btnBindDir').addEventListener('click', onBindDir);
  document.getElementById('btnExport').addEventListener('click', onExport);
  document.getElementById('btnImport').addEventListener('click', onImport);
  document.getElementById('btnClear').addEventListener('click', onClear);

  const langSelect = document.getElementById('langSelect');
  langSelect.value = I18n.lang();
  langSelect.addEventListener('change', () => switchLanguage(langSelect.value));

  setStatus(t('status.ready'));
}

// ─────────────────────── Language switching ───────────────────────

function switchLanguage(lang) {
  const xml = workspace ? Blockly.Xml.workspaceToDom(workspace) : null;

  I18n.setLanguage(lang);
  applyUiTranslations();

  if (workspace) {
    workspace.dispose();
    workspace = null;
  }

  // Re-register blocks with new translations
  for (const key of Object.keys(Blockly.Blocks)) {
    if (key.startsWith('sts_')) delete Blockly.Blocks[key];
  }
  defineBlocks();

  const toolbox = buildToolbox();
  workspace = Blockly.inject('blocklyDiv', {
    toolbox: toolbox,
    grid: { spacing: 20, length: 3, colour: '#ccc', snap: true },
    zoom: { controls: true, wheel: true, startScale: 1.0, maxScale: 3, minScale: 0.3 },
    trashcan: true,
    move: { scrollbars: true, drag: true, wheel: true },
    theme: Blockly.Themes.Classic,
    renderer: 'zelos',
  });

  if (xml) {
    try { Blockly.Xml.domToWorkspace(xml, workspace); } catch (_) {}
  }

  document.getElementById('btnSave').addEventListener('click', onSave);
  document.getElementById('btnLoad').addEventListener('click', onLoad);
  document.getElementById('btnBindDir').addEventListener('click', onBindDir);
  document.getElementById('btnExport').addEventListener('click', onExport);
  document.getElementById('btnImport').addEventListener('click', onImport);
  document.getElementById('btnClear').addEventListener('click', onClear);

  setStatus(t('status.ready'));
}

function applyUiTranslations() {
  document.title = 'SpireScratch — STS2';
  setText('headerTitle', t('ui.title'));
  setText('labelName', t('ui.name'));
  const nameInput = document.getElementById('scriptName');
  if (nameInput) nameInput.placeholder = t('ui.namePlaceholder');
  setText('btnSave', t('ui.save'));
  setText('btnLoad', t('ui.load'));
  setText('btnBindDir', t('ui.bindFolder'));
  setText('btnExport', t('ui.exportXml'));
  setText('btnImport', t('ui.importXml'));
  setText('btnClear', t('ui.clear'));
  setText('footerHint', t('ui.footer'));
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
        { kind: 'block', type: 'sts_trigger' },
      ]},
      { kind: 'category', name: t('cat.conditions'), colour: '210', contents: [
        { kind: 'block', type: 'sts_hp_condition' },
        { kind: 'block', type: 'sts_floor_condition' },
        { kind: 'block', type: 'sts_power_condition' },
        { kind: 'block', type: 'sts_var_condition' },
        { kind: 'sep', gap: '20' },
        { kind: 'block', type: 'sts_logic_and' },
        { kind: 'block', type: 'sts_logic_or' },
        { kind: 'block', type: 'sts_logic_not' },
      ]},
      { kind: 'category', name: t('cat.control'), colour: '120', contents: [
        { kind: 'block', type: 'sts_if' },
        { kind: 'block', type: 'sts_foreach_enemy' },
        { kind: 'block', type: 'sts_repeat', fields: { COUNT: 3 } },
      ]},
      { kind: 'category', name: t('cat.actions'), colour: '330', contents: [
        { kind: 'block', type: 'sts_apply_power', fields: { POWER_ID: 'Strength', AMOUNT: 1 } },
        { kind: 'block', type: 'sts_add_card', fields: { CARD_ID: 'Strike' } },
        { kind: 'block', type: 'sts_use_potion', fields: { POTION_ID: 'FirePotion' } },
        { kind: 'block', type: 'sts_save_slot' },
      ]},
      { kind: 'category', name: t('cat.variables'), colour: '60', contents: [
        { kind: 'block', type: 'sts_set_var', fields: { VAR_NAME: 'counter', VALUE: 0 } },
        { kind: 'block', type: 'sts_incr_var', fields: { VAR_NAME: 'counter', VALUE: 1 } },
      ]},
    ],
  };
}

// ─────────────────────── Save ───────────────────────

async function onSave() {
  const json = generateScript();
  if (!json) { setStatus(t('status.noTrigger'), true); return; }

  const name = document.getElementById('scriptName').value.trim() || 'untitled';
  const fileName = sanitizeFileName(name) + '.json';

  if (scriptsDirHandle) {
    try {
      const fileHandle = await scriptsDirHandle.getFileHandle(fileName, { create: true });
      const writable = await fileHandle.createWritable();
      await writable.write(json);
      await writable.close();
      setStatus(t('status.saved', fileName));
      return;
    } catch (e) {
      console.warn('Directory write failed, falling back to download:', e);
    }
  }

  downloadFile(fileName, json);
  setStatus(t('status.downloaded', fileName));
}

// ─────────────────────── Load ───────────────────────

async function onLoad() {
  try {
    let fileHandle;
    if (scriptsDirHandle) {
      const entries = [];
      for await (const entry of scriptsDirHandle.values()) {
        if (entry.kind === 'file' && entry.name.endsWith('.json'))
          entries.push(entry);
      }
      if (entries.length === 0) { setStatus(t('status.noScripts'), true); return; }

      const picked = prompt(
        t('status.promptLoad') + '\n\n' +
        entries.map(e => '  \u2022 ' + e.name).join('\n'),
        entries[0].name
      );
      if (!picked) return;
      fileHandle = await scriptsDirHandle.getFileHandle(picked);
    } else {
      [fileHandle] = await window.showOpenFilePicker({
        types: [{ description: 'JSON Scripts', accept: { 'application/json': ['.json'] } }],
      });
    }

    const file = await fileHandle.getFile();
    const text = await file.text();
    loadScript(text, file.name);
  } catch (e) {
    if (e.name !== 'AbortError') setStatus(t('status.loadFailed', e.message), true);
  }
}

// ─────────────────────── Bind Directory ───────────────────────

async function onBindDir() {
  try {
    scriptsDirHandle = await window.showDirectoryPicker({ mode: 'readwrite' });
    document.getElementById('dirLabel').textContent = t('status.dirBoundLabel', scriptsDirHandle.name);
    setStatus(t('status.dirBound', scriptsDirHandle.name));
  } catch (e) {
    if (e.name !== 'AbortError') setStatus(t('status.bindFailed', e.message), true);
  }
}

// ─────────────────────── Export / Import XML ───────────────────────

function onExport() {
  const xml = Blockly.Xml.workspaceToDom(workspace);
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
    const text = await file.text();
    try {
      workspace.clear();
      const dom = Blockly.utils.xml.textToDom(text);
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
    setStatus(t('status.cleared'));
  }
}

// ─────────────────────── Generate JSON ───────────────────────

function generateScript() {
  const topBlocks = workspace.getTopBlocks(true);
  const trigger = topBlocks.find(b => b.type === 'sts_trigger');
  if (!trigger) return null;

  const rawCode = StsGen.blockToCode(trigger);
  if (!rawCode) return null;

  try {
    const parsed = JSON.parse(rawCode);
    const name = document.getElementById('scriptName').value.trim() || 'Untitled Script';
    const script = {
      name: name,
      trigger: parsed.trigger,
      rootCondition: parsed.rootCondition || null,
      rootAction: parsed.rootAction || null,
      enabled: true,
      _blocklyXml: getWorkspaceXml(),
    };
    return JSON.stringify(script, null, 2);
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
      const dom = Blockly.utils.xml.textToDom(script._blocklyXml);
      Blockly.Xml.domToWorkspace(dom, workspace);
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
  const dom = Blockly.Xml.workspaceToDom(workspace);
  return Blockly.Xml.domToText(dom);
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
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = name;
  a.click();
  URL.revokeObjectURL(url);
}

document.addEventListener('DOMContentLoaded', initEditor);
