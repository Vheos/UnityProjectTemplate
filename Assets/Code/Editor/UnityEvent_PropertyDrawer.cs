namespace UnityProject.Editor;
using System.Reflection;
using System.Text;

[CustomPropertyDrawer(typeof(UnityEventBase), true)]
public class UnityEventCompactDrawer : PropertyDrawer
{
	protected class State
	{
		internal ReorderableList m_ReorderableList;
		public SerializedProperty property;
		public int lastSelectedIndex;
	}

	private static readonly MethodInfo BuildPopupList = typeof(UnityEventDrawer).GetMethod("BuildPopupList", BindingFlags.Static | BindingFlags.NonPublic);
	private static readonly MethodInfo GetEventParams = typeof(UnityEventDrawer).GetMethod("GetEventParams", BindingFlags.Static | BindingFlags.NonPublic);
	private static readonly MethodInfo GetDummyEvent = typeof(UnityEventDrawer).GetMethod("GetDummyEvent", BindingFlags.Static | BindingFlags.NonPublic);
	private static GUIStyle foldoutHeader;

	private static float VerticalSpacing
		=> EditorGUIUtility.standardVerticalSpacing;

	private const float Spacing = 3;

	private static readonly GUIContent DropdownIcon = EditorGUIUtility.IconContent("icon dropdown");
	private static readonly GUIContent MixedValueContent = EditorGUIUtility.TrTextContent("—", "Mixed Values");
	private static readonly GUIContent TempContent = new();

	private const string kNoFunctionString = "No Function";

	//Persistent Listener Paths
	private const string kInstancePath = "m_Target";
	private const string kCallStatePath = "m_CallState";
	private const string kArgumentsPath = "m_Arguments";
	private const string kModePath = "m_Mode";
	private const string kMethodNamePath = "m_MethodName";

	//ArgumentCache paths
	internal const string kFloatArgument = "m_FloatArgument";
	internal const string kIntArgument = "m_IntArgument";
	internal const string kObjectArgument = "m_ObjectArgument";
	internal const string kStringArgument = "m_StringArgument";
	internal const string kBoolArgument = "m_BoolArgument";
	internal const string kObjectArgumentAssemblyTypeName = "m_ObjectArgumentAssemblyTypeName";
	private string m_Text;
	private UnityEventBase m_DummyEvent;
	private SerializedProperty m_Prop;
	private SerializedProperty m_ListenersArray;
	private const int kExtraSpacing = 2;

	//State:
	private ReorderableList m_ReorderableList;
	private int m_LastSelectedIndex;
	private State currentState;
	private readonly Dictionary<string, State> m_States = new();

