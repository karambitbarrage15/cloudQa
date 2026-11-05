# CloudQA Automated Testing Assessment

This repository contains robust, production-grade solutions for both CloudQA assessment tasks:

1. **[Task 1: Resilient Form Automation (C# + Selenium)](./Task1-ResilientFormAutomation)**
   - Resilient test automation for First Name, Gender, and State.
   - Self-healing / multi-strategy locator engine designed to keep working across page structure changes, reordered fields, modified attributes, or changed element types.
   - Configurable target URL and form fields.

2. **[Task 2: Plain-English Automated Test Runner (Node.js + Playwright + AI)](./Task2-PlainEnglishTestRunner)**
   - Universal natural-language test executor that takes plain-English test instructions (e.g. *"Log in as a customer and verify the dashboard is shown"*) and runs them autonomously in a real browser.
   - Tested against the PHPTravels demo site and general web applications.
   - Produces structured pass/fail reports, execution traces, and screenshot artifacts.

---

## Repository Structure

```
cloudQa/
├── Task1-ResilientFormAutomation/      # C# .NET 8 + Selenium solution
│   ├── ResilientFormAutomation.sln
│   ├── ResilientFormAutomation/
│   │   ├── Config/                    # Configurable URLs and field descriptors
│   │   ├── Locators/                  # Multi-strategy resilient locator chain
│   │   ├── Actions/                   # Form interaction handlers (text, radio, dropdown)
│   │   └── Tests/                     # Test suite including resilience verification
│   └── README.md                      # Setup, execution, approach & limitations
│
├── Task2-PlainEnglishTestRunner/       # Node.js + Playwright + AI test runner
│   ├── src/
│   │   ├── agent/                     # Autonomous test agent loop
│   │   ├── browser/                   # Playwright browser controller & DOM snapshotter
│   │   ├── ai/                        # AI vision & reasoning client
│   │   └── reporting/                 # HTML/JSON test reporting
│   ├── test-cases/                    # Sample plain-English test suites
│   └── README.md                      # Setup, execution, approach & limitations
│
└── README.md                          # Root overview
```
