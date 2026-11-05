import { chromium } from 'playwright';
import path from 'path';
import fs from 'fs';

/**
 * Encapsulates the Playwright browser session, multi-tab page tracking,
 * and resilient action dispatching.
 */
export class BrowserController {
  constructor(options = {}) {
    this.headless = options.headless ?? true;
    this.slowMo = options.slowMo ?? 100;
    this.timeout = options.timeout ?? 15000;
    this.browser = null;
    this.context = null;
    this.activePage = null;
  }

  async launch() {
    this.browser = await chromium.launch({
      headless: this.headless,
      slowMo: this.slowMo,
      args: ['--no-sandbox', '--disable-dev-shm-usage', '--disable-blink-features=AutomationControlled']
    });

    this.context = await this.browser.newContext({
      viewport: { width: 1366, height: 768 },
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
    });

    // Automatically follow newly opened tabs (e.g. target="_blank" links)
    this.context.on('page', async (newPage) => {
      await newPage.waitForLoadState('domcontentloaded');
      this.activePage = newPage;
      this.setupPageListeners(newPage);
    });

    this.activePage = await this.context.newPage();
    this.setupPageListeners(this.activePage);
  }

  setupPageListeners(page) {
    page.setDefaultTimeout(this.timeout);
    page.on('dialog', async (dialog) => {
      console.log(`[Browser] Auto-accepting dialog: "${dialog.message()}"`);
      await dialog.accept();
    });
  }

  async navigate(url) {
    if (!url.startsWith('http://') && !url.startsWith('https://')) {
      url = 'https://' + url;
    }
    await this.activePage.goto(url, { waitUntil: 'domcontentloaded', timeout: this.timeout });
    await this.wait(1500); // Allow dynamic hydration
  }

  async click(refId, fallbackText = null) {
    // 1. Try finding by data-qa-ref attribute
    if (refId) {
      const locator = this.activePage.locator(`[data-qa-ref="${refId}"]`);
      if (await locator.count() > 0) {
        await locator.first().scrollIntoViewIfNeeded();
        await locator.first().click({ timeout: 5000 });
        await this.wait(1000);
        return true;
      }
    }

    // 2. Fallback: try finding by visible text or role
    if (fallbackText) {
      const textLocator = this.activePage.getByText(fallbackText, { exact: false });
      if (await textLocator.count() > 0) {
        await textLocator.first().click({ timeout: 5000 });
        await this.wait(1000);
        return true;
      }
    }

    throw new Error(`Could not click element with ref: ${refId} or text: "${fallbackText}"`);
  }

  async type(refId, value, fallbackPlaceholder = null) {
    let locator = null;

    if (refId) {
      const refLocator = this.activePage.locator(`[data-qa-ref="${refId}"]`);
      if (await refLocator.count() > 0) {
        locator = refLocator.first();
      }
    }

    if (!locator && fallbackPlaceholder) {
      locator = this.activePage.getByPlaceholder(fallbackPlaceholder).first();
    }

    if (!locator) {
      throw new Error(`Could not find input element to type into (ref: ${refId})`);
    }

    await locator.scrollIntoViewIfNeeded();
    await locator.fill('');
    await locator.pressSequentially(value, { delay: 30 });
    await locator.press('Tab');
    await this.wait(500);
    return true;
  }

  async selectOption(refId, value) {
    const locator = this.activePage.locator(`[data-qa-ref="${refId}"]`);
    if (await locator.count() > 0) {
      await locator.first().selectOption({ label: value }).catch(async () => {
        await locator.first().selectOption({ value: value });
      });
      await this.wait(500);
      return true;
    }
    throw new Error(`Could not find select dropdown (ref: ${refId})`);
  }

  async scroll(direction = 'down', amount = 400) {
    const deltaY = direction.toLowerCase() === 'up' ? -amount : amount;
    await this.activePage.mouse.wheel(0, deltaY);
    await this.wait(500);
  }

  async wait(ms = 1000) {
    await this.activePage.waitForTimeout(ms);
  }

  async takeScreenshot(filepath) {
    const dir = path.dirname(filepath);
    if (!fs.existsSync(dir)) {
      fs.mkdirSync(dir, { recursive: true });
    }
    await this.activePage.screenshot({ path: filepath, fullPage: false });
    return filepath;
  }

  async getScreenshotBase64() {
    const buffer = await this.activePage.screenshot({ fullPage: false });
    return buffer.toString('base64');
  }

  getCurrentUrl() {
    return this.activePage ? this.activePage.url() : '';
  }

  async getPageTitle() {
    return this.activePage ? await this.activePage.title() : '';
  }

  async close() {
    if (this.context) await this.context.close().catch(() => {});
    if (this.browser) await this.browser.close().catch(() => {});
  }
}
