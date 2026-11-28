/* NISMA — atelier. Aucune dépendance : le surlignage est lexical, la compilation
   est faite par NISMA Core derrière /api/compile. */

(function () {
  'use strict';

  var KEYWORDS = {
    // sections
    declare: 1, modifiers: 1, model: 1, process: 1, start: 1,
    // déclarations
    grid: 1, update: 1, seed: 1, agent: 1, abstract: 1, tag: 1, field: 1,
    produces: 1, absorbs: 1, lets: 1, with: 1, description: 1,
    mob: 1, life: 1, diff: 1, dirs: 1, opacity: 1, walls: 1, icon: 1, size: 1, desc: 1,
    parallel: 1, sequential: 1,
    // comportements
    spawns: 1, dies: 1, reacts: 1, into: 1, neighbour: 1, when: 1,
    and: 1, or: 1, not: 1, finds: 1, only: 1, nothing: 1, any: 1,
    in: 1, near: 1, age: 1, step: 1, at: 1, is: 1, p: 1,
    // modificateurs et actions
    every: 1, steps: 1, everywhere: 1, set: 1, add: 1, pause: 1, capture: 1,
    stop: 1, activate: 1, deactivate: 1, square: 1, random: 1,
    // processus
    becomes: 1, stays: 1, moves: 1, after: 1, neighbour: 1
  };

  var TOKEN = new RegExp(
    '(\\/\\/[^\\n]*)' +                       // 1 commentaire
    '|("(?:[^"\\\\]|\\\\.)*")' +              // 2 chaîne
    '|(^\\s*#{1,2}[^\\n]*)' +                 // 3 titre de section ou de bloc
    '|(^\\s*[-*]\\s)' +                       // 4 puce
    '|(\\b\\d+(?:\\.\\d+)?\\b)' +             // 5 nombre
    '|([A-Za-z_\\u00C0-\\u024F][\\w\\u00C0-\\u024F]*)' + // 6 mot
    '|(->|&&|\\|\\||>=|<=|==|!=|\\u2208|[><=!%])' +      // 7 opérateur
    '|([()\\[\\],;:.])',                      // 8 ponctuation
    'g'
  );

  var source = document.getElementById('source');
  var highlight = document.getElementById('highlight');
  var gutter = document.getElementById('gutter');
  var statusEl = document.getElementById('status');
  var compileBtn = document.getElementById('compile');
  var downloadBtn = document.getElementById('download');
  var examplesEl = document.getElementById('examples');
  var jsonEl = document.getElementById('json');
  var summaryEl = document.getElementById('tab-summary');
  var problemsEl = document.getElementById('problems');
  var problemCount = document.getElementById('problemCount');

  var errorLine = 0;
  var lastNbd2 = null;
  var lastName = 'modele.nbd2';
  var busy = false;

  // ---------------------------------------------------------------- surlignage

  function escapeHtml(text) {
    return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }

  function span(cls, text) {
    return '<span class="' + cls + '">' + escapeHtml(text) + '</span>';
  }

  function classifyWord(word) {
    if (KEYWORDS[word.toLowerCase()]) return span('kw', word);
    if (/^[A-Z\u00C0-\u00DE]/.test(word)) return span('typ', word);
    return escapeHtml(word);
  }

  function highlightLine(line) {
    var out = '';
    var last = 0;
    var match;
    TOKEN.lastIndex = 0;
    while ((match = TOKEN.exec(line)) !== null) {
      if (match.index > last) out += escapeHtml(line.slice(last, match.index));
      var token = match[0];
      if (match[1]) out += span('com', token);
      else if (match[2]) out += span('str', token);
      else if (match[3]) out += span('hdr', token);
      else if (match[4]) out += span('bul', token);
      else if (match[5]) out += span('num', token);
      else if (match[6]) out += classifyWord(token);
      else if (match[7]) out += span('op', token);
      else out += span('pun', token);
      last = match.index + token.length;
    }
    out += escapeHtml(line.slice(last));
    return out;
  }

  function render() {
    var lines = source.value.split('\n');
    var html = '';
    var numbers = '';
    for (var i = 0; i < lines.length; i++) {
      var isError = (i + 1) === errorLine;
      html += '<div class="line' + (isError ? ' error' : '') + '">' +
        (highlightLine(lines[i]) || '\u200b') + '</div>';
      numbers += '<div' + (isError ? ' class="error"' : '') + '>' + (i + 1) + '</div>';
    }
    highlight.innerHTML = html;
    gutter.innerHTML = numbers;
    syncScroll();
  }

  function syncScroll() {
    highlight.scrollTop = source.scrollTop;
    highlight.scrollLeft = source.scrollLeft;
    gutter.scrollTop = source.scrollTop;
  }

  // ---------------------------------------------------------------- état

  function setStatus(text, kind) {
    statusEl.textContent = text;
    statusEl.className = 'status' + (kind ? ' ' + kind : '');
  }

  function setBusy(value) {
    busy = value;
    compileBtn.disabled = value;
    document.body.style.cursor = value ? 'progress' : '';
  }

  function goToLine(line) {
    var lines = source.value.split('\n');
    var offset = 0;
    for (var i = 0; i < line - 1 && i < lines.length; i++) offset += lines[i].length + 1;
    source.focus();
    source.setSelectionRange(offset, offset + (lines[line - 1] || '').length);
    var perLine = parseFloat(getComputedStyle(source).lineHeight) || 21;
    source.scrollTop = Math.max(0, (line - 3) * perLine);
    syncScroll();
  }

  // ---------------------------------------------------------------- onglets

  function activateTab(name) {
    var buttons = document.querySelectorAll('.tabs button');
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].classList.toggle('active', buttons[i].getAttribute('data-tab') === name);
    }
    var tabs = document.querySelectorAll('.tab');
    for (var j = 0; j < tabs.length; j++) {
      tabs[j].classList.toggle('active', tabs[j].id === 'tab-' + name);
    }
    if (name === 'help') loadHelp();
  }

  // ---------------------------------------------------------------- compilation

  function showProblems(problems) {
    problemsEl.innerHTML = '';
    if (!problems.length) {
      problemCount.classList.add('hidden');
      problemsEl.innerHTML = '<li class="empty">Aucun problème.</li>';
      return;
    }
    problemCount.textContent = problems.length;
    problemCount.classList.remove('hidden');
    problems.forEach(function (problem) {
      var li = document.createElement('li');
      var where = document.createElement('div');
      where.className = 'where';
      where.textContent = problem.line > 0 ? 'ligne ' + problem.line : 'erreur';
      where.addEventListener('click', function () {
        if (problem.line > 0) goToLine(problem.line);
      });
      var what = document.createElement('div');
      what.className = 'what';
      what.textContent = problem.message;
      li.appendChild(where);
      li.appendChild(what);
      problemsEl.appendChild(li);
    });
  }

  function showSummary(summary, notes) {
    var html = '<dl class="summary-grid">' +
      '<dt>grains</dt><dd>' + summary.grains + '</dd>' +
      '<dt>champs</dt><dd>' + summary.fields + '</dd>' +
      '<dt>comportements</dt><dd>' + summary.behaviours + '</dd>' +
      '<dt>modificateurs</dt><dd>' + summary.modifiers + '</dd>' +
      '<dt>cellules</dt><dd>' + summary.cells + '</dd>' +
      '<dt>fichier</dt><dd>' + summary.bytes + ' octets</dd>' +
      '</dl>';
    if (notes && notes.length) {
      html += '<ul class="notes">';
      notes.forEach(function (note) { html += '<li>' + escapeHtml(note) + '</li>'; });
      html += '</ul>';
    }
    summaryEl.innerHTML = html;
  }

  function compile() {
    if (busy) return;
    setBusy(true);
    setStatus('compilation…');

    fetch('/api/compile', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ script: source.value })
    })
      .then(function (response) { return response.json(); })
      .then(function (data) {
        if (data.ok) {
          lastNbd2 = data.nbd2;
          lastName = guessFileName();
          downloadBtn.disabled = false;
          errorLine = 0;
          render();
          showProblems([]);
          showSummary(data.summary, data.notes);
          jsonEl.textContent = data.nbd2;
          setStatus(data.summary.grains + ' grains · ' + data.summary.behaviours +
            ' comportements · ' + data.summary.cells + ' cellules', 'ok');
          activateTab('summary');
        } else {
          lastNbd2 = null;
          downloadBtn.disabled = true;
          errorLine = data.error.line || 0;
          render();
          showProblems([{ line: data.error.line || 0, message: data.error.message }]);
          setStatus('erreur ligne ' + (data.error.line || '?'), 'bad');
          activateTab('problems');
        }
      })
      .catch(function (error) {
        setStatus('serveur injoignable', 'bad');
        showProblems([{ line: 0, message: String(error) }]);
        activateTab('problems');
      })
      .finally(function () { setBusy(false); });
  }

  function guessFileName() {
    var match = source.value.match(/^\s*-\s*model\s+"([^"]+)"/m);
    if (!match) return 'modele.nbd2';
    return match[1].replace(/[^\w\-À-ÿ ]+/g, '').trim().replace(/\s+/g, '_') + '.nbd2';
  }

  function download() {
    if (!lastNbd2) return;
    var blob = new Blob([lastNbd2], { type: 'application/json' });
    var url = URL.createObjectURL(blob);
    var link = document.createElement('a');
    link.href = url;
    link.download = lastName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
  }

  // ---------------------------------------------------------------- exemples

  function loadExamples() {
    fetch('/api/examples')
      .then(function (response) { return response.json(); })
      .then(function (examples) {
        if (!examples.length) {
          examplesEl.innerHTML = '<option value="">aucun exemple</option>';
          return;
        }
        var html = '<option value="">exemples…</option>';
        examples.forEach(function (example, index) {
          html += '<option value="' + index + '">' + escapeHtml(example.name) + '</option>';
        });
        examplesEl.innerHTML = html;
        examplesEl._examples = examples;
      })
      .catch(function () { examplesEl.innerHTML = '<option value="">exemples indisponibles</option>'; });
  }

  examplesEl.addEventListener('change', function () {
    var examples = examplesEl._examples || [];
    var index = examplesEl.value;
    if (index === '') return;
    var example = examples[Number(index)];
    if (!example) return;
    source.value = example.source;
    errorLine = 0;
    render();
    setStatus(example.name, '');
  });

  // ---------------------------------------------------------------- événements

  source.addEventListener('input', function () {
    render();
    save();
  });
  source.addEventListener('scroll', syncScroll);

  source.addEventListener('keydown', function (event) {
    if (event.key === 'Tab') {
      event.preventDefault();
      insertAtCursor('  ');
      return;
    }
    if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
      event.preventDefault();
      compile();
      return;
    }
    if ((event.ctrlKey || event.metaKey) && (event.key === 's' || event.key === 'S')) {
      event.preventDefault();
      download();
    }
  });

  function insertAtCursor(text) {
    var start = source.selectionStart;
    var end = source.selectionEnd;
    source.value = source.value.slice(0, start) + text + source.value.slice(end);
    source.selectionStart = source.selectionEnd = start + text.length;
    render();
    save();
  }

  compileBtn.addEventListener('click', compile);
  downloadBtn.addEventListener('click', download);

  var tabButtons = document.querySelectorAll('.tabs button');
  for (var i = 0; i < tabButtons.length; i++) {
    tabButtons[i].addEventListener('click', function () {
      activateTab(this.getAttribute('data-tab'));
    });
  }

  // ---------------------------------------------------------------- persistance

  function save() {
    try {
      localStorage.setItem('nisma.ide.source', source.value);
    } catch (error) { /* mode privé : on continue sans persistance */ }
  }

  function restore() {
    var saved = null;
    try { saved = localStorage.getItem('nisma.ide.source'); } catch (error) { saved = null; }
    source.value = saved || [
      '// Bienvenue dans l\'atelier NISMA.',
      '// Ctrl+Entrée compile, Ctrl+S enregistre le .nbd2.',
      '',
      '# declare',
      '- grid 50 50',
      '- seed 42',
      '- agent Cell [Lime] walls',
      '',
      '# model',
      '- Birth: Cell spawns when finds 3 Cell',
      '- Death: Cell dies when finds Cell < 2 or finds Cell > 3',
      '',
      '# start',
      '- 178 Cell random',
      ''
    ].join('\n');
  }

  /* ---- aide du langage -------------------------------------------------- */

  var helpNav = document.getElementById('help-nav');
  var helpBody = document.getElementById('help-body');
  var helpIndex = document.getElementById('help-index');
  var helpSearch = document.getElementById('help-search');
  var helpToggle = document.getElementById('help-toggle');
  var helpTopics = null;
  var helpChapters = [];
  var helpAllOpen = false;

  function normalize(text) {
    var lower = (text || '').toLowerCase();
    if (lower.normalize) lower = lower.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    return lower;
  }

  /* Le HTML rendu est plat : chaque titre est regroupé avec ce qui le suit dans un
     <details>. On obtient des sections repliables sans toucher au Markdown, et un
     sommaire qui connaît la hiérarchie des titres. */
  function structureHelp() {
    var nodes = Array.prototype.slice.call(helpBody.childNodes);
    helpBody.innerHTML = '';
    helpChapters = [];

    var stack = [{ level: 1, el: helpBody }];
    for (var i = 0; i < nodes.length; i++) {
      var node = nodes[i];
      if (!(node.nodeType === 1 && /^H[1-3]$/.test(node.tagName))) {
        stack[stack.length - 1].el.appendChild(node);
        continue;
      }

      var level = parseInt(node.tagName.charAt(1), 10);
      if (level === 1) {
        var title = document.createElement('h1');
        title.className = 'doc-title';
        title.textContent = node.textContent;
        helpBody.appendChild(title);
        stack = [{ level: 1, el: helpBody }];
        continue;
      }

      while (stack.length > 1 && stack[stack.length - 1].level >= level) stack.pop();

      var details = document.createElement('details');
      details.className = 'chapter lvl-' + level;
      var summary = document.createElement('summary');
      summary.textContent = node.textContent;
      details.appendChild(summary);
      var body = document.createElement('div');
      body.className = 'chapter-body';
      details.appendChild(body);
      stack[stack.length - 1].el.appendChild(details);
      stack.push({ level: level, el: body });

      helpChapters.push({
        level: level,
        text: node.textContent,
        key: normalize(node.textContent),
        details: details
      });
    }

    for (var c = 0; c < helpChapters.length; c++)
      helpChapters[c].body = normalize(helpChapters[c].details.textContent);
  }

  function buildHelpIndex() {
    helpIndex.innerHTML = '';
    for (var i = 0; i < helpChapters.length; i++) {
      (function (chapter, index) {
        var item = document.createElement('button');
        item.className = 'index-item lvl-' + chapter.level;
        item.textContent = chapter.text;
        item.setAttribute('data-i', String(index));
        item.title = chapter.text;
        item.addEventListener('click', function () { revealChapter(index); });
        helpIndex.appendChild(item);
      })(helpChapters[i], i);
    }
  }

  function revealChapter(index) {
    var chapter = helpChapters[index];
    if (!chapter) return;
    var node = chapter.details;
    while (node) {
      if (node.tagName === 'DETAILS') node.open = true;
      node = node.parentElement;
    }
    chapter.details.scrollIntoView({ block: 'start' });
    var items = helpIndex.querySelectorAll('.index-item');
    for (var i = 0; i < items.length; i++) items[i].classList.toggle('active', i === index);
  }

  /* Recherche : le sommaire se filtre sur le titre *et* sur le texte de la section. */
  function filterHelp() {
    var query = normalize(helpSearch.value).trim();
    var items = helpIndex.querySelectorAll('.index-item');
    var matches = [];
    for (var i = 0; i < items.length; i++) {
      var chapter = helpChapters[i];
      var match = !query || chapter.key.indexOf(query) >= 0 || chapter.body.indexOf(query) >= 0;
      items[i].classList.toggle('hidden', !match);
      if (match && query) matches.push(i);
    }
    if (matches.length === 1) revealChapter(matches[0]);
  }

  function setAllChapters(open) {
    helpAllOpen = open;
    var all = helpBody.querySelectorAll('details.chapter');
    for (var i = 0; i < all.length; i++) all[i].open = open;
    helpToggle.textContent = open ? 'Tout replier' : 'Tout déplier';
  }

  function showHelpTopic(index) {
    if (!helpTopics || !helpTopics[index]) return;
    helpBody.innerHTML = helpTopics[index].html;
    structureHelp();
    buildHelpIndex();
    helpSearch.value = '';
    setAllChapters(false);
    var chips = helpNav.querySelectorAll('button');
    for (var i = 0; i < chips.length; i++) chips[i].classList.toggle('active', i === index);
  }

  function loadHelp() {
    if (helpTopics) return;
    helpTopics = [];
    fetch('/api/help')
      .then(function (response) { return response.json(); })
      .then(function (data) {
        helpTopics = data.topics || [];
        helpNav.innerHTML = '';
        for (var i = 0; i < helpTopics.length; i++) {
          (function (topic, index) {
            var chip = document.createElement('button');
            chip.textContent = topic.title;
            chip.addEventListener('click', function () { showHelpTopic(index); });
            helpNav.appendChild(chip);
          })(helpTopics[i], i);
        }
        if (helpTopics.length) showHelpTopic(0);
      })
      .catch(function () {
        helpBody.innerHTML = '<p class="empty">Aide indisponible : le serveur ne répond pas.</p>';
      });
  }

  if (helpSearch) helpSearch.addEventListener('input', filterHelp);
  if (helpToggle) helpToggle.addEventListener('click', function () { setAllChapters(!helpAllOpen); });

  /* ---- tutoriels -------------------------------------------------------- */

  var tutorialButton = document.getElementById('tutorials');
  var tutorialDialog = document.getElementById('tutorial-dialog');
  var tutorialList = document.getElementById('tutorial-list');
  var tutorialContent = document.getElementById('tutorial-content');
  var tutorialsAsked = false;

  function openDialog(dialog) {
    if (!dialog || dialog.open) return;
    if (typeof dialog.showModal === 'function') dialog.showModal();
    else dialog.setAttribute('open', 'open');
  }

  function useScript(code) {
    source.value = code.replace(/\s+$/, '') + '\n';
    render();
    save();
    if (tutorialDialog && tutorialDialog.open) tutorialDialog.close();
    setStatus('script chargé dans l\'éditeur');
  }

  /* Un bouton sur chaque modèle complet : le tutoriel montre, l'éditeur essaie. */
  function addEditorButtons(root) {
    var blocks = root.querySelectorAll('pre[data-lang="ni"]');
    for (var i = 0; i < blocks.length; i++) {
      (function (pre) {
        var box = document.createElement('div');
        box.className = 'code-box';
        pre.parentNode.insertBefore(box, pre);
        box.appendChild(pre);
        var button = document.createElement('button');
        button.className = 'code-open';
        button.textContent = 'Ouvrir dans l\'éditeur';
        button.addEventListener('click', function () { useScript(pre.textContent); });
        box.appendChild(button);
      })(blocks[i]);
    }
  }

  function showTutorial(id, item) {
    var items = tutorialList.querySelectorAll('button');
    for (var i = 0; i < items.length; i++) items[i].classList.toggle('active', items[i] === item);
    tutorialContent.innerHTML = '<p class="empty">Chargement…</p>';
    fetch('/api/tutorials/' + encodeURIComponent(id))
      .then(function (response) { return response.json(); })
      .then(function (data) {
        tutorialContent.innerHTML = data.html;
        tutorialContent.scrollTop = 0;
        addEditorButtons(tutorialContent);
      })
      .catch(function () {
        tutorialContent.innerHTML = '<p class="empty">Tutoriel indisponible.</p>';
      });
  }

  function loadTutorials() {
    if (tutorialsAsked) return;
    tutorialsAsked = true;
    fetch('/api/tutorials')
      .then(function (response) { return response.json(); })
      .then(function (data) {
        var list = data.tutorials || [];
        tutorialList.innerHTML = '';
        for (var i = 0; i < list.length; i++) {
          (function (tutoriel, first) {
            var item = document.createElement('button');
            item.className = 'tutorial-item';
            var title = document.createElement('strong');
            title.textContent = tutoriel.title;
            var summary = document.createElement('span');
            summary.textContent = tutoriel.summary;
            item.appendChild(title);
            item.appendChild(summary);
            item.addEventListener('click', function () { showTutorial(tutoriel.id, item); });
            tutorialList.appendChild(item);
            if (first) showTutorial(tutoriel.id, item);
          })(list[i], i === 0);
        }
        if (!list.length) tutorialList.innerHTML = '<p class="empty">Aucun tutoriel.</p>';
      })
      .catch(function () { tutorialList.innerHTML = '<p class="empty">Liste indisponible.</p>'; });
  }

  if (tutorialButton) tutorialButton.addEventListener('click', function () {
    openDialog(tutorialDialog);
    loadTutorials();
  });

  /* ---- à propos ---------------------------------------------------------- */

  var aboutButton = document.getElementById('about');
  var aboutDialog = document.getElementById('about-dialog');
  var aboutAsked = false;

  function openAbout() {
    if (!aboutDialog) return;
    if (!aboutDialog.open) {
      if (typeof aboutDialog.showModal === 'function') aboutDialog.showModal();
      else aboutDialog.setAttribute('open', 'open');
    }
    if (aboutAsked) return;
    aboutAsked = true;
    fetch('/api/about')
      .then(function (response) { return response.json(); })
      .then(function (data) {
        document.getElementById('about-version').textContent = data.version || 'inconnue';
        document.getElementById('about-commit').textContent = data.commit || 'inconnu';
        var link = document.getElementById('about-repo');
        if (data.repository) link.href = data.repository;
      })
      .catch(function () {
        document.getElementById('about-version').textContent = 'indisponible';
        document.getElementById('about-commit').textContent = 'inconnu';
      });
  }

  if (aboutButton) aboutButton.addEventListener('click', openAbout);

  restore();
  render();
  loadExamples();
  setStatus('prêt');
})();
