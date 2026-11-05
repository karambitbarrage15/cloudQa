/**
 * Scans the active browser page and extracts an indexed semantic snapshot of all interactive elements.
 * Assigns each element a reference identifier ([E1], [E2], ...) and attaches overlay labels
 * so that both text-based reasoning and visual AI vision can pinpoint exact controls.
 */
export class DomSnapshotter {
  /**
   * Extracts interactive elements from the current page.
   * @param {import('playwright').Page} page
   * @param {boolean} highlightOnPage Whether to overlay visual tag badges on the DOM
   * @returns {Promise<{ elements: Array<any>, pageSummary: string }>}
   */
  static async extractSnapshot(page, highlightOnPage = true) {
    return await page.evaluate(({ highlight }) => {
      // Remove any previously rendered tag badges
      document.querySelectorAll('.qa-agent-badge').forEach(b => b.remove());

      const interactiveSelectors = [
        'button',
        'a[href]',
        'input',
        'select',
        'textarea',
        '[role="button"]',
        '[role="tab"]',
        '[role="menuitem"]',
        '[role="option"]',
        '[role="link"]',
        '[role="checkbox"]',
        '[role="radio"]',
        '[onclick]',
        'summary'
      ];

      const rawElements = Array.from(document.querySelectorAll(interactiveSelectors.join(', ')));

      // Add headings and alert/status banners for semantic context
      const contextNodes = Array.from(document.querySelectorAll('h1, h2, h3, .alert, [role="alert"], .badge'));

      const isElementVisible = (el) => {
        if (!el) return false;
        const style = window.getComputedStyle(el);
        if (style.display === 'none' || style.visibility === 'hidden' || style.opacity === '0') {
          return false;
        }
        const rect = el.getBoundingClientRect();
        return rect.width > 0 && rect.height > 0;
      };

      const cleanText = (str) => {
        if (!str) return '';
        return str.replace(/\s+/g, ' ').trim().substring(0, 80);
      };

      const elements = [];
      let index = 1;

      for (const el of rawElements) {
        if (!isElementVisible(el)) continue;

        const tag = el.tagName.toLowerCase();
        const type = el.getAttribute('type') || '';
        const role = el.getAttribute('role') || '';
        const text = cleanText(el.innerText || el.textContent || '');
        const placeholder = el.getAttribute('placeholder') || '';
        const ariaLabel = el.getAttribute('aria-label') || '';
        const name = el.getAttribute('name') || '';
        const id = el.id || '';
        const value = (tag === 'input' || tag === 'select' || tag === 'textarea') ? (el.value || '') : '';
        const href = el.getAttribute('href') || '';

        const refId = `E${index++}`;

        // Tag the DOM element directly with a data attribute for quick lookup
        el.setAttribute('data-qa-ref', refId);

        // Optionally overlay visual badge for screenshot clarity
        if (highlight) {
          try {
            const rect = el.getBoundingClientRect();
            const badge = document.createElement('div');
            badge.className = 'qa-agent-badge';
            badge.innerText = refId;
            badge.style.position = 'absolute';
            badge.style.left = `${rect.left + window.scrollX}px`;
            badge.style.top = `${rect.top + window.scrollY}px`;
            badge.style.backgroundColor = '#d63384';
            badge.style.color = '#ffffff';
            badge.style.fontSize = '10px';
            badge.style.fontFamily = 'monospace';
            badge.style.fontWeight = 'bold';
            badge.style.padding = '1px 3px';
            badge.style.borderRadius = '3px';
            badge.style.zIndex = '999999';
            badge.style.pointerEvents = 'none';
            document.body.appendChild(badge);
          } catch (e) { }
        }

        elements.push({
          ref: refId,
          tag,
          type: type.toLowerCase(),
          role: role.toLowerCase(),
          text,
          placeholder,
          ariaLabel,
          name,
          id,
          value,
          href: href.startsWith('http') || href.startsWith('/') ? href : ''
        });
      }

      // Collect headings & key text landmarks
      const headings = contextNodes
        .filter(isElementVisible)
        .map(h => cleanText(h.innerText))
        .filter(t => t.length > 2)
        .slice(0, 10);

      const pageSummary = {
        title: document.title,
        url: window.location.href,
        headings,
        totalInteractiveCount: elements.length
      };

      return { elements, pageSummary };
    }, { highlight: highlightOnPage });
  }

  /**
   * Cleans up visual badges from the page.
   * @param {import('playwright').Page} page
   */
  static async clearBadges(page) {
    try {
      await page.evaluate(() => {
        document.querySelectorAll('.qa-agent-badge').forEach(b => b.remove());
      });
    } catch { }
  }
}
