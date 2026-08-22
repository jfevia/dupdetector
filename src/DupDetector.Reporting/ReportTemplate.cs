namespace DupDetector.Reporting;

/// <summary>
///     The HTML shell of the report.
/// </summary>
public static class ReportTemplate
{

    private const string Content = """
<!DOCTYPE markup>
<markup lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>DupDetector Report</title>
<style>{{STYLE}}</style>
</head>
<body>
<header>
  <h1>DupDetector Report</h1>
  <span class="badge {{LABEL_CLASS}}">{{LABEL}}</span>
  <p class="meta" id="meta"></p>
</header>
<main>
  <section class="cards" aria-label="Summary">
    <div class="card">
      <span class="label">Duplication (code lines)</span>
      <span class="value">{{CODE_SCORE}}%</span>
      <span class="note">{{DUPLICATE_CODE_LINES}} of {{CODE_LINES}} analysable lines</span>
    </div>
    <div class="card">
      <span class="label">Duplication (physical)</span>
      <span class="value">{{SCORE}}%</span>
      <span class="note">{{DUPLICATE_LINES}} of {{TOTAL_LINES}} physical lines</span>
    </div>
    <div class="card">
      <span class="label">Clusters</span>
      <span class="value">{{CLUSTERS}}</span>
      <span class="note">{{SUPPRESSED}} more withheld by thresholds</span>
    </div>
    <div class="card">
      <span class="label">Files</span>
      <span class="value">{{FILES}}</span>
      <span class="note">{{EXCLUDED_FILES}} excluded during discovery</span>
    </div>
  </section>
  <section id="scope-section" hidden>
    <h2>What this report measured</h2>
    <ul id="scope" class="scope"></ul>
  </section>
  <section>
    <h2>File hotspots</h2>
    <div id="files"></div>
  </section>
  <section id="projects-section" hidden>
    <h2>Projects</h2>
    <div id="projects"></div>
  </section>
  <section>
    <h2>Clusters</h2>
    <label class="visually-hidden" for="filter">Filter clusters</label>
    <input id="filter" type="search" placeholder="Filter by id, file or member" />
    <p class="meta" id="cluster-count" role="status"></p>
    <table id="clusters">
      <caption class="visually-hidden">Duplicate clusters, sortable by column</caption>
      <thead><tr>
        <th aria-sort="descending"><button type="button" data-sort="score">Score</button></th>
        <th aria-sort="none"><button type="button" data-sort="removableLines">Removable</button></th>
        <th aria-sort="none"><button type="button" data-sort="lines">Lines</button></th>
        <th aria-sort="none"><button type="button" data-sort="occurrences">Copies</button></th>
        <th aria-sort="none"><button type="button" data-sort="fileSpread">Files</button></th>
        <th>Id</th>
        <th>Instances</th>
      </tr></thead>
      <tbody></tbody>
    </table>
  </section>
</main>
<script id="report-data" type="application/json">{{DATA}}</script>
<script>{{SCRIPT}}</script>
</body>
</markup>

""";

    /// <summary>
    ///     Gets the asset text.
    /// </summary>
    public static string Text
    {
        get
        {
            return Content;
        }
    }
}
