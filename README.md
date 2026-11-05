# CloudQA Automated Testing Assessment

[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Node.js](https://img.shields.io/badge/Node.js-20.x-green.svg)](https://nodejs.org/)
[![Playwright](https://img.shields.io/badge/Playwright-Automated-red.svg)](https://playwright.dev/)
[![Selenium](https://img.shields.io/badge/Selenium-WebDriver_4-orange.svg)](https://www.selenium.dev/)

This repository contains production-ready, fully automated solutions for both CloudQA assessment tasks:

1. **[Task 1: Resilient Form Automation (C# + Selenium WebDriver)](./Task1-ResilientFormAutomation)**
2. **[Task 2: Plain-English Automated Test Runner (Node.js + Playwright + AI)](./Task2-PlainEnglishTestRunner)**

---

## 📑 Table of Contents

- [Quick Start: Run Both Tasks](#-quick-start-run-both-tasks)
  - [Run Task 1 (C# + Selenium)](#1-run-task-1-c--selenium)
  - [Run Task 2 (Playwright + AI)](#2-run-task-2-playwright--ai)
- [Task 1 Overview: Self-Healing Form Automation](#-task-1-overview-self-healing-form-automation)
  - [Key Resilient Features](#key-resilient-features)
  - [Mutation Test Results](#mutation-test-results)
- [Task 2 Overview: Plain-English Autonomous Test Agent](#-task-2-overview-plain-english-autonomous-test-agent)
  - [Key Autonomous Features](#key-autonomous-features)
  - [Tested Scenarios](#tested-scenarios)
- [Submission Notes: Approach & Limitations](#-submission-notes-approach--limitations)
- [5-Minute Screen Recording Presentation Script](#-5-minute-screen-recording-presentation-script)

---

## ⚡ Quick Start: Run Both Tasks

### 1. Run Task 1 (C# + Selenium)
*Requires .NET 8.0 SDK and Google Chrome.*

```bash
cd Task1-ResilientFormAutomation

# 1. Run CLI against the live CloudQA form:
dotnet run --project ResilientFormAutomation.Cli -- --headless

# 2. Run against any custom/modified URL:
dotnet run --project ResilientFormAutomation.Cli -- --url "https://custom-url.com/form"

# 3. Run the complete NUnit Resilience Test Suite (5 tests: Live + 4 mutated pages):
dotnet test
```

### 2. Run Task 2 (Playwright + AI)
*Requires Node.js 18+ or 20+.*

```bash
cd Task2-PlainEnglishTestRunner

# 1. Install dependencies:
npm install
npx playwright install chromium

# 2. Run the PHPTravels Customer Login test (from prompt):
node src/index.js --url "https://phptravels.com/demo" --test "Log in as a customer and verify the dashboard is shown"

# 3. Run a generic unseen test case on an external site:
node src/index.js --url "https://example.cypress.io" --test "Click on 'type', enter 'hello@world.com', and verify the text appears"

# 4. Run the automated multi-site test suite:
npm test
```

---

## 🛡️ Task 1 Overview: Self-Healing Form Automation

The objective of Task 1 is to automate three form fields (**First Name**, **Gender**, and **State**) on `https://app.cloudqa.io/home/AutomationPracticeForm` such that the test **keeps working even if the page layout changes, attributes change, fields are reordered, or distractor fields are added**.

### Key Resilient Features
- **Zero reliance on brittle XPaths or single CSS selectors**: Does not use hard-coded paths like `//div[2]/form/div[1]/input`.
- **Chain-of-Strategies Locator Engine (`SmartLocatorChain`)**:
  1. *Label Association*: Searches `<label>` tags via `for` attributes, nested inputs, common containers, and siblings.
  2. *Attribute Semantics*: Evaluates `aria-label`, `placeholder`, `name`, `data-testid`, and `id` against field synonyms.
  3. *Proximity Heuristics*: Scans adjacent `<span>`, `<div>`, `<td>` text nodes and binds the nearest compatible control.
  4. *Shadow DOM & Slotted Components*: Traverses Web Component shadow roots and `<slot>` elements (as used in the CloudQA practice form).
  5. *In-Page JS Multi-Signal Evaluator*: Computes a composite confidence score across visibility, distance, and negative indicators (e.g. penalizing "Last Name" when searching for "First Name").
- **Reactive SPA Event Dispatching**: Dispatches synthetic `input` and `change` events so React, Angular, and Vue state updates properly.
- **Configurable URL**: Modifiable via `appsettings.json` or CLI flag (`--url`).

### Mutation Test Results
All 5 automated tests pass in `ResilientFormAutomation.Tests`:
- `Test01`: Live CloudQA form on `app.cloudqa.io` (PASS)
- `Test02`: Mutated Page 1 with randomized IDs, altered classes, and no `for` attributes (PASS)
- `Test03`: Mutated Page 2 with inverted field order (State $\rightarrow$ Gender $\rightarrow$ First Name) and nested labels (PASS)
- `Test04`: Mutated Page 3 with distractor fields like "Middle Name", "Last Name", and "State of Driver License" (PASS)
- `Test05`: Mutated Page 4 with modern label-less form using only ARIA attributes (PASS)

---

## 🤖 Task 2 Overview: Plain-English Autonomous Test Agent

The objective of Task 2 is to take a test case in plain English (e.g. *"Log in as a customer and verify the dashboard is shown"*) and run it automatically in a browser with a **PASS / FAIL** verdict, without hard-coding to one website.

### Key Autonomous Features
- **Iterative OODA Loop**: Continuously observes the DOM, reasons about the next action, executes via Playwright, and inspects the resulting state.
- **Semantic Element Registry**: Assigns dynamic references `[E1]`, `[E2]` to visible interactive controls and overlays visual badges during screenshot captures.
- **Pluggable AI Reasoning**:
  - Multimodal **Google Gemini** (`gemini-2.0-flash` / `gemini-1.5-pro`) and **OpenAI GPT-4o**.
  - Built-in **Autonomous Semantic Agent** fallback that allows instant zero-config evaluation without needing API keys.
- **Visual HTML Audit Trail**: Generates timestamped reports in `reports/` showing the AI's reasoning, executed actions, and before/after screenshots for every step.

---

## 📝 Submission Notes: Approach & Limitations

### Task 1: Approach & Limitations
- **Approach**: Form fields are treated as semantic concepts rather than DOM paths. By cascading from label binding down to visual proximity and JavaScript multi-signal ranking, the locator survives attribute churn and DOM refactoring.
- **Limitations**:
  - Multi-strategy evaluation takes slightly longer (50–300 ms per field) than static selectors.
  - Highly ambiguous forms (e.g. two unlabeled text inputs side by side without placeholder or ARIA hints) require parent container scoping.
  - Forms drawn entirely on HTML5 Canvas / WebGL cannot be queried via standard DOM.

### Task 2: Approach & Limitations
- **Approach**: An autonomous agent decomposes natural language goals into atomic browser actions using visual and DOM comprehension. It handles unseen sites and new test phrasing dynamically.
- **Limitations**:
  - LLM API calls introduce latency (0.8s–2s per step) compared to static scripts.
  - CAPTCHA gates (Cloudflare Turnstile, reCAPTCHA) intentionally block automated agents.
  - Extremely vague instructions (e.g. *"Explore the app"*) require defined completion criteria to assert a pass or fail.

---

## 🎥 5-Minute Screen Recording Presentation Script

Use this structured script to present your solution in under 5 minutes:

### Minute 1: Introduction & Architecture Overview (0:00 - 1:00)
1. **Show GitHub Repo**: Show `karambitbarrage15/cloudQa` with the clean two-folder structure.
2. **Explain Task 1 Goal**: Explain why traditional selectors break (IDs change, classes update, DOM shifts) and present the **Multi-Strategy Smart Locator Chain**.
3. **Explain Task 2 Goal**: Present the autonomous agent that turns plain English instructions into Playwright actions.

### Minute 2: Task 1 Live Execution & Mutations (1:00 - 2:30)
1. **Run the CLI**:
   ```bash
   cd Task1-ResilientFormAutomation
   dotnet run --project ResilientFormAutomation.Cli
   ```
   Show the console output: First Name, Gender, and State resolved with confidence scores and execution times.
2. **Run Mutation Tests**:
   ```bash
   dotnet test
   ```
   Highlight that all 5 tests pass, demonstrating resilience against randomized IDs, inverted field order, and distractor fields.

### Minute 3: Task 2 Live Execution on PHPTravels (2:30 - 3:45)
1. **Run PHPTravels Customer Demo**:
   ```bash
   cd Task2-PlainEnglishTestRunner
   node src/index.js --url "https://phptravels.com/demo" --test "Log in as a customer and verify the dashboard is shown"
   ```
2. **Explain the Agent Trace**:
   - Point out Step 1: Discovering and clicking the customer portal link.
   - Point out Step 2: Entering customer credentials.
   - Point out Step 3: Verifying the dashboard and outputting `✔ PASS`.
3. **Open the Generated HTML Report**:
   Open `reports/report_<timestamp>.html` in Chrome to show the step-by-step screenshots and AI thoughts.

### Minute 4: Task 2 on an Unseen External Site (3:45 - 4:30)
1. **Run Generic Test Case**:
   ```bash
   node src/index.js --url "https://example.cypress.io" --test "Click on 'type', enter 'hello@world.com', and verify the text appears"
   ```
2. **Highlight Generalization**: Explain that zero selectors were hardcoded for Cypress; the agent reasoned through the intent dynamically.

### Minute 5: Wrap-up & Honest Limitations (4:30 - 5:00)
1. Summarize the trade-offs: Multi-strategy latency vs. maintenance savings, and LLM flexibility vs. CAPTCHA constraints.
2. Close with: *"Both solutions are production-grade, documented, and fully reproducible."*
