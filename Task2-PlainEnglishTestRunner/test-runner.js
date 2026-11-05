import fs from 'fs';
import path from 'path';
import { AutonomousTestAgent } from './src/agent/AutonomousTestAgent.js';
import { TestReporter } from './src/reporting/TestReporter.js';

async function runSuite() {
  const casesPath = path.resolve(process.cwd(), 'test-cases', 'sample-tests.json');
  const testCases = JSON.parse(fs.readFileSync(casesPath, 'utf-8'));

  console.log(`\n==========================================================`);
  console.log(`🚀 RUNNING PLAIN-ENGLISH TEST SUITE (${testCases.length} Test Cases)`);
  console.log(`==========================================================\n`);

  const results = [];
  const reporter = new TestReporter();

  for (const tc of testCases) {
    console.log(`\n▶ Starting Test: "${tc.name}"`);
    console.log(`  Instruction: "${tc.test}"`);
    console.log(`  Target URL : ${tc.url}`);

    const agent = new AutonomousTestAgent({
      goal: tc.test,
      initialUrl: tc.url,
      headless: true,
      maxSteps: 8
    });

    const result = await agent.run();
    results.push({ ...tc, ...result });

    reporter.printConsoleSummary(result);
    reporter.generateHtmlReport(result);
  }

  console.log(`\n==========================================================`);
  console.log(`🏁 SUITE SUMMARY`);
  console.log(`==========================================================`);
  let passedCount = 0;
  for (const r of results) {
    const icon = r.verdict === 'PASS' ? '✔ PASS' : '✖ FAIL';
    if (r.verdict === 'PASS') passedCount++;
    console.log(`  ${icon} | ${r.name.padEnd(35)} | ${(r.durationMs / 1000).toFixed(1)}s | Steps: ${r.stepsExecuted}`);
  }
  console.log(`----------------------------------------------------------`);
  console.log(`Total: ${results.length} | Passed: ${passedCount} | Failed: ${results.length - passedCount}`);
  console.log(`==========================================================\n`);

  process.exit(passedCount === results.length ? 0 : 1);
}

runSuite().catch(err => {
  console.error('Test suite failed:', err);
  process.exit(1);
});
