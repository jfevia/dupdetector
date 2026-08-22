(function () {
  "use strict";

  var report = JSON.parse(document.getElementById("report-data").textContent);
  var clusters = report.clusters || [];
  var files = report.fileScores || [];
  var projects = report.projectScores || [];
  var sortKey = "score";
  var ascending = false;
  var filterTimer = null;

  function text(value) {
    var node = document.createElement("span");
    node.textContent = value == null ? "" : value;
    return node.innerHTML;
  }

  function pct(value) {
    return (value || 0).toFixed(1) + "%";
  }

  function barRow(label, percentage, title) {
    return '<div class="bar"><span class="name" title="' + text(title) + '">' + text(label) +
      '</span><span class="track"><span class="fill" style="width:' + Math.min(percentage || 0, 100) +
      '%"></span></span><span class="pct">' + pct(percentage) + "</span></div>";
  }

  function renderMeta() {
    var meta = report.metadata;
    if (!meta) { return; }
    var parts = ["v" + meta.toolVersion, meta.generatedAtUtc, meta.targetPath];
    if (meta.commit) { parts.push(meta.commit.slice(0, 12)); }
    var host = document.getElementById("meta");
    host.textContent = parts.join("  \u00b7  ");
    host.title = meta.commandLine;
  }

  function renderScope() {
    var scope = report.scope;
    if (!scope || !scope.limitations || scope.limitations.length === 0) { return; }
    document.getElementById("scope-section").hidden = false;
    document.getElementById("scope").innerHTML = scope.limitations.map(function (note) {
      return "<li>" + text(note) + "</li>";
    }).join("");
  }

  function renderFiles() {
    var host = document.getElementById("files");
    var top = files.slice().sort(function (a, b) { return b.percentage - a.percentage; }).slice(0, 20);
    if (top.length === 0) {
      host.textContent = "No files analysed.";
      return;
    }
    host.innerHTML = top.map(function (file) {
      var detail = file.duplicateLines + " of " + file.totalLines + " lines, " +
        file.clusterCount + " cluster(s), widest spans " + file.widestClusterSpread + " file(s)";
      return barRow(file.file, file.percentage, file.file + " \u2014 " + detail);
    }).join("");
  }

  function renderProjects() {
    if (projects.length === 0) { return; }
    document.getElementById("projects-section").hidden = false;
    document.getElementById("projects").innerHTML = projects.map(function (project) {
      return barRow(project.project, project.percentage,
        project.project + ": " + project.duplicateLines + " of " + project.totalLines + " lines");
    }).join("");
  }

  function matches(cluster, needle) {
    if (!needle) { return true; }
    if (cluster.id.toLowerCase().indexOf(needle) >= 0) { return true; }
    return cluster.instances.some(function (instance) {
      return instance.file.toLowerCase().indexOf(needle) >= 0 ||
        instance.member.toLowerCase().indexOf(needle) >= 0;
    });
  }

  function renderClusters() {
    var needle = document.getElementById("filter").value.trim().toLowerCase();
    var rows = clusters.filter(function (cluster) { return matches(cluster, needle); });

    rows.sort(function (a, b) {
      var left = a[sortKey];
      var right = b[sortKey];
      if (left === right) { return a.id < b.id ? -1 : 1; }
      return ascending ? left - right : right - left;
    });

    document.getElementById("cluster-count").textContent =
      rows.length + " of " + clusters.length + " clusters shown";

    document.querySelector("#clusters tbody").innerHTML = rows.map(function (cluster) {
      var tags = "";
      if (cluster.isExact) { tags += '<span class="tag exact" title="Every copy is structurally identical.">exact</span>'; }
      if (cluster.isProductionDuplicate) {
        tags += '<span class="tag prod" title="Exact, spans at least two projects, and AT LEAST ONE copy is production code. Some copies may be tests.">production</span>';
      }
      if (cluster.isCohesive === false) {
        tags += '<span class="tag partial" title="The grouping budget was exhausted; some members may not resemble one another.">partial</span>';
      }
      var instances = cluster.instances.map(function (instance) {
        return text(instance.file) + ' <span class="tag">' + text(instance.member) + "</span> " +
          instance.startLine + "-" + instance.endLine;
      }).join("<br />");
      var snippet = cluster.normalizedSnippet
        ? "<details><summary>Normalized shape</summary><pre>" + text(cluster.normalizedSnippet) + "</pre></details>"
        : "";
      var id = text(cluster.id);
      return '<tr id="' + id + '"><td>' + cluster.score.toFixed(2) + "</td><td>" + cluster.removableLines +
        "</td><td>" + cluster.lines + "</td><td>" + cluster.occurrences + "</td><td>" + cluster.fileSpread +
        '</td><td class="mono"><a href="#' + id + '">' + id + "</a>" + tags + "</td><td>" +
        instances + snippet + "</td></tr>";
    }).join("");
  }

  Array.prototype.forEach.call(document.querySelectorAll("#clusters th button[data-sort]"), function (button) {
    button.addEventListener("click", function () {
      var key = button.getAttribute("data-sort");
      ascending = key === sortKey ? !ascending : false;
      sortKey = key;
      Array.prototype.forEach.call(document.querySelectorAll("#clusters thead th"), function (header) {
        if (header.hasAttribute("aria-sort")) { header.setAttribute("aria-sort", "none"); }
      });
      button.parentNode.setAttribute("aria-sort", ascending ? "ascending" : "descending");
      renderClusters();
    });
  });

  document.getElementById("filter").addEventListener("input", function () {
    // Debounced: filtering re-renders every row, which stalls typing on large reports.
    window.clearTimeout(filterTimer);
    filterTimer = window.setTimeout(renderClusters, 150);
  });

  renderMeta();
  renderScope();
  renderFiles();
  renderProjects();
  renderClusters();
})();