	private State GetState(SerializedProperty prop)
	{
		string key = prop.propertyPath;
		m_States.TryGetValue(key, out State state);
		// ensure the cached SerializedProperty is synchronized (case 974069)
		if (state == null || state.m_ReorderableList.serializedProperty.serializedObject != prop.serializedObject)
		{
			state ??= new State();

			SerializedProperty listenersArray = prop.FindPropertyRelative("m_PersistentCalls.m_Calls");
			state.m_ReorderableList =
				new ReorderableList(prop.serializedObject, listenersArray, true, true, true, true)
				{
					drawHeaderCallback = null,
					drawFooterCallback = _ => { },
					drawElementCallback = DrawEvent,
					elementHeightCallback = OnGetElementHeight,
					drawElementBackgroundCallback = DrawElementBackground,
					onSelectCallback = OnSelectEvent,
					onReorderCallback = OnReorderEvent,
					onAddCallback = OnAddEvent,
					onRemoveCallback = OnRemoveEvent,

					headerHeight = 0,
					footerHeight = 0,
				};

			m_States[key] = state;
		}

		return state;
	}
	private void DrawElementBackground(Rect rect, int index, bool active, bool focused)
	{
		bool isPro = EditorGUIUtility.isProSkin;
		Color color = GUI.color;

		// Dark-blue color in Light theme looks super ugly with reorderable lists :(
		focused = isPro && focused;

		ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, active, focused, true);
		GUI.color = color;
	}
	private State RestoreState(SerializedProperty property)
	{
		State state = GetState(property);

		m_ListenersArray = state.m_ReorderableList.serializedProperty;
		m_ReorderableList = state.m_ReorderableList;
		m_LastSelectedIndex = state.lastSelectedIndex;
		m_ReorderableList.index = m_LastSelectedIndex;

		return state;
	}
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		m_Prop = property;
		m_Text = label.text;

		currentState = RestoreState(property);
		currentState.property = property;

		OnGUI(position);
		currentState.lastSelectedIndex = m_LastSelectedIndex;
	}
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		RestoreState(property);

		float height = 0f;
		if (m_ReorderableList != null)
		{
			if (!m_ReorderableList.serializedProperty.isExpanded)
				return EditorGUIUtility.singleLineHeight + VerticalSpacing + VerticalSpacing;

			height = m_ReorderableList.GetHeight();
			height += EditorGUIUtility.singleLineHeight;
		}

		return height + VerticalSpacing;
	}
	public void OnGUI(Rect rect)
	{
		if (m_ListenersArray == null || !m_ListenersArray.isArray)
			return;

		m_DummyEvent = GetDummyEvent.Invoke(null, new[] { m_Prop }) as UnityEventBase;
		if (m_DummyEvent == null)
			return;

		if (m_ReorderableList != null)
		{
			if (ReorderableList.defaultBehaviours == null)
				m_ReorderableList.DoList(Rect.zero);

			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			rect.xMin += 8 * oldIndent;

			Rect headerRect = new(rect.x, rect.y, rect.width, 18);
			Rect listRect = new(rect) { yMin = headerRect.yMax };

			ReorderableList.defaultBehaviours.DrawHeaderBackground(headerRect);
			bool isExpanded = DrawListHeader(headerRect, m_ReorderableList);

			if (isExpanded)
			{
				ReorderableList.defaultBehaviours.draggingHandle.fixedWidth = 6;

				m_ReorderableList.DoList(listRect);

				ReorderableList.defaultBehaviours.draggingHandle.fixedWidth = 0;
			}

			EditorGUI.indentLevel = oldIndent;
		}
	}
	protected virtual bool DrawListHeader(Rect rect, ReorderableList list)
	{
		const int sizeWidth = 24;
		const int buttonsWidth = 54;

		SerializedProperty property = list.serializedProperty;

		rect.xMin += 16;
		rect.yMin += 1;
		rect.height = EditorGUIUtility.singleLineHeight;

		Rect foldoutRect = new(rect);
		foldoutRect.width -= buttonsWidth + sizeWidth;
		foldoutRect.height -= 1;

		foldoutHeader ??= new GUIStyle(EditorStyles.foldoutHeader)
		{
			richText = true,
			fontStyle = FontStyle.Normal,
			clipping = TextClipping.Clip,
			fixedHeight = 0,
			padding = new RectOffset(14, 5, 2, 2),
		};

		// Header
		{
			string eventParams = (string)GetEventParams.Invoke(null, new[] { m_DummyEvent });
			string hex = EditorGUIUtility.isProSkin ? "ffffff" : "000000";
			string text = (string.IsNullOrEmpty(m_Text) ? "Event" : m_Text) + $"<color=#{hex}70>{eventParams}</color>";

			property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, property.isExpanded, text, foldoutHeader);
			EditorGUI.EndFoldoutHeaderGroup();
		}

		Rect sizeRect = new(rect) { x = foldoutRect.xMax, width = sizeWidth };
		sizeRect.yMin += 1;
		sizeRect.height -= 1;

		// Size field
		{
			EditorGUI.BeginChangeCheck();
			GUIStyle numberField = EditorStyles.numberField;
			numberField.contentOffset = new Vector2(0, -1);

			int arraySize = EditorGUI.IntField(sizeRect, property.arraySize);

			numberField.contentOffset = Vector2.zero;
			if (EditorGUI.EndChangeCheck())
				property.arraySize = arraySize;
		}

		Rect footerRect = new(rect) { x = sizeRect.xMax + 12, width = buttonsWidth };
		footerRect.yMin += 1;

		// Footer buttons
		{
			GUIStyle footerBg = ReorderableList.defaultBehaviours.footerBackground;
			footerBg.fixedHeight = 0.01f;

			ReorderableList.defaultBehaviours.DrawFooter(footerRect, list);

			footerBg.fixedHeight = 0;
		}

		return property.isExpanded;
	}
	private static PersistentListenerMode GetMode(SerializedProperty mode) => (PersistentListenerMode)mode.enumValueIndex;
	private float OnGetElementHeight(int index)
	{
		if (m_ReorderableList == null)
			return 0;

		SerializedProperty element = m_ListenersArray.GetArrayElementAtIndex(index);

		SerializedProperty mode = element.FindPropertyRelative(kModePath);
		PersistentListenerMode modeEnum = GetMode(mode);

		float spacing = VerticalSpacing + kExtraSpacing;

		return modeEnum is PersistentListenerMode.Object or not PersistentListenerMode.Void and not PersistentListenerMode.EventDefined
			? EditorGUIUtility.singleLineHeight * 2 + VerticalSpacing + spacing
			: EditorGUIUtility.singleLineHeight + spacing;
	}
	protected virtual void DrawEvent(Rect rect, int index, bool isActive, bool isFocused)
	{
		SerializedProperty pListener = m_ListenersArray.GetArrayElementAtIndex(index);

		Rect contentRect = rect;
		contentRect.xMin -= 6;
		contentRect.xMax += 2;
		contentRect.y += 1;

		Rect[] subRects = GetRowRects(contentRect);
		Rect enabledRect = subRects[0];
		Rect goRect = subRects[1];
		Rect functionRect = subRects[2];
		Rect argRect = subRects[3];

		// find the current event target...
		SerializedProperty callState = pListener.FindPropertyRelative(kCallStatePath);
		SerializedProperty mode = pListener.FindPropertyRelative(kModePath);
		SerializedProperty arguments = pListener.FindPropertyRelative(kArgumentsPath);
		SerializedProperty listenerTarget = pListener.FindPropertyRelative(kInstancePath);
		SerializedProperty methodName = pListener.FindPropertyRelative(kMethodNamePath);

		Color c = GUI.backgroundColor;
		GUI.backgroundColor = Color.white;

		UnityEventCallState callStateEnum = (UnityEventCallState)callState.enumValueIndex;
		bool isEditorAndRuntime = callStateEnum == UnityEventCallState.EditorAndRuntime;
		bool isRuntime = callStateEnum == UnityEventCallState.RuntimeOnly;

		Rect toggleRect = enabledRect;
		toggleRect.width = 16;

		if (isEditorAndRuntime || isRuntime && Application.isPlaying)
		{
			Rect markRect = new(rect) { width = 2 };
			markRect.x -= 20;
			EditorGUI.DrawRect(markRect, new Color(1, 0.7f, 0.4f, 1));
		}

		Event evt = Event.current;
		Color color = GUI.color;
		Vector2 mousePos = evt.mousePosition;
		{
			bool isHover = toggleRect.Contains(mousePos);
			if (isHover)
			{
				// Ooh, these beautiful 2-pixels of rounded edges..
				GUI.DrawTexture(toggleRect, Texture2D.whiteTexture, ScaleMode.ScaleToFit, true, 1, new Color(1, 1, 1, 0.15f), Vector4.zero, 2);
			}
		}

		GUI.color = new Color(1, 1, 1, 0.75f);
		GUI.Box(toggleRect, DropdownIcon, EditorStyles.centeredGreyMiniLabel);
		GUI.color = color;

		GUI.color = new Color(0, 0, 0, 0);
		EditorGUI.PropertyField(toggleRect, callState, GUIContent.none);
		GUI.color = color;

		bool isOff = callStateEnum == UnityEventCallState.Off;
		EditorGUI.BeginDisabledGroup(isOff);

		EditorGUI.BeginChangeCheck();
		{
			GUI.Box(goRect, GUIContent.none);
			EditorGUI.PropertyField(goRect, listenerTarget, GUIContent.none);
			if (EditorGUI.EndChangeCheck())
				methodName.stringValue = null;
		}

		PersistentListenerMode modeEnum = GetMode(mode);
		//only allow argument if we have a valid target / method
		if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(methodName.stringValue))
			modeEnum = PersistentListenerMode.Void;
		SerializedProperty argument = modeEnum switch
		{
			PersistentListenerMode.Float => arguments.FindPropertyRelative(kFloatArgument),
			PersistentListenerMode.Int => arguments.FindPropertyRelative(kIntArgument),
			PersistentListenerMode.Object => arguments.FindPropertyRelative(kObjectArgument),
			PersistentListenerMode.String => arguments.FindPropertyRelative(kStringArgument),
			PersistentListenerMode.Bool => arguments.FindPropertyRelative(kBoolArgument),
			_ => arguments.FindPropertyRelative(kIntArgument),
		};
		string desiredArgTypeName = arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName).stringValue;
		Type desiredType = typeof(UnityObject);
		if (!string.IsNullOrEmpty(desiredArgTypeName))
			desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(UnityObject);

		argRect.xMin = goRect.xMax + Spacing;

		if (modeEnum == PersistentListenerMode.Object)
		{
			EditorGUI.BeginChangeCheck();
			UnityObject result = EditorGUI.ObjectField(argRect, GUIContent.none, argument.objectReferenceValue, desiredType, true);
			if (EditorGUI.EndChangeCheck())
				argument.objectReferenceValue = result;
		}
		else if (modeEnum is not PersistentListenerMode.Void and not PersistentListenerMode.EventDefined)
		{
			EditorGUI.PropertyField(argRect, argument, GUIContent.none);
		}

		using (new EditorGUI.DisabledScope(listenerTarget.objectReferenceValue == null))
		{
			EditorGUI.BeginProperty(functionRect, GUIContent.none, methodName);
			{
				GUIContent buttonContent;
				if (EditorGUI.showMixedValue)
				{
					buttonContent = MixedValueContent;
				}
				else
				{
					StringBuilder buttonLabel = new();
					if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(methodName.stringValue))
					{
						buttonLabel.Append(kNoFunctionString);
					}
					else if (!UnityEventDrawer.IsPersistantListenerValid(m_DummyEvent, methodName.stringValue, listenerTarget.objectReferenceValue, GetMode(mode), desiredType))
					{
						string instanceString = "UnknownComponent";
						UnityObject instance = listenerTarget.objectReferenceValue;
						if (instance != null)
							instanceString = instance.GetType().Name;

						buttonLabel.Append(string.Format("<Missing {0}.{1}>", instanceString, methodName.stringValue));
					}
					else
					{
						buttonLabel.Append(listenerTarget.objectReferenceValue.GetType().Name);

						if (!string.IsNullOrEmpty(methodName.stringValue))
						{
							buttonLabel.Append(".");
							if (methodName.stringValue.StartsWith("set_"))
								buttonLabel.Append(methodName.stringValue[4..]);
							else
								buttonLabel.Append(methodName.stringValue);
						}
					}

					TempContent.text = buttonLabel.ToString();
					buttonContent = TempContent;
				}

				if (GUI.Button(functionRect, buttonContent, EditorStyles.popup))
				{
					GenericMenu popup = BuildPopupList.Invoke(null, new object[] { listenerTarget.objectReferenceValue, m_DummyEvent, pListener }) as GenericMenu;
					popup.DropDown(functionRect);
				}
			}

			EditorGUI.EndProperty();
		}

		EditorGUI.EndDisabledGroup();
		GUI.backgroundColor = c;
	}
	private Rect[] GetRowRects(Rect rect)
	{
		Rect[] rects = new Rect[4];

		rect.height = EditorGUIUtility.singleLineHeight;
		rect.y += 2;

		Rect enabledRect = rect;
		enabledRect.width = 16 + Spacing - 1;

		Rect goRect = rect;
		goRect.xMin = enabledRect.xMax;
		goRect.width = rect.width;
		// Shrink object field when inspector is small
		goRect.width *= Mathf.Lerp(0, 0.4f, (rect.width - 125) / (350 - 100));
		goRect.width = Mathf.Max(goRect.width, 35);

		Rect functionRect = rect;
		functionRect.xMin = goRect.xMax + Spacing;

		Rect argRect = rect;
		argRect.y += EditorGUIUtility.singleLineHeight + VerticalSpacing;

		rects[0] = enabledRect;
		rects[1] = goRect;
		rects[2] = functionRect;
		rects[3] = argRect;
		return rects;
	}
	protected virtual void OnRemoveEvent(ReorderableList list)
	{
		ReorderableList.defaultBehaviours.DoRemoveButton(list);
		m_LastSelectedIndex = list.index;
	}
	protected virtual void OnAddEvent(ReorderableList list)
	{
		if (m_ListenersArray.hasMultipleDifferentValues)
		{
			//When increasing a multi-selection array using Serialized Property
			//Data can be overwritten if there is mixed values.
			//The Serialization system applies the Serialized data of one object, to all other objects in the selection.
			//We handle this case here, by creating a SerializedObject for each object.
			//Case 639025.
			foreach (UnityObject targetObject in m_ListenersArray.serializedObject.targetObjects)
			{
				using SerializedObject temSerialziedObject = new(targetObject);
				SerializedProperty listenerArrayProperty = temSerialziedObject.FindProperty(m_ListenersArray.propertyPath);
				listenerArrayProperty.arraySize += 1;
				temSerialziedObject.ApplyModifiedProperties();
			}

			m_ListenersArray.serializedObject.SetIsDifferentCacheDirty();
			m_ListenersArray.serializedObject.Update();
			list.index = list.serializedProperty.arraySize - 1;
		}
		else
		{
			ReorderableList.defaultBehaviours.DoAddButton(list);
		}

		m_LastSelectedIndex = list.index;
		SerializedProperty pListener = m_ListenersArray.GetArrayElementAtIndex(list.index);

		SerializedProperty callState = pListener.FindPropertyRelative(kCallStatePath);
		SerializedProperty listenerTarget = pListener.FindPropertyRelative(kInstancePath);
		SerializedProperty methodName = pListener.FindPropertyRelative(kMethodNamePath);
		SerializedProperty mode = pListener.FindPropertyRelative(kModePath);
		SerializedProperty arguments = pListener.FindPropertyRelative(kArgumentsPath);

		callState.enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
		listenerTarget.objectReferenceValue = null;
		methodName.stringValue = null;
		mode.enumValueIndex = (int)PersistentListenerMode.Void;
		arguments.FindPropertyRelative(kFloatArgument).floatValue = 0;
		arguments.FindPropertyRelative(kIntArgument).intValue = 0;
		arguments.FindPropertyRelative(kObjectArgument).objectReferenceValue = null;
		arguments.FindPropertyRelative(kStringArgument).stringValue = null;
		arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName).stringValue = null;
	}
	protected virtual void OnSelectEvent(ReorderableList list) => m_LastSelectedIndex = list.index;
	protected virtual void OnReorderEvent(ReorderableList list) => m_LastSelectedIndex = list.index;
}