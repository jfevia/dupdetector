using System.Text;
using System.Text.Json;

namespace DupDetector;

/// <summary>
/// Renders a DetectionReport as YAML, JSON, or HTML.
/// </summary>
public class Reporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string Render(DetectionReport report, string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => JsonSerializer.Serialize(report, JsonOptions),
            "html" => RenderHtml(report),
            _ => RenderYaml(report)
        };
    }

    // ----------------------------------------------------------------
    // Simple recursive YAML serializer (no external library)
    // ----------------------------------------------------------------

    private static string RenderYaml(DetectionReport report)
    {
        var sb = new StringBuilder();
        AppendYamlObject(sb, report, 0);
        return sb.ToString();
    }

    private static void AppendYamlValue(StringBuilder sb, object? value, int indent)
    {
        switch (value)
        {
            case null:
                sb.AppendLine("null");
                break;
            case string s:
                sb.AppendLine(YamlEscapeString(s));
                break;
            case bool b:
                sb.AppendLine(b ? "true" : "false");
                break;
            case int or long or short or byte:
                sb.AppendLine(value?.ToString() ?? "null");
                break;
            case double d:
                sb.AppendLine(d.ToString("G"));
                break;
            case float f:
                sb.AppendLine(f.ToString("G"));
                break;
            case System.Collections.IEnumerable list:
                sb.AppendLine();
                foreach (var item in list)
                {
                    sb.Append(new string(' ', indent));
                    sb.Append("- ");
                    if (item is string || item is int || item is long || item is double || item is bool)
                    {
                        AppendYamlValue(sb, item, indent + 2);
                    }
                    else
                    {
                        sb.AppendLine();
                        AppendYamlObject(sb, item, indent + 2);
                    }
                }
                break;
            default:
                // Complex object
                sb.AppendLine();
                AppendYamlObject(sb, value, indent);
                break;
        }
    }

    private static void AppendYamlObject(StringBuilder sb, object obj, int indent)
    {
        var type = obj.GetType();
        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var prop in props)
        {
            var key = ToCamelCase(prop.Name);
            var val = prop.GetValue(obj);
            sb.Append(new string(' ', indent));
            sb.Append(key);
            sb.Append(": ");
            AppendYamlValue(sb, val, indent + 2);
        }
    }

    private static string YamlEscapeString(string s)
    {
        if (s.Length == 0) return "\"\"";
        if (s.Any(c => c == ':' || c == '#' || c == '\n' || c == '\r' || c == '"' || c == '\''))
        {
            var escaped = s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
            return $"\"{escaped}\"";
        }
        return s;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    // ----------------------------------------------------------------
    // Static HTML report (vanilla JS + inline CSS, no backend)
    // Uses C# raw string literal ($$""") to avoid escaping conflicts
    // with embedded JavaScript.
    // ----------------------------------------------------------------

    private static string RenderHtml(DetectionReport report)
    {
        var summary = report.Summary;
        var scoreColor = summary.ScoreLabel switch
        {
            "low" => "#22c55e",
            "medium" => "#f59e0b",
            "high" => "#f97316",
            _ => "#ef4444"
        };
        var scoreLabel = char.ToUpperInvariant(summary.ScoreLabel[0]) + summary.ScoreLabel[1..];
        var score = summary.DuplicationScore.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        var totalDups = summary.TotalDuplicates.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var totalDupLines = summary.TotalDuplicateLines.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        var totalFiles = summary.TotalFiles.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var clustersJson = JsonSerializer.Serialize(report.Clusters, JsonOptions);
        var fileScoresJson = JsonSerializer.Serialize(report.FileScores, JsonOptions);
        var projectScoresJson = JsonSerializer.Serialize(report.ProjectScores, JsonOptions);

        var sb = new StringBuilder();
        sb.Append(HtmlHead);
        sb.Append($$"""
  <div class="cards">
    <div class="card">
      <div class="label">Duplication Score</div>
      <div class="value" style="color:{{scoreColor}}">{{score}}</div>
      <div class="sub"><span class="score-badge" style="background:{{scoreColor}}22;color:{{scoreColor}}">{{scoreLabel}}</span></div>
    </div>
    <div class="card">
      <div class="label">Duplicate Clusters</div>
      <div class="value">{{totalDups}}</div>
      <div class="sub">unique duplicate groups</div>
    </div>
    <div class="card">
      <div class="label">Duplicate Lines</div>
      <div class="value">{{totalDupLines}}</div>
      <div class="sub">total across all clusters</div>
    </div>
    <div class="card">
      <div class="label">Files Analyzed</div>
      <div class="value">{{totalFiles}}</div>
      <div class="sub">C&#35; source files</div>
    </div>
  </div>
""");
        sb.Append(HtmlBody);
        sb.Append($$"""
<script>
const CL={{clustersJson}};
const FS={{fileScoresJson}};
const PS={{projectScoresJson}};
""");
        sb.Append(HtmlScript);
        return sb.ToString();
    }

    // Static HTML head and body (no C# interpolation needed here)
    private static readonly string HtmlHead = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>DupDetector Report</title>
  <style>
    *,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
    body{font-family:system-ui,-apple-system,sans-serif;background:#0f172a;color:#e2e8f0;line-height:1.5}
    a{color:#60a5fa;text-decoration:none}
    a:hover{text-decoration:underline}
    header{background:#1e293b;padding:1.5rem 2rem;border-bottom:1px solid #334155;display:flex;align-items:center;gap:1rem}
    header h1{font-size:1.5rem;font-weight:700;color:#f1f5f9}
    .badge{font-size:.75rem;padding:.2rem .6rem;border-radius:9999px;background:#334155;color:#94a3b8}
    .container{max-width:1400px;margin:0 auto;padding:2rem}
    .cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:1rem;margin-bottom:2rem}
    .card{background:#1e293b;border:1px solid #334155;border-radius:.75rem;padding:1.25rem}
    .card .label{font-size:.75rem;color:#64748b;text-transform:uppercase;letter-spacing:.05em;margin-bottom:.4rem}
    .card .value{font-size:1.75rem;font-weight:700;color:#f1f5f9}
    .card .sub{font-size:.8rem;color:#94a3b8;margin-top:.25rem}
    .score-badge{display:inline-block;padding:.15rem .6rem;border-radius:.375rem;font-size:.8rem;font-weight:600;text-transform:uppercase}
    section{margin-bottom:2.5rem}
    section h2{font-size:1.1rem;font-weight:600;color:#94a3b8;text-transform:uppercase;letter-spacing:.05em;margin-bottom:1rem;border-bottom:1px solid #334155;padding-bottom:.5rem}
    .bar-row{display:flex;align-items:center;gap:.75rem;margin-bottom:.5rem}
    .bar-row .fname{flex:0 0 45%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#cbd5e1;font-family:monospace;font-size:.78rem}
    .bar-track{flex:1;background:#1e293b;border-radius:9999px;height:10px;overflow:hidden;border:1px solid #334155}
    .bar-fill{height:100%;border-radius:9999px;transition:width .4s}
    .bar-row .pct{flex:0 0 3.5rem;text-align:right;font-size:.78rem;color:#94a3b8}
    table{width:100%;border-collapse:collapse;font-size:.85rem}
    th{text-align:left;padding:.6rem .75rem;background:#1e293b;color:#64748b;font-size:.75rem;text-transform:uppercase;letter-spacing:.05em;border-bottom:1px solid #334155;cursor:pointer;user-select:none}
    th:hover{color:#94a3b8}
    th .si{margin-left:4px;opacity:.4}
    th.sorted .si{opacity:1;color:#60a5fa}
    td{padding:.6rem .75rem;border-bottom:1px solid #1e293b;vertical-align:top}
    tr:hover td{background:#1e293b55}
    .mono{font-family:monospace;font-size:.78rem;color:#93c5fd}
    .tag{display:inline-block;padding:.1rem .45rem;border-radius:.25rem;font-size:.72rem;font-weight:600;background:#1e3a5f;color:#93c5fd}
    .tag.sp{background:#1e3a2f;color:#86efac}
    .tag.oc{background:#3b1f2f;color:#f9a8d4}
    .sr{display:flex;gap:.75rem;margin-bottom:1rem;align-items:center}
    .sr input{flex:1;background:#1e293b;border:1px solid #334155;border-radius:.5rem;padding:.5rem .75rem;color:#e2e8f0;font-size:.85rem;outline:none}
    .sr input:focus{border-color:#3b82f6}
    .tn{margin-bottom:.25rem}
    .tt{cursor:pointer;display:flex;align-items:center;gap:.5rem;padding:.4rem .5rem;border-radius:.375rem;background:#1e293b;border:1px solid #334155;font-size:.85rem}
    .tt:hover{background:#273549}
    .tt .arr{transition:transform .2s;font-size:.7rem;color:#475569}
    .tt.open .arr{transform:rotate(90deg)}
    .tc{margin-left:1.25rem;margin-top:.25rem;display:none}
    .tc.open{display:block}
    .sb{background:#0f172a;border:1px solid #334155;border-radius:.5rem;padding:.75rem 1rem;margin-top:.5rem;overflow-x:auto}
    .sb pre{font-family:monospace;font-size:.78rem;color:#a5b4fc;white-space:pre-wrap;word-break:break-all}
    .ir{display:flex;gap:.5rem;align-items:center;padding:.3rem .5rem;font-size:.8rem;border-left:2px solid #334155;margin:.2rem 0}
    .ir .ifl{font-family:monospace;color:#7dd3fc;font-size:.75rem}
    .ir .iln{color:#94a3b8;font-size:.73rem}
    .interp{background:#1e293b;border:1px solid #334155;border-radius:.75rem;padding:1.25rem;margin-bottom:2rem}
    .interp ul{padding-left:1.25rem}
    .interp li{margin-bottom:.3rem;font-size:.85rem;color:#94a3b8}
    .interp li strong{color:#cbd5e1}
  </style>
</head>
<body>
<header>
  <h1>&#128269; DupDetector Report</h1>
  <span class="badge">Static Report</span>
</header>
<div class="container">
""";

    private static readonly string HtmlBody = """
  <div class="interp">
    <strong style="color:#f1f5f9">Score Interpretation</strong>
    <ul style="margin-top:.5rem">
      <li><strong>0&#8211;10 (low):</strong> Minimal duplication. Codebase is healthy.</li>
      <li><strong>10&#8211;30 (medium):</strong> Moderate duplication. Consider extracting shared logic.</li>
      <li><strong>30&#8211;60 (high):</strong> Significant duplication. Prioritize refactoring hotspots.</li>
      <li><strong>60&#8211;100 (critical):</strong> Severe duplication. Immediate refactoring recommended.</li>
    </ul>
  </div>
  <section id="file-hotspots"><h2>File Hotspots</h2><div id="fc"></div></section>
  <section id="project-scores"><h2>Project Scores</h2><div id="pc"></div></section>
  <section id="top-duplicates">
    <h2>Top Duplicate Clusters</h2>
    <div class="sr"><input type="text" id="cs" placeholder="Search clusters by file, method, or ID&#8230;" /></div>
    <table id="ct">
      <thead><tr>
        <th data-col="id">ID <span class="si">&#8597;</span></th>
        <th data-col="score" class="sorted">Score <span class="si">&#8595;</span></th>
        <th data-col="lines">Lines <span class="si">&#8597;</span></th>
        <th data-col="occ">Occurrences <span class="si">&#8597;</span></th>
        <th data-col="spread">Spread <span class="si">&#8597;</span></th>
        <th>Files</th>
      </tr></thead>
      <tbody id="ctb"></tbody>
    </table>
  </section>
  <section id="tree-view">
    <h2>Duplication Tree</h2>
    <div class="sr"><input type="text" id="ts" placeholder="Filter tree by file or cluster ID&#8230;" /></div>
    <div id="tr"></div>
  </section>
</div>
""";

    private static readonly string HtmlScript = """
function esc(s){return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');}
function sc(v){if(v<10)return'#22c55e';if(v<30)return'#f59e0b';if(v<60)return'#f97316';return'#ef4444';}
function sf(p){const a=p.replace(/\\/g,'/').split('/');return a.length>3?'…/'+a.slice(-3).join('/'):p.replace(/\\/g,'/');}
(function(){
  const el=document.getElementById('fc');
  const top=[...FS].sort((a,b)=>b.score-a.score).slice(0,20);
  if(!top.length){el.innerHTML='<p style="color:#64748b">No file data.</p>';return;}
  el.innerHTML=top.map(f=>`<div class="bar-row" title="${esc(f.file)}"><span class="fname">${esc(sf(f.file))}</span><div class="bar-track"><div class="bar-fill" style="width:${f.score}%;background:${sc(f.score)}"></div></div><span class="pct">${f.score.toFixed(1)}%</span></div>`).join('');
})();
(function(){
  const el=document.getElementById('pc');
  const top=[...PS].sort((a,b)=>b.score-a.score).slice(0,15);
  if(!top.length){el.innerHTML='<p style="color:#64748b">No project data.</p>';return;}
  el.innerHTML=top.map(p=>`<div class="bar-row" title="${esc(p.project)}"><span class="fname">${esc(sf(p.project))}</span><div class="bar-track"><div class="bar-fill" style="width:${p.score}%;background:${sc(p.score)}"></div></div><span class="pct">${p.score.toFixed(1)}%</span></div>`).join('');
})();
let td2=CL.map(c=>({id:c.id,score:c.metrics.duplicationScore,lines:c.metrics.lines,occ:c.metrics.occurrences,spread:c.metrics.spread,files:c.instances.map(i=>i.file),instances:c.instances}));
let sc2='score',sa=false;
function rt(data){
  const tb=document.getElementById('ctb');
  tb.innerHTML=data.map(r=>`<tr><td><a class="mono" href="#tree-${esc(r.id)}">${esc(r.id)}</a></td><td><strong style="color:${sc(r.score)}"> ${r.score.toFixed(2)}</strong></td><td>${r.lines}</td><td><span class="tag oc">${r.occ}&times;</span></td><td><span class="tag sp">${r.spread} file${r.spread>1?'s':''}</span></td><td style="max-width:300px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap" title="${esc(r.files.join(', '))}"><span class="mono">${esc(r.files.map(sf).join(', '))}</span></td></tr>`).join('');
}
function sort2(col){
  if(sc2===col)sa=!sa;else{sc2=col;sa=col==='id';}
  document.querySelectorAll('#ct th').forEach(th=>{th.classList.remove('sorted');const i=th.querySelector('.si');if(i)i.textContent='↕';});
  const th=document.querySelector('#ct th[data-col="'+col+'"]');
  if(th){th.classList.add('sorted');const i=th.querySelector('.si');if(i)i.textContent=sa?'↑':'↓';}
  refresh2();
}
function refresh2(){
  const q=(document.getElementById('cs').value||'').toLowerCase();
  let data=td2.filter(r=>!q||r.id.toLowerCase().includes(q)||r.files.some(f=>f.toLowerCase().includes(q))||(r.instances&&r.instances.some(i=>(i.method||'').toLowerCase().includes(q))));
  data.sort((a,b)=>{let va=a[sc2],vb=b[sc2];if(typeof va==='string'){va=va.toLowerCase();vb=vb.toLowerCase();}if(va<vb)return sa?-1:1;if(va>vb)return sa?1:-1;return 0;});
  rt(data);
}
document.querySelectorAll('#ct th[data-col]').forEach(th=>th.addEventListener('click',()=>sort2(th.dataset.col)));
document.getElementById('cs').addEventListener('input',refresh2);
refresh2();
(function(){
  const root=document.getElementById('tr');
  const bf={};
  CL.forEach(c=>{c.instances.forEach(inst=>{if(!bf[inst.file])bf[inst.file]=[];bf[inst.file].push({cluster:c,inst});});});
  const files=Object.keys(bf).sort();
  root.innerHTML=files.map(file=>{
    const items=bf[file];
    const cids=[...new Set(items.map(i=>i.cluster.id))];
    const inner=cids.map(cid=>{
      const c=items.find(i=>i.cluster.id===cid).cluster;
      const insts=c.instances.filter(i=>i.file===file);
      return `<div class="tn" id="tree-${esc(cid)}"><div class="tt" onclick="tog(this)"><span class="arr">&#9658;</span><span class="mono">${esc(cid)}</span><span class="tag" style="margin-left:auto">score ${c.metrics.duplicationScore.toFixed(2)}</span><span class="tag oc">${c.metrics.occurrences}&times;</span><span class="tag sp">${c.metrics.lines} lines</span></div><div class="tc">${insts.map(inst=>`<div class="ir"><span class="ifl">${esc(sf(inst.file))}</span><span class="iln">lines ${inst.startLine}&ndash;${inst.endLine}</span><span class="tag">${esc(inst.method)}</span></div>`).join('')}<div class="sb"><pre>${esc(c.normalizedSnippet.slice(0,800))}</pre></div></div></div>`;
    }).join('');
    return `<div class="tn"><div class="tt" onclick="tog(this)"><span class="arr">&#9658;</span><span style="color:#7dd3fc;font-family:monospace;font-size:.8rem">${esc(sf(file))}</span><span class="tag" style="margin-left:auto">${cids.length} cluster${cids.length>1?'s':''}</span></div><div class="tc">${inner}</div></div>`;
  }).join('');
})();
function tog(t){t.classList.toggle('open');const c=t.nextElementSibling;if(c&&c.classList.contains('tc'))c.classList.toggle('open');}
document.getElementById('ts').addEventListener('input',function(){
  const q=this.value.toLowerCase();
  document.querySelectorAll('#tr .tn').forEach(n=>{if(!q){n.style.display='';return;}n.style.display=n.textContent.toLowerCase().includes(q)?'':'none';});
});
document.getElementById('ctb').addEventListener('click',function(e){
  const a=e.target.closest('a[href^="#tree-"]');
  if(!a)return;
  const id=a.getAttribute('href').slice(1);
  const el=document.getElementById(id);
  if(!el)return;
  let p=el.closest('.tc');
  while(p){p.classList.add('open');const t=p.previousElementSibling;if(t&&t.classList.contains('tt'))t.classList.add('open');p=p.parentElement&&p.parentElement.closest('.tc');}
  const t=el.querySelector('.tt'),c=el.querySelector('.tc');
  if(t)t.classList.add('open');if(c)c.classList.add('open');
  setTimeout(()=>el.scrollIntoView({behavior:'smooth',block:'start'}),100);
});
</script>
</body>
</html>
""";
}
