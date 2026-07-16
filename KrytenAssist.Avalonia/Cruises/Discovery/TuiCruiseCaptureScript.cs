namespace KrytenAssist.Avalonia.Cruises.Discovery;

public static class TuiCruiseCaptureScript
{
    public const int MaximumCandidates = 10;
    public const int MaximumFieldLength = 512;

    public const string Script = """
        (() => {
          const bounded = value => String(value || '').trim().slice(0, 512);
          const roots = [document].concat(
            Array.from(document.querySelectorAll('tui-product-cards'))
              .map(element => element.shadowRoot)
              .filter(Boolean)
              .slice(0, 3));
          const queryAll = selector => roots.flatMap(root => Array.from(root.querySelectorAll(selector)));
          const isoDate = value => {
            const match = /^(\d{1,2})([A-Za-z]{3})(\d{2,4})$/.exec(value || '');
            if (!match) return null;
            const months = {Jan:'01',Feb:'02',Mar:'03',Apr:'04',May:'05',Jun:'06',Jul:'07',Aug:'08',Sep:'09',Oct:'10',Nov:'11',Dec:'12'};
            const month = months[match[2][0].toUpperCase() + match[2].slice(1).toLowerCase()];
            if (!month) return null;
            const year = match[3].length === 2 ? `20${match[3]}` : match[3];
            return `${year}-${month}-${match[1].padStart(2, '0')}`;
          };
          const itineraryUrl = element => {
            const value = element.href || element.getAttribute('href') ||
              element.getAttribute('data-href') || element.getAttribute('data-url');
            if (!value) return null;
            try {
              const url = new URL(value, document.location.href);
              return /\/cruise\/bookitineraries\//i.test(url.pathname) || url.searchParams.has('itineraryCodeOne')
                ? url
                : null;
            } catch { return null; }
          };
          const links = queryAll('a[href],[data-href],[data-url]')
            .map(element => ({element, url: itineraryUrl(element)}))
            .filter(item => item.url)
            .filter((item, index, items) => index === items.findIndex(other => other.url.href === item.url.href))
            .slice(0, 10);
          const semanticText = selector => queryAll(selector)
            .slice(0, 40)
            .map(element => bounded(`${element.getAttribute('aria-label') || ''} ${element.textContent || ''}`));
          const shipNamePattern = /\bMarella\s+(?:Discovery(?:\s+2)?|Explorer(?:\s+2)?|Voyager)\b/i;
          const shipEvidence = queryAll(
              'button,[role="tab"],[aria-label],[data-testid*="ship" i],h1,h2,h3,h4,h5,h6,dt,dd,label,strong,p,span')
            .slice(0, 500)
            .map(element => bounded(`${element.getAttribute('aria-label') || ''} ${element.innerText || element.textContent || ''}`))
            .filter(value => value.length <= 160)
            .map(value => value.match(shipNamePattern)?.[0])
            .find(Boolean);
          const priceEvidence = semanticText('[data-testid*="price" i],[class*="price" i],[aria-label*="£"]')
            .filter(value => !/discount|saving|save\b/i.test(value))
            .map(value => value.match(/£\s*([\d,]+(?:\.\d{1,2})?)/i))
            .find(Boolean);
          const promotionEvidence = semanticText('[data-testid*="discount" i],[class*="discount" i],[data-testid*="saving" i]')
            .find(value => /£\s*[\d,]+(?:\.\d{1,2})?\s*(?:total\s+)?discount|(?:total\s+)?discount[^£]*£\s*[\d,]+(?:\.\d{1,2})?/i.test(value));
          const knownShips = {'150013': 'Marella Discovery 2'};
          const candidates = links.map(item => {
            const link = item.element;
            const url = item.url;
            const container = link.closest('article,[data-testid],[data-cruise-card],li') || link.parentElement;
            const text = bounded(container?.innerText);
            const pathName = decodeURIComponent(url.pathname.split('/').pop() || '');
            const code = url.searchParams.get('itineraryCodeOne') || url.searchParams.get('itineraryCode');
            const titlePart = code && pathName.endsWith(`-${code}`) ? pathName.slice(0, -(code.length + 1)) : pathName;
            const ship = text.match(shipNamePattern)?.[0] ||
              container?.querySelector('[data-ship-name],[data-ship],.ship-name')?.getAttribute('data-ship-name') ||
              container?.querySelector('[data-ship-name],[data-ship],.ship-name')?.textContent ||
              shipEvidence ||
              knownShips[url.searchParams.get('shipCode')];
            const price = text.match(/£\s*([\d,]+(?:\.\d{1,2})?)\s*(pp|per person)\b/i);
            const totalPrice = text.match(/£\s*([\d,]+(?:\.\d{1,2})?)\s*Total price\b/i);
            const discount = text.match(/£\s*([\d,]+(?:\.\d{1,2})?)\s*pp\s*discount\b/i);
            return {
              providerOfferId: bounded(url.searchParams.get('packageId') || code || pathName),
              title: bounded(titlePart.replace(/-/g, ' ')),
              shipName: bounded(ship),
              departureDate: isoDate(url.searchParams.get('sailingDate') || ''),
              durationNights: Number.parseInt(url.searchParams.get('cruiseDuration') || '', 10) || null,
              departurePort: null,
              itinerarySummary: null,
              prices: price
                ? [{amount: Number(price[1].replace(/,/g, '')), currency: 'GBP', basis: 'per person'}]
                    .concat(totalPrice ? [{amount: Number(totalPrice[1].replace(/,/g, '')), currency: 'GBP', basis: 'total based on 2 sharing'}] : [])
                : priceEvidence
                  ? [{amount: Number(priceEvidence[1].replace(/,/g, '')), currency: 'GBP', basis: 'displayed price'}]
                  : [],
              promotionSummary: discount
                ? bounded(`£${discount[1]} per person discount`)
                : bounded(promotionEvidence) || null
            };
          });
          return JSON.stringify({version: 1, candidates});
        })()
        """;
}
