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

		private readonly Dictionary<string, TextField> _dynamicInputFields = new(StringComparer.OrdinalIgnoreCase);
		private Button _backButton;
		private Button _generateButton;
		private Button _loadTemplateButton;
		private VisualElement _page1Container;
		private VisualElement _page2Container; 
		private VisualElement _placeholderFieldsContainer;
		private Button _refreshButton;
		private VisualElement _root;
		private ScaffoldLoader _scaffoldLoader;
		private ScaffoldFile _selectedTemplateFile;
		private DropdownField _templateDropdown;
		private Label _validationLabel;

		public void CreateGUI()
		{
			_root = rootVisualElement;

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
					"Packages/com.nonatomic.scaffoldkit/Editor/Window/ScaffoldWindowStyle.uss");
			
			if (styleSheet != null)
			{
				_root.styleSheets.Add(styleSheet);
			}
			else
			{
				Debug.LogWarning("[ScaffoldWindow] Stylesheet not found at expected path. Using default styles.");
			}

			_scaffoldLoader = new();
			_scaffoldLoader.FindAndLoadAllScaffoldFiles();

			// Create Page Containers
			_page1Container = new() { name = "Page1" };
			_page1Container.AddToClassList("page-container");
			_root.Add(_page1Container);

			_page2Container = new() { name = "Page2" };
			_page2Container.AddToClassList("page-container");
			_root.Add(_page2Container);

			// Build UI Pages
			BuildPage1();
			BuildPage2();

			// Initial State
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

		// Page 1: Template Selection
		private void BuildPage1()
		{
			_page1Container.Clear();

			// Logo
			var logoImg = new Image { image = Resources.Load<Texture2D>("logo") };
			logoImg.AddToClassList("logo");
			_page1Container.Add(logoImg);

			AddLineBreak(_page1Container);

			// Template Selection Section
			var templateSection = new VisualElement();
			templateSection.AddToClassList("section");
			_page1Container.Add(templateSection);

			var templateLabel = new Label("Select Template:");
			templateLabel.AddToClassList("section-label");
			templateSection.Add(templateLabel);

			_templateDropdown = new();
			_templateDropdown.AddToClassList("template-dropdown");
			templateSection.Add(_templateDropdown);

			// Button Container
			var buttonContainer = new VisualElement();
			buttonContainer.AddToClassList("button-container");
			_page1Container.Add(buttonContainer);

			// Load Button
			_loadTemplateButton = new(OnLoadTemplateClicked) { text = "Load Template" };
			_loadTemplateButton.AddToClassList("primary-btn");
			buttonContainer.Add(_loadTemplateButton);

			// Refresh Button
			_refreshButton = new(RefreshTemplates);
			_refreshButton.AddToClassList("refresh-btn");
			buttonContainer.Add(_refreshButton);
			var refreshIcon = new Image { image = Resources.Load<Texture2D>("Icons/refresh") };
			refreshIcon.AddToClassList("button-icon");
			_refreshButton.Add(refreshIcon);
		}

		// Page 2: Placeholder Input
		private void BuildPage2()
		{
			_page2Container.Clear();

			// Logo
			var logoImg = new Image { image = Resources.Load<Texture2D>("logo") };
			logoImg.AddToClassList("logo");
			_page2Container.Add(logoImg);

			AddLineBreak(_page2Container);

			// Placeholder Section
			var placeholderSection = new VisualElement();
			placeholderSection.AddToClassList("section");
			_page2Container.Add(placeholderSection);

			var placeholderLabel = new Label("Configure Template Placeholders:");
			placeholderLabel.AddToClassList("section-label");
			placeholderSection.Add(placeholderLabel);

			// Validation Message Area
			_validationLabel = new();
			_validationLabel.AddToClassList("validation-label");
			_validationLabel.style.display = DisplayStyle.None;
			placeholderSection.Add(_validationLabel);


			// ScrollView for Placeholder Fields
			var scrollView = new ScrollView(ScrollViewMode.Vertical);
			scrollView.AddToClassList("placeholder-scrollview");
			placeholderSection.Add(scrollView);

			_placeholderFieldsContainer = new() { name = "PlaceholderFields" };
			_placeholderFieldsContainer.AddToClassList("placeholder-fields-container");
			scrollView.Add(_placeholderFieldsContainer);

			// Button Container
			var buttonContainer2 = new VisualElement();
			buttonContainer2.AddToClassList("button-container");
			_page2Container.Add(buttonContainer2);

			// Back Button
			_backButton = new(OnBackClicked);
			_backButton.AddToClassList("secondary-btn"); 
			buttonContainer2.Add(_backButton);
			var backIcon = new Image { image = Resources.Load<Texture2D>("Icons/back") };
			backIcon.AddToClassList("button-icon");
			_backButton.Add(backIcon);

			// Generate Button
			_generateButton = new(GenerateStructure) { text = "Generate Structure" };
			_generateButton.AddToClassList("primary-btn");
			buttonContainer2.Add(_generateButton);
		}

		private void ShowPage(int pageNumber)
		{
			_page1Container.style.display = pageNumber == 1 ? DisplayStyle.Flex : DisplayStyle.None;
			_page2Container.style.display = pageNumber == 2 ? DisplayStyle.Flex : DisplayStyle.None;
			
			// Clear validation messages when switching pages
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

			// Get valid template names
			var templateChoices = _scaffoldLoader.LoadedTemplates
				.Where(t => t?.TemplateData != null && !string.IsNullOrEmpty(t.TemplateData.TemplateName))
				.Select(t => t.TemplateData.TemplateName)
				.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
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

				// Set index or default to first item
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

		// Callback when dropdown selection changes
		private void OnTemplateSelectionChanged(ChangeEvent<string> evt)
		{
			_selectedTemplateFile = _scaffoldLoader.GetTemplateByName(evt.newValue);
		}

		private void RefreshTemplates()
		{
			_scaffoldLoader.ReloadAllTemplates();
			UpdateTemplateDropdown(); // This will repopulate and reset the dropdown
			ShowMessage("Templates refreshed.", false); // Show feedback
		}
		
		/// <summary>
		///     Populates the UI with input fields based on the template's placeholder definitions.
		/// </summary>
		private void PopulatePlaceholderFields()
		{
			_placeholderFieldsContainer.Clear(); // Clear previous fields
			_dynamicInputFields.Clear(); // Clear mapping

			if (_selectedTemplateFile?.TemplateData?.PlaceholderDefinitions == null)
			{
				ShowMessage("Template has no placeholder definitions.", true);
				return;
			}

			var definitions = _selectedTemplateFile.TemplateData.PlaceholderDefinitions;

			if (definitions.Count == 0)
			{
				ShowMessage("No placeholders defined in this template.", false); // Informational, not an error
				return;
			}

			// Sort definitions by the 'Order' property
			definitions.Sort((a, b) => a.Order.CompareTo(b.Order));

			// Create a temporary dictionary to resolve default values that reference others
			var defaultValues = definitions.ToDictionary(d =>
			{
				return $"{{{{{d.Key}}}}}";
			}, d => d.DefaultValue ?? "", StringComparer.OrdinalIgnoreCase);
			var resolvedDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			// Simple substitution pass for defaults (can be improved for multi-level dependencies)
			foreach (var def in definitions)
			{
				var placeholderKey = $"{{{{{def.Key}}}}}";
				var resolvedValue = def.DefaultValue ?? "";
				resolvedValue = PlaceholderUtils.ApplyPlaceholders(resolvedValue, defaultValues);
				resolvedDefaults[placeholderKey] = resolvedValue;
			}

			// Create UI fields
			foreach (var definition in definitions)
			{
				if (string.IsNullOrWhiteSpace(definition.Key) || string.IsNullOrWhiteSpace(definition.Label))
				{
					Debug.LogWarning($"Skipping placeholder definition with missing Key or Label in template '{_selectedTemplateFile.FileName}'.");
					continue;
				}

				var fieldContainer = new VisualElement();
				fieldContainer.AddToClassList("field-container");

				// Label with potential help icon/tooltip
				var label = new Label(definition.Label + ":");
				label.AddToClassList("field-label");
				if (!string.IsNullOrWhiteSpace(definition.Description))
				{
					label.tooltip = definition.Description;
				}

				fieldContainer.Add(label);

				// Input Field
				var textField = new TextField
				{
					name = definition.Key,
					value = resolvedDefaults.GetValueOrDefault($"{{{{{definition.Key}}}}}", "")
				};
				textField.AddToClassList("text-field");
				fieldContainer.Add(textField);

				_placeholderFieldsContainer.Add(fieldContainer);
				_dynamicInputFields.Add(definition.Key, textField);
			}
		}

		/// <summary>
		///     Scans the template structure (directories, files, content) to find all used placeholders `{{KEY}}`.
		/// </summary>
		/// <returns>A HashSet containing the keys (e.g., "NAMESPACE") of all used placeholders.</returns>
		private HashSet<string> FindUsedPlaceholdersInTemplate()
		{
			var usedPlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (_selectedTemplateFile?.TemplateData == null)
			{
				return usedPlaceholders;
			}

			Action<string> scanString = input =>
			{
				if (string.IsNullOrEmpty(input)) return;
			
				var matches = PlaceholderRegex.Matches(input);
				foreach (Match match in matches)
				{
					if (match.Success && match.Groups.Count > 1)
					{
						var key = match.Groups[1].Value.Trim();
						if (!string.IsNullOrWhiteSpace(key))
						{
							usedPlaceholders.Add(key);
						}
					}
				}
			};

			Action<JToken> scanJToken = null;
			scanJToken = token =>
			{
				if (token == null) return;
		
				switch (token.Type)
				{
					case JTokenType.String:
						scanString(token.Value<string>());
						break;
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
				if (dirData == null)
				{
					return;
				}

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
			
			// Scan root files
			if (rootData.Files != null)
			{
				foreach (var file in rootData.Files)
				{
					scanString(file.Name);
					scanString(file.Extension);
					scanJToken(file.Content);
				}
			}

			// Scan root directories
			if (rootData.SubDirectories != null)
			{
				foreach (var subDir in rootData.SubDirectories)
				{
					scanDirectory(subDir);
				}
			}

			return usedPlaceholders;
		}


		/// <summary>
		///     Validates the selected template's placeholder definitions against actual usage.
		/// </summary>
		/// <returns>True if validation passes, false otherwise. Updates _validationLabel.</returns>
		private bool ValidatePlaceholders()
		{
			if (_selectedTemplateFile?.TemplateData == null)
			{
				return false;
			}

			var definedKeys = new HashSet<string>(
				_selectedTemplateFile.TemplateData.PlaceholderDefinitions
					.Select(d => d.Key)
					.Where(k => !string.IsNullOrWhiteSpace(k)),
				StringComparer.OrdinalIgnoreCase
			);

			var usedKeys = FindUsedPlaceholdersInTemplate();

			var errors = new List<string>();
			var warnings = new List<string>();

			// Check for used placeholders that are not defined
			foreach (var usedKey in usedKeys)
			{
				if (!definedKeys.Contains(usedKey))
				{
					errors.Add($"Placeholder `{{{{{usedKey}}}}}` is used in the template structure but not defined in `placeholderDefinitions`.");
				}
			}

			// Check for defined placeholders that are not used
			foreach (var definedKey in definedKeys)
			{
				if (!usedKeys.Contains(definedKey))
				{
					warnings.Add($"Placeholder `{definedKey}` is defined in `placeholderDefinitions` but not used anywhere in the template structure.");
				}
			}

			// Display Validation Results
			var message = new StringBuilder();
			if (errors.Any())
			{
				message.AppendLine("Errors found:");
				errors.ForEach(e => message.AppendLine($"- {e}"));
				if (warnings.Any())
				{
					message.AppendLine();
				}
			}

			if (warnings.Any())
			{
				message.AppendLine("Warnings found:");
				warnings.ForEach(w => message.AppendLine($"- {w}"));
			}

			if (message.Length > 0)
			{
				_validationLabel.text = message.ToString();
				_validationLabel.style.display = DisplayStyle.Flex;
				_validationLabel.EnableInClassList("error", errors.Any());
				_validationLabel.EnableInClassList("warning", warnings.Any() && !errors.Any());
				return false;
			}

			_validationLabel.text = "Validation successful.";
			_validationLabel.style.display = DisplayStyle.None;
			_validationLabel.EnableInClassList("error", false);
			_validationLabel.EnableInClassList("warning", false);
			return true;
		}
		
		private void OnLoadTemplateClicked()
		{
			if (_selectedTemplateFile == null)
			{
				EditorUtility.DisplayDialog("Error", "Please select a valid template from the dropdown.", "OK");
				return;
			}

			// Check if template data loaded correctly
			if (_selectedTemplateFile.TemplateData == null)
			{
				EditorUtility.DisplayDialog("Error", 
					$"Failed to parse template data for '{_selectedTemplateFile.FileName}'. Check console for errors.",
					"OK");
				
				_scaffoldLoader.LoadScaffoldFile(_selectedTemplateFile.FilePath); 
				return;
			}

			PopulatePlaceholderFields();
			if (!ValidatePlaceholders())
			{
				// Optionally keep user on page 2 but show errors, or force back to page 1
				// For now, let's proceed to page 2 but show the validation messages.
				Debug.LogError($"Placeholder validation failed for template '{_selectedTemplateFile.TemplateData.TemplateName}'. See details in the Scaffold Generator window.");
			}

			ShowPage(2);
		}

		private void OnBackClicked()
		{
			ShowPage(1);
			_dynamicInputFields.Clear();
			_placeholderFieldsContainer.Clear();
			_selectedTemplateFile = null;
			UpdateTemplateDropdown();
		}

		private void GenerateStructure()
		{
			if (_selectedTemplateFile == null || _selectedTemplateFile.TemplateData == null)
			{
				EditorUtility.DisplayDialog("Error", "No template loaded or template data is invalid.", "OK");
				ShowPage(1);
				return;
			}

			// Re-validate before generating
			if (!ValidatePlaceholders())
			{
				EditorUtility.DisplayDialog("Validation Error", "Placeholder validation failed. Please check the messages in the window and the template file.", "OK");
				return;
			}


			// Collect placeholder values from input fields
			var placeholderValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var missingValues = new List<string>();

			foreach (var definition in _selectedTemplateFile.TemplateData.PlaceholderDefinitions)
			{
				var key = definition.Key;
				if (_dynamicInputFields.TryGetValue(key, out var textField))
				{
					var value = textField.value;
					// Basic validation: Check if required fields (e.g., those without defaults?) are empty
					// More complex validation (regex, etc.) could be added here based on definition properties
					if (string.IsNullOrWhiteSpace(value))
					{
						// Consider if empty is allowed based on definition? For now, warn for all empty.
						// Let's assume empty is okay unless specifically marked as required later.
						// missingValues.Add(definition.Label); // Example: Track missing values
					}

					placeholderValues[$"{{{{{key}}}}}"] = value ?? "";
				}
				else
				{
					// This shouldn't happen if PopulatePlaceholderFields worked correctly
					Debug.LogError($"Could not find input field for defined placeholder key: {key}");
					EditorUtility.DisplayDialog("Internal Error",
						$"Could not find input field for placeholder: {key}. Please reload the template.", "OK");
					return; // Prevent generation if UI state is inconsistent
				}
			}

			// Optional: Check for missing values if needed
			// if (missingValues.Count > 0)
			// {
			//    EditorUtility.DisplayDialog("Missing Values", $"Please provide values for:\n\n- {string.Join("\n- ", missingValues)}", "OK");
			//    return;
			// }

			// Get target path from Project window selection
			var selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			// If nothing is selected, default to "Assets" or show error? Let ScaffoldGenerator handle path logic.

			// Call the Generator
			try
			{
				ScaffoldGenerator.Generate(_selectedTemplateFile, selectionPath, placeholderValues);

				Debug.Log(
					$"Scaffold generation initiated for template: '{_selectedTemplateFile.TemplateData.TemplateName}' at path: '{selectionPath ?? "Assets"}'.");
				// Optionally close window or show success message
				// EditorUtility.DisplayDialog("Success", $"Scaffold '{_selectedTemplateFile.TemplateData.TemplateName}' generation initiated.", "OK");
				// Close(); // Close window on success
				ShowMessage("Generation successful!", false);
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error during Scaffold generation: {ex.Message}\n{ex.StackTrace}");
				EditorUtility.DisplayDialog("Generation Error", $"An error occurred during generation:\n\n{ex.Message}",
					"OK");
				// Don't close window on error
			}
		}

		private void AddLineBreak(VisualElement container)
		{
			var lineBreak = new VisualElement();
			lineBreak.AddToClassList("line-break");
			container.Add(lineBreak);
		}

		// Helper to show status messages (could replace _validationLabel usage for non-validation messages)
		private void ShowMessage(string message, bool isError)
		{
			// Could use EditorUtility.DisplayDialog or a dedicated label in the UI
			Debug.Log($"[ScaffoldWindow] {(isError ? "Error" : "Info")}: {message}");
			if (_validationLabel == null) return;
		
			_validationLabel.text = message;
			_validationLabel.EnableInClassList("error", isError);
			_validationLabel.EnableInClassList("warning", false);
			_validationLabel.style.display = DisplayStyle.Flex;
		}
	}
}