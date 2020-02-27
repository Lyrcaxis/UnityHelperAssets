using System;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;

namespace Sirenix.OdinInspector.CustomProcessors {

	public class OrderRelativeToAttributePropertyProcessor : OdinPropertyProcessor {

		List<InspectorPropertyInfo> _propertiesToBeReordered = new List<InspectorPropertyInfo>();

		public override void ProcessMemberProperties(List<InspectorPropertyInfo> memberInfos) {
			_propertiesToBeReordered.Clear();

			foreach (InspectorPropertyInfo mInfo in memberInfos) {
				if (mInfo.GetAttribute<OrderRelativeToAttribute>() != null) {
					_propertiesToBeReordered.Add(mInfo);
				}
			}

			if (_propertiesToBeReordered.Count == 0) { return; }
			for (int i = 0; i < memberInfos.Count; i++) { memberInfos[i].Order = 10 * i; }

			foreach (InspectorPropertyInfo propInfo in _propertiesToBeReordered) {
				OrderRelativeToAttribute attr = propInfo.GetAttribute<OrderRelativeToAttribute>();
				bool memberFound = false;
				for (int i = 0; i < memberInfos.Count; i++) {
					if (memberInfos[i].PropertyName == attr.Member) {
						propInfo.Order = memberInfos[i].Order + attr.OrderAfterMember;

						PropertyOrderAttribute targetsPropertyOrderAttribute = propInfo.GetAttribute<PropertyOrderAttribute>();
						if (targetsPropertyOrderAttribute != null) {
							propInfo.GetEditableAttributesList().Add(new PropertyOrderAttribute(targetsPropertyOrderAttribute.Order));
						}

						memberFound = true;
						break;
					}
				}

				if (!memberFound) { 
					Debug.LogError($"[{typeof(OrderRelativeToAttribute)}({propInfo.PropertyName})]: Couldn't find member with name {attr.Member}."); 
				}

			}
		}

	}
}
#endif

/// <summary>
/// Orders a property relatively to a member.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
[DontApplyToListElements]
public class OrderRelativeToAttribute : ShowInInspectorAttribute {
	public string Member;
	public int OrderAfterMember;

	/// <summary>
	/// Draws a property after the specified member.
	/// </summary>
	/// <param name="MemberName">The name of the member to which this property will be placed after.</param>
	public OrderRelativeToAttribute(string MemberName) {
		Member = MemberName;
		OrderAfterMember = 1;
	}

	/// <summary>
	/// Draws a property in an order relative to a member.
	/// </summary>
	/// <param name="MemberName">The name of the member to which this property's ordering will be adjusted relatively.</param>
	/// <param name="AdditionalOrder">The relative position of the property (-9 to 9).</param>
	public OrderRelativeToAttribute(string MemberName, int AdditionalOrder) {
		Member = MemberName;
		#if UNITY_EDITOR
		if (Mathf.Abs(AdditionalOrder) > 9) {
			Debug.LogWarning("Max Additional Order for attributes is 9.");
			AdditionalOrder = 9 * (int) Mathf.Sign(AdditionalOrder);
		}
		#endif
		OrderAfterMember = AdditionalOrder;
	}
}
