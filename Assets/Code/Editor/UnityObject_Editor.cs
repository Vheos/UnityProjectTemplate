namespace UnityProject.Editor;

[CustomEditor(typeof(UnityObject), true)]
[CanEditMultipleObjects]
public class UnityObject_Editor : UnityEditor.Editor
{
	// Fields
	private const string ScriptPropertyName = "m_Script";

	// Methods
	public override void OnInspectorGUI()
	{
		DrawPropertiesExcluding(serializedObject, ScriptPropertyName);
		serializedObject.ApplyModifiedProperties();
	}
}