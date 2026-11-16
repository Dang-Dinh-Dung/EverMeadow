using Evermeadow.Core.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Evermeadow.Presentation.SkillTool.Editor
{
    /// <summary>
    /// Left-hand node palette of the Skill Tool: drag an entry onto the canvas to add that node where it is dropped, or click
    /// it to add the node at the middle of the canvas. Start is not listed (one per skill, always present).
    /// </summary>
    /// <example>
    /// <code>
    /// var pane = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
    /// pane.Add(new SkillNodePalette(graphView));
    /// pane.Add(graphView);
    /// </code>
    /// </example>
    public sealed class SkillNodePalette : VisualElement
    {
        private const float DragThreshold = 4f;

        private static SkillNodeKind[] Entries => SkillSchema.Kinds;

        private readonly SkillGraphView _graph;

        /// <summary>Creates the palette for <paramref name="graph"/>.</summary>
        public SkillNodePalette(SkillGraphView graph)
        {
            _graph = graph;
            style.width = 168;
            style.flexShrink = 0;
            style.paddingLeft = 6;
            style.paddingRight = 6;
            style.paddingTop = 6;
            style.backgroundColor = new Color(0.17f, 0.17f, 0.17f);
            style.borderRightWidth = 1;
            style.borderRightColor = new Color(0.10f, 0.10f, 0.10f);

            Add(new Label("Nodes") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
            foreach (SkillNodeKind kind in Entries) Add(CreateEntry(kind, SkillToolNotes.Node(kind)));
            Add(new Label("Kéo node vào canvas, hoặc bấm để thêm vào giữa. Rê chuột lên node, trường hay cổng để xem giải thích.")
            {
                style = { whiteSpace = WhiteSpace.Normal, marginTop = 8, fontSize = 10, color = new Color(0.65f, 0.65f, 0.65f) }
            });
        }

        private VisualElement CreateEntry(SkillNodeKind kind, string tip)
        {
            Color color = SkillNodeView.ColorOf(kind);
            var entry = new Label(kind.ToString())
            {
                tooltip = tip,
                style =
                {
                    height = 24, marginBottom = 3, paddingLeft = 8, unityTextAlign = TextAnchor.MiddleLeft,
                    backgroundColor = new Color(0.24f, 0.24f, 0.24f),
                    borderLeftWidth = 5, borderLeftColor = color,
                    borderTopLeftRadius = 3, borderBottomLeftRadius = 3, borderTopRightRadius = 3, borderBottomRightRadius = 3
                }
            };

            Vector2 downAt = Vector2.zero;
            bool pressed = false, dragging = false;
            entry.RegisterCallback<MouseEnterEvent>(_ => entry.style.backgroundColor = new Color(0.30f, 0.30f, 0.30f));
            entry.RegisterCallback<MouseLeaveEvent>(_ => entry.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f));
            entry.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button != 0) return;
                pressed = true;
                dragging = false;
                downAt = e.mousePosition;
                e.StopPropagation();
            });
            entry.RegisterCallback<MouseMoveEvent>(e =>
            {
                if (!pressed || dragging || (e.mousePosition - downAt).sqrMagnitude < DragThreshold * DragThreshold) return;
                dragging = true;
                pressed = false;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.SetGenericData(SkillGraphView.DragKey, kind);
                DragAndDrop.StartDrag(kind.ToString());
                e.StopPropagation();
            });
            entry.RegisterCallback<MouseUpEvent>(e =>
            {
                if (e.button != 0) return;
                if (pressed && !dragging) _graph.AddNodeAtCenter(kind);
                pressed = false;
                dragging = false;
                e.StopPropagation();
            });
            return entry;
        }
    }
}
