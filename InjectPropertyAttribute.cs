using System;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

/// <summary>
/// Injects a property to be drawn on a member with specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, Inherited = true)]
public class InjectPropertyAttribute : ShowInInspectorAttribute {

	public string MemberPath { get; private set; }
	public bool DrawAboveTarget { get; private set; }

	/// <summary>
	/// Injects the property to be drawn before or after the target member.
	/// </summary>
	/// <param name="MemberPath">Path of the member to be injected on.</param>
	/// <param name="DrawAboveTarget">Should the property be drawn above the target member?</param>
	public InjectPropertyAttribute(string MemberPath, bool DrawAboveTarget = true) {
		this.MemberPath = MemberPath;
		this.DrawAboveTarget = DrawAboveTarget;
	}

}

#if UNITY_EDITOR
namespace Sirenix.OdinInspector.CustomProcessors {

	// Draws according to the original drawer on the specified member of the InjectPropertyAttribute.
	[DrawerPriority(super: 999)]
	class InjectedPropertyDrawer : OdinAttributeDrawer<InjectDrawerAttribute> {

		protected override void DrawPropertyLayout(GUIContent label) {
			if (!Attribute.DrawTargetPropertyFirst) { CallNextDrawer(label); }
			DrawInjectedProperty();
			if (Attribute.DrawTargetPropertyFirst) { CallNextDrawer(label); }

		}

		void DrawInjectedProperty() {
			InjectDrawer.ShouldDraw = true;
			Attribute.targetProperty.Draw();
			InjectDrawer.ShouldDraw = false;
		}
	}

	// The original drawer of the InjectPropertyAttribute.
	// Skips drawing initially and can drawn on demand with ShouldDraw.
	[DrawerPriority(super: 1000)]
	public class InjectDrawer : OdinAttributeDrawer<InjectPropertyAttribute> {
		public static bool ShouldDraw;
		bool hasValidPath;

		protected override void Initialize() {
			if (Property.IsInitialized()) { return; }

			string[] pathMembers = Attribute.MemberPath.Split('.');

			InspectorProperty targetP = Property.Parent;
			if (targetP == null) { targetP = Property.SerializationRoot; }

			foreach (string nextPath in pathMembers) {
				targetP = targetP.Children.Get(nextPath);
				if (targetP == null) { return; }
			}

			Property.AddAttribute(new InitializedAttribute());
			Property.UpdateOnNextRepaint();
			targetP.AddAttribute(new InjectDrawerAttribute(Property, Attribute.DrawAboveTarget));
			targetP.UpdateOnNextRepaint();

			hasValidPath = true;
		}

		protected override void DrawPropertyLayout(GUIContent label) {
			if (!Property.IsInitialized()) {
				if (!hasValidPath) {
					try { EditorGUILayout.HelpBox($"{Attribute.MemberPath} is not a valid path.", MessageType.Error); }
					catch { Debug.LogWarning($"{Attribute.MemberPath} is not a valid path."); } // Unity was sometimes throwing an exception here so why not.
					CallNextDrawer(label);
				}
				else if (ShouldDraw) { Debug.LogError("Was asked to draw even when property is not initialized."); }
			}
			else if (ShouldDraw) {
				Property.Update(true);
				CallNextDrawer(label);
			}
		}

	}

	// Helper attribute to be attached to the target of InjectPropertyAttribute.
	class InjectDrawerAttribute : Attribute {
		public InspectorProperty targetProperty;
		public bool DrawTargetPropertyFirst;

		public InjectDrawerAttribute(InspectorProperty target, bool DrawTargetFirst) {
			targetProperty = target;
			DrawTargetPropertyFirst = DrawTargetFirst;
		}
	}

	// Attribute that marks a property as initialized.
	class InitializedAttribute : Attribute { }

	static class InspectorPropertyExtensions {
		public static bool IsInitialized(this InspectorProperty p) => p.Attributes.Any(x => x is InitializedAttribute);
		public static void AddAttribute(this InspectorProperty p, Attribute attribute) => p.Info.GetEditableAttributesList().Add(attribute);
		public static void UpdateOnNextRepaint(this InspectorProperty p) => p.Tree.DelayActionUntilRepaint(p.RefreshSetup);
	}
}

#endif
