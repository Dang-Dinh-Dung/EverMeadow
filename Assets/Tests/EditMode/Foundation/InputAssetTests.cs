using System.Collections.Generic;
using System.Linq;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Input.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Evermeadow.Tests.EditMode.Foundation
{
    /// <summary>Static checks of EvermeadowInput.inputactions against GDD Input Rule 2 and the rebind rules on its real slots.</summary>
    public class InputAssetTests
    {
        private const string AssetPath = "Assets/Game/Input/EvermeadowInput.inputactions";
        private const string Kb = "Keyboard&Mouse";
        private const string Gp = "Gamepad";

        private InputActionAsset _source;
        private UnityInputAdapter _adapter;
        private RebindController _rebind;
        private List<BindingSlot> _slots;
        private readonly List<int> _conflicts = new List<int>();

        [SetUp]
        public void SetUp()
        {
            _source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.IsNotNull(_source, AssetPath);
            var service = InputFixture.Create(new FakeClock());
            _adapter = new UnityInputAdapter(_source, service, new RecordingLogSink());
            _rebind = new RebindController(_adapter, service, null);
            _slots = _rebind.BuildSlots().ToList();
        }

        [TearDown]
        public void TearDown()
        {
            _rebind.Dispose();
            _adapter.Dispose();
        }

        // Fixture: GDD Input Rule 2 default keys per action and device group.
        private static readonly (InputActionId action, string group, string[] paths)[] Rule2 =
        {
            (InputActionId.Jump, Kb, new[] { "<Keyboard>/space" }), (InputActionId.Jump, Gp, new[] { "<Gamepad>/buttonSouth" }),
            (InputActionId.Attack, Kb, new[] { "<Keyboard>/j", "<Mouse>/leftButton" }), (InputActionId.Attack, Gp, new[] { "<Gamepad>/buttonWest" }),
            (InputActionId.Dodge, Kb, new[] { "<Keyboard>/leftShift", "<Keyboard>/k" }), (InputActionId.Dodge, Gp, new[] { "<Gamepad>/rightShoulder", "<Gamepad>/rightTrigger" }),
            (InputActionId.Plunge, Kb, new string[0]), (InputActionId.DropThrough, Kb, new string[0]),
            (InputActionId.Interact, Kb, new[] { "<Keyboard>/e" }), (InputActionId.Interact, Gp, new[] { "<Gamepad>/buttonNorth" }),
            (InputActionId.Heal, Kb, new[] { "<Keyboard>/q" }), (InputActionId.Heal, Gp, new[] { "<Gamepad>/leftShoulder" }),
            (InputActionId.EatBuff, Kb, new[] { "<Keyboard>/r" }), (InputActionId.EatBuff, Gp, new[] { "<Gamepad>/leftTrigger" }),
            (InputActionId.Run, Kb, new[] { "<Keyboard>/leftCtrl" }), (InputActionId.Run, Gp, new[] { "<Gamepad>/buttonEast" }),
            (InputActionId.Map, Kb, new[] { "<Keyboard>/m" }), (InputActionId.Map, Gp, new[] { "<Gamepad>/select" }),
            (InputActionId.Pause, Kb, new[] { "<Keyboard>/escape" }), (InputActionId.Pause, Gp, new[] { "<Gamepad>/start" }),
            (InputActionId.Confirm, Kb, new[] { "<Keyboard>/e", "<Keyboard>/enter", "<Keyboard>/space" }), (InputActionId.Confirm, Gp, new[] { "<Gamepad>/buttonSouth" }),
            (InputActionId.Cancel, Kb, new[] { "<Keyboard>/escape", "<Keyboard>/backspace" }), (InputActionId.Cancel, Gp, new[] { "<Gamepad>/buttonEast", "<Gamepad>/start" }),
            (InputActionId.TabLeft, Kb, new[] { "<Keyboard>/q" }), (InputActionId.TabLeft, Gp, new[] { "<Gamepad>/leftShoulder" }),
            (InputActionId.TabRight, Kb, new[] { "<Keyboard>/r" }), (InputActionId.TabRight, Gp, new[] { "<Gamepad>/rightShoulder" }),
            (InputActionId.Click, Kb, new[] { "<Mouse>/leftButton" }),
            (InputActionId.Reel, Kb, new[] { "<Keyboard>/space", "<Mouse>/leftButton", "<Keyboard>/e" }), (InputActionId.Reel, Gp, new[] { "<Gamepad>/buttonSouth", "<Gamepad>/buttonNorth" }),
        };

        private static string[] PathsOf(InputAction action, string group) =>
            action.bindings.Where(b => !b.isComposite && !b.isPartOfComposite && b.groups != null && b.groups.Contains(group) &&
                                       !string.IsNullOrEmpty(b.path)) // empty secondary slots are unbound
                .Select(b => b.path).ToArray();

        [Test, Description("AC-R02")]
        public void DefaultAsset_EveryActionMatchesRule2Table()
        {
            foreach (var row in Rule2)
            {
                InputAction a = _adapter.ActionFor(row.action);
                Assert.IsNotNull(a, $"{row.action} missing (GUID {ActionIds.For(row.action)})");
                CollectionAssert.AreEquivalent(row.paths, PathsOf(a, row.group), $"{row.action} / {row.group}");
            }
            Assert.IsNotNull(_adapter.Asset.FindAction(ActionIds.Move));
            Assert.IsNotNull(_adapter.Asset.FindAction(ActionIds.Navigate));
        }

        [Test, Description("AC-R02")]
        public void DefaultAsset_InteractNeverSharesKeyWithAttack()
        {
            foreach (string group in new[] { Kb, Gp })
            {
                var interact = PathsOf(_adapter.ActionFor(InputActionId.Interact), group);
                var attack = PathsOf(_adapter.ActionFor(InputActionId.Attack), group);
                CollectionAssert.IsEmpty(interact.Intersect(attack), group);
            }
        }

        [Test, Description("Rule 17: defaults obey the rules they enforce")]
        public void DefaultAsset_NoConflictingSlots()
        {
            var pairs = new List<(int a, int b)>();

            RebindRules.FindConflicts(_slots, pairs);

            Assert.IsEmpty(pairs, string.Join("; ", pairs.Select(p => $"{_slots[p.a].Key}={_slots[p.a].Path} vs {_slots[p.b].Key}")));
        }

        [Test, Description("ADR-0001 Decision 3–4: project settings use manual updates and no project-wide actions")]
        public void ProjectSettings_ManualUpdateAndNoProjectWideActions()
        {
            bool hasSettings = EditorBuildSettings.TryGetConfigObject("com.unity.input.settings", out InputSettings settings);
            bool hasProjectWide = EditorBuildSettings.TryGetConfigObject("com.unity.input.settings.actions", out InputActionAsset _);

            Assert.IsTrue(hasSettings, "Input Settings asset registered");
            Assert.AreEqual(InputSettings.UpdateMode.ProcessEventsManually, settings.updateMode);
            Assert.IsFalse(hasProjectWide, "project-wide actions removed");
        }

        [Test, Description("V8: WASD composite is DigitalNormalized (default mode)")]
        public void DefaultAsset_MoveCompositesUseDefaultMode()
        {
            InputAction move = _adapter.Asset.FindAction(ActionIds.Move);

            var heads = move.bindings.Where(b => b.isComposite).ToArray();

            CollectionAssert.AreEquivalent(new[] { "WASD", "Arrows" }, heads.Select(b => b.name));
            Assert.That(heads.Select(b => b.path), Is.All.EqualTo("2DVector"), "no mode parameter: default DigitalNormalized");
        }

        private int Slot(string key, string path) =>
            _slots.FindIndex(s => s.Key == key && s.Path == path);

        [Test, Description("AC-R36")]
        public void Rebind_JumpToJ_ConflictWithAttackThenSwapOrEnterFree()
        {
            int jumpSpace = Slot("Jump", "<Keyboard>/space");

            RebindOutcome toJ = RebindRules.Evaluate(_slots, jumpSpace, "<Keyboard>/j", _conflicts);
            var conflicted = _conflicts.Select(i => _slots[i].Action).ToArray();
            RebindOutcome swap = RebindRules.EvaluateSwap(_slots, jumpSpace, "<Keyboard>/j", _conflicts);
            RebindOutcome toEnter = RebindRules.Evaluate(_slots, jumpSpace, "<Keyboard>/enter", _conflicts);

            Assert.AreEqual(RebindOutcome.Conflict, toJ);
            CollectionAssert.AreEqual(new[] { InputActionId.Attack }, conflicted);
            Assert.AreEqual(RebindOutcome.Apply, swap);
            Assert.AreEqual(RebindOutcome.Apply, toEnter, "Enter is only used in Menu");
        }

        [Test, Description("AC-R37")]
        public void Rebind_FixedKeysMenuKeysAndPauseSecondary_AsSpecified()
        {
            int mapKey = Slot("Map", "<Keyboard>/m");
            int mapPad = Slot("Map", "<Gamepad>/select");
            int confirmPad = Slot("Confirm", "<Gamepad>/buttonSouth");
            int pauseEsc = Slot("Pause", "<Keyboard>/escape");

            Assert.AreEqual(RebindOutcome.RejectedFixedKey, RebindRules.Evaluate(_slots, mapKey, "<Keyboard>/escape", _conflicts));
            Assert.AreEqual(RebindOutcome.RejectedFixedKey, RebindRules.Evaluate(_slots, mapPad, "<Gamepad>/start", _conflicts));
            Assert.AreEqual(RebindOutcome.RejectedFixedSlot, RebindRules.Evaluate(_slots, pauseEsc, "<Keyboard>/p", _conflicts));
            Assert.AreEqual(RebindOutcome.RejectedMenuUnbound, RebindRules.Evaluate(_slots, confirmPad, string.Empty, _conflicts));

            // Even through Swap: the conflicting fixed Esc slot can never be handed over.
            Assert.AreEqual(RebindOutcome.RejectedFixedKey, RebindRules.EvaluateSwap(_slots, mapKey, "<Keyboard>/escape", new List<int> { pauseEsc }));
        }

        [Test, Description("AC-R37: a secondary key for Pause is accepted")]
        public void Rebind_AddSecondaryPauseKey_Applies()
        {
            var slots = new List<BindingSlot>(_slots)
            {
                new BindingSlot { Key = "Pause", Action = InputActionId.Pause, Modes = ActionCatalog.ModeMask(InputActionId.Pause), Device = DeviceKind.KeyboardMouse, Path = string.Empty }
            };

            RebindOutcome outcome = RebindRules.Evaluate(slots, slots.Count - 1, "<Keyboard>/p", _conflicts);

            Assert.AreEqual(RebindOutcome.Apply, outcome);
        }

        [Test, Description("Rule 17: Move parts are separate keys")]
        public void Rebind_MoveUpToS_ConflictsWithMoveDown()
        {
            int up = Slot("Move/up", "<Keyboard>/w");

            RebindOutcome outcome = RebindRules.Evaluate(_slots, up, "<Keyboard>/s", _conflicts);

            Assert.AreEqual(RebindOutcome.Conflict, outcome);
            Assert.That(_conflicts.Select(i => _slots[i].Key), Has.Member("Move/down"));
        }

        [Test, Description("Rule 17: swap that creates a new conflict is refused")]
        public void EvaluateSwap_SwappedSlotWouldConflict_Rejected()
        {
            // B (mode 1) and T (mode 2) share "y" legally; C (modes 1+2) holds "x".
            var slots = new List<BindingSlot>
            {
                new BindingSlot { Key = "B", Modes = 1, Path = "y" },
                new BindingSlot { Key = "T", Modes = 2, Path = "y" },
                new BindingSlot { Key = "C", Modes = 3, Path = "x" },
            };
            var conflicts = new List<int>();

            RebindOutcome first = RebindRules.Evaluate(slots, 1, "x", conflicts);
            RebindOutcome swap = RebindRules.EvaluateSwap(slots, 1, "x", conflicts); // C would get "y", clashing with B in mode 1

            Assert.AreEqual(RebindOutcome.Conflict, first);
            CollectionAssert.AreEqual(new[] { 2 }, conflicts);
            Assert.AreEqual(RebindOutcome.RejectedSwapConflict, swap);
        }

        private static int IndexOfPath(InputAction action, string path)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].path == path) return i;
            }
            return -1;
        }

        [Test, Description("Controls screen: every row has a primary and a secondary slot on each device it can rebind")]
        public void ControlsRows_TwoSlotsPerDevice()
        {
            var views = new List<RebindSlotView>();
            foreach (Evermeadow.Presentation.Menus.ControlsRow row in Evermeadow.Presentation.Menus.ControlsLayout.Rows)
            {
                if (row.IsHeader) continue;
                foreach (DeviceKind device in new[] { DeviceKind.KeyboardMouse, DeviceKind.Gamepad })
                {
                    if (device == DeviceKind.Gamepad && row.FixedGamepadText != null) continue;
                    if (row.Part != null) _rebind.GetAxisSlots(row.Axis, row.Part, device, views);
                    else _rebind.GetSlots(row.Action, device, views);
                    Assert.GreaterOrEqual(views.Count, 2, $"{row.Label} / {device}");
                }
            }
        }

        [Test, Description("Rule 17 load checks: saved keys that make two actions of a mode share a key → both back to defaults")]
        public void ApplyBindingOverrides_Conflict_ResetsBothActions()
        {
            InputAction jump = _adapter.ActionFor(InputActionId.Jump);
            int space = IndexOfPath(jump, "<Keyboard>/space");
            jump.ApplyBindingOverride(space, "<Keyboard>/j"); // J is Attack's key
            string saved = _rebind.ExportBindingOverrides();

            bool clean = _rebind.ApplyBindingOverrides(saved);

            Assert.IsFalse(clean);
            Assert.AreEqual("<Keyboard>/space", jump.bindings[space].effectivePath);
        }

        [Test, Description("Rule 17 load checks: unreadable data → every default and false")]
        public void ApplyBindingOverrides_Unreadable_DefaultsAndFalse()
        {
            InputAction jump = _adapter.ActionFor(InputActionId.Jump);
            int space = IndexOfPath(jump, "<Keyboard>/space");
            jump.ApplyBindingOverride(space, "<Keyboard>/u");

            bool clean = _rebind.ApplyBindingOverrides("{ this is not json");

            Assert.IsFalse(clean);
            Assert.AreEqual("<Keyboard>/space", jump.bindings[space].effectivePath);
        }

        [Test, Description("Saved keys that pass the checks are applied as they are")]
        public void ApplyBindingOverrides_Valid_Applied()
        {
            InputAction jump = _adapter.ActionFor(InputActionId.Jump);
            int space = IndexOfPath(jump, "<Keyboard>/space");
            jump.ApplyBindingOverride(space, "<Keyboard>/u");
            string saved = _rebind.ExportBindingOverrides();
            _rebind.ResetBindings();

            bool clean = _rebind.ApplyBindingOverrides(saved);

            Assert.IsTrue(clean);
            Assert.AreEqual("<Keyboard>/u", jump.bindings[space].effectivePath);
        }
    }
}
