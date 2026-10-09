const screens = {
  overview: { title: 'Alles im Überblick.', description: 'Die Startseite zeigt deine Jobs und Makros, geplante Automationen und laufende Ausführungen.', file: '/startseite.png', width: 1600, height: 980, alt: 'DesktopAutomation: Startübersicht mit geplanten Automationen für den Arbeitsplatz und die Dateisicherung.', caption: 'Startübersicht mit Beispieldaten.' },
  files: { title: 'Zwei Ordner nach Bestätigung sichern.', description: 'Eine Auswahl steuert den Kopierzweig. Ohne Bestätigung erscheint eine alternative Rückmeldung.', file: '/sicherung.png', width: 1600, height: 980, alt: 'Job Eingang mit Bestätigung sichern mit zwei Kopieraktionen und einem alternativen Zweig.', caption: 'Dateisicherung mit Benutzerauswahl und Bedingung.' },
  programs: { title: 'Programme bei Bedarf starten.', description: 'Der Prozessstatus entscheidet, ob der Editor gestartet wird. Anschließend öffnet der Job den Rechner.', file: '/arbeitsplatz.png', width: 1600, height: 980, alt: 'Job Arbeitsplatz nach Zeitplan mit Prozessabfrage, bedingtem Start und Abschlussmeldung.', caption: 'Prozessstatus und bedingte Programmstarts.' },
  choice: { title: 'Unterlagen passend vorbereiten.', description: 'Die Antwort steuert, welche Dateien kopiert werden. Ihr Label wird am Ende als Ergebnis weiterverwendet.', file: '/unterlagen.png', width: 1600, height: 980, alt: 'Job Arbeitsunterlagen vorbereiten mit Benutzerauswahl, zwei Zweigen und einer Ergebnisbindung.', caption: 'Verzweigter Job mit Datenfluss und optionalem Makro.' },
  automations: { title: 'Den Start festlegen.', description: 'Verknüpfe einen Auslöser mit einem Job und lege fest, wie weitere Starts während einer laufenden Ausführung behandelt werden.', file: '/automationen.png', width: 1600, height: 980, alt: 'Automation Arbeitsplatz nach Zeitplan mit Werktagen, Startzeit und Ausführungsregeln.', caption: 'Automationseinstellungen im Light-Theme mit Beispieldaten.' },
  macros: { title: 'Eingaben als Makro bearbeiten.', description: 'Ordne Klicks, Texteingaben und Tastenkombinationen in Gruppen. Passe einzelne Eingaben und ihre Verzögerungen an.', file: '/makro-editor.png', width: 1600, height: 980, alt: 'Makro-Editor Rechnung erfassen mit gruppierten Maus- und Tastatureingaben und einer Tastenkombination im Inspektor.', caption: 'Makro-Editor mit Gruppen und Ablaufvorschau.' },
  history: { title: 'Ausführungen nachvollziehen.', description: 'Sieh, welche Abläufe erfolgreich waren, noch laufen oder ein Problem hatten. Filtere den Verlauf und öffne die Details.', file: '/verlauf.png', width: 1600, height: 980, alt: 'Verlaufsübersicht mit erfolgreichen und fehlgeschlagenen Beispielausführungen sowie Warnungen.', caption: 'Ausführungsverlauf im Light-Theme mit Beispieldaten.' }
};
const tabs = [...document.querySelectorAll('[data-screen]')];
function selectScreen(key) {
  const item = screens[key];
  if (!item) return;
  document.querySelector('#screen-title').textContent = item.title;
  document.querySelector('#screen-description').textContent = item.description;
  const img = document.querySelector('#screen-image');
  img.src = item.file;
  img.alt = item.alt;
  img.width = item.width;
  img.height = item.height;
  document.querySelector('#screen-link').href = item.file;
  document.querySelector('#screen-full').href = item.file;
  document.querySelector('#screen-caption').textContent = item.caption;
  document.querySelector('#screen-panel').setAttribute('aria-labelledby', 'tab-' + key);
  tabs.forEach(tab => {
    const active = tab.dataset.screen === key;
    tab.setAttribute('aria-selected', String(active));
    tab.tabIndex = active ? 0 : -1;
  });
}
tabs.forEach((tab, index) => {
  tab.addEventListener('click', () => selectScreen(tab.dataset.screen));
  tab.addEventListener('keydown', event => {
    let next;
    if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
    else if (event.key === 'ArrowLeft') next = (index - 1 + tabs.length) % tabs.length;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = tabs.length - 1;
    else return;
    event.preventDefault();
    tabs[next].focus();
    selectScreen(tabs[next].dataset.screen);
  });
});
document.querySelectorAll('[data-screen-target]').forEach(link => {
  link.addEventListener('click', () => selectScreen(link.dataset.screenTarget));
});
const menuToggle = document.querySelector('.menu-toggle');
const mainNav = document.querySelector('#main-nav');
function closeMenu() {
  menuToggle.setAttribute('aria-expanded', 'false');
  mainNav.classList.remove('is-open');
}
menuToggle.addEventListener('click', () => {
  const open = menuToggle.getAttribute('aria-expanded') !== 'true';
  menuToggle.setAttribute('aria-expanded', String(open));
  mainNav.classList.toggle('is-open', open);
});
mainNav.querySelectorAll('a').forEach(link => link.addEventListener('click', closeMenu));
document.addEventListener('keydown', event => {
  if (event.key === 'Escape' && menuToggle.getAttribute('aria-expanded') === 'true') {
    closeMenu();
    menuToggle.focus();
  }
});

// Direct links to technical fields also work when their reference is collapsed.
const docsNavigation = document.querySelector('.docs-navigation');
if (docsNavigation && window.matchMedia('(max-width:800px)').matches) {
  docsNavigation.open = false;
}
function revealReferenceHash() {
  let id;
  try { id = decodeURIComponent(location.hash.slice(1)); } catch { return; }
  if (!id) return;
  const target = document.getElementById(id);
  if (!target) return;
  let parent = target;
  while (parent) {
    if (parent.tagName === 'DETAILS') parent.open = true;
    parent = parent.parentElement;
  }
  target.scrollIntoView({ block: 'start' });
}
window.addEventListener('hashchange', revealReferenceHash);
document.addEventListener('click', event => {
  const link = event.target.closest('a[href]');
  if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
  const destination = new URL(link.href, location.href);
  if (destination.origin === location.origin && destination.pathname === location.pathname &&
      destination.hash && destination.hash === location.hash) revealReferenceHash();
});
// Keep anchor and sidebar offsets correct when the breadcrumb wraps or text is enlarged.
const siteHeader = document.querySelector('.site-header-shell');
if (siteHeader) {
  const updateHeaderOffset = () => document.documentElement.style.setProperty('--site-top', siteHeader.getBoundingClientRect().height + 'px');
  updateHeaderOffset();
  new ResizeObserver(updateHeaderOffset).observe(siteHeader);
}
revealReferenceHash();
