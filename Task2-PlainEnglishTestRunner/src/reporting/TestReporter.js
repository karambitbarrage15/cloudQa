import fs from 'fs';
import path from 'path';

/**
 * Generates structured console and HTML reports for autonomous test runs.
 */
export class TestReporter {
  constructor(options = {}) {
    this.outputDir = options.outputDir || path.resolve(process.cwd(), 'reports');
  }

  printConsoleSummary(result) {
    const isPass = result.verdict === 'PASS';
    console.log(`\n======================================================`);
    console.log(`📊 TEST EXECUTION SUMMARY`);
    console.log(`======================================================`);
    console.log(`Goal       : "${result.goal}"`);
    console.log(`Result     : ${isPass ? '\x1b[32m✔ PASS\x1b[0m' : '\x1b[31m✖ FAIL\x1b[0m'}`);
    console.log(`Duration   : ${(result.durationMs / 1000).toFixed(2)} seconds`);
    console.log(`Steps Run  : ${result.stepsExecuted}`);
    console.log(`Summary    : ${result.summary}`);
    console.log(`======================================================\n`);
  }

  generateHtmlReport(result) {
    if (!fs.existsSync(this.outputDir)) {
      fs.mkdirSync(this.outputDir, { recursive: true });
    }

    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const htmlPath = path.join(this.outputDir, `report_${timestamp}.html`);
    const jsonPath = path.join(this.outputDir, `result_${timestamp}.json`);

    // Write JSON artifact
    fs.writeFileSync(jsonPath, JSON.stringify(result, null, 2), 'utf-8');

    // Build Step HTML cards
    const stepCards = result.history.map((step) => {
      let relScreenshot = '';
      if (step.screenshot && fs.existsSync(step.screenshot)) {
        relScreenshot = path.relative(this.outputDir, step.screenshot).replace(/\\/g, '/');
      }

      return `
        <div class="step-card">
          <div class="step-header">
            <span class="step-badge">Step ${step.stepNumber}</span>
            <span class="step-action">${escapeHtml(step.action.toUpperCase())}</span>
            <span class="step-time">${step.timestamp ? new Date(step.timestamp).toLocaleTimeString() : ''}</span>
          </div>
          <div class="step-body">
            <p><strong>URL:</strong> <a href="${escapeHtml(step.url)}" target="_blank">${escapeHtml(step.url)}</a></p>
            <p><strong>Title:</strong> ${escapeHtml(step.title)}</p>
            <div class="thought-box">
              <strong>🤖 AI Thought:</strong> <em>"${escapeHtml(step.thought)}"</em>
            </div>
            ${step.value ? `<p><strong>Input Value:</strong> <code>${escapeHtml(step.value)}</code></p>` : ''}
            ${step.error ? `<p class="error-msg"><strong>Error:</strong> ${escapeHtml(step.error)}</p>` : ''}
            ${relScreenshot ? `
              <div class="screenshot-container">
                <a href="${relScreenshot}" target="_blank">
                  <img src="${relScreenshot}" alt="Step ${step.stepNumber} Screenshot" class="screenshot-thumb" />
                </a>
              </div>
            ` : ''}
          </div>
        </div>
      `;
    }).join('\n');

    const isPass = result.verdict === 'PASS';
    const badgeColor = isPass ? '#28a745' : '#dc3545';

    const htmlContent = `
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Autonomous Test Report - ${escapeHtml(result.goal)}</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }
    body { background-color: #f8f9fa; color: #212529; padding: 24px; line-height: 1.5; }
    .container { max-width: 900px; margin: 0 auto; }
    .hero-banner { background: #ffffff; border-radius: 12px; padding: 24px; box-shadow: 0 4px 12px rgba(0,0,0,0.06); margin-bottom: 24px; }
    .verdict-badge { display: inline-block; padding: 6px 14px; border-radius: 20px; font-weight: 700; color: #fff; background-color: ${badgeColor}; font-size: 16px; margin-bottom: 12px; }
    h1 { font-size: 22px; margin-bottom: 10px; color: #111827; }
    .meta-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; margin-top: 16px; border-top: 1px solid #e5e7eb; padding-top: 16px; }
    .meta-item strong { display: block; font-size: 12px; color: #6b7280; text-transform: uppercase; }
    .meta-item span { font-size: 15px; font-weight: 600; color: #1f2937; }
    .timeline-title { font-size: 18px; margin: 24px 0 16px; font-weight: 700; }
    .step-card { background: #ffffff; border-radius: 8px; border: 1px solid #e5e7eb; margin-bottom: 16px; overflow: hidden; box-shadow: 0 2px 6px rgba(0,0,0,0.03); }
    .step-header { background: #f3f4f6; padding: 12px 16px; display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid #e5e7eb; }
    .step-badge { background: #4f46e5; color: #fff; padding: 3px 8px; border-radius: 4px; font-size: 12px; font-weight: 600; }
    .step-action { font-family: monospace; font-weight: 700; color: #374151; font-size: 13px; }
    .step-time { font-size: 12px; color: #9ca3af; }
    .step-body { padding: 16px; }
    .thought-box { background: #eff6ff; border-left: 4px solid #3b82f6; padding: 10px 14px; margin: 12px 0; border-radius: 0 6px 6px 0; font-size: 14px; }
    .screenshot-thumb { max-width: 100%; height: auto; border: 1px solid #d1d5db; border-radius: 6px; margin-top: 12px; transition: transform 0.2s; }
    .screenshot-thumb:hover { transform: scale(1.01); }
    .error-msg { color: #dc2626; font-size: 13px; margin-top: 8px; }
  </style>
</head>
<body>
  <div class="container">
    <div class="hero-banner">
      <span class="verdict-badge">${result.verdict}</span>
      <h1>${escapeHtml(result.goal)}</h1>
      <p style="color: #4b5563;">${escapeHtml(result.summary)}</p>
      <div class="meta-grid">
        <div class="meta-item"><strong>Duration</strong><span>${(result.durationMs / 1000).toFixed(2)}s</span></div>
        <div class="meta-item"><strong>Steps Executed</strong><span>${result.stepsExecuted}</span></div>
        <div class="meta-item"><strong>Timestamp</strong><span>${new Date().toLocaleString()}</span></div>
      </div>
    </div>

    <h2 class="timeline-title">Autonomous Action Audit Trail</h2>
    ${stepCards}
  </div>
</body>
</html>
    `;

    fs.writeFileSync(htmlPath, htmlContent, 'utf-8');
    return { htmlPath, jsonPath };
  }
}

function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
