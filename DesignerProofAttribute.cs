/*
 * ******** Disclaimer *********
 * 
 * Developers will have to use some means of setting the "IsUserDeveloper" bool (call EditorPrefs.SetBool("IsUserDeveloper", true); )
 * from EditorPrefs to `true`, or the properties marked as [DesignerProof] will be Read Only.
 * 
 * ********* Disclaimer ********* 
 */
 
using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
#endif


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method, inherited = true)]
public class DesignerProofAttribute : Attribute { }

#if UNITY_EDITOR

namespace OdinInspector.CustomProcessors {
	// Editor attribute to trigger Read Only behaviour
	class DrawReadOnlyEditorAttribute : Attribute { }

	class ReadOnlyEditorDrawer : OdinAttributeDrawer<DrawReadOnlyEditorAttribute> {
		protected override void DrawPropertyLayout(GUIContent label) {
			EditorGUI.BeginDisabledGroup(true);
      {
			   CallNextDrawer(label);
      }
			EditorGUI.EndDisabledGroup();
		}
	}

  // Adds the Editor attribute to properties marked as [DesignerProof]
	class DesignerProofProcessor : OdinAttributeProcessor {
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes) {
			// Don't apply for developers.
			if (EditorPrefs.GetBool("IsUserDeveloper", false) == true) { return; }

			if (attributes.Any(x => x is DesignerProofAttribute)) { attributes.Add(new DrawReadOnlyEditorAttribute()); }
		}
	}
  
}
#endif

