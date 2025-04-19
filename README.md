<div align=center>   

<p align="center">
  <img src="Readme~/logo.png" width="600" alt="ScaffoldKit Logo">
</p>

# Generate Unity project structures instantly.

Define reusable templates for any directory structure, including files, content, and placeholders, directly within Unity. Use ScaffoldKit to instantly generate consistent scaffolding for new features, packages, or projects, saving time and reducing setup errors.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![PullRequests](https://img.shields.io/badge/PRs-welcome-blueviolet)](http://makeapullrequest.com)
[![Releases](https://img.shields.io/github/v/release/PaulNonatomic/ScaffoldKit)](https://github.com/PaulNonatomic/ScaffoldKit/releases)
[![Unity](https://img.shields.io/badge/Unity-2022.3+-black.svg)](https://unity3d.com/pt/get-unity/download/archive)

</div>

---

## Installation
To install Scaffold Kit in your Unity project, add the package from the git URL: `https://github.com/PaulNonatomic/ScaffoldKit.git` using the Unity package manager (`Window > Package Manager > + > Add package from git URL...`).

## Samples
Example `.skt` templates, such as a standard Unity Package structure, are available for import via the **Samples** tab in the Unity Package Manager window after installing ScaffoldKit. These provide practical examples of template structure and placeholder definitions.

## Overview

Setting up consistent folder structures, assembly definitions, and basic scripts for new features or modules in Unity can be repetitive and error-prone. ScaffoldKit streamlines this process by allowing you to define **Scaffold Templates** (`.skt` files). These templates capture directory layouts, file contents, and use **Placeholder Definitions** for powerful customization. With a few clicks, you can generate complex structures anywhere in your project, ensuring consistency and accelerating development.

![ScaffoldKit Generator Window Page 1](https://github.com/user-attachments/assets/fa98a29d-a976-47a2-9457-09036862e993)
![ScaffoldKit Generator Window Page 2](https://github.com/user-attachments/assets/ee7f37f0-5107-4a8e-a50f-6d7b33159e10)

## Features

* **Create Templates from Scratch:** Define `.skt` templates manually or via a simple editor action.
* **Export Existing Folders:** Convert any existing folder structure within your Unity project into a reusable `.skt` template.
* **Editor Window Generation:** A user-friendly editor window (`Assets/Create/ScaffoldKit/Generate Scaffold`) guides you through selecting a template, filling in placeholders, and generating the structure.
* **Enhanced Placeholder System:** Define placeholders with custom UI labels, descriptions, default values, and display order using `placeholderDefinitions`.
* **Automatic C# Namespace Calculation:** Ensures correct namespaces in generated `.cs` and `.asmdef` files based on folder structure.
* **File Content Embedding:** Include default content for various file types within your templates.
* **Custom Asset Handling:** `.skt` files have a unique icon and a custom inspector preview.
* **Refresh Templates:** Easily reload templates if they are modified or added while the generator window is open.
* **Validation:** The generator window validates placeholder definitions against their usage in the template structure.

## Core Concepts

* **Scaffold Templates (`.skt` files):** These are JSON files that define the desired directory and file structure. They specify folder names, file names, file extensions, initial file content, and placeholder definitions. `.skt` files have a custom icon and inspector preview within Unity.
* **Placeholders (`{{PLACEHOLDER_NAME}}`):** Used within folder/file names, extensions, and content strings to mark parts that need customization during generation.
* **Placeholder Definitions (`placeholderDefinitions`):** A required section within the `.skt` file that explicitly defines each placeholder used in the template. This controls how placeholders appear in the Generator Window UI, allowing for descriptions, default values, and ordering.
* **Automatic Namespace Generation:** For C# scripts (`.cs`) and Assembly Definitions (`.asmdef`), ScaffoldKit automatically calculates the appropriate namespace based on the folder structure relative to the root generation path and the `{{NAMESPACE}}` placeholder value. You can control which folders contribute to the namespace using the `contributesToNamespace` flag within the template's directory definitions.
* **Content Handling:** Templates can store initial file content. ScaffoldKit handles different types:
    * **Plain Text:** (.cs, .txt, .md, .xml, etc.) stored as a JSON string.
    * **JSON:** (.json, .asmdef, .asmref) stored as a JSON object or array.
    * **Other/Binary:** Content can be omitted (stored as `null`) if only the file structure is needed.

## How to Use

### 1. Creating a Scaffold Template (`.skt`)

You have two ways to create templates:

**a) From Scratch:**

1.  Navigate to the desired folder in your Unity Project window.
2.  Right-click and select `Assets/Create/ScaffoldKit/New Scaffold Template`.
3.  This creates a `NewScaffold.skt` file with a basic structure, including default `placeholderDefinitions` for `NAMESPACE` and `FEATURE_NAME`.
4.  Select the file and click "Edit Template" in the Inspector (or open it in your preferred text/JSON editor).
5.  Define your structure (`subDirectories`, `files`) and ensure you add a corresponding entry in the `placeholderDefinitions` array for *every* `{{PLACEHOLDER}}` you use.

**b) From an Existing Folder:**

1.  Select the folder in your Unity Project window that you want to use as a base for your template.
2.  Right-click and select `Assets/Create/ScaffoldKit/New Scaffold Template From Folder`.
3.  ScaffoldKit will analyze the folder's structure and contents, creating a new `.skt` file (e.g., `FolderName.skt`) in the *parent* directory.
4.  This generated template will capture the subdirectories and files but will have an **empty** `placeholderDefinitions` array.
5.  **You must manually edit the `.skt` file** to add `placeholderDefinitions` for any parts of the exported structure you intend to make dynamic using `{{PLACEHOLDERS}}`.

### 2. Understanding Template Structure

* `templateName`: The name displayed in the Scaffold Generator window's dropdown.
* `templateVersion`: Version number for your template.
* **`placeholderDefinitions` (Array):** Defines the placeholders used in the template. Each object in the array should contain:
    * `key` (string, required): The identifier used within `{{...}}` (e.g., "FEATURE\_NAME").
    * `label` (string, required): User-friendly name shown in the Generator UI (e.g., "Feature Name").
    * `description` (string, optional): Tooltip/help text shown in the UI.
    * `defaultValue` (string, optional): Default value pre-filled in the UI input field. Can reference other placeholders like `"{{NAMESPACE}}.{{FEATURE_NAME}}"`.
    * `order` (integer, optional): Controls the display order in the UI (lower numbers appear first). Defaults to a high value if omitted.
* `subDirectories` (Array): Defines folders to create. Each object contains:
    * `name` (string): Folder name (can use placeholders).
    * `contributesToNamespace` (boolean, optional): If true, adds this folder's name to the C# namespace path.
    * `subDirectories` (Array): Nested subfolders.
    * `files` (Array): Files within this folder.
* `files` (Array): Defines files to create at the current level. Each object contains:
    * `name` (string): File name without extension (can use placeholders).
    * `extension` (string): File extension without dot (can use placeholders).
    * `content` (string, object, array, boolean, number, or null): Initial file content.

### 3. Defining and Using Placeholders

1.  **Define:** For every placeholder like `{{MY_PLACEHOLDER}}` you intend to use in your template's `name`, `extension`, or `content` fields, you **must** add a corresponding definition object within the `placeholderDefinitions` array.
2.  **Use:** Insert `{{KEY}}` (where `KEY` matches the `key` in a definition) into the strings for directory names, file names, extensions, or file content where you need customization.
3.  **Generator UI:** When you load the template in the Generator window (`Assets > Create > ScaffoldKit > Generate Scaffold`), it will display input fields based *only* on the `placeholderDefinitions`, ordered by the `order` field, using the `label`, `description`, and `defaultValue`.
4.  **Validation:** The Generator window will warn you if placeholders are used in the structure but not defined, or defined but not used.

**Key Placeholders (Common Conventions):**

* `{{NAMESPACE}}`: **Crucial.** Defines the root namespace. The final namespace for a C# file will be `{{NAMESPACE}}` plus any segments added by folders with `contributesToNamespace: true`.
* `{{DOMAIN_NAME}}`: **Recommended.** Use this for the primary identifier of your scaffolded element (e.g., the feature name or package domain). It's commonly used for the `.asmdef` file name and its `name` property.

### 4. Generating Structures

1.  **Open the Generator Window:** Go to `Assets > Create > ScaffoldKit > Generate Scaffold`.
2.  **Select Template:** Choose your desired `.skt` template from the dropdown. Use the refresh button if needed.
3.  **Load Template:** Click "Load Template".
4.  **Fill Placeholders:** The window will switch to Page 2, showing input fields based on your `placeholderDefinitions`. Fill these in carefully. Review any validation messages.
5.  **Select Target Location:** **Important:** In the *Unity Project window*, select the folder where you want the root of the generated structure to be created.
6.  **Generate:** Click "Generate Structure".

ScaffoldKit will create the directories and files defined in the template at the selected location, replacing placeholders and calculating namespaces as configured.

## Example `.skt` Template (Simple Feature with Definitions)

```json
{
  "templateName": "Simple Feature Module",
  "templateVersion": "1.1.0",
  "placeholderDefinitions": [
    {
      "key": "NAMESPACE",
      "label": "Root Namespace",
      "description": "Base C# namespace for the feature (e.g., MyCompany.MyFeature).",
      "defaultValue": "MyCompany.MyFeature",
      "order": 1
    },
    {
      "key": "FEATURE_NAME",
      "label": "Feature Name",
      "description": "Core name for the feature (e.g., Inventory). Used in class names.",
      "defaultValue": "NewFeature",
      "order": 5
    },
    {
      "key": "DOMAIN_NAME",
      "label": "Assembly Domain Name",
      "description": "Name for .asmdef files (e.g., MyCompany.MyFeature.Runtime).",
      "defaultValue": "{{NAMESPACE}}", // Example default referencing another placeholder
      "order": 10
    }
    // Note: GUID placeholders like {{GUID:...}} are not handled by default.
    // See the note below regarding GUIDs.
  ],
  "subDirectories": [
    {
      "name": "Scripts",
      "contributesToNamespace": true,
      "subDirectories": [
        {
          "name": "Runtime",
          "contributesToNamespace": true,
          "subDirectories": [],
          "files": [
            {
              "name": "{{FEATURE_NAME}}Controller",
              "extension": "cs",
              "content": "using UnityEngine;\n\nnamespace {{NAMESPACE}}.Scripts.Runtime // Calculated: Root.Scripts.Runtime\n{\n    public class {{FEATURE_NAME}}Controller : MonoBehaviour\n    {\n        public void Initialize()\n        {\n            Debug.Log(\"{{FEATURE_NAME}} Initialized!\");\n        }\n    }\n}"
            }
          ]
        },
        {
          "name": "Editor",
          "contributesToNamespace": true,
          "subDirectories": [],
          "files": [
            {
              "name": "{{FEATURE_NAME}}Editor",
              "extension": "cs",
              "content": "using UnityEditor;\nusing UnityEngine;\n\nnamespace {{NAMESPACE}}.Scripts.Editor // Calculated: Root.Scripts.Editor\n{\n    [CustomEditor(typeof({{NAMESPACE}}.Scripts.Runtime.{{FEATURE_NAME}}Controller))]\n    public class {{FEATURE_NAME}}Editor : Editor\n    {\n        public override void OnInspectorGUI()\n        {\n            DrawDefaultInspector();\n            // Add custom editor logic here\n        }\n    }\n}"
            }
          ]
        }
      ],
      "files": []
    },
    {
      "name": "Tests",
      "contributesToNamespace": true, // {{NAMESPACE}}.Tests
      "subDirectories": [
        {
          "name": "Runtime",
          "contributesToNamespace": true, // {{NAMESPACE}}.Tests.Runtime
          "files": [
            {
              "name": "{{DOMAIN_NAME}}.Tests.Runtime",
              "extension": "asmdef",
              "content": {
                "name": "{{DOMAIN_NAME}}.Tests.Runtime", // Placeholder used
                "rootNamespace": "", // Will be calculated: {{NAMESPACE}}.Tests.Runtime
                "references": [
                  "GUID:{{GUID:UnityEngine.TestRunner}}", // Example: Reference Test Runner
                  "GUID:{{GUID:UnityEditor.TestRunner}}", // Example: Reference Test Runner
                  "GUID:{{GUID:{{DOMAIN_NAME}}.Runtime}}" // Example: Reference the main runtime asmdef
                ],
                "includePlatforms": [],
                "excludePlatforms": [],
                "allowUnsafeCode": false,
                "overrideReferences": true, // Important for tests
                "precompiledReferences": [
                  "nunit.framework.dll"
                ],
                "autoReferenced": false, // Test assemblies usually aren't auto-referenced
                "defineConstraints": [
                  "UNITY_INCLUDE_TESTS"
                ],
                "versionDefines": [],
                "noEngineReferences": false
              }
            }
          ],
          "subDirectories": []
        }
      ],
      "files": []
    }
  ],
  "files": [
    {
      "name": "{{DOMAIN_NAME}}.Runtime", // Use DOMAIN_NAME for the main asmdef
      "extension": "asmdef",
      "content": {
        "name": "{{DOMAIN_NAME}}.Runtime", // Placeholder used
        "rootNamespace": "{{NAMESPACE}}", // Will be calculated: {{NAMESPACE}}
        "references": [],
        "includePlatforms": [],
        "excludePlatforms": [],
        "allowUnsafeCode": false,
        "overrideReferences": false,
        "precompiledReferences": [],
        "autoReferenced": true,
        "defineConstraints": [],
        "versionDefines": [],
        "noEngineReferences": false
      }
    }
  ]
}
(Note: For GUID: references in .asmdef content, like GUID:{{GUID:UnityEngine.TestRunner}} or GUID:{{GUID:{{DOMAIN_NAME}}.Runtime}},, the current placeholder system doesn't automatically resolve these GUIDs. You would need to either hardcode known GUIDs for common assemblies like TestRunner or manually find an replace the GUID:{{GUID:...}} placeholders after generation if referencing newly generated asmdefs.)