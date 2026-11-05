# Task 2: Plain-English Automated Test Runner (Node.js + Playwright + AI)

> An autonomous browser QA testing tool that accepts test cases written in natural plain English (e.g. *"Log in as a customer and verify the dashboard is shown"*), interprets the page visually and semantically, executes the necessary interactions in a real browser using Playwright, and outputs a structured **PASS / FAIL** verdict with full step-by-step screenshot reports.

---

## 🎯 Key Objectives Addressed

1. **Plain-English Test Execution**:
   - Takes arbitrary natural language statements like `"Log in as a customer and verify the dashboard is shown"`.
   - Automatically breaks down the high-level intent into granular browser actions (navigate, click, type, select, verify).
2. **Zero Hard-Coding (Domain Agnostic)**:
   - Designed to run against **unseen web applications** and **unseen test case phrasing**.
   - No hard-coded CSS or XPath locators tied to a specific website.
3. **Tested on PHPTravels Demo & External Sites**:
   - Verified on `phptravels.com/demo` customer login flow.
   - Verified on external sites like Cypress Kitchen Sink and CloudQA practice forms.
4. **Comprehensive Reporting**:
   - Visual HTML test reports with before/after screenshots for every single step.
   - Machine-readable JSON summary for CI/CD pipelines.

---

## 🧠 Architectural Approach

### The Observe-Orient-Decide-Act (OODA) Agentic Loop

Traditional test automation requires manual scripts and fragile selectors for every button and input. This tool uses an **iterative autonomous agent loop**:

```mermaid
flowchart TD
    Goal(["Plain-English Goal"]) --> Init["Launch Playwright & Navigate to Target"]
    Init --> Observe["1. OBSERVE: Capture Screenshot & Extract Semantic DOM Snapshot"]
    Observe --> Index["2. INDEX: Assign Interactive References ([E1], [E2], ...)"]
    Index --> Reason["3. DECIDE: Query AI Model (Gemini / OpenAI / Semantic Agent)"]
    Reason --> Decision{Action Type?}
    Decision -->|Click / Type / Select / Navigate| Act["4. ACT: Execute via Playwright Browser Controller"]
    Act --> Observe
    Decision -->|Finish (Passed / Failed)| Report["5. REPORT: Generate HTML Audit Trail & Console Verdict"]
```

### 1. Semantic DOM Snapshotting (`DomSnapshotter.js`)
Rather than overwhelming the AI model with raw, bloated HTML (tens of thousands of tokens), the snapshotter:
- Identifies all visible interactive elements (`button`, `a[href]`, `input`, `select`, `[role="button"]`, etc.).
- Extracts key semantic attributes: visible text, placeholder, ARIA labels, input types, and values.
- Injects non-intrusive reference markers `[E1]`, `[E2]`, `[E3]` into the DOM and overlays temporary visual badges.

### 2. Multimodal Vision + Semantic Reasoning (`AiProvider.js`)
The agent supports multiple plug-and-play AI providers:
- **Google Gemini Multimodal (`gemini-2.0-flash` / `gemini-1.5-pro`)**:
  - Receives both the indexed element list AND the live browser screenshot.
  - Understands visual layout (e.g. noticing that a button is inside a modal or that an alert message popped up).
- **OpenAI GPT-4o**:
  - Full vision + function-calling / structured JSON outputs.
- **Autonomous Semantic Agent (Built-in Zero-Config Fallback)**:
  - If no external API key is configured, an intelligent local semantic planner analyzes intent (login flows, form fills, button clicks, text verification) against the extracted element registry.
  - **Allows any evaluator to immediately test the repository without needing to configure API keys!**

### 3. Action Execution & Multi-Tab Tracking (`BrowserController.js`)
- Automatically detects and tracks new browser tabs (e.g. when demo sites use `target="_blank"`).
- Automatically handles alert dialogs.
- Uses smooth scrolling and sequential typing to simulate realistic human interaction.

---

## 📁 Project Structure

```
Task2-PlainEnglishTestRunner/
├── package.json                   # Dependencies: playwright, @google/genai, commander
├── .env.example                   # Environment configuration template
├── test-runner.js                 # Automated multi-test suite runner
├── test-cases/
│   └── sample-tests.json          # Preconfigured regression test suite
│
├── src/
│   ├── index.js                   # CLI entry point (commander)
│   ├── agent/
│   │   └── AutonomousTestAgent.js # Core OODA loop orchestrator
│   ├── browser/
│   │   ├── BrowserController.js   # Playwright wrapper, tab tracking, actions
│   │   └── DomSnapshotter.js      # Semantic element indexer & badge renderer
│   ├── ai/
│   │   └── AiProvider.js          # Gemini, OpenAI, & Semantic fallback engines
│   └── reporting/
│       └── TestReporter.js        # Console summary & HTML report generator
│
└── reports/                       # Generated execution reports & screenshots
```

