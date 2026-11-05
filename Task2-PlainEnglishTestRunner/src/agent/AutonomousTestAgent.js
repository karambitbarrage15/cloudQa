import path from 'path';
import { BrowserController } from '../browser/BrowserController.js';
import { DomSnapshotter } from '../browser/DomSnapshotter.js';
import { AiProviderFactory } from '../ai/AiProvider.js';

/**
 * Autonomous QA Test Agent.
 * Runs an iterative Observe-Orient-Decide-Act (OODA) loop until the natural language
 * test case is resolved with a PASS or FAIL verdict.
 */
export class AutonomousTestAgent {
  constructor(options = {}) {
    this.goal = options.goal;
    this.initialUrl = options.initialUrl;
    this.maxSteps = options.maxSteps || 15;
    this.headless = options.headless ?? true;
    this.providerName = options.provider;
    this.modelName = options.model;
    this.artifactsDir = options.artifactsDir || path.resolve(process.cwd(), 'reports', 'artifacts');

    this.browser = new BrowserController({
      headless: this.headless,
      slowMo: options.slowMo || 150
    });

    this.ai = AiProviderFactory.create({
      provider: this.providerName,
      model: this.modelName
    });

    this.stepHistory = [];
  }

  async run() {
    console.log(`\n======================================================`);
    console.log(`🤖 AUTONOMOUS PLAIN-ENGLISH TEST AGENT`);
    console.log(`   Goal      : "${this.goal}"`);
    console.log(`   Target    : ${this.initialUrl || '(Inferred from test case)'}`);
    console.log(`   AI Engine : ${this.ai.name}`);
    console.log(`   Headless  : ${this.headless}`);
    console.log(`======================================================\n`);

    const startTime = Date.now();
    let verdict = 'UNKNOWN';
    let summaryMessage = '';

    try {
      await this.browser.launch();

      // Navigate to initial target URL if specified
      if (this.initialUrl) {
        console.log(`[Step 0] Navigating to initial URL: ${this.initialUrl}`);
        await this.browser.navigate(this.initialUrl);
      }

      for (let step = 1; step <= this.maxSteps; step++) {
        console.log(`\n--- Step ${step}/${this.maxSteps} ---`);

        // 1. OBSERVE: Capture DOM snapshot and Screenshot
        const { elements, pageSummary } = await DomSnapshotter.extractSnapshot(this.browser.activePage, true);
        const currentUrl = this.browser.getCurrentUrl();
        const pageTitle = await this.browser.getPageTitle();

        const stepScreenshotName = `step_${step}_${Date.now()}.png`;
        const stepScreenshotPath = path.join(this.artifactsDir, stepScreenshotName);
        await this.browser.takeScreenshot(stepScreenshotPath);
        const screenshotBase64 = await this.browser.getScreenshotBase64();

        // Clear visual badges after screenshot is taken
        await DomSnapshotter.clearBadges(this.browser.activePage);

        console.log(`[Observe] URL: "${currentUrl}" | Title: "${pageTitle}" | Interactive elements: ${elements.length}`);

        // 2. DECIDE: Consult AI Provider
        console.log(`[Reasoning] Querying ${this.ai.name}...`);
        const decision = await this.ai.decideNextAction({
          goal: this.goal,
          currentUrl,
          pageTitle,
          elements,
          history: this.stepHistory,
          screenshotBase64
        });

        console.log(`[Thought]  "${decision.thought}"`);
        console.log(`[Action]   ${decision.action.toUpperCase()} ${decision.targetRef ? `-> [${decision.targetRef}]` : ''} ${decision.value ? `(Value: "${decision.value}")` : ''}`);

        // Record step in history
        const stepRecord = {
          stepNumber: step,
          url: currentUrl,
          title: pageTitle,
          action: decision.action,
          targetRef: decision.targetRef,
          value: decision.value,
          thought: decision.thought,
          screenshot: stepScreenshotPath,
          timestamp: new Date().toISOString()
        };
        this.stepHistory.push(stepRecord);

        // 3. ACT: Execute the decided action
        if (decision.action === 'finish') {
          verdict = decision.status === 'PASSED' ? 'PASS' : 'FAIL';
          summaryMessage = decision.thought;
          break;
        }

        try {
          await this.executeAction(decision, elements);
        } catch (actErr) {
          console.warn(`[Execution Warning] Action error: ${actErr.message}`);
          stepRecord.error = actErr.message;
        }

        // Allow DOM to settle
        await this.browser.wait(1200);

        if (step === this.maxSteps) {
          verdict = 'FAIL';
          summaryMessage = `Exceeded maximum configured steps (${this.maxSteps}) without confirming pass conditions.`;
        }
      }
    } catch (fatalErr) {
      verdict = 'FAIL';
      summaryMessage = `Test execution encountered unhandled error: ${fatalErr.message}`;
      console.error(`[Fatal Error]`, fatalErr);
    } finally {
      await this.browser.close();
    }

    const durationMs = Date.now() - startTime;

    return {
      goal: this.goal,
      verdict,
      durationMs,
      summary: summaryMessage,
      stepsExecuted: this.stepHistory.length,
      history: this.stepHistory
    };
  }

  async executeAction(decision, elements) {
    const { action, targetRef, value, fallbackText } = decision;

    switch (action.toLowerCase()) {
      case 'click': {
        const el = elements.find(e => e.ref === targetRef);
        const text = fallbackText || (el ? el.text : null);
        await this.browser.click(targetRef, text);
        break;
      }
      case 'type': {
        const el = elements.find(e => e.ref === targetRef);
        const placeholder = el ? el.placeholder : null;
        await this.browser.type(targetRef, value, placeholder);
        break;
      }
      case 'select': {
        await this.browser.selectOption(targetRef, value);
        break;
      }
      case 'navigate': {
        await this.browser.navigate(value);
        break;
      }
      case 'scroll': {
        await this.browser.scroll(value || 'down');
        break;
      }
      case 'wait': {
        await this.browser.wait(2000);
        break;
      }
      default:
        console.warn(`[Unknown Action] "${action}", skipping.`);
    }
  }
}
