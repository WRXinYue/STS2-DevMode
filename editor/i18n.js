/**
 * SpireScratch — internationalization module.
 * Auto-detects browser language, persists choice to localStorage.
 */

const I18n = (() => {
  const translations = {
    en: {
      // ── UI ──
      'ui.title':        'SpireScratch',
      'ui.name':         'Name:',
      'ui.namePlaceholder': 'Script name',
      'ui.defaultName':  'My Script',
      'ui.save':         'Save',
      'ui.load':         'Load',
      'ui.bindFolder':   'Bind Folder',
      'ui.exportXml':    'Export XML',
      'ui.importXml':    'Import XML',
      'ui.clear':        'Clear',
      'ui.footer':       'STS2 DevMode \u2022 SpireScratch Visual Scripting',
      'ui.loading':      'Loading...',

      // ── Status messages ──
      'status.ready':        'Ready \u2014 drag blocks to build a script.',
      'status.saved':        'Saved: {0}',
      'status.downloaded':   'Downloaded: {0}',
      'status.noTrigger':    'Error: no trigger block found.',
      'status.noScripts':    'No scripts found in directory.',
      'status.loadFailed':   'Load failed: {0}',
      'status.bindFailed':   'Bind failed: {0}',
      'status.dirBound':     'Directory bound \u2014 saves go directly to {0}',
      'status.dirBoundLabel':'Bound: {0}',
      'status.exported':     'Workspace exported.',
      'status.imported':     'Workspace imported from {0}',
      'status.importError':  'Import error: {0}',
      'status.cleared':      'Workspace cleared.',
      'status.loaded':       'Loaded: {0} (with workspace)',
      'status.loadedJson':   'Loaded: {0} (JSON only \u2014 blocks not restored)',
      'status.parseError':   'Parse error: {0}',
      'status.confirmClear': 'Clear workspace?',
      'status.promptLoad':   'Enter script file name to load:',

      // ── Toolbox categories ──
      'cat.trigger':    'Trigger',
      'cat.conditions': 'Conditions',
      'cat.control':    'Control',
      'cat.actions':    'Actions',
      'cat.variables':  'Variables',

      // ── Block: trigger ──
      'block.trigger.when':    'When %1',
      'block.trigger.do':      'do %1',
      'block.trigger.tooltip':  'Runs when the selected game event occurs.',
      'trigger.combatStart':   'Combat Start',
      'trigger.combatEnd':     'Combat End',
      'trigger.turnStart':     'Turn Start',
      'trigger.turnEnd':       'Turn End',
      'trigger.onDraw':        'On Draw',
      'trigger.onDamageDealt': 'On Damage Dealt',
      'trigger.onDamageTaken': 'On Damage Taken',
      'trigger.onPotionUsed':  'On Potion Used',
      'trigger.onCardPlayed':  'On Card Played',
      'trigger.onShuffle':     'On Shuffle',

      // ── Block: conditions ──
      'block.hp.msg':       'HP %1 %2 %%',
      'block.hp.tooltip':   'Check player HP percentage.',
      'block.hp.below':     'below',
      'block.hp.above':     'above',
      'block.floor.msg':    'Floor %1 %2',
      'block.floor.tooltip':'Check current floor number.',
      'block.floor.above':  'above',
      'block.floor.below':  'below',
      'block.power.msg':    '%1 power %2',
      'block.power.tooltip':'Check if player has a specific power.',
      'block.power.has':    'has',
      'block.power.notHas': 'not has',
      'block.var.msg':      'var %1 %2 %3',
      'block.var.tooltip':  'Compare a script variable.',
      'block.and.msg':      '%1 AND %2',
      'block.and.tooltip':  'Both conditions must be true.',
      'block.or.msg':       '%1 OR %2',
      'block.or.tooltip':   'At least one condition must be true.',
      'block.not.msg':      'NOT %1',
      'block.not.tooltip':  'Invert a condition.',

      // ── Block: control ──
      'block.if.msg':        'if %1',
      'block.if.do':         'do %1',
      'block.if.else':       'else %1',
      'block.if.tooltip':    'Conditional execution.',
      'block.foreach.msg':   'for each enemy do %1',
      'block.foreach.tooltip':'Execute actions targeting all enemies.',
      'block.repeat.msg':    'repeat %1 times %2',
      'block.repeat.tooltip':'Repeat actions a fixed number of times.',

      // ── Block: actions ──
      'block.applyPower.msg':    'apply power %1 \u00d7 %2 to %3',
      'block.applyPower.tooltip':'Apply a power to a target.',
      'block.addCard.msg':       'add card %1',
      'block.addCard.tooltip':   'Add a card to the deck.',
      'block.usePotion.msg':     'use potion %1',
      'block.usePotion.tooltip': 'Use a potion from inventory.',
      'block.saveSlot.msg':      'save to slot %1',
      'block.saveSlot.tooltip':  'Quick-save to a slot.',
      'target.player':     'Player',
      'target.allEnemies': 'All Enemies',
      'target.allies':     'Allies',

      // ── Block: variables ──
      'block.setVar.msg':     'set var %1 = %2',
      'block.setVar.tooltip': 'Set a script variable to a value.',
      'block.incrVar.msg':    'change var %1 by %2',
      'block.incrVar.tooltip':'Increment a script variable.',
    },

    zh: {
      // ── UI ──
      'ui.title':        'SpireScratch',
      'ui.name':         '\u540d\u79f0:',
      'ui.namePlaceholder': '\u811a\u672c\u540d\u79f0',
      'ui.defaultName':  '\u6211\u7684\u811a\u672c',
      'ui.save':         '\u4fdd\u5b58',
      'ui.load':         '\u52a0\u8f7d',
      'ui.bindFolder':   '\u7ed1\u5b9a\u6587\u4ef6\u5939',
      'ui.exportXml':    '\u5bfc\u51fa XML',
      'ui.importXml':    '\u5bfc\u5165 XML',
      'ui.clear':        '\u6e05\u7a7a',
      'ui.footer':       'STS2 DevMode \u2022 SpireScratch \u53ef\u89c6\u5316\u811a\u672c',
      'ui.loading':      '\u52a0\u8f7d\u4e2d...',

      // ── Status messages ──
      'status.ready':        '\u5c31\u7eea \u2014 \u62d6\u62fd\u79ef\u6728\u6765\u6784\u5efa\u811a\u672c\u3002',
      'status.saved':        '\u5df2\u4fdd\u5b58: {0}',
      'status.downloaded':   '\u5df2\u4e0b\u8f7d: {0}',
      'status.noTrigger':    '\u9519\u8bef: \u672a\u627e\u5230\u89e6\u53d1\u5668\u79ef\u6728\u3002',
      'status.noScripts':    '\u76ee\u5f55\u4e2d\u672a\u627e\u5230\u811a\u672c\u3002',
      'status.loadFailed':   '\u52a0\u8f7d\u5931\u8d25: {0}',
      'status.bindFailed':   '\u7ed1\u5b9a\u5931\u8d25: {0}',
      'status.dirBound':     '\u76ee\u5f55\u5df2\u7ed1\u5b9a \u2014 \u4fdd\u5b58\u76f4\u63a5\u5199\u5165 {0}',
      'status.dirBoundLabel':'\u5df2\u7ed1\u5b9a: {0}',
      'status.exported':     '\u5de5\u4f5c\u533a\u5df2\u5bfc\u51fa\u3002',
      'status.imported':     '\u5df2\u4ece {0} \u5bfc\u5165\u5de5\u4f5c\u533a\u3002',
      'status.importError':  '\u5bfc\u5165\u9519\u8bef: {0}',
      'status.cleared':      '\u5de5\u4f5c\u533a\u5df2\u6e05\u7a7a\u3002',
      'status.loaded':       '\u5df2\u52a0\u8f7d: {0}\uff08\u542b\u5de5\u4f5c\u533a\uff09',
      'status.loadedJson':   '\u5df2\u52a0\u8f7d: {0}\uff08\u4ec5 JSON \u2014 \u79ef\u6728\u672a\u6062\u590d\uff09',
      'status.parseError':   '\u89e3\u6790\u9519\u8bef: {0}',
      'status.confirmClear': '\u786e\u5b9a\u6e05\u7a7a\u5de5\u4f5c\u533a\uff1f',
      'status.promptLoad':   '\u8f93\u5165\u8981\u52a0\u8f7d\u7684\u811a\u672c\u6587\u4ef6\u540d:',

      // ── Toolbox categories ──
      'cat.trigger':    '\u89e6\u53d1\u5668',
      'cat.conditions': '\u6761\u4ef6',
      'cat.control':    '\u63a7\u5236',
      'cat.actions':    '\u52a8\u4f5c',
      'cat.variables':  '\u53d8\u91cf',

      // ── Block: trigger ──
      'block.trigger.when':    '\u5f53 %1',
      'block.trigger.do':      '\u6267\u884c %1',
      'block.trigger.tooltip':  '\u5f53\u6307\u5b9a\u7684\u6e38\u620f\u4e8b\u4ef6\u53d1\u751f\u65f6\u6267\u884c\u3002',
      'trigger.combatStart':   '\u6218\u6597\u5f00\u59cb',
      'trigger.combatEnd':     '\u6218\u6597\u7ed3\u675f',
      'trigger.turnStart':     '\u56de\u5408\u5f00\u59cb',
      'trigger.turnEnd':       '\u56de\u5408\u7ed3\u675f',
      'trigger.onDraw':        '\u62bd\u724c\u65f6',
      'trigger.onDamageDealt': '\u9020\u6210\u4f24\u5bb3\u65f6',
      'trigger.onDamageTaken': '\u53d7\u5230\u4f24\u5bb3\u65f6',
      'trigger.onPotionUsed':  '\u4f7f\u7528\u836f\u6c34\u65f6',
      'trigger.onCardPlayed':  '\u6253\u51fa\u5361\u724c\u65f6',
      'trigger.onShuffle':     '\u6d17\u724c\u65f6',

      // ── Block: conditions ──
      'block.hp.msg':       'HP %1 %2 %%',
      'block.hp.tooltip':   '\u68c0\u67e5\u73a9\u5bb6 HP \u767e\u5206\u6bd4\u3002',
      'block.hp.below':     '\u4f4e\u4e8e',
      'block.hp.above':     '\u9ad8\u4e8e',
      'block.floor.msg':    '\u697c\u5c42 %1 %2',
      'block.floor.tooltip':'\u68c0\u67e5\u5f53\u524d\u697c\u5c42\u3002',
      'block.floor.above':  '\u9ad8\u4e8e',
      'block.floor.below':  '\u4f4e\u4e8e',
      'block.power.msg':    '%1 \u80fd\u529b %2',
      'block.power.tooltip':'\u68c0\u67e5\u73a9\u5bb6\u662f\u5426\u62e5\u6709\u6307\u5b9a\u80fd\u529b\u3002',
      'block.power.has':    '\u62e5\u6709',
      'block.power.notHas': '\u6ca1\u6709',
      'block.var.msg':      '\u53d8\u91cf %1 %2 %3',
      'block.var.tooltip':  '\u6bd4\u8f83\u811a\u672c\u53d8\u91cf\u3002',
      'block.and.msg':      '%1 \u4e14 %2',
      'block.and.tooltip':  '\u4e24\u4e2a\u6761\u4ef6\u90fd\u5fc5\u987b\u4e3a\u771f\u3002',
      'block.or.msg':       '%1 \u6216 %2',
      'block.or.tooltip':   '\u81f3\u5c11\u4e00\u4e2a\u6761\u4ef6\u4e3a\u771f\u3002',
      'block.not.msg':      '\u975e %1',
      'block.not.tooltip':  '\u53cd\u8f6c\u6761\u4ef6\u3002',

      // ── Block: control ──
      'block.if.msg':        '\u5982\u679c %1',
      'block.if.do':         '\u6267\u884c %1',
      'block.if.else':       '\u5426\u5219 %1',
      'block.if.tooltip':    '\u6761\u4ef6\u6267\u884c\u3002',
      'block.foreach.msg':   '\u5bf9\u6bcf\u4e2a\u654c\u4eba\u6267\u884c %1',
      'block.foreach.tooltip':'\u5bf9\u6240\u6709\u654c\u4eba\u6267\u884c\u52a8\u4f5c\u3002',
      'block.repeat.msg':    '\u91cd\u590d %1 \u6b21 %2',
      'block.repeat.tooltip':'\u91cd\u590d\u6267\u884c\u52a8\u4f5c\u6307\u5b9a\u6b21\u6570\u3002',

      // ── Block: actions ──
      'block.applyPower.msg':    '\u65bd\u52a0\u80fd\u529b %1 \u00d7 %2 \u7ed9 %3',
      'block.applyPower.tooltip':'\u5bf9\u76ee\u6807\u65bd\u52a0\u4e00\u4e2a\u80fd\u529b\u3002',
      'block.addCard.msg':       '\u6dfb\u52a0\u5361\u724c %1',
      'block.addCard.tooltip':   '\u5c06\u5361\u724c\u52a0\u5165\u724c\u7ec4\u3002',
      'block.usePotion.msg':     '\u4f7f\u7528\u836f\u6c34 %1',
      'block.usePotion.tooltip': '\u4f7f\u7528\u80cc\u5305\u4e2d\u7684\u836f\u6c34\u3002',
      'block.saveSlot.msg':      '\u4fdd\u5b58\u5230\u69fd\u4f4d %1',
      'block.saveSlot.tooltip':  '\u5feb\u901f\u4fdd\u5b58\u5230\u6307\u5b9a\u69fd\u4f4d\u3002',
      'target.player':     '\u73a9\u5bb6',
      'target.allEnemies': '\u6240\u6709\u654c\u4eba',
      'target.allies':     '\u53cb\u65b9',

      // ── Block: variables ──
      'block.setVar.msg':     '\u8bbe\u7f6e\u53d8\u91cf %1 = %2',
      'block.setVar.tooltip': '\u8bbe\u7f6e\u811a\u672c\u53d8\u91cf\u7684\u503c\u3002',
      'block.incrVar.msg':    '\u53d8\u91cf %1 \u589e\u52a0 %2',
      'block.incrVar.tooltip':'\u589e\u52a0\u811a\u672c\u53d8\u91cf\u7684\u503c\u3002',
    },
  };

  let currentLang = 'en';

  function detectLanguage() {
    const saved = localStorage.getItem('spirescratch_lang');
    if (saved && translations[saved]) return saved;
    const nav = (navigator.language || '').toLowerCase();
    if (nav.startsWith('zh')) return 'zh';
    return 'en';
  }

  function setLanguage(lang) {
    if (!translations[lang]) lang = 'en';
    currentLang = lang;
    localStorage.setItem('spirescratch_lang', lang);
  }

  function t(key, ...args) {
    const dict = translations[currentLang] || translations.en;
    let str = dict[key] ?? translations.en[key] ?? key;
    args.forEach((a, i) => { str = str.replace(`{${i}}`, a); });
    return str;
  }

  function lang() { return currentLang; }
  function available() { return Object.keys(translations); }

  return { detectLanguage, setLanguage, t, lang, available };
})();
