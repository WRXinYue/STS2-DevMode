/**
 * SpireScratch — JSON code generators for each block type.
 * Produces the JSON AST that DevMode's ScriptManager can parse.
 */

const StsGen = new Blockly.Generator('STS_JSON');

// Statement blocks connected via next get collected into a Sequence array.
StsGen.scrub_ = function (block, code, _opt_thisOnly) {
  const next = block.nextConnection && block.nextConnection.targetBlock();
  if (next) {
    return code + ',\n' + StsGen.blockToCode(next);
  }
  return code;
};

// ─────────────────────── Helpers ───────────────────────

function statementsToAction(block, name) {
  const raw = StsGen.statementToCode(block, name);
  if (!raw) return null;
  const items = raw.split(',\n').filter(Boolean);
  if (items.length === 0) return null;
  if (items.length === 1) return items[0];
  return `{"type":"Sequence","steps":[${items.join(',')}]}`;
}

function conditionCode(block, name) {
  return StsGen.valueToCode(block, name, 0) || 'null';
}

// ─────────────────────── Trigger ───────────────────────

StsGen.forBlock = StsGen.forBlock || {};

StsGen.forBlock['sts_trigger'] = function (block) {
  const trigger = block.getFieldValue('TRIGGER');
  const body = statementsToAction(block, 'DO');
  return JSON.stringify({
    trigger: trigger,
    rootAction: body ? JSON.parse(body) : null,
  });
};

// ─────────────────────── Conditions ───────────────────────

StsGen.forBlock['sts_hp_condition'] = function (block) {
  const op = block.getFieldValue('OP');
  const value = block.getFieldValue('VALUE');
  return [`{"type":"${op}","value":"${value}"}`, 0];
};

StsGen.forBlock['sts_floor_condition'] = function (block) {
  const op = block.getFieldValue('OP');
  const value = block.getFieldValue('VALUE');
  return [`{"type":"${op}","value":"${value}"}`, 0];
};

StsGen.forBlock['sts_power_condition'] = function (block) {
  const op = block.getFieldValue('OP');
  const powerId = block.getFieldValue('POWER_ID');
  return [`{"type":"${op}","value":"${powerId}"}`, 0];
};

StsGen.forBlock['sts_var_condition'] = function (block) {
  const varName = block.getFieldValue('VAR_NAME');
  const op = block.getFieldValue('OP');
  const value = block.getFieldValue('VALUE');
  return [`{"type":"VarCompare","varName":"${varName}","op":"${op}","value":${value}}`, 0];
};

StsGen.forBlock['sts_logic_and'] = function (block) {
  const a = conditionCode(block, 'A');
  const b = conditionCode(block, 'B');
  return [`{"type":"AND","children":[${a},${b}]}`, 0];
};

StsGen.forBlock['sts_logic_or'] = function (block) {
  const a = conditionCode(block, 'A');
  const b = conditionCode(block, 'B');
  return [`{"type":"OR","children":[${a},${b}]}`, 0];
};

StsGen.forBlock['sts_logic_not'] = function (block) {
  const cond = conditionCode(block, 'COND');
  return [`{"type":"NOT","child":${cond}}`, 0];
};

// ─────────────────────── Control ───────────────────────

StsGen.forBlock['sts_if'] = function (block) {
  const cond = conditionCode(block, 'CONDITION');
  const thenBody = statementsToAction(block, 'THEN');
  const elseBody = statementsToAction(block, 'ELSE');
  let obj = `{"type":"If","condition":${cond},"then":${thenBody || 'null'}`;
  if (elseBody) obj += `,"else":${elseBody}`;
  obj += '}';
  return obj;
};

StsGen.forBlock['sts_foreach_enemy'] = function (block) {
  const body = statementsToAction(block, 'BODY');
  return `{"type":"ForEachEnemy","body":${body || 'null'}}`;
};

StsGen.forBlock['sts_repeat'] = function (block) {
  const count = block.getFieldValue('COUNT');
  const body = statementsToAction(block, 'BODY');
  return `{"type":"Repeat","count":${count},"body":${body || 'null'}}`;
};

// ─────────────────────── Actions ───────────────────────

StsGen.forBlock['sts_apply_power'] = function (block) {
  const powerId = block.getFieldValue('POWER_ID');
  const amount = block.getFieldValue('AMOUNT');
  const target = block.getFieldValue('TARGET');
  return `{"type":"ApplyPower","targetId":"${powerId}","amount":${amount},"target":"${target}"}`;
};

StsGen.forBlock['sts_add_card'] = function (block) {
  const cardId = block.getFieldValue('CARD_ID');
  return `{"type":"AddCard","targetId":"${cardId}","amount":1,"target":"Player"}`;
};

StsGen.forBlock['sts_use_potion'] = function (block) {
  const potionId = block.getFieldValue('POTION_ID');
  return `{"type":"UsePotion","targetId":"${potionId}","amount":1,"target":"Player"}`;
};

StsGen.forBlock['sts_save_slot'] = function (block) {
  const slot = block.getFieldValue('SLOT');
  return `{"type":"SaveSlot","targetId":"","amount":1,"target":"Player"}`;
};

// ─────────────────────── Variables ───────────────────────

StsGen.forBlock['sts_set_var'] = function (block) {
  const varName = block.getFieldValue('VAR_NAME');
  const value = block.getFieldValue('VALUE');
  return `{"type":"SetVar","varName":"${varName}","value":${value}}`;
};

StsGen.forBlock['sts_incr_var'] = function (block) {
  const varName = block.getFieldValue('VAR_NAME');
  const value = block.getFieldValue('VALUE');
  return `{"type":"IncrVar","varName":"${varName}","delta":${value}}`;
};
