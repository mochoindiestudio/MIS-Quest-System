using UnityEngine;

namespace MochoIndieStudio.QuestSystem
{
    /// <summary>
    /// Optional convenience: owns a <see cref="QuestLog"/>, pumps its <see cref="QuestLog.Tick"/>
    /// from <c>Update</c>, and optionally registers a set of <see cref="QuestList"/>s on
    /// <c>Awake</c>. Purely a lifecycle wrapper -- the core <see cref="QuestLog"/> stays free of any
    /// engine dependency, so a game that manages its own update loop can ignore this component.
    /// </summary>
    [AddComponentMenu("MIS Quest System/Quest Log Host")]
    public sealed class QuestLogHost : MonoBehaviour
    {
        [Tooltip("Quest lists registered on Awake. Optional -- you can also register from code.")]
        [SerializeField]
        private QuestList[] questLists;

        [Tooltip("When off, you must call Tick() yourself; the Log is still created and registered.")]
        [SerializeField]
        private bool tickInUpdate = true;

        /// <summary>The log this component drives. Created in <c>Awake</c>.</summary>
        public QuestLog Log { get; private set; }

        private void Awake()
        {
            Log = new QuestLog();

            if (questLists != null)
            {
                for (int i = 0; i < questLists.Length; i++)
                {
                    Log.Register(questLists[i]);
                }
            }
        }

        private void Update()
        {
            if (tickInUpdate)
            {
                Log?.Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            Log?.Dispose();
            Log = null;
        }
    }
}

