/**
 * SpireScratch — custom Blockly block definitions for STS2.
 * Must be called AFTER I18n is initialized.
 */

function defineBlocks() {
  const t = I18n.t;

  Blockly.defineBlocksWithJsonArray([
    // ─────────────────────── Trigger ───────────────────────
    {
      type: 'sts_trigger',
      message0: t('block.trigger.when'),
      args0: [{
        type: 'field_dropdown',
        name: 'TRIGGER',
        options: [
          [t('trigger.combatStart'),   'CombatStart'],
          [t('trigger.combatEnd'),     'CombatEnd'],
          [t('trigger.turnStart'),     'TurnStart'],
          [t('trigger.turnEnd'),       'TurnEnd'],
          [t('trigger.onDraw'),        'OnDraw'],
          [t('trigger.onDamageDealt'), 'OnDamageDealt'],
          [t('trigger.onDamageTaken'), 'OnDamageTaken'],
          [t('trigger.onPotionUsed'),  'OnPotionUsed'],
          [t('trigger.onCardPlayed'),  'OnCardPlayed'],
          [t('trigger.onShuffle'),     'OnShuffle'],
        ],
      }],
      message1: t('block.trigger.do'),
      args1: [{ type: 'input_statement', name: 'DO' }],
      colour: 45,
      hat: 'cap',
      tooltip: t('block.trigger.tooltip'),
    },

    // ─────────────────────── Conditions ───────────────────────
    {
      type: 'sts_hp_condition',
      message0: t('block.hp.msg'),
      args0: [
        {
          type: 'field_dropdown', name: 'OP',
          options: [[t('block.hp.below'), 'HpBelow'], [t('block.hp.above'), 'HpAbove']],
        },
        { type: 'field_number', name: 'VALUE', value: 50, min: 0, max: 100 },
      ],
      output: 'Boolean', colour: 210, tooltip: t('block.hp.tooltip'),
    },
    {
      type: 'sts_floor_condition',
      message0: t('block.floor.msg'),
      args0: [
        {
          type: 'field_dropdown', name: 'OP',
          options: [[t('block.floor.above'), 'FloorAbove'], [t('block.floor.below'), 'FloorBelow']],
        },
        { type: 'field_number', name: 'VALUE', value: 5, min: 0 },
      ],
      output: 'Boolean', colour: 210, tooltip: t('block.floor.tooltip'),
    },
    {
      type: 'sts_power_condition',
      message0: t('block.power.msg'),
      args0: [
        {
          type: 'field_dropdown', name: 'OP',
          options: [[t('block.power.has'), 'HasPower'], [t('block.power.notHas'), 'NotHasPower']],
        },
        { type: 'field_input', name: 'POWER_ID', text: 'Strength' },
      ],
      output: 'Boolean', colour: 210, tooltip: t('block.power.tooltip'),
    },
    {
      type: 'sts_var_condition',
      message0: t('block.var.msg'),
      args0: [
        { type: 'field_input', name: 'VAR_NAME', text: 'counter' },
        {
          type: 'field_dropdown', name: 'OP',
          options: [['>', '>'], ['>=', '>='], ['<', '<'], ['<=', '<='], ['==', '=='], ['!=', '!=']],
        },
        { type: 'field_number', name: 'VALUE', value: 0 },
      ],
      output: 'Boolean', colour: 210, tooltip: t('block.var.tooltip'),
    },
    {
      type: 'sts_logic_and',
      message0: t('block.and.msg'),
      args0: [
        { type: 'input_value', name: 'A', check: 'Boolean' },
        { type: 'input_value', name: 'B', check: 'Boolean' },
      ],
      output: 'Boolean', colour: 210, tooltip: t('block.and.tooltip'),
    },
    {
      type: 'sts_logic_or',
      message0: t('block.or.msg'),
      args0: [
        { type: 'input_value', name: 'A', check: 'Boolean' },
        { type: 'input_value', name: 'B', check: 'Boolean' },
      ],
      output: 'Boolean', colour: 210, tooltip: t('block.or.tooltip'),
    },
    {
      type: 'sts_logic_not',
      message0: t('block.not.msg'),
      args0: [{ type: 'input_value', name: 'COND', check: 'Boolean' }],
      output: 'Boolean', colour: 210, tooltip: t('block.not.tooltip'),
    },

    // ─────────────────────── Control ───────────────────────
    {
      type: 'sts_if',
      message0: t('block.if.msg'),
      args0: [{ type: 'input_value', name: 'CONDITION', check: 'Boolean' }],
      message1: t('block.if.do'),
      args1: [{ type: 'input_statement', name: 'THEN' }],
      message2: t('block.if.else'),
      args2: [{ type: 'input_statement', name: 'ELSE' }],
      previousStatement: null, nextStatement: null, colour: 120,
      tooltip: t('block.if.tooltip'),
    },
    {
      type: 'sts_foreach_enemy',
      message0: t('block.foreach.msg'),
      args0: [{ type: 'input_statement', name: 'BODY' }],
      previousStatement: null, nextStatement: null, colour: 120,
      tooltip: t('block.foreach.tooltip'),
    },
    {
      type: 'sts_repeat',
      message0: t('block.repeat.msg'),
      args0: [
        { type: 'field_number', name: 'COUNT', value: 3, min: 1, max: 100 },
        { type: 'input_statement', name: 'BODY' },
      ],
      previousStatement: null, nextStatement: null, colour: 120,
      tooltip: t('block.repeat.tooltip'),
    },

    // ─────────────────────── Actions ───────────────────────
    {
      type: 'sts_apply_power',
      message0: t('block.applyPower.msg'),
      args0: [
        { type: 'field_input', name: 'POWER_ID', text: 'Strength' },
        { type: 'field_number', name: 'AMOUNT', value: 1, min: 1 },
        {
          type: 'field_dropdown', name: 'TARGET',
          options: [
            [t('target.player'), 'Player'],
            [t('target.allEnemies'), 'AllEnemies'],
            [t('target.allies'), 'Allies'],
          ],
        },
      ],
      previousStatement: null, nextStatement: null, colour: 330,
      tooltip: t('block.applyPower.tooltip'),
    },
    {
      type: 'sts_add_card',
      message0: t('block.addCard.msg'),
      args0: [{ type: 'field_input', name: 'CARD_ID', text: 'Strike' }],
      previousStatement: null, nextStatement: null, colour: 330,
      tooltip: t('block.addCard.tooltip'),
    },
    {
      type: 'sts_use_potion',
      message0: t('block.usePotion.msg'),
      args0: [{ type: 'field_input', name: 'POTION_ID', text: 'FirePotion' }],
      previousStatement: null, nextStatement: null, colour: 330,
      tooltip: t('block.usePotion.tooltip'),
    },
    {
      type: 'sts_save_slot',
      message0: t('block.saveSlot.msg'),
      args0: [{ type: 'field_number', name: 'SLOT', value: 0, min: 0, max: 99 }],
      previousStatement: null, nextStatement: null, colour: 330,
      tooltip: t('block.saveSlot.tooltip'),
    },

    // ─────────────────────── Variables ───────────────────────
    {
      type: 'sts_set_var',
      message0: t('block.setVar.msg'),
      args0: [
        { type: 'field_input', name: 'VAR_NAME', text: 'counter' },
        { type: 'field_number', name: 'VALUE', value: 0 },
      ],
      previousStatement: null, nextStatement: null, colour: 60,
      tooltip: t('block.setVar.tooltip'),
    },
    {
      type: 'sts_incr_var',
      message0: t('block.incrVar.msg'),
      args0: [
        { type: 'field_input', name: 'VAR_NAME', text: 'counter' },
        { type: 'field_number', name: 'VALUE', value: 1 },
      ],
      previousStatement: null, nextStatement: null, colour: 60,
      tooltip: t('block.incrVar.tooltip'),
    },
  ]);
}
