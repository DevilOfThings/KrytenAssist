namespace KrytenAssist.Avalonia.Cruises.Discovery;

public static class TuiCruiseCaptureScript
{
    public const int MaximumCandidates = 10;
    public const int MaximumFieldLength = 512;
    public const int MaximumSourceReferenceLength = 4_000;

    public const string Script = """
        (() => {
          const bounded = value => String(value || '').trim().slice(0, 512);
          const boundedReference = value => String(value || '').trim().slice(0, 4000);
          const boundedCardText = value => String(value || '').trim().slice(0, 4000);
          const roots = Array.from(document.querySelectorAll('tui-product-cards'))
            .map(element => element.shadowRoot)
            .filter(Boolean)
            .slice(0, 3);
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
              return /\/cruise\/bookitineraries\//i.test(url.pathname) &&
                (url.searchParams.has('itineraryCodeOne') || url.searchParams.has('itineraryCode'))
                ? url
                : null;
            } catch { return null; }
          };
          const shadowCardLinks = queryAll('a[href],[data-href],[data-url]')
            .map(element => ({
              element,
              card: element.closest('[data-testid="product-card"]'),
              url: itineraryUrl(element)
            }));
          const modernCards = Array.from(document.querySelectorAll('section.ResultListItem__cruiseResultItem'));
          const modernCardLinks = modernCards.flatMap(card =>
            Array.from(card.querySelectorAll('a[href],[data-href],[data-url]'))
              .map(element => ({element, card, url: itineraryUrl(element)})));
          const cardLinks = shadowCardLinks.concat(modernCardLinks)
            .slice(0, 100)
            .filter(item => item.card && item.url);
          const uniqueLinks = cardLinks.filter((item, index, items) =>
            index === items.findIndex(other => other.url.href === item.url.href));
          const wasTruncated = uniqueLinks.length > 10;
          const links = uniqueLinks.slice(0, 10);
          const shipNamePattern = /\bMarella\s+(?:Discovery(?:\s+2)?|Explorer(?:\s+2)?|Voyager)\b/i;
          const knownShips = {
            '150013': 'Marella Discovery 2',
            '150014': 'Marella Explorer',
            '150016': 'Marella Voyager'
          };
          const candidates = links.map(item => {
            const card = item.card;
            const url = item.url;
            const text = boundedCardText(card.innerText || card.textContent);
            const pathName = url.pathname.split('/').pop() || '';
            const code = url.searchParams.get('itineraryCodeOne') || url.searchParams.get('itineraryCode');
            const titlePart = code && pathName.endsWith(`-${code}`) ? pathName.slice(0, -(code.length + 1)) : pathName;
            const shipElement = card.querySelector('[data-ship-name],[data-ship],.ship-name,[data-testid*="ship" i]');
            const ship = text.match(shipNamePattern)?.[0] ||
              shipElement?.getAttribute('data-ship-name') ||
              shipElement?.textContent ||
              knownShips[url.searchParams.get('shipCode')];
            const price = text.match(/£\s*([\d,]+(?:\.\d{1,2})?)\s*(?:pp|per\s+person)\b/i);
            const totalPrice = text.match(/Total\s+Price\s*£\s*([\d,]+(?:\.\d{1,2})?)/i) ||
              text.match(/£\s*([\d,]+(?:\.\d{1,2})?)\s*Total\s+Price\b/i);
            const discount = text.match(/Includes\s+£\s*[\d,]+(?:\.\d{1,2})?\s*pp(?:\s+online)?\s+discount\b/i) ||
              text.match(/£\s*[\d,]+(?:\.\d{1,2})?\s*pp(?:\s+online)?\s+discount\b/i);
            return {
              sourceReference: boundedReference(url.href),
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
                : [],
              promotionSummary: discount
                ? bounded(discount[0])
                : null
            };
          });
          return JSON.stringify({version: 1, wasTruncated, candidates});
        })()
        """;
}
