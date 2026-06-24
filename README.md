# PowerShellScriptAnalyzer

PowerShellScriptAnalyzer is a desktop application built with C# and Windows Forms designed to audit, review, and analyze PowerShell scripts using advanced AI models. It helps administrators and developers quickly identify potential security risks, optimization opportunities, and bugs in their automation scripts.

## 🚀 Features

* **Multi-LLM Integration:** Seamlessly connect with **OpenAI**, **Google Gemini**, and **Anthropic Claude** to get diverse AI perspectives on your code.
* **Local Database Caching:** Uses a local SQLite database to store settings, configurations, and analysis results efficiently.
* **Batch / Global Analysis:** Scan and analyze multiple scripts at once to save time.
* **Safety Friction Mechanism:** Includes a built-in "speed bump" (PIN confirmation) for global actions to prevent accidental mass API requests and unexpected token consumption.

---

## 🔒 Security & Privacy First

This application is designed with repository safety in mind:
* **Zero Hardcoded Keys:** Your API keys are **never** stored in the source code, `App.config`, or project files.
* **Local-Only Storage:** API keys are stored securely on your local machine inside an SQLite database (`scripts_db.sqlite`) located in your user profile:
  `%APPDATA%\PowerShellScriptAnalyzer\`
* **GitHub Safe:** You can push this repository to GitHub without worrying about leaking your private API tokens.

---

## 💡 How It Works (The Global Analysis Safety PIN)

To prevent accidental clicks that could trigger massive re-analysis of all your scripts—potentially draining your AI API limits and raising your costs—the **Global Analysis** button requires a safety PIN confirmation. 

* **Default Safety PIN:** `8203`

Think of it as a friendly UI/UX "brake pedal" to give you a second to think before launching a large-scale API automated task!

---

## 🛠️ Prerequisites

* Windows OS
* .NET Framework (or .NET Core/6+/8+ depending on your target)
* API Keys from at least one supported provider:
  * OpenAI API
  * Google AI Studio (Gemini)
  * Anthropic Console (Claude)

---

## 🚀 Getting Started

1. **Clone the Repository:**
   ```bash
   git clone [https://github.com/YOUR_USERNAME/PowerShellScriptAnalyzer.git](https://github.com/YOUR_USERNAME/PowerShellScriptAnalyzer.git)