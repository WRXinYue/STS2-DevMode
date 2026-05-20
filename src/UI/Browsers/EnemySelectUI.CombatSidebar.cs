using System.Linq;
using DevMode.Actions;
using DevMode.Icons;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace DevMode.UI;

internal static partial class EnemySelectUI {
    internal const string CombatToolsContextId = "enemy.combatTools";

    private static CombatEnemySidebarPanel? _combatSidebar;
    private static DevPanelSidebarHost? _combatGameHost;

    internal static void EnsureGameContextPane(DevPanelSidebarHost host) {
        if (_combatGameHost == host && _combatSidebar != null)
            return;

        _combatGameHost = host;
        _combatSidebar = new CombatEnemySidebarPanel();
        DevPanelUI.RegisterContextProvider(CombatToolsContextId, _combatSidebar);
    }

    internal static void RefreshCombatContext() {
        _combatSidebar?.Refresh();
    }

    internal sealed partial class CombatEnemySidebarPanel : IDevPanelSidebarProvider {
        private const float IconBtnSize = 36f;

        private readonly VBoxContainer _root;
        private readonly VBoxContainer _actions;
        private readonly VBoxContainer _enemyList;
        private bool _hasContent;

        public CombatEnemySidebarPanel() {
            _root = new VBoxContainer {
                Name = "EnemyCombatToolsSidebar",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            _root.AddThemeConstantOverride("separation", 4);
            _root.Alignment = BoxContainer.AlignmentMode.Center;

            _actions = new VBoxContainer();
            _actions.AddThemeConstantOverride("separation", 4);
            _actions.Alignment = BoxContainer.AlignmentMode.Center;

            _enemyList = new VBoxContainer();
            _enemyList.AddThemeConstantOverride("separation", 4);
            _enemyList.Alignment = BoxContainer.AlignmentMode.Center;

            _root.AddChild(_actions);
            _root.AddChild(_enemyList);
        }

        public Control Root => _root;

        public string Title => I18N.T("enemy.combatSidebar.title", "Combat");

        public string Hint => I18N.T("enemy.combatSidebar.hint",
            "Add or remove enemies in the current fight.");

        public bool HasContent => _hasContent;

        public void Refresh() {
            ClearChildren(_actions);
            ClearChildren(_enemyList);

            if (!DevModeState.IsActive
                || CombatManager.Instance?.IsInProgress != true
                || CombatEnemyActions.GetCombatState() == null) {
                _hasContent = false;
                return;
            }

            _hasContent = true;

            _actions.AddChild(CreateIconButton(
                MdiIcon.Plus,
                I18N.T("enemy.combatSidebar.addEncounter", "Add encounter to combat"),
                () => OpenAddEncounter()));

            _actions.AddChild(CreateIconButton(
                MdiIcon.BugOutline,
                I18N.T("enemy.combatSidebar.addMonster", "Add monster to combat"),
                () => OpenAddMonster()));

            var enemies = CombatEnemyActions.GetCurrentEnemies().Where(e => !e.IsDead).ToList();
            foreach (var enemy in enemies) {
                string name = enemy.Monster?.Title?.GetFormattedText() ?? I18N.T("enemy.unknownName", "???");
                string hp = $"{enemy.CurrentHp}/{enemy.MaxHp}";
                var captured = enemy;
                _enemyList.AddChild(CreateIconButton(
                    MdiIcon.Skull,
                    I18N.T("enemy.combatSidebar.killOne", "Kill {0} ({1} HP)", name, hp),
                    () => {
                        TaskHelper.RunSafely(KillAndRefresh(captured));
                    },
                    tint: new Color(1f, 0.45f, 0.45f)));
            }

            if (enemies.Count > 0) {
                _enemyList.AddChild(CreateIconButton(
                    MdiIcon.Skull,
                    I18N.T("enemy.combatSidebar.killAll", "Kill all enemies"),
                    () => TaskHelper.RunSafely(KillAllAndRefresh()),
                    tint: new Color(1f, 0.3f, 0.3f)));
            }
        }

        private static async System.Threading.Tasks.Task KillAndRefresh(Creature enemy) {
            await CombatEnemyActions.KillEnemy(enemy);
            DevPanelUI.RefreshContextPane();
        }

        private static async System.Threading.Tasks.Task KillAllAndRefresh() {
            await CombatEnemyActions.KillAllEnemies();
            DevPanelUI.RefreshContextPane();
        }

        private static void OpenAddEncounter() {
            var globalUi = NRun.Instance?.GlobalUi;
            if (globalUi == null)
                return;

            ShowEncounterOverlay(globalUi, null, enc => {
                TaskHelper.RunSafely(SpawnEncounterAndRefresh(enc));
            });
        }

        private static async System.Threading.Tasks.Task SpawnEncounterAndRefresh(EncounterModel enc) {
            await CombatEnemyActions.AddEncounterMonsters(enc);
            DevPanelUI.RefreshContextPane();
        }

        private static void OpenAddMonster() {
            var globalUi = NRun.Instance?.GlobalUi;
            if (globalUi == null)
                return;

            ShowMonsterSpawnOverlay(globalUi, monster => {
                TaskHelper.RunSafely(SpawnMonsterAndRefresh(monster));
            });
        }

        private static async System.Threading.Tasks.Task SpawnMonsterAndRefresh(MonsterModel monster) {
            await CombatEnemyActions.AddMonster(monster);
            DevPanelUI.RefreshContextPane();
        }

        private static Button CreateIconButton(MdiIcon icon, string tooltip, System.Action onPressed, Color? tint = null) {
            var btn = new Button {
                CustomMinimumSize = new Vector2(IconBtnSize, IconBtnSize),
                FocusMode = Control.FocusModeEnum.None,
                MouseFilter = Control.MouseFilterEnum.Stop,
                TooltipText = tooltip,
                IconAlignment = HorizontalAlignment.Center,
                Icon = icon.Texture(18, tint ?? DevModeTheme.TextPrimary),
            };

            var flat = new StyleBoxFlat {
                BgColor = Colors.Transparent,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft = 4,
                ContentMarginRight = 4,
                ContentMarginTop = 4,
                ContentMarginBottom = 4,
            };
            btn.AddThemeStyleboxOverride("normal", flat);
            btn.AddThemeStyleboxOverride("hover", flat);
            btn.AddThemeStyleboxOverride("pressed", flat);
            btn.AddThemeStyleboxOverride("focus", flat);
            btn.Pressed += onPressed;
            return btn;
        }

        private static void ClearChildren(VBoxContainer host) {
            foreach (var child in host.GetChildren())
                ((Node)child).QueueFree();
        }
    }
}