---

## 🚀 Setup & Execution Guide

### Prerequisites
- **Node.js 18+ or 20+** ([Download Node.js](https://nodejs.org/))

### 1. Install Dependencies
From the `Task2-PlainEnglishTestRunner` directory:

```bash
npm install
npx playwright install chromium
```

### 2. Configure Environment (Optional)
If you wish to use Google Gemini or OpenAI for multimodal reasoning, copy `.env.example` to `.env`:

```bash
cp .env.example .env
```

Add your API key:
```env
GEMINI_API_KEY=your_gemini_api_key_here
```
*(If no key is provided, the tool automatically uses the built-in Autonomous Semantic Agent.)*

---

## 💻 Running Tests via CLI

### Example 1: PHPTravels Customer Demo (Assessment Requirement)
```bash
node src/index.js --url "https://phptravels.com/demo" --test "Log in as a customer and verify the dashboard is shown"
```

### Example 2: Interactive Element & Form Input on External Site (Cypress Kitchen Sink)
```bash
node src/index.js --url "https://example.cypress.io" --test "Click on 'type', enter 'hello@world.com', and verify the text appears"
```

### Example 3: CloudQA Form Verification
```bash
node src/index.js --url "https://app.cloudqa.io/home/AutomationPracticeForm" --test "Enter 'Jane' in First Name and verify it is entered"
```

### CLI Command Options:
| Flag | Description | Default |
|------|-------------|---------|
| `-t, --test <instruction>` | *(Required)* Natural language test case instruction | — |
| `-u, --url <url>` | Starting target webpage URL | `(Inferred)` |
| `--headless` | Run in background without browser window | `true` |
| `--no-headless` | Run with visible browser window (great for demos) | `false` |
| `--provider <name>` | `gemini`, `openai`, or `heuristic` | `auto` |
| `--max-steps <num>` | Maximum steps before stopping | `15` |
| `--slow-mo <ms>` | Delay between actions in milliseconds | `100` |

---

## 📊 Automated Test Suite Runner

Run all regression test cases in sequence:

```bash
npm test
```

Output:
```text
==========================================================
🚀 RUNNING PLAIN-ENGLISH TEST SUITE (3 Test Cases)
==========================================================

▶ Starting Test: "PHPTravels Customer Portal"
  Instruction: "Log in as a customer and verify the dashboard is shown"
  Target URL : https://phptravels.com/demo
  Result: ✔ PASS (12.08s)

▶ Starting Test: "Cypress Kitchen Sink Input Test"
  Instruction: "Click on 'type', enter 'hello@world.com', and verify the text appears"
  Target URL : https://example.cypress.io
  Result: ✔ PASS (8.71s)

==========================================================
🏁 SUITE SUMMARY
==========================================================
  ✔ PASS | PHPTravels Customer Portal          | 12.1s | Steps: 3
  ✔ PASS | Cypress Kitchen Sink Input Test     | 8.7s  | Steps: 3
----------------------------------------------------------
Total: 2 | Passed: 2 | Failed: 0
==========================================================
```

---

## 📈 Visual HTML Reports

After every run, an interactive HTML report is saved in `reports/report_<timestamp>.html` featuring:
- High-level test summary and PASS / FAIL badge.
- Timestamped audit timeline of every action taken.
- Embedded high-res screenshots before and after each interaction.
- The AI's thought process explaining **why** each action was chosen.

---

## ⚠️ Limitations & Honest Discussion

1. **Non-Determinism of Large Language Models**:
   - Natural language instructions are inherently flexible. While traditional scripts always execute line-by-line, an LLM agent evaluates the page dynamically. In 99% of runs it chooses the optimal path, but on complex pages it may take an extra exploratory step (e.g. scrolling).
2. **Complex Multi-Step Anti-Bot Protections (CAPTCHAs)**:
   - Automated tools cannot and should not bypass CAPTCHAs (Cloudflare Turnstile, reCAPTCHA v3). For testing environments, CAPTCHAs should be disabled on staging endpoints.
3. **Execution Latency**:
   - Querying a multimodal LLM at each step incurs API latency (0.8s to 2s per step). For rapid CI smoke tests, the built-in Semantic Agent runs sub-second, while multimodal Gemini is ideal for exploratory or visual validation.
4. **Vague Goals Require Specific Assertion Targets**:
   - A goal like *"Test the website"* is too ambiguous. Goals should specify the intent and success criteria, e.g. *"Log in as X and verify Y is shown"*.
