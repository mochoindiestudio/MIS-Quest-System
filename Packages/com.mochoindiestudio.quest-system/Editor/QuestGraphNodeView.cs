using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MochoIndieStudio.QuestSystem.Editor
{
    /// <summary>
    /// Base view for a node rendered inside <see cref="QuestGraphView"/>. Persists the node's canvas
    /// position (and user-set width) back into the model via the callbacks supplied by the subclass,
    /// so the graph stays a direct reflection of the serialized data.
    /// </summary>
    public abstract class QuestGraphNodeView : Node
    {
        /// <summary>Lower bound for a resized node's width, in canvas pixels.</summary>
        private const float MinNodeWidth = 220f;

        private readonly QuestGraphView graph;
        private readonly Action<Vector2> persistPosition;
        private readonly Action<float> persistWidth;
        private readonly Func<float> readWidth;

        protected QuestGraphNodeView(
            QuestGraphView graph,
            Vector2 initialPosition,
            Func<float> readWidth,
            Action<Vector2> persistPosition,
            Action<float> persistWidth)
        {
            this.graph = graph;
            this.persistPosition = persistPosition;
            this.persistWidth = persistWidth;
            this.readWidth = readWidth;

            SetPosition(new Rect(initialPosition, Vector2.zero));

            style.minWidth = MinNodeWidth;
            float savedWidth = readWidth != null ? readWidth() : 0f;
            if (savedWidth > 0f)
            {
                style.width = savedWidth;
            }

            capabilities |= Capabilities.Resizable;
            hierarchy.Add(new ResizableElement());

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        /// <summary>Every drag / box-move / programmatic move routes through here -- so grid snapping
        /// and position persistence happen in one place.</summary>
        public override void SetPosition(Rect newPos)
        {
            if (graph != null && graph.SnapToGrid)
            {
                newPos.x = Mathf.Round(newPos.x / QuestGraphView.GridSpacing) * QuestGraphView.GridSpacing;
                newPos.y = Mathf.Round(newPos.y / QuestGraphView.GridSpacing) * QuestGraphView.GridSpacing;
            }

            base.SetPosition(newPos);
            persistPosition?.Invoke(newPos.position);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            persistPosition?.Invoke(GetPosition().position);

            float width = evt.newRect.width;
            float current = readWidth != null ? readWidth() : 0f;
            if (width > 0f && !Mathf.Approximately(width, current))
            {
                persistWidth?.Invoke(width);
                graph?.MarkDirty();
            }
        }

        /// <summary>Creates a horizontal edge port carrying <paramref name="userData"/>.</summary>
        protected static Port CreatePort(Direction direction, Port.Capacity capacity, object userData)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, direction, capacity, typeof(bool));
            port.portName = string.Empty;
            port.userData = userData;
            return port;
        }

        /// <summary>Inserts a small icon at the start of the node's title bar.</summary>
        protected void SetHeaderIcon(string iconPath, float size = 16f)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (texture == null)
            {
                return;
            }

            titleContainer.Insert(0, new Image
            {
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                style = { width = size, height = size, marginLeft = 4, alignSelf = Align.Center }
            });
        }

        /// <summary>A word-wrapping, vertically-growing multiline field bound to a serialized string.</summary>
        protected static PropertyField MultilineField(SerializedProperty property, string label, float minHeight = 48f)
        {
            var field = new PropertyField(property, label) { style = { minHeight = minHeight } };
            return field;
        }
    }
}
