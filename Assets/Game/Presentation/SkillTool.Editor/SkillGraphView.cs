using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Evermeadow.Core.Skills;
using Evermeadow.Presentation.SpriteAnimation;
using Evermeadow.Presentation.Vfx;
using Evermeadow.Presentation.Vfx.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Evermeadow.Presentation.SkillTool.Editor
{
    /// <summary>Port type marker of flow (parent → child) ports.</summary>
    public sealed class ExecFlow
    {
    }

    /// <summary>Tag on a field input port.</summary>
    public sealed class FieldTag
    {
        /// <summary>Field definition.</summary>
        public SkillFieldDef Field;
    }

    /// <summary>Tag on an output port.</summary>
    public sealed class OutputTag
    {
        /// <summary>Output definition.</summary>
        public SkillOutputDef Output;
    }

    /// <summary>
    /// A flow edge (parent → child) that shows its trigger as a label at its middle, like the reference tool ("When the parent
    /// node starts"). Click the label (or right-click the edge) to pick another trigger the parent offers.
    /// </summary>
    public sealed class TriggerEdge : Edge
    {
        private const float LabelWidth = 150f, LabelHeight = 16f;
        private readonly Label _label;

        /// <summary>Raised when the trigger changes.</summary>
        public Action Changed;

        /// <summary>When the child starts.</summary>
        public SkillTrigger Trigger { get; private set; } = SkillTrigger.Start;

        /// <summary>Creates an edge with the default trigger (parent starts).</summary>
        public TriggerEdge()
        {
            _label = new Label(SkillToolNotes.Trigger(SkillTrigger.Start))
            {
                tooltip = SkillToolNotes.EdgeLabel,
                pickingMode = PickingMode.Position,
                style =
                {
                    position = Position.Absolute, width = LabelWidth, height = LabelHeight, fontSize = 10, unityTextAlign = TextAnchor.MiddleCenter,
                    color = new Color(0.92f, 0.92f, 0.92f), backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.75f),
                    borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3
                }
            };
            _label.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0) return;
                ShowTriggerMenu();
                e.StopPropagation();
            });
            Add(_label);
            RegisterCallback<GeometryChangedEvent>(_ => PlaceLabel());   // nodes laid out after the edge was routed
        }

        /// <summary>Sets the trigger and its label.</summary>
        public void SetTrigger(SkillTrigger t)
        {
            Trigger = t;
            _label.text = SkillToolNotes.Trigger(t);
        }

        /// <summary>Triggers the parent node offers.</summary>
        public SkillTrigger[] Allowed => (output?.node as SkillNodeView)?.Def.Triggers ?? new[] { SkillTrigger.Start };

        /// <summary>Opens the trigger menu.</summary>
        public void ShowTriggerMenu()
        {
            var menu = new GenericMenu();
            foreach (SkillTrigger t in Allowed)
            {
                SkillTrigger pick = t;
                menu.AddItem(new GUIContent(SkillToolNotes.Trigger(t)), t == Trigger, () =>
                {
                    SetTrigger(pick);
                    Changed?.Invoke();
                });
            }
            menu.ShowAsContext();
        }

        /// <inheritdoc />
        public override bool UpdateEdgeControl()
        {
            bool ok = base.UpdateEdgeControl();
            PlaceLabel();
            return ok;
        }

        // Centre the label on the wire: the middle of the two port centres (= the middle of the slanted segment), converted
        // from world space into this edge's space. The edge control's own layout can lag a frame behind, so it is not used.
        private void PlaceLabel()
        {
            Vector2 mid;
            if (output != null && input != null) mid = this.WorldToLocal(((Vector2)output.GetGlobalCenter() + (Vector2)input.GetGlobalCenter()) * 0.5f);
            else if (edgeControl != null) mid = (edgeControl.from + edgeControl.to) * 0.5f;   // being dragged: from / to are in edge space
            else return;
            if (float.IsNaN(mid.x) || float.IsNaN(mid.y)) return;
            _label.style.left = mid.x - LabelWidth / 2;
            _label.style.top = mid.y - LabelHeight / 2;
        }
    }

    /// <summary>
    /// Node canvas of the Skill Tool (GDD Skill &amp; Moveset Rule 5–6, user decision 2026-11-09, modelled on the reference
    /// tool): Start (id 0) and nodes built from <see cref="SkillSchema"/>, flow edges parent → child carrying a trigger, and
    /// data wires from outputs into fields. Drag nodes from the palette or right-click to add; Delete removes; Ctrl+C / Ctrl+V
    /// copy and paste; memos are yellow sticky notes. Positions are saved with the skill.
    /// </summary>
    public sealed class SkillGraphView : GraphView
    {
        /// <summary><c>DragAndDrop</c> generic-data key carrying a <see cref="SkillNodeKind"/> from the node palette.</summary>
        public const string DragKey = "Evermeadow.SkillTool.NodeKind";

        private const string ClipPrefix = "EVERMEADOW_SKILL_CLIPBOARD\n";
        private bool _loading;
        private string _skillId = "";

        /// <summary>Raised on any edit (node, edge, field, memo, move).</summary>
        public event Action Changed;

        /// <summary>The Start node.</summary>
        public SkillNodeView StartNode { get; private set; }

        /// <summary>Creates an empty canvas.</summary>
        public SkillGraphView()
        {
            SetupZoom(0.2f, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            style.flexGrow = 1;

            graphViewChanged = OnGraphViewChanged;
            serializeGraphElements = Serialize;
            canPasteSerializedData = data => data != null && data.StartsWith(ClipPrefix, StringComparison.Ordinal);
            unserializeAndPaste = (op, data) => Paste(data);

            RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (!(DragAndDrop.GetGenericData(DragKey) is SkillNodeKind)) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.StopPropagation();
            });
            RegisterCallback<DragPerformEvent>(e =>
            {
                if (!(DragAndDrop.GetGenericData(DragKey) is SkillNodeKind kind)) return;
                DragAndDrop.AcceptDrag();
                DragAndDrop.SetGenericData(DragKey, null);
                AddNodeAtWorld(kind, e.mousePosition);
                e.StopPropagation();
            });
        }

        // ------------------------------------------------------------------ load / export

        /// <summary>Replaces the canvas content with <paramref name="g"/>.</summary>
        public void Load(SkillGraphData g, bool frame)
        {
            _loading = true;
            try
            {
                DeleteElements(graphElements.ToList());
                _skillId = g.Id;
                StartNode = null;
                var views = new Dictionary<int, SkillNodeView>();
                foreach (SkillNodeData n in g.Nodes.OrderBy(n => n.Id))
                {
                    if (views.ContainsKey(n.Id)) continue;
                    SkillNodeView v = CreateView(n.Clone(), g.Owner);
                    views[n.Id] = v;
                    if (n.Kind == SkillNodeKind.Start && StartNode == null) StartNode = v;
                }
                if (StartNode == null)
                {
                    StartNode = CreateView(new SkillNodeData { Id = 0, Kind = SkillNodeKind.Start }, g.Owner);
                    views[0] = StartNode;
                }
                foreach (SkillLink l in g.Links)
                    if (views.TryGetValue(l.From, out SkillNodeView from) && views.TryGetValue(l.To, out SkillNodeView to) && to.FlowIn != null)
                        ConnectFlow(from, to, l.When);
                foreach (SkillNodeView v in views.Values) WireRefs(v, views);
                foreach (SkillMemo m in g.Memos) AddMemo(new Rect(m.X, m.Y, m.W, m.H), m.Title, m.Text);
                RefreshWires();
            }
            finally
            {
                _loading = false;
            }
            if (frame) schedule.Execute(() => FrameAll()).StartingIn(50);
        }

        /// <summary>The canvas as a graph (not validated).</summary>
        public SkillGraphData Export()
        {
            var g = new SkillGraphData { Id = _skillId, Owner = StartNode?.Owner ?? SkillOwner.Player };
            foreach (SkillNodeView v in nodes.OfType<SkillNodeView>().OrderBy(n => n.Id)) g.Nodes.Add(v.ToData());
            foreach (TriggerEdge e in edges.OfType<TriggerEdge>())
                if (e.output?.node is SkillNodeView from && e.input?.node is SkillNodeView to)
                    g.Links.Add(new SkillLink { From = from.Id, To = to.Id, When = e.Trigger });
            foreach (StickyNote s in graphElements.OfType<StickyNote>())
            {
                Rect r = s.GetPosition();
                g.Memos.Add(new SkillMemo { X = r.x, Y = r.y, W = r.width, H = r.height, Title = s.title ?? "", Text = s.contents ?? "" });
            }
            return g;
        }

        // ------------------------------------------------------------------ editing

        /// <summary>Adds a node of <paramref name="kind"/> at a panel (world) position, e.g. where it was dropped.</summary>
        public void AddNodeAtWorld(SkillNodeKind kind, Vector2 worldPosition)
        {
            if (StartNode == null) return;
            Vector2 p = contentViewContainer.WorldToLocal(worldPosition);
            SkillNodeView v = CreateView(new SkillNodeData { Id = NextId(), Kind = kind, X = p.x, Y = p.y }, StartNode.Owner);
            ClearSelection();
            AddToSelection(v);
            RaiseChanged();
        }

        /// <summary>Adds a node of <paramref name="kind"/> at the middle of the visible canvas.</summary>
        public void AddNodeAtCenter(SkillNodeKind kind) => AddNodeAtWorld(kind, worldBound.center);

        /// <summary>Adds a memo at the middle of the visible canvas.</summary>
        public void AddMemoAtCenter()
        {
            Vector2 p = contentViewContainer.WorldToLocal(worldBound.center);
            AddMemo(new Rect(p.x - 150, p.y - 60, 300, 120), "Memo", "");
            RaiseChanged();
        }

        /// <summary>Copies the selection to the system clipboard.</summary>
        public void CopySelection()
        {
            List<GraphElement> picked = selection.OfType<GraphElement>().ToList();
            if (picked.Count > 0) EditorGUIUtility.systemCopyBuffer = Serialize(picked);
        }

        /// <summary>Pastes skill nodes from the system clipboard.</summary>
        public void PasteClipboard()
        {
            string data = EditorGUIUtility.systemCopyBuffer;
            if (canPasteSerializedData(data)) Paste(data);
        }

        /// <inheritdoc />
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            bool flow = startPort.portType == typeof(ExecFlow);
            return ports.ToList().Where(p =>
                p.direction != startPort.direction && p.node != startPort.node &&
                (flow ? p.portType == typeof(ExecFlow) : p.portType != typeof(ExecFlow) && DataCompatible(startPort, p))).ToList();
        }

        /// <inheritdoc />
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is TriggerEdge edge)
            {
                foreach (SkillTrigger t in edge.Allowed)
                {
                    SkillTrigger pick = t;
                    evt.menu.AppendAction("Condition/" + SkillToolNotes.Trigger(t), _ =>
                    {
                        edge.SetTrigger(pick);
                        RaiseChanged();
                    }, _ => edge.Trigger == pick ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                }
                evt.menu.AppendSeparator();
            }
            Vector2 at = contentViewContainer.WorldToLocal(evt.mousePosition);
            foreach (SkillNodeKind kind in SkillSchema.Kinds)
            {
                SkillNodeKind k = kind;
                evt.menu.AppendAction("Add Node/" + k, _ => AddNodeAtWorld(k, evt.mousePosition));
            }
            evt.menu.AppendAction("Add Memo", _ =>
            {
                AddMemo(new Rect(at.x, at.y, 300, 120), "Memo", "");
                RaiseChanged();
            });
            evt.menu.AppendSeparator();
            base.BuildContextualMenu(evt);
        }

        // ------------------------------------------------------------------ internals

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (!_loading && change.elementsToRemove != null) change.elementsToRemove.RemoveAll(e => e == StartNode);
            if (change.edgesToCreate != null)
                foreach (Edge e in change.edgesToCreate)
                {
                    if (!(e is TriggerEdge t)) continue;
                    t.Changed = RaiseChanged;
                    // A new wire into End defaults to "when the parent ends": "when it starts" almost always cuts the attack
                    // animation before it shows (End returns the owner to idle).
                    if (!_loading && t.input?.node is SkillNodeView to && to.Kind == SkillNodeKind.End &&
                        t.output?.node is SkillNodeView from && from.Def.Allows(SkillTrigger.End))
                        t.SetTrigger(SkillTrigger.End);
                }
            schedule.Execute(RefreshWires);
            RaiseChanged();
            return change;
        }

        private SkillNodeView CreateView(SkillNodeData data, SkillOwner owner)
        {
            var v = new SkillNodeView(data, owner, RaiseChanged);
            v.SetPosition(new Rect(data.X, data.Y, 0, 0));
            AddElement(v);
            return v;
        }

        private void ConnectFlow(SkillNodeView from, SkillNodeView to, SkillTrigger when)
        {
            var e = new TriggerEdge { output = from.FlowOut, input = to.FlowIn };
            e.SetTrigger(from.Def.Allows(when) ? when : SkillTrigger.Start);
            e.Changed = RaiseChanged;
            from.FlowOut.Connect(e);
            to.FlowIn.Connect(e);
            AddElement(e);
        }

        private void WireRefs(SkillNodeView v, Dictionary<int, SkillNodeView> views)
        {
            foreach (KeyValuePair<string, string> r in v.PendingRefs)
            {
                if (!SkillGraphData.TrySplitRef(r.Value, out int srcId, out string output) || !views.TryGetValue(srcId, out SkillNodeView src)) continue;
                Port o = src.OutputPort(output), i = v.FieldPort(r.Key);
                if (o == null || i == null) continue;
                var e = new Edge { output = o, input = i };
                o.Connect(e);
                i.Connect(e);
                AddElement(e);
            }
        }

        private void AddMemo(Rect r, string title, string text)
        {
            var note = new StickyNote(r.position) { title = title ?? "", contents = text ?? "" };
            note.SetPosition(r);
            note.RegisterCallback<StickyNoteChangeEvent>(_ => RaiseChanged());
            AddElement(note);
        }

        private static bool DataCompatible(Port a, Port b)
        {
            Port output = a.direction == Direction.Output ? a : b;
            Port input = a.direction == Direction.Output ? b : a;
            return output.userData is OutputTag o && input.userData is FieldTag f &&
                   SkillSchema.CanAssign(o.Output.Type, o.Output.EnumType, f.Field.Type, f.Field.EnumType);
        }

        private int NextId()
        {
            int max = 0;
            foreach (SkillNodeView v in nodes.OfType<SkillNodeView>()) max = Math.Max(max, v.Id);
            return max + 1;
        }

        private void RefreshWires()
        {
            foreach (SkillNodeView v in nodes.OfType<SkillNodeView>()) v.RefreshWires();
        }

        private void RaiseChanged()
        {
            if (!_loading) Changed?.Invoke();
        }

        // Copy: selected nodes (never Start), the links and wires among them, and selected memos.
        private string Serialize(IEnumerable<GraphElement> elements)
        {
            List<GraphElement> list = elements.ToList();
            var picked = new HashSet<SkillNodeView>(list.OfType<SkillNodeView>().Where(v => v.Kind != SkillNodeKind.Start));
            var g = new SkillGraphData { Id = "100000" };
            foreach (SkillNodeView v in picked) g.Nodes.Add(v.ToData());
            foreach (TriggerEdge e in edges.OfType<TriggerEdge>())
                if (e.output?.node is SkillNodeView from && e.input?.node is SkillNodeView to && picked.Contains(from) && picked.Contains(to))
                    g.Links.Add(new SkillLink { From = from.Id, To = to.Id, When = e.Trigger });
            foreach (StickyNote s in list.OfType<StickyNote>())
            {
                Rect r = s.GetPosition();
                g.Memos.Add(new SkillMemo { X = r.x, Y = r.y, W = r.width, H = r.height, Title = s.title ?? "", Text = s.contents ?? "" });
            }
            return ClipPrefix + SkillXmlWriter.Write(g);
        }

        // Paste: new ids, +40 px offset; wires to copied nodes follow the copies, wires to nodes still on the canvas stay.
        private void Paste(string data)
        {
            if (StartNode == null || !canPasteSerializedData(data)) return;
            if (!SkillXmlReader.TryParseData(data.Substring(ClipPrefix.Length), "100000", out SkillGraphData clip, new List<SkillError>())) return;
            var map = new Dictionary<int, int>();
            int next = NextId();
            foreach (SkillNodeData n in clip.Nodes.Where(n => n.Kind != SkillNodeKind.Start)) map[n.Id] = next++;

            var existing = nodes.OfType<SkillNodeView>().ToDictionary(v => v.Id);
            var created = new Dictionary<int, SkillNodeView>();
            ClearSelection();
            foreach (SkillNodeData n in clip.Nodes.Where(n => map.ContainsKey(n.Id)))
            {
                SkillNodeData copy = n.Clone();
                copy.Id = map[n.Id];
                copy.X += 40;
                copy.Y += 40;
                copy.Refs.Clear();
                foreach (KeyValuePair<string, string> r in n.Refs)
                {
                    if (!SkillGraphData.TrySplitRef(r.Value, out int src, out string output)) continue;
                    if (map.TryGetValue(src, out int moved)) copy.Refs[r.Key] = moved + "." + output;
                    else if (existing.ContainsKey(src)) copy.Refs[r.Key] = r.Value;
                }
                SkillNodeView v = CreateView(copy, StartNode.Owner);
                created[copy.Id] = v;
                AddToSelection(v);
            }
            foreach (SkillLink l in clip.Links)
                if (map.TryGetValue(l.From, out int f) && map.TryGetValue(l.To, out int t))
                    ConnectFlow(created[f], created[t], l.When);
            var all = nodes.OfType<SkillNodeView>().ToDictionary(v => v.Id);
            foreach (SkillNodeView v in created.Values) WireRefs(v, all);
            foreach (SkillMemo m in clip.Memos) AddMemo(new Rect(m.X + 40, m.Y + 40, m.W, m.H), m.Title, m.Text);
            RefreshWires();
            RaiseChanged();
        }
    }

    /// <summary>
    /// One node built from its <see cref="SkillNodeDef"/>: flow ports in the title bar, a row per field (input port, name,
    /// editor; a wired field shows its source instead of the editor), outputs at the bottom, and an area preview for Search
    /// and Hit.
    /// </summary>
    public sealed class SkillNodeView : Node
    {
        private readonly SkillNodeData _data;
        private readonly Action _changed;
        private readonly Dictionary<string, Port> _fieldPorts = new Dictionary<string, Port>();
        private readonly Dictionary<string, VisualElement> _editors = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, VisualElement> _rows = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, string> _baseTips = new Dictionary<string, string>();
        private readonly Dictionary<string, Label> _wired = new Dictionary<string, Label>();
        private readonly Dictionary<string, Port> _outputs = new Dictionary<string, Port>();
        private readonly VisualElement _preview;
        private SkillOwner _owner;

        /// <summary>Node id.</summary>
        public int Id => _data.Id;
        /// <summary>Kind.</summary>
        public SkillNodeKind Kind => _data.Kind;
        /// <summary>Definition.</summary>
        public SkillNodeDef Def => _data.Def;
        /// <summary>Owner chosen on the Start node.</summary>
        public SkillOwner Owner => _owner;
        /// <summary>Flow input (null on Start).</summary>
        public Port FlowIn { get; }
        /// <summary>Flow output.</summary>
        public Port FlowOut { get; }
        /// <summary>Wires read from the file, connected by the graph after every node exists.</summary>
        public IReadOnlyDictionary<string, string> PendingRefs => _data.Refs;

        /// <summary>Creates the view of <paramref name="data"/> (edited in place).</summary>
        public SkillNodeView(SkillNodeData data, SkillOwner owner, Action changed)
        {
            _data = data;
            _owner = owner;
            _changed = changed;
            title = $"{data.Id}:{data.Kind}";
            titleContainer.style.backgroundColor = ColorOf(data.Kind);
            titleContainer.tooltip = SkillToolNotes.Node(data.Kind);
            extensionContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.96f);
            extensionContainer.style.paddingLeft = 2;
            extensionContainer.style.paddingRight = 4;
            extensionContainer.style.paddingBottom = 4;
            style.minWidth = 300;

            if (data.Kind == SkillNodeKind.Start)
            {
                capabilities &= ~Capabilities.Deletable;
                capabilities &= ~Capabilities.Copiable;
            }
            else
            {
                FlowIn = Port.Create<TriggerEdge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(ExecFlow));
                FlowIn.portName = "";
                FlowIn.tooltip = "Luồng vào: nối từ node cha (hoặc Start). Mỗi node có một node cha.";
                titleContainer.Insert(0, FlowIn);
            }
            FlowOut = Port.Create<TriggerEdge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(ExecFlow));
            FlowOut.portName = "";
            FlowOut.tooltip = "Luồng ra: kéo sang các node con. Một node có thể có nhiều node con; điều kiện ghi trên từng dây.";
            titleContainer.Add(FlowOut);
            if (data.Kind == SkillNodeKind.End) FlowOut.style.display = DisplayStyle.None;   // End has no children

            if (data.Kind == SkillNodeKind.Start)
            {
                var ownerField = new EnumField("Owner", _owner) { tooltip = SkillToolNotes.Owner };
                ownerField.labelElement.style.minWidth = 80;
                ownerField.RegisterValueChangedCallback(e =>
                {
                    _owner = (SkillOwner)e.newValue;
                    _changed?.Invoke();
                });
                extensionContainer.Add(ownerField);
            }

            string group = null;
            foreach (SkillFieldDef f in Def.Fields)
            {
                if (f.Group != group)
                {
                    group = f.Group;
                    if (!string.IsNullOrEmpty(group))
                        extensionContainer.Add(new Label(group) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 4, marginLeft = 4, color = new Color(0.75f, 0.75f, 0.75f) } });
                }
                extensionContainer.Add(FieldRow(f));
            }

            if (data.Kind == SkillNodeKind.Search || data.Kind == SkillNodeKind.Hit)
            {
                _preview = new VisualElement { style = { height = 150, marginTop = 4, backgroundColor = new Color(0.12f, 0.12f, 0.12f) } };
                _preview.generateVisualContent += DrawArea;
                _previewCaption = new Label { style = { whiteSpace = WhiteSpace.Normal, fontSize = 10, marginLeft = 4, marginBottom = 2 } };
                _preview.tooltip = "Xem trước vùng giống hệt lúc chạy (Show Hit Area): nhìn ngang, nhân vật hướng phải, đơn vị ô thật.";
                extensionContainer.Add(_preview);
                extensionContainer.Add(_previewCaption);
                UpdatePreviewCaption();
            }

            foreach (SkillOutputDef o in Def.Outputs) extensionContainer.Add(OutputRow(o));
            RefreshActive();
            RefreshPorts();
            expanded = true;
            RefreshExpandedState();
        }

        /// <summary>Input port of a field, or null.</summary>
        public Port FieldPort(string field) => _fieldPorts.TryGetValue(field, out Port p) ? p : null;

        /// <summary>Output port by output name, or null.</summary>
        public Port OutputPort(string output) => _outputs.TryGetValue(output, out Port p) ? p : null;

        /// <summary>The node as data: values, wires from the connected ports, and its position.</summary>
        public SkillNodeData ToData()
        {
            SkillNodeData d = _data.Clone();
            Rect r = GetPosition();
            d.X = r.x;
            d.Y = r.y;
            d.Refs.Clear();
            foreach (KeyValuePair<string, Port> fp in _fieldPorts)
            {
                Edge e = fp.Value.connections.FirstOrDefault();
                if (e?.output?.node is SkillNodeView src && e.output.userData is OutputTag o) d.Refs[fp.Key] = src.Id + "." + o.Output.Name;
            }
            return d;
        }

        /// <summary>Shows the source of wired fields instead of their editor.</summary>
        public void RefreshWires()
        {
            foreach (KeyValuePair<string, Port> fp in _fieldPorts)
            {
                Edge e = fp.Value.connections.FirstOrDefault();
                bool wired = e?.output?.node is SkillNodeView && e.output.userData is OutputTag;
                _editors[fp.Key].style.display = wired ? DisplayStyle.None : DisplayStyle.Flex;
                _wired[fp.Key].style.display = wired ? DisplayStyle.Flex : DisplayStyle.None;
                if (wired) _wired[fp.Key].text = ((SkillNodeView)e.output.node).Id + "." + ((OutputTag)e.output.userData).Output.Name;
            }
        }

        private VisualElement FieldRow(SkillFieldDef f)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, minHeight = 20 } };
            Port p = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, PortType(f.Type));
            p.portName = f.Name;
            p.portColor = ColorOf(f.Type);
            p.userData = new FieldTag { Field = f };
            p.tooltip = SkillToolNotes.Field(Kind, f.Name) + $"\n[{f.Type}{(f.EnumType != null ? ": " + f.EnumType.Name : "")}, mặc định {f.Default}]";
            p.style.minWidth = 190;
            row.Add(p);
            _fieldPorts[f.Name] = p;

            VisualElement editor = Editor(f);
            editor.style.flexGrow = 1;
            editor.style.minWidth = 110;
            editor.tooltip = p.tooltip;
            row.Add(editor);
            _editors[f.Name] = editor;

            var wired = new Label { style = { display = DisplayStyle.None, flexGrow = 1, color = new Color(0.55f, 0.85f, 1f), unityFontStyleAndWeight = FontStyle.Italic } };
            row.Add(wired);
            _wired[f.Name] = wired;
            _rows[f.Name] = row;
            _baseTips[f.Name] = p.tooltip;
            return row;
        }

        private VisualElement OutputRow(SkillOutputDef o)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, minHeight = 18 } };
            Port p = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, PortType(o.Type));
            p.portName = o.Name;
            p.portColor = ColorOf(o.Type);
            p.userData = new OutputTag { Output = o };
            p.tooltip = SkillToolNotes.Field(Kind, o.Name) + $"\n[{o.Type}] — nối dây sang trường của node con / cháu.";
            row.Add(p);
            _outputs[o.Name] = p;
            return row;
        }

        private VisualElement Editor(SkillFieldDef f)
        {
            string v = _data.Get(f.Name);
            if (IsVfxField(Kind, f.Name)) return VfxKeyEditor(f, v);
            if (Kind == SkillNodeKind.Animation && f.Name == "AnimationType") return AnimationSlotEditor(f, v);
            switch (f.Type)
            {
                case SkillFieldType.Float:
                {
                    var x = new FloatField { value = (float)ParseDouble(v), isDelayed = true };
                    x.RegisterValueChangedCallback(e => Set(f, SkillSchema.Num(e.newValue)));
                    return x;
                }
                case SkillFieldType.Bool:
                {
                    var x = new Toggle { value = v == "true" };
                    x.RegisterValueChangedCallback(e => Set(f, e.newValue ? "true" : "false"));
                    return x;
                }
                case SkillFieldType.Int:
                {
                    int.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int i);
                    var x = new IntegerField { value = i, isDelayed = true };
                    x.RegisterValueChangedCallback(e => Set(f, e.newValue.ToString(CultureInfo.InvariantCulture)));
                    return x;
                }
                case SkillFieldType.Long:
                {
                    long.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long l);
                    var x = new LongField { value = l, isDelayed = true };
                    x.RegisterValueChangedCallback(e => Set(f, e.newValue.ToString(CultureInfo.InvariantCulture)));
                    return x;
                }
                case SkillFieldType.Vec3:
                {
                    var x = new Vector3Field { value = ParseVec3(v) };
                    x.RegisterValueChangedCallback(e => Set(f, SkillSchema.Num(e.newValue.x) + "," + SkillSchema.Num(e.newValue.y) + "," + SkillSchema.Num(e.newValue.z)));
                    if (f.Name == "SubValue")
                    {
                        _subValue = x;
                        UpdateSubValueLabels();
                    }
                    return x;
                }
                case SkillFieldType.Enum:
                {
                    Enum value = Enum.IsDefined(f.EnumType, v) ? (Enum)Enum.Parse(f.EnumType, v) : (Enum)Enum.GetValues(f.EnumType).GetValue(0);
                    var x = new EnumField(value);
                    x.RegisterValueChangedCallback(e => Set(f, e.newValue.ToString()));
                    return x;
                }
                case SkillFieldType.Flags:
                {
                    long bits = 0;
                    if (SkillSchema.TryFlags(f.EnumType, v, out string canonical))
                        foreach (string part in canonical.Split('|'))
                            bits |= Convert.ToInt64(Enum.Parse(f.EnumType, part));
                    var x = new EnumFlagsField((Enum)Enum.ToObject(f.EnumType, bits));
                    x.RegisterValueChangedCallback(e => Set(f, SkillSchema.FlagsText(f.EnumType, Convert.ToInt64(e.newValue))));
                    return x;
                }
                default:
                {
                    var x = new TextField { value = v, isDelayed = true };
                    x.RegisterValueChangedCallback(e => Set(f, e.newValue ?? ""));
                    return x;
                }
            }
        }

        /// <summary>True for the string fields that hold an effect key (picked from the VFX browser).</summary>
        public static bool IsVfxField(SkillNodeKind kind, string field) =>
            (kind == SkillNodeKind.Effect && field == "PrefabName") || (kind == SkillNodeKind.Hit && field == "HitEffectAddressable");

        // Effect key: a button showing the key; clicking opens the effect browser (list + preview). Orange when the key is unknown.
        // Animation slot: a button showing the slot; clicking lists the skill slots of the shared animation slot list (user
        // decision 2026-11-14), automatic slots in a sub-menu. Orange when the value is not in the list.
        private VisualElement AnimationSlotEditor(SkillFieldDef f, string v)
        {
            var button = new Button { style = { unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 2, marginRight = 2 } };
            void Show(string slot)
            {
                bool empty = string.IsNullOrEmpty(slot);
                bool known = !empty && SkillToolFiles.AnimationSlots().Contains(slot);
                button.text = (empty ? "(chọn animation…)" : slot) + "  ▾";
                button.style.color = empty ? new Color(0.6f, 0.6f, 0.6f) : known ? new Color(0.9f, 0.9f, 0.9f) : new Color(1f, 0.6f, 0.2f);
                button.tooltip = SkillToolNotes.Field(Kind, f.Name) + (empty || known ? "" : "\n⚠ '" + slot + "' không có trong " + AnimationSlots.FilePath + ".");
            }
            Show(v);
            button.clicked += () =>
            {
                List<string> slots = SkillToolFiles.AnimationSlots();
                string current = _data.Get(f.Name);
                var menu = new GenericMenu();
                foreach (string slot in AnimationSlots.SkillSlots(slots))
                {
                    string pick = slot;
                    menu.AddItem(new GUIContent(Group(slot) + slot), slot == current, () => Pick(pick));
                }
                menu.AddSeparator("");
                foreach (string slot in slots.Where(AnimationSlots.IsAutomatic))
                {
                    string pick = slot;
                    menu.AddItem(new GUIContent("Tự phát (di chuyển / phản ứng)/" + slot), slot == current, () => Pick(pick));
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("(không phát animation)"), current.Length == 0, () => Pick(""));
                menu.AddItem(new GUIContent("Sửa danh sách slot…"), false, () => SkillToolFiles.EditAnimationSlots());
                menu.ShowAsContext();
            };
            return button;

            void Pick(string slot)
            {
                if (button.panel == null) return;
                Set(f, slot);
                Show(slot);
            }
        }

        // attack1 → "attack/", skill3 → "skill/", idle2 → "idle/": groups the menu by the letters before the number.
        private static string Group(string slot)
        {
            int i = slot.Length;
            while (i > 0 && char.IsDigit(slot[i - 1])) i--;
            return i > 0 && i < slot.Length ? slot.Substring(0, i) + "/" : "";
        }

        private VisualElement VfxKeyEditor(SkillFieldDef f, string v)
        {
            var button = new Button { style = { unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 2, marginRight = 2 } };
            void Show(string key)
            {
                bool empty = string.IsNullOrEmpty(key);
                bool known = !empty && VfxCatalogBuilder.Exists(key);
                button.text = (empty ? "(chọn effect…)" : key) + "  ▾";
                button.style.color = empty ? new Color(0.6f, 0.6f, 0.6f) : known ? new Color(0.9f, 0.9f, 0.9f) : new Color(1f, 0.6f, 0.2f);
                button.tooltip = SkillToolNotes.Field(Kind, f.Name) + (empty || known ? "" : "\n⚠ Không tìm thấy prefab '" + key + "' trong " + VfxNaming.Root + ".");
            }
            Show(v);
            button.clicked += () =>
            {
                VfxCategory? hint = Kind == SkillNodeKind.Hit ? VfxCategory.Hit : (VfxCategory?)null;
                VfxPickerWindow.Pick(_data.Get(f.Name), hint, key =>
                {
                    if (button.panel == null) return;   // node was removed / graph reloaded meanwhile
                    Set(f, key ?? "");
                    Show(key);
                });
            };
            return button;
        }

        private Vector3Field _subValue;
        private Label _previewCaption;

        // SubValue's three numbers mean different things per area: name them (Arc: angle, radius; Box: width, length, height).
        private void UpdateSubValueLabels()
        {
            if (_subValue == null) return;
            bool box = _data.Get("AreaType") == nameof(SkillAreaType.Box);
            string[] names = box ? new[] { "W", "L", "H" } : new[] { "∠", "R", "–" };
            string[] tips = box
                ? new[] { "W — rộng (ô); dùng làm chiều cao khi H = 0", "L — dài (ô), theo hướng mặt", "H — cao (ô)" }
                : new[] { "∠ — góc mở (độ), 360 = tròn", "R — bán kính (ô), phải > 0 thì mới có vùng", "Không dùng cho Arc" };
            string[] ids = { "unity-x-input", "unity-y-input", "unity-z-input" };
            for (int i = 0; i < 3; i++)
            {
                FloatField part = _subValue.Q<FloatField>(ids[i]);
                if (part == null) continue;
                part.label = names[i];
                part.tooltip = tips[i];
                part.labelElement.style.minWidth = 12;
                part.labelElement.style.width = StyleKeyword.Auto;
            }
        }

        private void Set(SkillFieldDef f, string value)
        {
            if (value == f.Default) _data.Values.Remove(f.Name);
            else _data.Values[f.Name] = value;
            if (f.Name == "AreaType" || f.Name == "SubValue" || f.Name == "AreaScale" || f.Name == "PivotOffset")
            {
                _preview?.MarkDirtyRepaint();
                UpdatePreviewCaption();
            }
            if (f.Name == "AreaType") UpdateSubValueLabels();
            RefreshActive();
            _changed?.Invoke();
        }

        // Fields that would be ignored (IsSteady off → Steady fields, PivotType = Creature → PivotPosition…; SkillFieldRules) are
        // faded and their editor locked, so nobody types a value that does nothing. The field name keeps its tooltip, which
        // starts with the reason (a disabled element shows no tooltip). Stored values are kept.
        private void RefreshActive()
        {
            foreach (KeyValuePair<string, VisualElement> kv in _rows)
            {
                bool active = SkillFieldRules.IsActive(_data, kv.Key, out string why);
                kv.Value.style.opacity = active ? 1f : 0.4f;
                if (_editors.TryGetValue(kv.Key, out VisualElement editor)) editor.SetEnabled(active);
                if (_fieldPorts.TryGetValue(kv.Key, out Port port))
                    port.tooltip = active ? _baseTips[kv.Key] : $"⛔ Không có tác dụng: {why}.\n\n{_baseTips[kv.Key]}";
            }
            if (_subValue == null) return;
            string[] ids = { "unity-x-input", "unity-y-input", "unity-z-input" };
            for (int i = 0; i < 3; i++) _subValue.Q<FloatField>(ids[i])?.SetEnabled(SkillFieldRules.SubValuePartActive(_data, i));
        }

        // Area preview: Arc = angle / radius from SubValue, Box = width / length; front points up; scaled to fit.
        // Area preview drawn exactly like the game draws it (Show Hit Area): side view, the owner faces right, real units
        // (1 u grid), the owner's 2.077 × 3 u body for scale, PivotOffset applied (x forward, y up). Arc: angle around the facing,
        // radius = SubValue y × AreaScale. Box: length = y forward, height = z (or x when z is 0), centred on the pivot.
        private const float BodyW = 2.077f, BodyH = 3f;

        private void DrawArea(MeshGenerationContext ctx)
        {
            Rect r = _preview.contentRect;
            if (r.width < 10 || r.height < 10) return;
            Painter2D p = ctx.painter2D;
            Vector3 sub = ParseVec3(_data.Get("SubValue"));
            Vector3 off = ParseVec3(_data.Get("PivotOffset"));
            float scale = (float)ParseDouble(_data.Get("AreaScale"));
            bool box = _data.Get("AreaType") == nameof(SkillAreaType.Box);
            float angle = Mathf.Clamp(sub.x, 0f, 360f), radius = Mathf.Max(0f, sub.y * scale);
            float length = Mathf.Max(0f, sub.y * scale), height = Mathf.Max(0f, (sub.z > 0 ? sub.z : sub.x) * scale);

            // World bounds: the body and the area, plus a margin.
            float minX = -BodyW / 2, maxX = BodyW / 2, minY = -BodyH / 2, maxY = BodyH / 2;
            if (box)
            {
                minX = Mathf.Min(minX, off.x); maxX = Mathf.Max(maxX, off.x + length);
                minY = Mathf.Min(minY, off.y - height / 2); maxY = Mathf.Max(maxY, off.y + height / 2);
            }
            else
            {
                minX = Mathf.Min(minX, off.x - radius); maxX = Mathf.Max(maxX, off.x + radius);
                minY = Mathf.Min(minY, off.y - radius); maxY = Mathf.Max(maxY, off.y + radius);
            }
            minX -= 0.5f; maxX += 0.5f; minY -= 0.5f; maxY += 0.5f;
            float k = Mathf.Min(r.width / (maxX - minX), r.height / (maxY - minY));
            float padX = (r.width - (maxX - minX) * k) / 2, padY = (r.height - (maxY - minY) * k) / 2;
            Vector2 S(float x, float y) => new Vector2(r.x + padX + (x - minX) * k, r.y + padY + (maxY - y) * k);

            // 1 u grid
            p.lineWidth = 1;
            p.strokeColor = new Color(1f, 1f, 1f, 0.06f);
            for (float gx = Mathf.Ceil(minX); gx <= maxX; gx++)
            {
                p.BeginPath(); p.MoveTo(S(gx, minY)); p.LineTo(S(gx, maxY)); p.Stroke();
            }
            for (float gy = Mathf.Ceil(minY); gy <= maxY; gy++)
            {
                p.BeginPath(); p.MoveTo(S(minX, gy)); p.LineTo(S(maxX, gy)); p.Stroke();
            }

            // Owner body + facing arrow
            p.strokeColor = new Color(0.75f, 0.75f, 0.75f, 0.8f);
            p.lineWidth = 1.5f;
            p.BeginPath();
            p.MoveTo(S(-BodyW / 2, -BodyH / 2)); p.LineTo(S(BodyW / 2, -BodyH / 2)); p.LineTo(S(BodyW / 2, BodyH / 2)); p.LineTo(S(-BodyW / 2, BodyH / 2));
            p.ClosePath();
            p.Stroke();
            p.BeginPath(); p.MoveTo(S(0, BodyH / 2 + 0.2f)); p.LineTo(S(0.7f, BodyH / 2 + 0.2f)); p.LineTo(S(0.5f, BodyH / 2 + 0.35f)); p.Stroke();

            // Area
            var green = new Color(0.2f, 0.95f, 0.35f);
            p.fillColor = new Color(green.r, green.g, green.b, 0.28f);
            p.strokeColor = green;
            p.lineWidth = 2;
            Vector2 c = S(off.x, off.y);
            bool empty = box ? length <= 0 || height <= 0 : angle <= 0 || radius <= 0;
            if (!empty)
            {
                p.BeginPath();
                if (box)
                {
                    p.MoveTo(S(off.x, off.y - height / 2)); p.LineTo(S(off.x + length, off.y - height / 2));
                    p.LineTo(S(off.x + length, off.y + height / 2)); p.LineTo(S(off.x, off.y + height / 2));
                    p.ClosePath();
                }
                else if (angle >= 359.9f) p.Arc(c, radius * k, Angle.Degrees(0), Angle.Degrees(360));
                else
                {
                    p.MoveTo(c);
                    p.Arc(c, radius * k, Angle.Degrees(-angle / 2), Angle.Degrees(angle / 2));
                    p.ClosePath();
                }
                p.Fill();
                p.Stroke();
            }
            p.fillColor = new Color(1f, 0.25f, 0.25f);
            p.BeginPath();
            p.Arc(c, 3f, Angle.Degrees(0), Angle.Degrees(360));
            p.Fill();
        }

        private void UpdatePreviewCaption()
        {
            if (_previewCaption == null) return;
            Vector3 sub = ParseVec3(_data.Get("SubValue"));
            Vector3 off = ParseVec3(_data.Get("PivotOffset"));
            float scale = (float)ParseDouble(_data.Get("AreaScale"));
            bool box = _data.Get("AreaType") == nameof(SkillAreaType.Box);
            string area = box
                ? $"Box dài {Num(sub.y * scale)} × cao {Num((sub.z > 0 ? sub.z : sub.x) * scale)} ô"
                : $"Arc {Num(Mathf.Clamp(sub.x, 0f, 360f))}°, bán kính {Num(sub.y * scale)} ô";
            string offset = off.x != 0 || off.y != 0 ? $" · lệch ({Num(off.x)}, {Num(off.y)})" : "";
            bool empty = box ? sub.y * scale <= 0 || (sub.z > 0 ? sub.z : sub.x) * scale <= 0 : sub.x <= 0 || sub.y * scale <= 0;
            _previewCaption.text = area + offset + (empty ? "  ⚠ kích thước 0: không có vùng" : "") +
                                   "\nNhìn ngang, mặt hướng phải · khung xám = nhân vật 2×3 ô · lưới 1 ô · chấm đỏ = pivot";
            _previewCaption.style.color = empty ? new Color(1f, 0.6f, 0.2f) : new Color(0.65f, 0.65f, 0.65f);
        }

        private static string Num(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static double ParseDouble(string s) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0.0;

        private static Vector3 ParseVec3(string s)
        {
            string[] p = (s ?? "").Split(',');
            return p.Length == 3 ? new Vector3((float)ParseDouble(p[0]), (float)ParseDouble(p[1]), (float)ParseDouble(p[2])) : Vector3.zero;
        }

        private static Type PortType(SkillFieldType t)
        {
            switch (t)
            {
                case SkillFieldType.Float: return typeof(float);
                case SkillFieldType.Bool: return typeof(bool);
                case SkillFieldType.Int: return typeof(int);
                case SkillFieldType.Long: return typeof(long);
                case SkillFieldType.String: return typeof(string);
                case SkillFieldType.Vec3: return typeof(Vector3);
                case SkillFieldType.LongArray: return typeof(long[]);
                case SkillFieldType.Vec3Array: return typeof(Vector3[]);
                default: return typeof(Enum);
            }
        }

        /// <summary>Wire colour of a data type.</summary>
        public static Color ColorOf(SkillFieldType t)
        {
            switch (t)
            {
                case SkillFieldType.Float: return new Color(0.45f, 0.70f, 1.00f);
                case SkillFieldType.Bool: return new Color(0.35f, 0.90f, 0.75f);
                case SkillFieldType.Int: return new Color(0.70f, 0.55f, 1.00f);
                case SkillFieldType.Long: return new Color(0.80f, 0.45f, 0.95f);
                case SkillFieldType.String: return new Color(1.00f, 0.75f, 0.35f);
                case SkillFieldType.Vec3: return new Color(1.00f, 0.45f, 0.75f);
                case SkillFieldType.LongArray: return new Color(0.60f, 0.30f, 0.75f);
                case SkillFieldType.Vec3Array: return new Color(0.80f, 0.30f, 0.55f);
                default: return new Color(0.95f, 0.85f, 0.40f);
            }
        }

        /// <summary>Title bar colour of a node kind (also used by the palette), close to the reference tool.</summary>
        public static Color ColorOf(SkillNodeKind k)
        {
            switch (k)
            {
                case SkillNodeKind.Start: return new Color(0.42f, 0.42f, 0.50f);
                case SkillNodeKind.Animation:
                case SkillNodeKind.Effect: return new Color(0.62f, 0.38f, 0.62f);
                case SkillNodeKind.Hit: return new Color(0.72f, 0.30f, 0.30f);
                case SkillNodeKind.Search: return new Color(0.30f, 0.30f, 0.36f);
                case SkillNodeKind.Move:
                case SkillNodeKind.MoveControl:
                case SkillNodeKind.LookAt: return new Color(0.25f, 0.48f, 0.72f);
                case SkillNodeKind.Knockback: return new Color(0.28f, 0.52f, 0.78f);
                case SkillNodeKind.CameraAction: return new Color(0.66f, 0.40f, 0.66f);
                case SkillNodeKind.Buff: return new Color(0.72f, 0.62f, 0.30f);
                case SkillNodeKind.End: return new Color(0.30f, 0.30f, 0.34f);
                default: return new Color(0.3f, 0.3f, 0.3f);
            }
        }
    }
}
