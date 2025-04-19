using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Generators;
using ScaffoldKit.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScaffoldKit.Editor.Window
{
	public class ScaffoldWindow : EditorWindow
	{
		private static readonly Regex PlaceholderRegex = new(@"\{\{(.+?)\}\}", RegexOptions.Compiled);
		private static ScaffoldWindow _window;
		private Button _backButton;
		private readonly Dictionary<string, Toggle> _booleanInputFields = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, DropdownField> _enumInputFields = new(StringComparer.OrdinalIgnoreCase);
		private Button _generateButton;
		private Button _loadTemplateButton;
		private VisualElement _page1Container;
		private VisualElement _page2Container;
		private VisualElement _placeholderFieldsContainer;
		private Button _refreshButton;
		private VisualElement _root;
		private ScaffoldLoader _scaffoldLoader;
		private ScaffoldFile _selectedTemplateFile;

		// Dictionaries for different input field types
		private readonly Dictionary<string, TextField> _stringInputFields = new(StringComparer.OrdinalIgnoreCase);
		private DropdownField _templateDropdown;
		private Label _validationLabel;

		public void CreateGUI()
		{
			_root = rootVisualElement;
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.nonatomic.scaffoldkit/Editor/Window/ScaffoldWindowStyle.uss");
			if (styleSheet != null)
			{
				_root.styleSheets.Add(styleSheet);
			}
			else
			{
				Debug.LogWarning("[ScaffoldWindow] Stylesheet not found.");
			}

			_scaffoldLoader = new();
			_scaffoldLoader.FindAndLoadAllScaffoldFiles();
			_page1Container = new() { name = "Page1" };
			_page1Container.AddToClassList("page-container");
			_root.Add(_page1Container);
			_page2Container = new() { name = "Page2" };
			_page2Container.AddToClassList("page-container");
			_root.Add(_page2Container);
			BuildPage1();
			BuildPage2();
			ShowPage(1);
			UpdateTemplateDropdown();
		}

		[MenuItem("Assets/Create/ScaffoldKit/Generate Scaffold", false, 100)]
		public static void ShowEditor()
		{
			_window = GetWindow<ScaffoldWindow>();
			_window.titleContent = new("Scaffold Generator");
			_window.minSize = new(450, 400);
		}

		private void BuildPage1()
		{
			_page1Container.Clear();
			
			var logoImg = new Image { image = Resources.Load<Texture2D>("logo") };
			logoImg.AddToClassList("logo");
			_page1Container.Add(logoImg);
			
			AddLineBreak(_page1Container);
			
			var templateSection = new VisualElement();
			templateSection.AddToClassList("section");
			_page1Container.Add(templateSection);
			
			var templateLabel = new Label("Select Template:");
			templateLabel.AddToClassList("section-label");
			templateSection.Add(templateLabel);
			
			_templateDropdown = new();
			_templateDropdown.AddToClassList("template-dropdown");
			templateSection.Add(_templateDropdown);
			
			var buttonContainer = new VisualElement();
			buttonContainer.AddToClassList("button-container");
			_page1Container.Add(buttonContainer);
			
			_loadTemplateButton = new(OnLoadTemplateClicked) { text = "Load Template" };
			_loadTemplateButton.AddToClassList("primary-btn");
			buttonContainer.Add(_loadTemplateButton);
			
			_refreshButton = new(RefreshTemplates);
			_refreshButton.AddToClassList("refresh-btn");
			buttonContainer.Add(_refreshButton);
			
			var refreshIcon = new Image { image = Resources.Load<Texture2D>("Icons/refresh") };
			refreshIcon.AddToClassList("button-icon");
			_refreshButton.Add(refreshIcon);
		}

		private void BuildPage2()
		{
			_page2Container.Clear();
			
			var logoImg = new Image { image = Resources.Load<Texture2D>("logo") };
			logoImg.AddToClassList("logo");
			_page2Container.Add(logoImg);
			
			AddLineBreak(_page2Container);
			
			var placeholderSection = new VisualElement();
			placeholderSection.AddToClassList("section");
			_page2Container.Add(placeholderSection);
			
			var placeholderLabel = new Label("Configure Template Placeholders:");
			placeholderLabel.AddToClassList("section-label");
			placeholderSection.Add(placeholderLabel);
			
			_validationLabel = new();
			_validationLabel.AddToClassList("validation-label");
			_validationLabel.style.display = DisplayStyle.None;
			placeholderSection.Add(_validationLabel);
			
			var scrollView = new ScrollView(ScrollViewMode.Vertical);
			scrollView.AddToClassList("placeholder-scrollview");
			placeholderSection.Add(scrollView);
			
			_placeholderFieldsContainer = new() { name = "PlaceholderFields" };
			_placeholderFieldsContainer.AddToClassList("placeholder-fields-container");
			scrollView.Add(_placeholderFieldsContainer);
			
			var buttonContainer2 = new VisualElement();
			buttonContainer2.AddToClassList("button-container");
			_page2Container.Add(buttonContainer2);
			
			_backButton = new(OnBackClicked);
			_backButton.AddToClassList("secondary-btn");
			buttonContainer2.Add(_backButton);
			
			var backIcon = new Image { image = Resources.Load<Texture2D>("Icons/back") };
			backIcon.AddToClassList("button-icon");
			_backButton.Add(backIcon);
			
			_generateButton = new(GenerateStructure) { text = "Generate Structure" };
			_generateButton.AddToClassList("primary-btn");
			buttonContainer2.Add(_generateButton);
		}

		private void ShowPage(int pageNumber)
		{
			_page1Container.style.display = pageNumber == 1 ? DisplayStyle.Flex : DisplayStyle.None;
			_page2Container.style.display = pageNumber == 2 ? DisplayStyle.Flex : DisplayStyle.None;
			
			if (_validationLabel != null)
			{
				_validationLabel.text = "";
				_validationLabel.style.display = DisplayStyle.None;
			}

			_root.MarkDirtyRepaint();
		}

		private void UpdateTemplateDropdown()
		{
			if (_templateDropdown == null || _scaffoldLoader == null)
			{
				return;
			}

			var templateChoices = _scaffoldLoader.LoadedTemplates
				.Where(t => t?.TemplateData != null && !string.IsNullOrEmpty(t.TemplateData.TemplateName))
				.Select(t => t.TemplateData.TemplateName).OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
				.ToList();
			var hasValidTemplates = templateChoices.Any();
			if (!hasValidTemplates)
			{
				templateChoices.Insert(0, "No valid templates found in project");
				_templateDropdown.choices = templateChoices;
				_templateDropdown.index = 0;
				_templateDropdown.SetEnabled(false);
				_loadTemplateButton?.SetEnabled(false);
				_selectedTemplateFile = null;
			}
			else
			{
				var currentSelection = _selectedTemplateFile?.TemplateData?.TemplateName;
				_templateDropdown.choices = templateChoices;
				_templateDropdown.SetEnabled(true);
				_loadTemplateButton?.SetEnabled(true);
				
				var selectionIndex = -1;
				if (!string.IsNullOrEmpty(currentSelection))
				{
					selectionIndex = templateChoices.IndexOf(currentSelection);
				}

				_templateDropdown.index = selectionIndex >= 0 ? selectionIndex : 0;
				if (_templateDropdown.index >= 0 && _templateDropdown.index < templateChoices.Count)
				{
					_selectedTemplateFile = _scaffoldLoader.GetTemplateByName(templateChoices[_templateDropdown.index]);
				}
				else
				{
					_selectedTemplateFile = null;
				}

				_templateDropdown.UnregisterValueChangedCallback(OnTemplateSelectionChanged);
				_templateDropdown.RegisterValueChangedCallback(OnTemplateSelectionChanged);
			}
		}

		private void OnTemplateSelectionChanged(ChangeEvent<string> evt)
		{
			_selectedTemplateFile = _scaffoldLoader.GetTemplateByName(evt.newValue);
		}

		private void RefreshTemplates()
		{
			_scaffoldLoader.ReloadAllTemplates();
			UpdateTemplateDropdown();
			ShowMessage("Templates refreshed.", false);
		}


		/// <summary>
		///     Populates the UI with input fields based on the template's placeholder definitions.
		///     Creates different controls based on PlaceholderDefinition.Type.
		/// </summary>
		private void PopulatePlaceholderFields()
		{
			// Clear previous fields and mappings
			_placeholderFieldsContainer.Clear();
			_stringInputFields.Clear();
			_booleanInputFields.Clear();
			_enumInputFields.Clear();

			if (_selectedTemplateFile?.TemplateData?.PlaceholderDefinitions == null) return;
			
			var definitions = _selectedTemplateFile.TemplateData.PlaceholderDefinitions;
			if (definitions.Count == 0) return;
			
			definitions.Sort((a, b) => a.Order.CompareTo(b.Order));

			// Resolve default values (simple substitution pass)
			var defaultValues = definitions.ToDictionary(d => $"{{{{{d.Key}}}}}", d => d.DefaultValue ?? "", StringComparer.OrdinalIgnoreCase);
			var resolvedDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var def in definitions)
			{
				var placeholderKey = $"{{{{{def.Key}}}}}";
				var resolvedValue = PlaceholderUtils.ApplyPlaceholders(def.DefaultValue ?? "", defaultValues);
				resolvedDefaults[placeholderKey] = resolvedValue;
			}

			// Create UI fields based on type
			foreach (var definition in definitions)
			{
				if (string.IsNullOrWhiteSpace(definition.Key) || string.IsNullOrWhiteSpace(definition.Label)) continue;
				
				var fieldContainer = new VisualElement();
				fieldContainer.AddToClassList("field-container");
				if (!string.IsNullOrWhiteSpace(definition.Description))
				{
					fieldContainer.tooltip = definition.Description;
				}

				// Switch based on placeholder type
				switch (definition.Type)
				{
					case PlaceholderType.Boolean:
						var toggle = new Toggle(definition.Label + ":")
						{
							name = definition.Key,
							value = definition.GetDefaultBoolean()
						};
						toggle.AddToClassList("placeholder-toggle");
						fieldContainer.Add(toggle);
						_booleanInputFields.Add(definition.Key, toggle);
						break;

					case PlaceholderType.Enum:
						var dropdownLabel = new Label(definition.Label + ":");
						dropdownLabel.AddToClassList("field-label");
						fieldContainer.Add(dropdownLabel);

						var dropdown = new DropdownField
						{
							name = definition.Key,
							choices = definition.Options ?? new List<string>(),
							value = definition.GetValidatedEnumDefault()
						};
						dropdown.AddToClassList("placeholder-dropdown");

						// Set index correctly after ensuring choices and value are valid
						if (dropdown.choices.Any() && dropdown.value != null &&
							dropdown.choices.Contains(dropdown.value))
						{
							dropdown.index = dropdown.choices.IndexOf(dropdown.value);
						}
						else if (dropdown.choices.Any())
						{
							// If default wasn't valid or null, but options exist, select the first
							dropdown.index = 0;
							dropdown.value = dropdown.choices[0];
						}
						else
						{
							// No options provided, disable dropdown
							dropdown.index = -1;
							dropdown.value = "Error: No options defined";
							dropdown.SetEnabled(false);
						}

						fieldContainer.Add(dropdown);
						_enumInputFields.Add(definition.Key, dropdown);
						break;

					case PlaceholderType.String:
					default:
						var stringLabel = new Label(definition.Label + ":");
						stringLabel.AddToClassList("field-label");
						fieldContainer.Add(stringLabel);

						var textField = new TextField
						{
							name = definition.Key,
							value = resolvedDefaults.GetValueOrDefault($"{{{{{definition.Key}}}}}", "")
						};
						textField.AddToClassList("text-field");
						fieldContainer.Add(textField);
						_stringInputFields.Add(definition.Key, textField);
						break;
				}

				_placeholderFieldsContainer.Add(fieldContainer);
			}
		}
		
		private HashSet<string> FindUsedPlaceholdersInTemplate()
		{
			var usedPlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_selectedTemplateFile?.TemplateData == null)
			{
				return usedPlaceholders;
			}

			Action<string> scanString = input =>
			{
				if (string.IsNullOrEmpty(input))
				{
					return;
				}

				var matches = PlaceholderRegex.Matches(input);
				foreach (Match match in matches)
				{
					if (!match.Success || match.Groups.Count <= 1) continue;
				
					var key = match.Groups[1].Value.Trim();
					if (!string.IsNullOrWhiteSpace(key))
					{
						usedPlaceholders.Add(key);
					}
				}
			};
			
			Action<JToken> scanJToken = null;
			scanJToken = token =>
			{
				if (token == null)
				{
					return;
				}

				switch (token.Type)
				{
					case JTokenType.String: scanString(token.Value<string>()); break;
					case JTokenType.Object:
						foreach (var prop in ((JObject)token).Properties())
						{
							scanString(prop.Name);
							scanJToken(prop.Value);
						}

						break;
					case JTokenType.Array:
						foreach (var item in (JArray)token)
						{
							scanJToken(item);
						}

						break;
				}
			};
			
			Action<DirectoryData> scanDirectory = null;
			scanDirectory = dirData =>
			{
				if (dirData == null) return;
				
				scanString(dirData.Name);
				if (dirData.Files != null)
				{
					foreach (var file in dirData.Files)
					{
						scanString(file.Name);
						scanString(file.Extension);
						scanJToken(file.Content);
					}
				}

				if (dirData.SubDirectories == null) return;
				
				foreach (var subDir in dirData.SubDirectories)
				{
					scanDirectory(subDir);
				}
			};
			
			var rootData = _selectedTemplateFile.TemplateData;
			if (rootData.Files != null)
			{
				foreach (var file in rootData.Files)
				{
					scanString(file.Name);
					scanString(file.Extension);
					scanJToken(file.Content);
				}
			}

			if (rootData.SubDirectories != null)
			{
				foreach (var subDir in rootData.SubDirectories)
				{
					scanDirectory(subDir);
				}
			}

			return usedPlaceholders;
		}

		private bool ValidatePlaceholders()
		{
			if (_selectedTemplateFile?.TemplateData == null) return false;
			
			var definedKeys = new HashSet<string>(_selectedTemplateFile.TemplateData.PlaceholderDefinitions
				.Select(d => d.Key)
				.Where(k => !string.IsNullOrWhiteSpace(k)), StringComparer.OrdinalIgnoreCase);
			
			var usedKeys = FindUsedPlaceholdersInTemplate();
			var errors = new List<string>();
			var warnings = new List<string>();
			
			foreach (var usedKey in usedKeys)
			{
				if (!definedKeys.Contains(usedKey))
				{
					errors.Add($"Placeholder `{{{{{usedKey}}}}}` is used but not defined.");
				}
			}

			foreach (var definedKey in definedKeys)
			{
				if (!usedKeys.Contains(definedKey))
				{
					warnings.Add($"Placeholder `{definedKey}` is defined but not used.");
				}
			}

			var message = new StringBuilder();
			if (errors.Any())
			{
				message.AppendLine("Errors:");
				errors.ForEach(e => message.AppendLine($"- {e}"));
				if (warnings.Any())
				{
					message.AppendLine();
				}
			}

			if (warnings.Any())
			{
				message.AppendLine("Warnings:");
				warnings.ForEach(w => message.AppendLine($"- {w}"));
			}

			if (message.Length > 0)
			{
				_validationLabel.text = message.ToString();
				_validationLabel.style.display = DisplayStyle.Flex;
				_validationLabel.EnableInClassList("error", errors.Any());
				_validationLabel.EnableInClassList("warning", warnings.Any() && !errors.Any());
				return !errors.Any();
			}

			_validationLabel.text = "";
			_validationLabel.style.display = DisplayStyle.None;
			_validationLabel.EnableInClassList("error", false);
			_validationLabel.EnableInClassList("warning", false);
			return true;
		}

		private void OnLoadTemplateClicked()
		{
			if (_selectedTemplateFile == null)
			{
				EditorUtility.DisplayDialog("Error", "Please select a valid template.", "OK");
				return;
			}

			if (_selectedTemplateFile.TemplateData == null)
			{
				EditorUtility.DisplayDialog("Error", $"Failed to parse template data for '{_selectedTemplateFile.FileName}'.", "OK");
				return;
			}

			PopulatePlaceholderFields();
			ValidatePlaceholders();
			ShowPage(2);
		}

		private void OnBackClicked()
		{
			ShowPage(1);
			_stringInputFields.Clear();
			_booleanInputFields.Clear();
			_enumInputFields.Clear();
			_placeholderFieldsContainer.Clear();
			UpdateTemplateDropdown();
		}

		/// <summary>
		///     Collects values from all dynamic input fields (Text, Toggle, Dropdown)
		///     and initiates the scaffold generation process.
		/// </summary>
		private void GenerateStructure()
		{
			if (_selectedTemplateFile?.TemplateData == null)
			{
				EditorUtility.DisplayDialog("Error", "No template loaded.", "OK");
				ShowPage(1);
				return;
			}

			if (!ValidatePlaceholders())
			{
				EditorUtility.DisplayDialog("Validation Error", "Placeholder validation failed.", "OK");
				return;
			}

			var placeholderValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			// Collect String values
			foreach (var kvp in _stringInputFields)
			{
				placeholderValues[$"{{{{{kvp.Key}}}}}"] = kvp.Value.value ?? "";
			}

			// Collect Bool values
			foreach (var kvp in _booleanInputFields)
			{
				placeholderValues[$"{{{{{kvp.Key}}}}}"] = kvp.Value.value.ToString().ToLowerInvariant();
			}

			// Collect Enum values (selected string)
			foreach (var kvp in _enumInputFields)
			{
				placeholderValues[$"{{{{{kvp.Key}}}}}"] = kvp.Value.value ?? ""; // Use the selected string value
			}

			// Get target path
			var selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);

			try
			{
				ScaffoldGenerator.Generate(_selectedTemplateFile, selectionPath, placeholderValues);
				Debug.Log(
					$"Scaffold generation initiated for template: '{_selectedTemplateFile.TemplateData.TemplateName}'.");
				ShowMessage("Generation successful!", false);
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error during Scaffold generation: {ex.Message}\n{ex.StackTrace}");
				EditorUtility.DisplayDialog("Generation Error", $"An error occurred during generation:\n\n{ex.Message}",
					"OK");
			}
		}
		
		private void AddLineBreak(VisualElement container)
		{
			var lb = new VisualElement();
			lb.AddToClassList("line-break");
			container.Add(lb);
		}

		private void ShowMessage(string message, bool isError)
		{
			Debug.Log($"[ScaffoldWindow] {(isError ? "Error" : "Info")}: {message}");
			if (_validationLabel == null) return;

			_validationLabel.text = message;
			_validationLabel.EnableInClassList("error", isError);
			_validationLabel.EnableInClassList("warning", false);
			_validationLabel.style.display = DisplayStyle.Flex;
		}
	}
}