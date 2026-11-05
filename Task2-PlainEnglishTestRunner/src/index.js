#!/usr/bin/env node
import 'dotenv/config';
import { Command } from 'commander';
import { AutonomousTestAgent } from './agent/AutonomousTestAgent.js';
import { TestReporter } from './reporting/TestReporter.js';

const program = new Command();

program
  .name('qa-agent')
  .description('Autonomous browser test agent that translates plain-English instructions into live automated tests')
  .version('1.0.0')
  .requiredOption('-t, --test <instruction>', 'Plain-English test instruction (e.g. "Log in as a customer and verify the dashboard is shown")')
  .option('-u, --url <url>', 'Starting webpage URL')
  .option('--headless', 'Run browser in headless mode', process.env.HEADLESS !== 'false')
  .option('--no-headless', 'Run browser in visible mode with UI window')
  .option('--provider <provider>', 'AI provider to use (gemini | openai | heuristic)', process.env.AI_PROVIDER || 'auto')
  .option('--model <model>', 'Model name for LLM provider')
  .option('--max-steps <number>', 'Maximum autonomous steps to take', (val) => parseInt(val, 10), 15)
  .option('--slow-mo <ms>', 'Slow-motion delay in milliseconds per action', (val) => parseInt(val, 10), 100)
  .parse(process.argv);

const options = program.opts();

async function main() {
  const agent = new AutonomousTestAgent({
    goal: options.test,
    initialUrl: options.url,
    headless: options.headless,
    provider: options.provider,
    model: options.model,
    maxSteps: options.maxSteps,
    slowMo: options.slowMo
  });

  const result = await agent.run();

  const reporter = new TestReporter();
  reporter.printConsoleSummary(result);
  const { htmlPath } = reporter.generateHtmlReport(result);

  console.log(`📄 Visual HTML Report: file://${htmlPath.replace(/\\/g, '/')}\n`);

  process.exit(result.verdict === 'PASS' ? 0 : 1);
}

main().catch((err) => {
  console.error('Fatal CLI execution error:', err);
  process.exit(1);
});
