using System;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
#endif

/// <summary>
/// Injects a property to be drawn on a member with specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, Inherited = true)]
public class InjectPropertyAttribute : ShowInInspectorAttribute {

	public string MemberPath { get; private set; }
	public bool DrawAboveTarget { get; private set; }

#if UNITY_EDITOR
	public InspectorProperty parentProperty { get; private set; }
	public bool IsInitialized { get; private set; }

	public void SetMemberPath(string value) => MemberPath = value;
	public void SetParentProperty(InspectorProperty value) => parentProperty = value;
	public bool MarkAsInitialized() => IsInitialized = true;
#endif

	/// <summary>
	/// Injects the property to be drawn before or after the target member.
	/// </summary>
	/// <param name="MemberPath">Path of the member to be injected towards.</param>
	/// <param name="DrawAboveTarget">Should the property be drawn above the target member?</param>
	public InjectPropertyAttribute(string MemberPath, bool DrawAboveTarget = true) {
		this.MemberPath = MemberPath;
		this.DrawAboveTarget = DrawAboveTarget;
	}
}

#if UNITY_EDITOR
namespace Sirenix.OdinInspector.CustomProcessors {

	// Helper attribute to be attached to the target of InjectPropertyAttribute.
	class InjectHelperAttribute : Attribute {
		public InspectorProperty targetProperty;

		public bool DrawTargetPropertyFirst;

		public InjectHelperAttribute(InspectorProperty target, bool DrawTargetFirst) {
			targetProperty = target;
			DrawTargetPropertyFirst = DrawTargetFirst;
		}
	}

	// Hey, if it works, it works :)
	[ResolverPriority(-10000)]
	class InjectorHack : OdinPropertyProcessor {
		public static List<InjectorHackHelper> ToProcessList = new List<InjectorHackHelper>(100);

		public override void ProcessMemberProperties(List<InspectorPropertyInfo> propertyInfos) {
			foreach (InspectorPropertyInfo pInfo in propertyInfos) {
				foreach (InjectorHackHelper item in ToProcessList) {
					if (item.info.GetMemberInfo() != pInfo.GetMemberInfo()) { continue; }
					pInfo.GetEditableAttributesList().Add(item.attr);
					ToProcessList.Remove(item);
					break;
				}
			}
		}
		internal class InjectorHackHelper {
			public InspectorPropertyInfo info;
			public InjectHelperAttribute attr;
		}
	}

	// Draws the original drawer on the specified member of the InjectPropertyAttribute.
	[DrawerPriority(super: 1000)]
	class InjectedPropertyDrawer : OdinAttributeDrawer<InjectHelperAttribute> {

		protected override void DrawPropertyLayout(GUIContent label) {
			if (!Attribute.DrawTargetPropertyFirst) { CallNextDrawer(label); }
			DrawInjectedProperty();
			if (Attribute.DrawTargetPropertyFirst) { CallNextDrawer(label); }
		}

		void DrawInjectedProperty() {
			InjectDrawer.ShouldDraw = true;
			{
				//InjectDrawer.Draw(Attribute.targetProperty);
				try { Attribute.targetProperty.Draw(); }
				catch { Debug.LogError("Error drawing property."); }
			}
			InjectDrawer.ShouldDraw = false;
		}
	}

	// The original drawer of the InjectPropertyAttribute.
	// Skips drawing initially and can drawn on demand with ShouldDraw.
	[DrawerPriority(super: 999)]
	class InjectDrawer : OdinAttributeDrawer<InjectPropertyAttribute> {
		public static bool ShouldDraw;
		public static bool SkipFrame;

		// Validating here because it's impossible to scan down the hierarchy from Property or Attribute Processors.
		protected override void Initialize() {
			string result = Validate(Property, Attribute);
			if (result != "Success") { Debug.LogError(result); }
		}

		protected override void DrawPropertyLayout(GUIContent label) {
			if (!Attribute.IsInitialized) {
				EditorGUILayout.HelpBox($"{Attribute.MemberPath} is not a valid path.", MessageType.Error);
				CallNextDrawer(label);
			}
			else if (SkipFrame) { SkipFrame = false; }
			else if (ShouldDraw) { CallNextDrawer(label); }
		}

		#region Validation
		string Validate(InspectorProperty Property, InjectPropertyAttribute attr) {
			if (!Property.IsReachableFromRoot()) { Debug.LogError($"{Property.Name} is not valid target for {attr}"); }

			string[] hiearchyMembers = GetFullHierarchyPath(Property, attr).Split('.');

			bool parentInitialized = false;
			InspectorProperty p = Property.SerializationRoot;

			foreach (string member in hiearchyMembers) {

				if (!parentInitialized) { if (IsParent(Property, p)) { attr.SetParentProperty(p); parentInitialized = true; } }

				InspectorProperty nextProp = p.Children.Get(member);
				if (nextProp != null) { p = nextProp; }
				else { return $"Could not find {member} in {p.Path}."; }
			}

			if (!parentInitialized) { return $"Parent not found for {Property.Name}."; }

			var newAttr = new InjectHelperAttribute(Property, attr.DrawAboveTarget);
			InjectorHack.ToProcessList.Add(new InjectorHack.InjectorHackHelper() { info = p.Info, attr = newAttr });
			p.Parent.RefreshSetup();

			attr.MarkAsInitialized();
			SkipFrame = true;
			return "Success";
		}


		string GetFullHierarchyPath(InspectorProperty Property, InjectPropertyAttribute attr) {
			string path = attr.MemberPath.TrimStart('@');
			if (Property.Path.Contains(".")) {
				string subStr = Property.Path.Substring(0, Property.Path.LastIndexOf('.'));
				path = subStr + "." + path;
			}
			return path;
		}

		bool IsParent(InspectorProperty Property, InspectorProperty CheckIfParent) {
			foreach (InspectorProperty item in CheckIfParent.Children) {
				if (item.Path == Property.Path) { return true; }
			}
			return false;
		}
		#endregion
	}

}
#endif
