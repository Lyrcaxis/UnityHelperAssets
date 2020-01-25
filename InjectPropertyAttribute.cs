using System;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Sirenix.OdinInspector.CustomProcessors {

	[ResolverPriority(300)]
	public class InjectPropertyProcessor : OdinPropertyProcessor {


		public static List<InspectorPropertyInfo> injectedProperties = new List<InspectorPropertyInfo>();

		public override void ProcessMemberProperties(List<InspectorPropertyInfo> memberInfos) {

			if (Property.Name == "ROOT") { injectedProperties.Clear(); }

			foreach (InspectorPropertyInfo mInfo in memberInfos) {
				InjectPropertyAttribute attr = mInfo.GetAttribute<InjectPropertyAttribute>();
				if (attr == null) { continue; }

				attr.SetMemberPath(attr.MemberPath.TrimStart('@'));

				if (attr.MemberPath.Contains(".")) {
					injectedProperties.Add(mInfo);
					attr.SetParentProperty(Property);
				}
				else {
					mInfo.GetEditableAttributesList().Add(new OrderRelativeToAttribute(attr.MemberPath));
					mInfo.GetEditableAttributesList().Remove(attr);
				}
			}



			for (int i = injectedProperties.Count - 1; i >= 0; i--) {
				InspectorPropertyInfo propInfo = injectedProperties[i];
				InjectPropertyAttribute attr = propInfo.GetAttribute<InjectPropertyAttribute>();

				bool isValidPath = ValidatePath(attr.MemberPath, out string memberName);
				if (!isValidPath) { continue; }

				for (int j = 0; i < memberInfos.Count; j++) {
					InspectorPropertyInfo mInfo = memberInfos[i];

					if (mInfo.PropertyName == memberName) {
						var newHelperAttr = new InjectPropertyHelperAttribute(propInfo.PropertyName, attr.parentProperty, attr.DrawAboveTarget);
						mInfo.GetEditableAttributesList().Add(newHelperAttr);

						// Check for existing PropertyOrder attribute on the target field.
						PropertyOrderAttribute targetsPropertyOrderAttribute = propInfo.GetAttribute<PropertyOrderAttribute>();
						if (targetsPropertyOrderAttribute != null) {
							var newPropOrderAttr = new PropertyOrderAttribute(targetsPropertyOrderAttribute.Order);
							propInfo.GetEditableAttributesList().Add(newPropOrderAttr);
						}

						attr.MarkAsInitialized();
						// TODO: Remove this and support lists (??)
						injectedProperties.Remove(propInfo);
						break;
					}
				}
			}

		}

		// Climb the path up and see if the property is on the right hierarchy
		bool ValidatePath(string MemberPath, out string memberName) {
			memberName = "";
			string Path = "";

			InspectorProperty _prop = Property;
			for (int i = 0; i < DotAmount(MemberPath); i++) {
				Path = $"{_prop.Name}." + Path;
				_prop = Property.Parent;
				if (_prop == null) { return false; }
			}

			if (!MemberPath.Contains(Path)) { return false; }
			memberName = MemberPath.Replace(Path, "");

			return true;
		}

		// Probably not the most efficient way but meh
		int DotAmount(string MemberPath) {
			int amount = 0;
			foreach (char c in MemberPath) {
				if (c == '.') { amount++; }
			}
			return amount;
		}

	}
	[DrawerPriority(DrawerPriorityLevel.SuperPriority)]
	public class InjectPropertyDrawerAttributeDrawer : OdinAttributeDrawer<InjectPropertyAttribute> {
		public static bool ShouldDraw;

		protected override void Initialize() {
			var attr = Attribute;
			var hiearchyMembers = attr.MemberPath.Split('.');

			var p = attr.parentProperty;
			foreach (var member in hiearchyMembers) {
				var nextProp = p.Children.Get(member);
				if (nextProp != null) { p = nextProp; }
				else {
					Debug.LogError($"Could not find {member} in {p.Path} ");
					return;
				}
			}
		}


		protected override void DrawPropertyLayout(GUIContent label) {
			if (!Attribute.IsInitialized) {
				EditorGUILayout.HelpBox($"{Attribute.MemberPath} is not a valid path.", MessageType.Error);
				CallNextDrawer(label);
			}
			else if (ShouldDraw) { CallNextDrawer(label); }
		}
	}

	[DrawerPriority(DrawerPriorityLevel.SuperPriority)]
	public class InjectPropertyHelperAttributeDrawer : OdinAttributeDrawer<InjectPropertyHelperAttribute> {

		protected override void DrawPropertyLayout(GUIContent label) {

			if (!Attribute.DrawTargetBefore) { CallNextDrawer(label); }

			DrawInjectedProperty();

			if (Attribute.DrawTargetBefore) { CallNextDrawer(label); }

		}

		void DrawInjectedProperty() {
			InjectPropertyDrawerAttributeDrawer.ShouldDraw = true;
			{
				InspectorProperty Prop = Attribute.parentProperty.Children.Get(Attribute.Member);
				Prop.Draw();
			}
			InjectPropertyDrawerAttributeDrawer.ShouldDraw = false;
		}
	}

	public class InjectPropertyHelperAttribute : ShowInInspectorAttribute {
		public InspectorProperty parentProperty;
		public bool DrawTargetBefore;
		public string Member;

		public InjectPropertyHelperAttribute(string MemberName, InspectorProperty prop, bool ShouldDrawBefore) {
			Member = MemberName;
			parentProperty = prop;
			DrawTargetBefore = ShouldDrawBefore;
		}
	}

}
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
