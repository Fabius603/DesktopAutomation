const input = document.querySelector('#doc-search');
const list = document.querySelector('#search-results');
const status = document.querySelector('#search-status');
let references = [];
const normalize = text => text.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('de');
function search() {
  const terms = normalize(input.value.trim()).split(/\s+/).filter(Boolean);
  list.replaceChildren();
  if (!terms.length) { status.textContent = 'Suche in Anleitungen und allen Referenzen.'; return; }
  const results = references.filter(item => terms.every(term => normalize(item.title + ' ' + item.group + ' ' + item.terms).includes(term)));
  status.textContent = results.length ? `${results.length} Treffer${results.length > 30 ? ' · die ersten 30 werden angezeigt' : ''}` : 'Keine Treffer. Versuche einen anderen Begriff.';
  for (const item of results.slice(0, 30)) {
    const row = document.createElement('li');
    const link = document.createElement('a');
    link.href = item.url;
    link.textContent = item.title;
    const group = document.createElement('span');
    group.textContent = item.group;
    row.append(link, group);
    list.append(row);
  }
}
input.disabled = true;
fetch('/doku/search-index.json').then(response => {
  if (!response.ok) throw new Error('Search unavailable');
  return response.json();
}).then(data => {
  references = data;
  input.disabled = false;
  input.addEventListener('input', search);
  search();
}).catch(() => { status.textContent = 'Die Suche ist gerade nicht verfügbar. Öffne einen Bereich unten.'; });
