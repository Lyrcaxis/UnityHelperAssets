#if UNITY_EDITOR
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Utilities {

	public abstract class ScriptableObjectCreator<T> : SerializedScriptableObject where T : ScriptableObject, new() {

		protected abstract string SavePath { get; }
		protected abstract void AdditionalInitialization(T obj);
		protected abstract bool AdditionalValidation();
		protected abstract void AdditionalReset();

		[ShowInInspector, PropertyOrder(0)]
		public string Name { get; set; }

		T _item;
		[ShowInInspector, PropertyOrder(1), InlineEditor, HideReferenceObjectPicker]
		public T item {
			get => _item;
			set { }
		}

		void OnEnable() {
			AdditionalReset();
			_item = CreateInstance<T>();
			_item.name = "New";
			Name = "";
		}


		[Button, PropertyOrder(99)]
		protected void CreateAsset() {

			if (!AdditionalValidation()) { return; }
			string path = "";

			if (!SavePath.StartsWith("Assets/")) { path += "Assets/"; }

			path += SavePath;
			if (!SavePath.EndsWith("/")) { path += "/"; }
			path += Name;
			path += ".asset";

			Debug.Log($"Create {Name} at: {path}...");
			if (!IsInputValid(path)) { return; }

			var obj = item;
			AdditionalInitialization(obj);

			AssetDatabase.CreateAsset(obj, path);
			EditorUtility.SetDirty(obj);

			Debug.Log($"{Name} successfully created as {path}");

			LastCreatedAsset = obj;
			OnEnable();
		}

		[SerializeField, HideInInspector] T LastCreatedAsset;

		[Button, PropertyOrder(100), PropertySpace]
		void GoToLastCreatedAsset() {
			if (LastCreatedAsset != null) {
				ProjectWindowUtil.ShowCreatedAsset(LastCreatedAsset);
			}
			else { Debug.Log("Last created asset cannot be found."); }
		}

		#region Path Validation Logic
		bool IsInputValid(string path) {
			if (!IsStringValid(SavePath, Path.GetInvalidPathChars())) {
				Debug.Log("Wrong Save Path");
				return false;
			}
			if (!IsStringValid(Name, Path.GetInvalidFileNameChars())) {
				Debug.Log("Wrong Name");
				return false;
			}
			if (!AssetDatabase.IsValidFolder(SavePath)) {
				Debug.Log("Wrong Folder");
				return false;
			}
			if (UnityEngine.Windows.File.Exists(path)) {
				Debug.Log("File already exists");
				return false;
			}

			return true;
		}

		bool IsStringValid(string s, char[] invalidCharacters) {
			if (String.IsNullOrWhiteSpace(s)) {return false; }
			
			foreach (var character in invalidCharacters) {
				if (s.Contains(character.ToString())) {
					Debug.Log(character);
					return false;
				}
			}

			if (s.Contains(".")) { return false; }

			return true;
		}
		#endregion
	}

}
#endif
