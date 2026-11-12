using System.Globalization;
using System.Net;
using System.Text;

namespace ChestCounter;

public static class Pretty
{
    public static string Playlist(string? playlist)
    {
        if (string.IsNullOrEmpty(playlist))
        {
            return L.T("mode inconnu", "unknown mode", "modo desconocido");
        }

        var p = playlist.ToLowerInvariant();
        var parts = new List<string>();
        if (p.Contains("habanero") || p.Contains("ranked")) parts.Add(L.T("Classée", "Ranked", "Clasificatoria"));
        if (p.Contains("nobuild")) parts.Add(L.T("Zéro construction", "Zero Build", "Cero construcción"));
        if (p.Contains("solo")) parts.Add(L.T("Solo", "Solo", "Solitario"));
        else if (p.Contains("duo")) parts.Add(L.T("Duo", "Duos", "Dúos"));
        else if (p.Contains("trio")) parts.Add(L.T("Trio", "Trios", "Tríos"));
        else if (p.Contains("squad")) parts.Add(L.T("Section", "Squads", "Escuadrones"));
        return parts.Count > 0 ? string.Join(" · ", parts) : playlist.Replace("Playlist_", "");
    }

    public static string Placement(MatchRecord m)
    {
        if (m.Placement is not int place || place <= 0)
        {
            return L.T("place inconnue", "unknown placement", "puesto desconocido");
        }
        if (place == 1)
        {
            return L.T("Top 1 !", "Victory Royale!", "¡Victoria magistral!");
        }
        return m.TotalPlayers is int total && total > 0 ? $"{L.Ordinal(place)} / {total}" : L.Ordinal(place);
    }

    public static string Clock(float seconds)
    {
        var s = (int)seconds;
        return $"{s / 60}:{s % 60:00}";
    }

    /// <summary>"à 7:16 · Coffre rare" (certain) or "vers 5:04 · Coffre rare (probable)".</summary>
    public static string Obtained(TreasureFind f)
    {
        if (f.ObtainedAtSeconds is not float t)
        {
            return L.T("moment et source inconnus", "time and source unknown", "momento y origen desconocidos");
        }
        var when = f.TimeExact ? $"{L.T("à", "at", "a los")} {Clock(t)}" : $"{L.T("vers", "around", "hacia los")} {Clock(t)}";
        return f.SourceCertain ? $"{when} · {f.SourceLabel}" : $"{when} · {f.SourceLabel} ({Likely})";
    }

    public static string Likely => L.T("probable", "likely", "probable");

    public static string ItemName(string item)
    {
        var name = item;
        foreach (var prefix in new[] { "WID_", "Athena", "AmmoData" })
        {
            name = name.Replace(prefix, "");
        }
        return name.Replace("_", " ").Trim();
    }
}

/// <summary>Replays analyzed so far / to analyze, shown as a loading card in the report.</summary>
public sealed record ScanProgress(int Done, int Total);

public static class ReportWriter
{
    public static readonly DateTime SessionStartUtc = DateTime.UtcNow;

    /// <summary>Set while several replays are being analyzed: the report shows a loading card and refreshes faster.</summary>
    public static volatile ScanProgress? Progress;

    /// <summary>A match belongs to this session if it ended after the tool was started (backlog replays do not count).</summary>
    public static bool IsThisSession(MatchRecord m)
    {
        var start = m.MatchStartUtc ?? m.AnalyzedAtUtc;
        if (start.Kind == DateTimeKind.Unspecified)
        {
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        }
        var end = start.ToUniversalTime().AddSeconds(m.DurationSeconds ?? 0);
        return end >= SessionStartUtc.AddMinutes(-2);
    }
    private static CultureInfo Fr => L.Culture;

    public static void WriteAll(Store store, Config config, TrackerData data)
    {
        try
        {
            var view = Profiles.View(data, config);
            WriteAtomic(store.ReportPath, Report(config, view, Profiles.List(data)));
            // The OBS page keeps what the overlay shows (a match in progress stays "live"), in the chosen style.
            OverlayStyle = config.OverlayStyle;
            OverlayColor = config.OverlayColor;
            OverlayAnimations = config.OverlayAnimations;
            WriteAtomic(store.OverlayPath, Overlay(LastOverlay ?? OverlayModel.Build(view, null, false, config.ShowSquadTotal)));
            WriteAtomic(store.CsvPath, Csv(data));
        }
        catch (Exception ex)
        {
            Console.WriteLine(L.T($"Impossible d'écrire le rapport : {ex.Message}", $"Cannot write the report: {ex.Message}", $"No se puede escribir el informe: {ex.Message}"));
        }
    }

    private static void WriteAtomic(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content, new UTF8Encoding(false));
        File.Move(tmp, path, overwrite: true);
    }

    private static string H(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static DateTime When(MatchRecord m) => (m.MatchStartUtc ?? m.AnalyzedAtUtc).ToLocalTime();

    // "Deep sea" look: dark navy panels, sea green for the Pirate Shop chance, gold for chests. Figures use Bahnschrift
    // and text Segoe UI Variable, both installed with Windows.
    public const string Tokens = """
        :root{color-scheme:dark;--bg:#060a07;--panel:#0b120d;--panel2:#101a13;--line:#1a281e;--line2:#2a3d2f;--ink:#d3e4d6;--ink2:#effbf0;--muted:#7c9381;
        --sea:#5aff96;--sea2:color-mix(in srgb,var(--sea) 62%,#000);--seabg:color-mix(in srgb,var(--sea) 11%,transparent);--seaglow:color-mix(in srgb,var(--sea) 30%,transparent);
        --gold:#ffc440;--goldbg:rgba(255,196,64,.11);--live:#ff5a5a;--livebg:rgba(255,90,90,.12);--star:#c9b8ff;--team:#7fd4ff;--warn:#ff9f5a;--warnbg:rgba(255,159,90,.10);
        --title:var(--ink2);--card:var(--panel);--cink:var(--ink);--cmuted:var(--muted);--track:#132017;--radius:3px;
        --mono:"Cascadia Mono",Consolas,"Courier New",monospace}
        """;

    /// <summary>Military look shared by the window and the report: stencil-like mono labels, reticle corners, a faint grid.</summary>
    public const string Military = """
        body{background-color:var(--bg);background-image:linear-gradient(color-mix(in srgb,var(--sea) 4%,transparent) 1px,transparent 1px),linear-gradient(90deg,color-mix(in srgb,var(--sea) 4%,transparent) 1px,transparent 1px);background-size:32px 32px}
        h1,h2,.pirate{font-family:var(--mono)!important;text-transform:uppercase;letter-spacing:.08em}
        h2:before{content:"// ";color:var(--sea)}
        .ret{position:relative}
        .ret:before,.ret:after{content:"";position:absolute;width:12px;height:12px;pointer-events:none;border:0 solid var(--sea);opacity:.85}
        .ret:before{top:-1px;left:-1px;border-top-width:2px;border-left-width:2px}
        .ret:after{bottom:-1px;right:-1px;border-bottom-width:2px;border-right-width:2px}
        """;


    private const string Style = Tokens + """
        *{box-sizing:border-box}
        html{background:var(--bg)}
        body{margin:0;background:var(--bg);color:var(--ink);font:14.5px/1.55 "Segoe UI Variable Text","Segoe UI",system-ui,sans-serif;-webkit-font-smoothing:antialiased}
        main{max-width:1120px;margin:0 auto;padding:32px 16px 56px;display:flex;flex-direction:column;gap:28px}
        .pirate{font-family:"Segoe UI Variable Display","Segoe UI",system-ui,sans-serif;font-weight:700;letter-spacing:-.01em}
        .num{font-family:Bahnschrift,"Segoe UI",sans-serif;font-variant-numeric:tabular-nums;letter-spacing:-.01em}
        header{display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:12px;padding-bottom:18px;border-bottom:1px solid var(--line)}
        .brand{display:flex;align-items:center;gap:14px}
        h1{margin:0;font-size:28px;line-height:1.1;color:var(--title)}
        .who{font-size:14px;color:var(--muted);margin-top:2px}.who b{color:var(--ink)}
        .muted{color:var(--muted)}
        h2{margin:0 0 12px;font-size:18px;line-height:1.3;color:var(--title)}
        .tiles{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:12px}
        .tile{background:var(--panel);color:var(--ink);border:1px solid var(--line);border-radius:var(--radius);padding:16px 18px;display:flex;flex-direction:column;gap:2px}
        .tile .v{font-size:32px;font-weight:600;line-height:1.1;color:var(--ink2)}
        .tile .l{font-size:13px;color:var(--muted)}
        .tile .s{font-size:12px;color:var(--muted);margin-top:4px}
        .tile.hero{grid-column:span 2;position:relative;justify-content:space-between;background:linear-gradient(160deg,var(--panel2),var(--panel) 70%);border-color:var(--line2)}
        .editbtn{display:none;position:absolute;top:12px;right:12px;width:34px;height:34px;padding:0;border-radius:10px;border:1px solid var(--line2);background:var(--panel2);color:var(--muted);cursor:pointer;place-items:center}
        .editbtn:hover{color:var(--gold);border-color:var(--gold)}.editbtn:focus-visible{outline:2px solid var(--gold);outline-offset:2px}
        html.incounter .editbtn{display:grid}
        .tile.hero .k{font-family:var(--mono);font-size:11px;letter-spacing:.14em;text-transform:uppercase;color:var(--muted);font-weight:700}
        .tile.hero .v{font-size:52px;color:var(--gold);line-height:1.05}
        .tile.hero .l{color:var(--muted)}
        @media (max-width:820px){.tiles{grid-template-columns:repeat(2,minmax(0,1fr))}.tile.hero{grid-column:span 2}}
        .card{background:var(--panel);color:var(--ink);border:1px solid var(--line);border-radius:var(--radius);padding:6px 18px;overflow-x:auto}
        .two{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}
        @media (max-width:820px){.two{grid-template-columns:1fr}}
        table{width:100%;border-collapse:collapse;font-variant-numeric:tabular-nums}
        th,td{text-align:left;padding:10px 8px;border-bottom:1px solid var(--line);vertical-align:middle}
        th{white-space:nowrap;font-family:var(--mono);font-size:11px;text-transform:uppercase;letter-spacing:.1em;color:var(--muted);font-weight:700}
        tr:last-child td{border-bottom:none}
        tbody tr:hover td,table tr:hover td{background:rgba(255,255,255,.015)}
        td.n{text-align:right;font-weight:600;color:var(--ink2)}
        td.nw{white-space:nowrap}
        .big{font-size:18px}
        .items{color:var(--muted);font-size:13px}
        .star{color:var(--star);font-weight:600}
        .gold{color:var(--gold)}
        .sw{display:inline-block;width:10px;height:10px;border-radius:3px;margin-right:10px;vertical-align:-1px}
        .meter{display:block;width:140px;height:6px;background:var(--track);border-radius:3px;overflow:hidden}
        .meter i{display:block;height:6px;border-radius:3px}
        .flag{display:flex;gap:12px;align-items:flex-start;background:var(--warnbg);color:var(--ink);border:1px solid rgba(255,138,92,.4);border-radius:12px;padding:12px 14px;font-size:14px;margin-top:12px}
        tr.unusual td{background:var(--warnbg)}
        .warn{color:var(--warn)}
        .chart{padding:18px 18px 12px}
        .bars{display:flex;align-items:stretch;gap:4px;height:150px;padding-top:26px}
        .col{flex:1;min-width:5px;display:flex;align-items:flex-end;position:relative;cursor:default}
        .bar{width:100%;background:linear-gradient(180deg,var(--gold),#d99a1e);border-radius:4px 4px 0 0;transition:filter .12s}
        .bar.top{background:linear-gradient(180deg,var(--sea),var(--sea2))}
        .bar.zero{background:var(--line);height:3px!important}
        .col:hover .bar{filter:brightness(1.18)}
        .tip{position:absolute;left:50%;transform:translateX(-50%);white-space:nowrap;background:var(--ink);color:var(--bg);
             padding:3px 8px;border-radius:6px;font-size:12px;font-weight:600;opacity:0;pointer-events:none;transition:opacity .12s;z-index:2}
        .col:nth-child(-n+3) .tip{left:0;transform:none}
        .col:nth-last-child(-n+3) .tip{left:auto;right:0;transform:none}
        .col:hover .tip{opacity:1}
        .axis{display:flex;justify-content:space-between;gap:8px;color:var(--muted);font-size:12px;margin-top:8px;border-top:1px solid var(--line);padding-top:6px}
        .head{display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:12px;margin-bottom:12px}
        .head h2{margin:0}
        .tsw{position:relative;display:inline-flex;align-items:center;gap:10px;min-height:36px;font-size:13px;color:var(--muted);cursor:pointer;user-select:none}
        .tsw input{position:absolute;opacity:0;width:1px;height:1px;margin:0}
        .tsw .trk{position:relative;width:36px;height:20px;border-radius:10px;background:var(--line2);flex:none;transition:background .15s}
        .tsw .trk:after{content:"";position:absolute;top:3px;left:3px;width:14px;height:14px;border-radius:50%;background:#fff;transition:transform .15s}
        .tsw input:checked+.trk{background:var(--sea2)}.tsw input:checked+.trk:after{transform:translateX(16px)}
        .tsw input:focus-visible+.trk{outline:2px solid var(--sea);outline-offset:2px}
        .tsw .badge{display:inline-block;margin-left:2px;padding:1px 8px;border-radius:999px;background:var(--warnbg);color:var(--warn);font-size:11px;font-weight:700}
        .statsbar{display:flex;justify-content:flex-end;margin:-22px 0 -10px}
        .squadtile,.sq{display:none}
        body.showsquad .squadtile{display:flex}body.showsquad .sq{display:table-cell}
        body.showsquad .tiles.hassquad{grid-template-columns:repeat(7,minmax(0,1fr))}
        .fold .head h2{display:flex;align-items:center;gap:4px}
        .foldbtn{display:inline-flex;align-items:center;gap:10px;background:none;border:0;padding:4px 0;margin:0;font:inherit;color:inherit;cursor:pointer;text-align:left}
        .foldbtn:focus-visible{outline:2px solid var(--sea);outline-offset:4px;border-radius:4px}
        .chev{width:8px;height:8px;border-right:2px solid var(--muted);border-bottom:2px solid var(--muted);transform:rotate(45deg);margin:0 6px 4px 2px;transition:transform .15s}
        .fold.closed .chev{transform:rotate(-45deg);margin-bottom:0}
        .fold.closed .foldbody{display:none}
        .fold.closed .head{margin-bottom:0}
        .fold.closed .head>:not(h2){display:none}
        .fold{border-top:1px solid var(--line);padding-top:16px}
        .team{display:none}
        body.showteam tr.team{display:table-row}
        body.showteam span.team{display:inline}
        tr.team td{opacity:.8}
        .by{white-space:nowrap}
        .by svg{vertical-align:-3px;margin-right:6px}
        footer{font-size:12px;color:var(--muted);border-top:1px solid var(--line);padding-top:14px}
        .loading{display:flex;gap:16px;align-items:center;background:var(--panel);color:var(--ink);border:1px solid var(--line2);border-radius:var(--radius);padding:16px 18px}
        .loading .muted{color:var(--muted)}
        .spin{width:28px;height:28px;flex:none;border:3px solid var(--line2);border-top-color:var(--sea);border-radius:50%;animation:spin .8s linear infinite}
        @keyframes spin{to{transform:rotate(360deg)}}
        @media (prefers-reduced-motion:reduce){.spin{animation-duration:3s}}
        .loading .grow{flex:1;min-width:0}
        .pbar{height:6px;background:var(--track);border-radius:3px;overflow:hidden;margin:8px 0 6px}
        .pbar i{display:block;height:100%;background:var(--sea);border-radius:3px}
        .profiles{margin-top:-16px;font-size:13px}
        .embed,.embed body{background:transparent;overflow:hidden}.embed main{padding:0;max-width:none}.embed header,.embed footer,.embed .profiles,.embed .notice{display:none}
        .info{position:relative;display:inline-flex;align-items:center;justify-content:center;width:17px;height:17px;margin-left:6px;border-radius:50%;border:1.5px solid var(--line2);color:var(--muted);font:700 11px/1 "Segoe UI",sans-serif;cursor:help;vertical-align:1px;outline:none}
        .info:hover,.info:focus{border-color:var(--sea);color:var(--sea)}
        .infotip{display:none;position:absolute;z-index:5;top:24px;right:-12px;width:320px;max-width:80vw;background:#132017;color:var(--ink);border:1px solid var(--line2);padding:10px 12px;border-radius:3px;font:400 13px/1.5 "Segoe UI",system-ui,sans-serif;text-align:left;box-shadow:0 12px 32px rgba(0,0,0,.5);letter-spacing:0;text-transform:none}
        .info:hover .infotip,.info:focus .infotip{display:block}
        h2 .info{vertical-align:2px}
        h2 .infotip{left:-12px;right:auto}
        tr.pg{display:none!important}
        .pager{display:flex;flex-wrap:wrap;align-items:center;gap:8px;padding:10px 0 8px;border-top:1px solid var(--line)}
        .pager[hidden],.pager button[hidden]{display:none}
        .pager .count{color:var(--muted);font-size:13px;margin-right:auto}
        .pager button{min-height:34px;padding:6px 14px;border-radius:9px;border:1px solid var(--line2);background:var(--panel2);color:var(--ink);font:600 13px "Segoe UI",system-ui,sans-serif;cursor:pointer}
        .pager button:hover{border-color:var(--sea);color:var(--sea)}
        .notice{background:var(--panel);color:var(--ink);border:1px solid var(--line);border-left:4px solid var(--gold);border-radius:12px;padding:12px 16px;white-space:pre-line}
        .notice.warning{border-left-color:var(--live);background:var(--livebg)}
        .exper{display:inline-block;font:700 11px "Segoe UI",system-ui,sans-serif;letter-spacing:.06em;text-transform:uppercase;padding:2px 8px;border-radius:999px;background:var(--warnbg);color:var(--warn);vertical-align:3px}
        .thour{background:var(--panel);color:var(--ink);border:1px solid var(--line);border-left:4px solid var(--gold);border-radius:12px;padding:10px 14px;margin-bottom:12px}
        .thour.live{border-left-color:var(--warn);background:var(--warnbg)}
        .thour .tt{font-weight:700}.thour .exp{font-weight:400;color:var(--muted)}
        .boost{display:inline-block;margin-left:8px;padding:1px 8px;border-radius:999px;background:var(--warnbg);color:var(--warn);font:700 12px "Segoe UI",system-ui,sans-serif;letter-spacing:0;text-transform:none;vertical-align:1px}
        .boost[hidden]{display:none}
        .odds.boosted{border-color:var(--warn)}
        tr.boosted td:first-child{box-shadow:inset 3px 0 0 var(--warn);padding-left:14px}
        .boostv{color:var(--warn);font-weight:700}
        .thour .tb{display:flex;flex-wrap:wrap;gap:6px 18px;font-size:14px;margin-top:4px}
        .chances{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}
        @media (max-width:820px){.chances{grid-template-columns:1fr}}
        .odds{background:var(--panel);color:var(--ink);border:1px solid var(--line);border-radius:var(--radius);padding:16px 18px;display:flex;flex-direction:column;gap:4px}
        .odds .k{font-family:var(--mono);font-size:11px;letter-spacing:.12em;text-transform:uppercase;color:var(--muted);font-weight:700;display:flex;justify-content:space-between;gap:8px}
        .odds .v{font-size:38px;font-weight:600;line-height:1.1;color:var(--ink2)}
        .odds .l,.odds .s{font-size:13px;color:var(--muted)}
        .odds[data-kind="pirate-shop"]{background:linear-gradient(160deg,var(--seabg),var(--panel) 65%);border-color:var(--seaglow)}
        .odds[data-kind="pirate-shop"] .v{color:var(--sea)}
        .odds{position:relative}.odds .tokenpic{position:absolute;right:14px;top:12px;width:84px;height:84px;filter:drop-shadow(0 0 14px var(--seaglow))}
        .odds[data-kind="pirate-shop"] .k,.odds[data-kind="pirate-shop"] .v,.odds[data-kind="pirate-shop"] .l{padding-right:92px}
        .meter.wide{width:100%;margin:6px 0 2px}.meter.wide i{background:var(--gold)}
        .odds[data-kind="pirate-shop"] .meter.wide i{background:var(--sea)}
        .gotbadge{display:none;color:var(--sea);letter-spacing:0;text-transform:none;font-weight:700}
        .odds.got{border-color:var(--sea)}.odds.got .gotbadge{display:inline}
        .collection{padding:10px 18px 14px}
        .wk{padding:10px 0}.wk+.wk{border-top:1px solid var(--line)}
        .wt{font-weight:600;margin-bottom:8px}.wk.now .wt{color:var(--gold)}
        .prizes{display:grid;grid-template-columns:repeat(auto-fill,minmax(140px,1fr));gap:10px}
        .prize{position:relative;display:flex;flex-direction:column;gap:2px;padding:8px;border:1px solid var(--line);border-radius:12px;cursor:pointer;background:var(--panel2)}
        .prize:hover{border-color:var(--line2)}
        .prize input{position:absolute;top:12px;left:12px;width:18px;height:18px;accent-color:var(--sea);cursor:pointer;z-index:1}
        .prize .pimg{display:block;aspect-ratio:1;border-radius:8px;background:var(--track);overflow:hidden;margin-bottom:4px}
        .prize img{width:100%;height:100%;object-fit:cover;display:block;filter:grayscale(.7);opacity:.6}
        .prize .pn{font-size:13px;font-weight:600;line-height:1.3}
        .prize .pk{font-size:12px;color:var(--muted)}
        .prize.got{border:1px solid var(--sea);box-shadow:0 0 0 1px var(--sea) inset}
        .prize.got img{filter:none;opacity:1}
        """;

    /// <summary>@font-face of the embedded headings font (empty if the resource is missing: the page falls back to Segoe UI).</summary>
    private static readonly string FontFace = LoadFontFace();

    /// <summary>The headings font, for the pages of the graphical interface too.</summary>
    public static string FontFaceCss => FontFace;

    private static string LoadFontFace()
    {
        try
        {
            using var s = typeof(ReportWriter).Assembly.GetManifestResourceStream("PirataOne.woff2");
            if (s == null)
            {
                return "";
            }
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return "@font-face{font-family:'Pirata One';src:url(data:font/woff2;base64," + Convert.ToBase64String(ms.ToArray()) + ") format('woff2');font-display:swap}";
        }
        catch
        {
            return "";
        }
    }

    private const string TeamIcon = """<svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true"><circle cx="9" cy="8" r="3.2" fill="none" stroke="#4cc3ff" stroke-width="2"/><path d="M3 20c0-3.5 2.7-6 6-6s6 2.5 6 6" fill="none" stroke="#4cc3ff" stroke-width="2"/><circle cx="17" cy="9" r="2.6" fill="none" stroke="#4cc3ff" stroke-width="2"/><path d="M16 14c3 0 5 2.2 5 5" fill="none" stroke="#4cc3ff" stroke-width="2"/></svg>""";
    private const string FlagIcon = """<svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" style="flex:none;margin-top:2px"><path d="M5 21V4M5 4h11l-2 4 2 4H5" fill="none" stroke="#ff8a5c" stroke-width="2" stroke-linejoin="round"/></svg>""";

    private static readonly Dictionary<string, string> KindColor = new()
    {
        [ChestKinds.Normal] = "#ffc940",
        [ChestKinds.Rare] = "#4c9dff",
        [ChestKinds.Buried] = "#c08a52",
        [ChestKinds.Extraction] = "#34e6c4",
        [ChestKinds.PosterChest] = "#b896ff",
        [ChestKinds.Other] = "#6f7d96",
    };

    public static string Report(Config config, TrackerData data, IReadOnlyList<Profile> profiles)
    {
        var progress = Progress;
        var matches = data.Matches.OrderByDescending(When).ToList();
        var totalChests = matches.Sum(m => m.ChestsOpened);
        // Shown as the total: counted chests + those added by hand. Averages and odds stay on the counted ones.
        var grandTotal = data.TotalChests;
        // The squad total is shown only when asked, with the switch above the tiles (it is inaccurate: teammates far from
        // the player are not in the replays).
        var teamChests = data.TeamChests;
        var avg = matches.Count > 0 ? (double)totalChests / matches.Count : 0;
        var best = matches.Count > 0 ? matches.Max(m => m.ChestsOpened) : 0;
        var treasures = matches.Sum(m => m.TreasureFinds.Count);
        var teamTreasures = matches.Sum(m => m.TeamTreasureFinds.Count);
        var session = matches.Where(IsThisSession).Sum(m => m.ChestsOpened);
        var player = matches.Select(m => m.PlayerName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? "";
        // Own treasures only: the team's are found with chests that are not counted here.
        var every = treasures > 0 ? (int)Math.Round((double)totalChests / treasures) : 0;

        var sb = new StringBuilder();
        var title = Brand.Name;
        var updated = L.T(
            $"Mis à jour le {DateTime.Now.ToString("dd/MM/yyyy à HH:mm:ss", Fr)} · actualisation auto",
            $"Updated {DateTime.Now.ToString("d MMM yyyy 'at' HH:mm:ss", Fr)} · auto refresh",
            $"Actualizado el {DateTime.Now.ToString("dd/MM/yyyy 'a las' HH:mm:ss", Fr)} · actualización automática");
        string Tile(string value, string label, string? sub = null) =>
            $"""<div class="tile"><div class="v num">{value}</div><div class="l">{label}</div>{(sub != null ? $"<div class=\"s\">{sub}</div>" : "")}</div>""";

        sb.Append($"""
            <!doctype html><html lang="{L.Code}"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <meta http-equiv="refresh" content="{(progress != null ? 2 : 15)}">
            <title>{title}</title>{Brand.FaviconLink}<style>{Style}{Military}{AccentCss}</style></head><body data-showsquad="{(config.ShowSquadInReport ? 1 : 0)}" data-showteam="{(config.ShowTeamTreasuresInReport ? 1 : 0)}"><main>
            <header><div class="brand">{Brand.Logo(44)}<div><h1 class="pirate">{title}</h1>{(player.Length > 0 ? $"<div class=\"who\"><b>{H(player)}</b></div>" : "")}</div></div>
            <span class="muted" style="font-size:13px">{updated}</span></header>
            {LoadingCard(progress)}{NoticeCards()}{ProfilesLine(profiles, player)}
            <section class="tiles{(teamChests > 0 ? " hassquad" : "")}">
              <div class="tile hero ret"><button type="button" class="editbtn" id="editchests" title="{H(L.T("Modifier mon nombre de coffres", "Edit my number of chests", "Modificar mi número de cofres"))}" aria-label="{H(L.T("Modifier mon nombre de coffres", "Edit my number of chests", "Modificar mi número de cofres"))}">{PencilIcon}</button><div class="k">{L.T("Coffres au total", "Chests in total", "Cofres en total")}</div><div class="v num">{grandTotal.ToString("N0", Fr)}</div><div class="l">{L.T($"ouverts ou détruits par toi · {session.ToString("N0", Fr)} depuis le lancement", $"opened or destroyed by you · {session.ToString("N0", Fr)} since the tool started", $"abiertos o destruidos por ti · {session.ToString("N0", Fr)} desde que se abrió")}{(data.ManualAdjust != 0 ? "<br>" + L.T($"dont {data.ManualAdjust.ToString("+#,0;-#,0", Fr)} ajoutés à la main", $"{data.ManualAdjust.ToString("+#,0;-#,0", Fr)} of them added by hand", $"de ellos, {data.ManualAdjust.ToString("+#,0;-#,0", Fr)} añadidos a mano") : "")}</div></div>
              {(teamChests > 0 ? Tile((grandTotal + teamChests).ToString("N0", Fr), L.T("total de l'équipe", "squad total", "total del equipo") + SquadInfo, L.T($"toi + {teamChests.ToString("N0", Fr)} par tes coéquipiers", $"you + {teamChests.ToString("N0", Fr)} by your teammates", $"tú + {teamChests.ToString("N0", Fr)} de tus compañeros")).Replace("<div class=\"tile\">", "<div class=\"tile squadtile\">") : "")}
              {Tile(matches.Count.ToString("N0", Fr), L.T("parties analysées", "matches analyzed", "partidas analizadas"))}
              {Tile(avg.ToString("0.0", Fr), L.T("coffres par partie", "chests per match", "cofres por partida"))}
              {Tile(best.ToString("N0", Fr), L.T("record en une partie", "best match", "récord en una partida"))}
              {Tile(treasures.ToString("N0", Fr), L.T("trésors trouvés", "treasures found", "tesoros encontrados"),
                  (every > 0 ? L.T($"1 tous les {every.ToString("N0", Fr)} coffres", $"1 every {every.ToString("N0", Fr)} chests", $"1 cada {every.ToString("N0", Fr)} cofres") : null)
                  + (teamTreasures > 0 ? (every > 0 ? "<br>" : "") + L.T($"+{teamTreasures} par l'équipe (estimation)", $"+{teamTreasures} by the team (estimate)", $"+{teamTreasures} del equipo (estimación)") : ""))}
            </section>
            {(teamChests > 0 ? $"<div class=\"statsbar\">{Switch("showsquad", L.T("Afficher le total d'équipe", "Show the squad total", "Mostrar el total del equipo") + $" <span class=\"badge\">{L.T("imprécis", "inaccurate", "impreciso")}</span>")}</div>" : "")}
            """);

        // Last 30 days, one bar per day (the best day stands out).
        var today = DateTime.Now.Date;
        var days = Enumerable.Range(0, 30).Select(i => today.AddDays(i - 29)).ToList();
        var perDay = days.Select(d => matches.Where(m => When(m).Date == d).Sum(m => m.ChestsOpened)).ToList();
        var max = Math.Max(1, perDay.Max());
        sb.Append($"<section><h2 class=\"pirate\">{L.T("Coffres par jour", "Chests per day", "Cofres por día")}</h2><div class=\"card chart\"><div class=\"bars\">");
        for (var i = 0; i < days.Count; i++)
        {
            var v = perDay[i];
            var h = (int)Math.Round(100.0 * v / max);
            var tip = $"{days[i].ToString(L.T("ddd dd/MM", "ddd d MMM", "ddd dd/MM"), Fr)}{L.T(" : ", ": ", ": ")}{L.Count(v, "coffre", "chest", "cofre")}";
            var cls = v == 0 ? " zero" : v == max ? " top" : "";
            sb.Append($"<div class=\"col\" aria-label=\"{H(tip)}\"><span class=\"tip\" style=\"bottom:calc({Math.Max(h, 3)}% + 6px)\">{H(tip)}</span><div class=\"bar{cls}\" style=\"height:{h}%\"></div></div>");
        }
        var bestDay = perDay[perDay.IndexOf(perDay.Max())];
        var bestDate = days[perDay.IndexOf(perDay.Max())];
        sb.Append($"</div><div class=\"axis\"><span>{days[0].ToString(L.T("dd/MM", "d MMM", "dd/MM"), Fr)}</span>"
            + (bestDay > 0 ? $"<span>{L.T("meilleur jour", "best day", "mejor día")} : {bestDate.ToString(L.T("dd/MM", "d MMM", "dd/MM"), Fr)} · {L.Count(bestDay, "coffre", "chest", "cofre")}</span>" : "")
            + $"<span>{L.T("aujourd'hui", "today", "hoy")}</span></div></div></section>");

        // By chest type (with the other interactions, not in the total) | where the player's treasures come from.
        var byKind = matches.SelectMany(m => m.Chests).GroupBy(c => ChestKinds.Canonical(c.Kind)).OrderByDescending(g => g.Count()).ToList();
        var kindMax = Math.Max(1, byKind.Count > 0 ? byKind.Max(g => g.Count()) : 1);
        var otherKinds = matches
            .SelectMany(m => m.OtherInteractions.Count > 0
                ? m.OtherInteractions
                : new Dictionary<string, int> { [ChestKinds.AmmoBox] = m.AmmoBoxesOpened, [ChestKinds.OtherContainer] = m.OtherContainersOpened })
            .GroupBy(kv => kv.Key)
            .Select(g => (Kind: g.Key, Count: g.Sum(kv => kv.Value)))
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .ToList();
        sb.Append($"<div class=\"two\"><section><h2 class=\"pirate\">{L.T("Par type de coffre", "By chest type", "Por tipo de cofre")}</h2><div class=\"card\"><table>");
        foreach (var g in byKind)
        {
            var color = KindColor.GetValueOrDefault(g.Key, "#6f7d96");
            var w = Math.Max(2, (int)Math.Round(100.0 * g.Count() / kindMax));
            sb.Append($"<tr><td><span class=\"sw\" style=\"background:{color}\"></span>{H(L.Name(g.Key))}</td><td style=\"width:150px\"><span class=\"meter\"><i style=\"width:{w}%;background:{color}\"></i></span></td><td class=\"n num big\">{g.Count().ToString("N0", Fr)}</td></tr>");
        }
        var destroyedTotal = matches.Sum(m => m.Chests.Count(c => c.Destroyed));
        var notes = new List<string>();
        if (destroyedTotal > 0)
        {
            notes.Add(L.T($"dont {destroyedTotal.ToString("N0", Fr)} détruits (cassés au lieu d'être ouverts)", $"of which {destroyedTotal.ToString("N0", Fr)} destroyed (broken instead of opened)", $"de ellos, {destroyedTotal.ToString("N0", Fr)} destruidos (rotos en lugar de abiertos)"));
        }
        foreach (var (kind, count) in otherKinds)
        {
            sb.Append($"<tr class=\"items\"><td>{H(L.Name(kind))} {L.T("(hors total)", "(not in the total)", "(fuera del total)")}</td><td></td><td class=\"n num\">{count.ToString("N0", Fr)}</td></tr>");
        }
        sb.Append("</table>");
        if (notes.Count > 0)
        {
            sb.Append($"<div class=\"items\" style=\"padding:10px 0 8px\">{string.Join(" · ", notes)}</div>");
        }
        sb.Append("</div></section>");

        var treasureRows = matches.SelectMany(m => m.TreasureFinds.Select(f => (m, f, Team: false))).ToList();
        var teamRows = matches.SelectMany(m => m.TeamTreasureFinds.Select(f => (m, f, Team: true))).ToList();
        sb.Append($"<section><h2 class=\"pirate\">{L.T("D'où viennent tes trésors", "Where your treasures come from", "De dónde vienen tus tesoros")}</h2><div class=\"card\"><table>");
        if (treasureRows.Count == 0)
        {
            sb.Append($"<tr><td class=\"items\">{L.T("Pas encore de trésor.", "No treasure yet.", "Todavía ningún tesoro.")}</td></tr>");
        }
        foreach (var g in treasureRows.GroupBy(r => r.f.SourceLabel).OrderByDescending(g => g.Count()))
        {
            var isChest = g.First().f.SourceIsChest;
            sb.Append(isChest
                ? $"<tr><td>{H(g.Key)}</td><td class=\"n num big\">{g.Count()}</td></tr>"
                : $"<tr class=\"unusual\"><td class=\"warn\">{H(g.Key)} <span style=\"font-size:12px\">· {L.T("fiabilité faible", "low reliability", "fiabilidad baja")}</span></td><td class=\"n num big warn\">{g.Count()}</td></tr>");
        }
        sb.Append("</table></div>");
        var unusual = treasureRows.Where(r => !r.f.SourceIsChest).ToList();
        if (unusual.Count > 0)
        {
            var count = L.Count(unusual.Count, "trésor obtenu", "trésors obtenus", "treasure", "treasures", "tesoro obtenido", "tesoros obtenidos");
            sb.Append($"<div class=\"flag\">{FlagIcon}<span>" + L.T(
                $"<b>{count} autrement qu'avec un coffre.</b> Fiabilité faible : il s'agit très probablement d'une erreur dans la façon dont les replays sont analysés.",
                $"<b>{count} obtained other than from a chest.</b> Low reliability: most likely a mistake in the way the replays are analyzed.",
                $"<b>{count} de otra forma que con un cofre.</b> Fiabilidad baja: muy probablemente un error en la forma de analizar las repeticiones.") + "</span></div>");
        }
        sb.Append("</section></div>");

        sb.Append(ChancesSection(grandTotal, matches));
        sb.Append(SeenSection(matches));
        sb.Append(CollectionSection(matches, data.ObtainedIds));

        // Treasures: the player's own, then (hidden unless asked) those found by teammates, shared with the whole team.
        if (treasureRows.Count > 0 || teamRows.Count > 0)
        {
            var namesInfo = L.T(
                "Pourquoi certains trésors ont un nom et d'autres non ? Un trésor majeur (tenue, pioche, piste, écran, véhicule, accès à la boutique pirate) sort d'un coffre doré qui porte son identifiant exact : il est nommé à coup sûr (★★), à condition que ce coffre soit dans ton replay, donc à moins d'environ 150 m de toi. Un gizmo est nommé quand il apparaît dans tes mains. Sinon le replay dit seulement qu'un trésor a été reçu, sans dire lequel : « Trésor (type inconnu) ». C'est toujours le cas pour un trésor trouvé par un coéquipier loin de toi.",
                "Why do some treasures have a name and others not? A major treasure (outfit, pickaxe, track, loading screen, vehicle, Pirate Shop Access) comes out of a golden chest that carries its exact id: it is named for sure (★★), as long as that chest is in your replay, that is within about 150 m of you. A gizmo is named when it shows up in your hands. Otherwise the replay only says that a treasure was received, not which one: \"Treasure (unknown type)\". That is always the case for a treasure found by a teammate far from you.",
                "¿Por qué unos tesoros tienen nombre y otros no? Un tesoro mayor (traje, pico, pista, pantalla de carga, vehículo, acceso a la tienda pirata) sale de un cofre dorado que lleva su identificador exacto: se nombra con seguridad (★★), siempre que ese cofre esté en tu repetición, es decir a menos de unos 150 m de ti. Un artilugio se nombra cuando aparece en tus manos. Si no, la repetición solo dice que se recibió un tesoro, no cuál: «Tesoro (tipo desconocido)». Siempre es el caso de un tesoro encontrado por un compañero lejos de ti.");
            var teamSwitch = teamRows.Count > 0
                ? Switch("showteam", L.T($"Afficher aussi l'équipe (+{teamRows.Count}, estimation)", $"Also show the team (+{teamRows.Count}, estimate)", $"Mostrar también el equipo (+{teamRows.Count}, estimación)"))
                : "";
            sb.Append(FoldStart("treasures", $"{L.T("Trésors trouvés", "Treasures found", "Tesoros encontrados")} · <span class=\"num\">{treasures}</span>", open: false, Info(namesInfo), teamSwitch));
            sb.Append($"<div class=\"card\"><table id=\"t-treasures\"><tr><th>{L.T("Partie", "Match", "Partida")}</th><th>{L.T("Trésor", "Treasure", "Tesoro")}</th><th>{L.T("Obtenu à", "Obtained at", "Momento")}</th><th>{L.T("Source", "Source", "Origen")}</th><th>{L.T("Par", "By", "Por")}</th></tr>");
            foreach (var (m, f, team) in treasureRows.Concat(teamRows).OrderByDescending(r => When(r.m)).ThenBy(r => r.f.ObtainedAtSeconds ?? 0))
            {
                var when = f.ObtainedAtSeconds is float t ? (f.TimeExact ? Pretty.Clock(t) : $"{L.T("vers", "around", "hacia los")} {Pretty.Clock(t)}") : "?";
                string source;
                if (team && string.IsNullOrEmpty(f.Teammate))
                {
                    source = $"<span class=\"items\">{L.T("coffre hors de ta vue", "chest out of your sight", "cofre fuera de tu vista")}</span>";
                }
                else if (!f.SourceIsChest)
                {
                    source = $"<span class=\"warn\">{H(f.SourceLabel)} · {L.T("fiabilité faible", "low reliability", "fiabilidad baja")}</span>";
                }
                else
                {
                    source = H(f.SourceLabel) + (f.SourceCertain ? "" : $" <span class=\"items\">({Pretty.Likely})</span>");
                }
                var who = !team ? L.T("toi", "you", "tú")
                    : TeamIcon + (string.IsNullOrEmpty(f.Teammate) ? L.T("un coéquipier", "a teammate", "un compañero") : H(f.Teammate));
                var cls = team ? "r team" : f.SourceIsChest ? "r" : "r unusual";
                var stars = f.FromGoldenChest ? "<span class=\"gold\">★★</span>" : "<span class=\"star\">★</span>";
                sb.Append($"<tr class=\"{cls}\"><td class=\"nw num items\">{L.DateTimeShort(When(m))}</td><td>{stars} <b>{H(f.DisplayName)}</b></td><td class=\"nw num\">{when}</td><td>{source}</td><td class=\"by\">{who}</td></tr>");
            }
            sb.Append($"</table>{Pager("t-treasures")}</div>{FoldEnd}");
        }

        // Latest matches.
        sb.Append(FoldStart("matches", $"{L.T("Dernières parties", "Latest matches", "Últimas partidas")} · <span class=\"num\">{matches.Count.ToString("N0", Fr)}</span>", open: false));
        sb.Append($"<div class=\"card\"><table id=\"t-matches\"><tr><th>{L.T("Date", "Date", "Fecha")}</th><th>{L.T("Mode", "Mode", "Modo")}</th><th>{L.T("Place", "Placement", "Puesto")}</th><th style=\"text-align:right\">{L.T("Coffres", "Chests", "Cofres")}</th>{(teamChests > 0 ? $"<th class=\"sq\" style=\"text-align:right\">{H(OverlayModel.SquadLabel)}</th>" : "")}<th>{L.T("Trésor", "Treasure", "Tesoro")}</th></tr>");
        foreach (var m in matches)
        {
            var treasure = string.Join("<br>", m.TreasureFinds.Select(f => $"<span class=\"{(f.FromGoldenChest ? "gold" : "star")}\">{(f.FromGoldenChest ? "★★" : "★")} {H(f.DisplayName)}</span>"))
                + string.Concat(m.TeamTreasureFinds.Select((f, i) => $"<span class=\"team\">{(i == 0 && m.TreasureFinds.Count == 0 ? "" : "<br>")}{TeamIcon} ★ {H(f.DisplayName)}</span>"));
            var win = m.Placement == 1;
            sb.Append($"<tr class=\"r\"><td class=\"nw num items\">{L.DateTimeShort(When(m))}</td><td>{H(Pretty.Playlist(m.Playlist))}</td><td class=\"nw num\"{(win ? " style=\"color:var(--gold);font-weight:700\"" : "")}>{H(Pretty.Placement(m))}</td><td class=\"n num big\">{m.ChestsOpened}{(m.VehicleChestsMissing ? $" <span class=\"warn\" title=\"{H(VehicleWarning)}\">⚠</span>" : "")}</td>{(teamChests > 0 ? $"<td class=\"n num sq\" title=\"{H(string.Join(", ", m.TeamChests.Where(x => x.Chests > 0).Select(x => $"{x.Name} {x.Chests}")))}\">{(m.TeamChests.Count > 0 ? "+" + m.TeamChestsOpened : "")}</td>" : "")}<td class=\"items\">{treasure}</td></tr>");
        }
        sb.Append($"</table>{Pager("t-matches")}</div>");
        var blind = matches.Count(m => m.VehicleChestsMissing);
        if (blind > 0)
        {
            sb.Append($"<div class=\"flag\">{FlagIcon}<span><b>⚠ {L.Count(blind, "partie", "parties", "match", "matches", "partida", "partidas")}</b> · {H(VehicleWarning)}</span></div>");
        }
        sb.Append(FoldEnd);

        // Page script: team rows shown or not, long tables folded ("show more"), treasures ticked as obtained.
        // Everything is remembered in this browser only (the page is rewritten every few seconds by the counter).
        const string script = """
            <script>(function(){
            function get(k){try{return localStorage.getItem(k)}catch(e){return null}}
            function put(k,v){try{localStorage.setItem(k,v)}catch(e){}}
            var pagers=[];
            [].forEach.call(document.querySelectorAll('.pager'),function(box){
              var t=document.getElementById(box.getAttribute('data-for'));if(!t)return;
              var step=+box.getAttribute('data-step')||10,key='chestcounter-rows-'+t.id,n=parseInt(get(key),10);if(!(n>=step))n=step;
              var more=box.querySelector('.more'),all=box.querySelector('.all'),less=box.querySelector('.less'),info=box.querySelector('.count');
              function rows(){return [].filter.call(t.querySelectorAll('tr.r'),function(r){return !r.classList.contains('team')||document.body.classList.contains('showteam')})}
              function apply(){
                [].forEach.call(t.querySelectorAll('tr.r'),function(r){r.classList.remove('pg')});
                var v=rows();v.forEach(function(r,i){if(i>=n)r.classList.add('pg')});
                var shown=Math.min(n,v.length);
                info.textContent=info.getAttribute('data-fmt').replace('{0}',shown).replace('{1}',v.length);
                more.hidden=all.hidden=shown>=v.length;less.hidden=n<=step;box.hidden=v.length<=step;
              }
              more.onclick=function(){n+=step*2;put(key,n);apply()};
              all.onclick=function(){n=1e9;put(key,n);apply()};
              less.onclick=function(){n=step;put(key,n);apply()};
              apply();pagers.push(apply);
            });
            if(window.self!==window.top)document.documentElement.classList.add('embed');
            // Opened in the counter's window (?t=...): choices are saved by the counter itself (config, data.json).
            var tok=new URLSearchParams(location.search).get('t');
            // In the counter's window, the pencil of "chests in total" opens the window's edit box.
            if(tok&&window.self!==window.top){document.documentElement.classList.add('incounter');var eb=document.getElementById('editchests');if(eb)eb.onclick=function(){try{window.parent.postMessage({cc:'editChests'},location.origin)}catch(e){}}}
            function post(action,value){if(!tok)return;try{fetch('/api/action',{method:'POST',headers:{'X-Token':tok,'Content-Type':'application/json'},body:JSON.stringify({action:action,value:value})})}catch(e){}}
            // Switches: the counter's setting, or (report opened as a file) what this browser remembers.
            function toggle(id,cls,action){
              var b=document.getElementById(id),key='chestcounter-'+cls,v=document.body.getAttribute('data-'+cls)==='1';
              if(!tok){var saved=get(key);if(saved!==null)v=saved==='1'}
              function set(x){document.body.classList.toggle(cls,x);if(b)b.checked=x;pagers.forEach(function(p){p()})}
              set(v);
              if(b)b.addEventListener('change',function(){set(b.checked);put(key,b.checked?'1':'0');post(action,b.checked)});
            }
            toggle('showteam','showteam','showTeamInReport');
            toggle('showsquad','showsquad','showSquadInReport');
            // Folding sections: the title opens or closes the whole section.
            [].forEach.call(document.querySelectorAll('section.fold'),function(s){
              var key='chestcounter-fold-'+s.getAttribute('data-fold'),btn=s.querySelector('.foldbtn'),open=s.getAttribute('data-open')==='1';
              var saved=get(key);if(saved!==null)open=saved==='1';
              function set(o){s.classList.toggle('closed',!o);btn.setAttribute('aria-expanded',o?'true':'false')}
              set(open);
              btn.addEventListener('click',function(){var o=s.classList.contains('closed');set(o);put(key,o?'1':'0')});
            });
            var ok={};try{ok=JSON.parse(get('chestcounter-obtained')||'{}')||{}}catch(e){}
            function send(id,on){post('obtained',{id:id,on:on})}
            var boxes=document.querySelectorAll('input[data-prize]');
            function refresh(){
              var n=0;[].forEach.call(boxes,function(c){c.parentNode.classList.toggle('got',c.checked);if(c.checked)n++});
              var cc=document.getElementById('colcount');if(cc)cc.textContent=n+'/'+boxes.length;
              [].forEach.call(document.querySelectorAll('[data-items]'),function(card){
                var got=card.getAttribute('data-items').split(' ').some(function(id){var c=document.querySelector('input[data-prize="'+id+'"]');return c&&c.checked});
                card.classList.toggle('got',got);
              });
            }
            [].forEach.call(boxes,function(c){
              if(!c.disabled&&!c.hasAttribute('data-saved'))c.checked=!!ok[c.getAttribute('data-prize')];
              c.addEventListener('change',function(){var id=c.getAttribute('data-prize');if(c.checked)ok[id]=1;else delete ok[id];put('chestcounter-obtained',JSON.stringify(ok));send(id,c.checked);refresh()});
            });
            refresh();
            })();</script>
            """;
        sb.Append(L.T(
            "<footer>Compte les coffres que tu as ouverts ou détruits toi-même, d'après tes replays (« rediffusions » dans Fortnite), analysés après chaque partie. Les caisses de munitions et de nourriture ne sont pas comptées. ★★ = coffre doré de la chasse au trésor, détecté de façon certaine. ★ = trésor reçu grâce à un de tes coffres (moment exact), nommé quand l'objet est identifiable. L'icône équipe marque un trésor trouvé par un coéquipier (estimation : déduit des trésors que tu reçois sans avoir ouvert de coffre, car la chasse au trésor les partage avec toute l'équipe) ; il n'entre pas dans tes totaux. Le total de l'équipe ajoute aux tiens les coffres ouverts ou détruits par tes coéquipiers, comptés de la même façon dans tes replays ; un coffre ouvert à plus de 150 m de toi peut manquer, car le replay ne l'enregistre pas.</footer>",
            "<footer>Counts the chests you opened or destroyed yourself, from your replays, analyzed after each match. Ammo boxes and food boxes are not counted. ★★ = golden treasure hunt chest, detected for sure. ★ = treasure received from one of your own chests (exact moment), named when the item can be identified. The team icon marks a treasure found by a teammate (estimate: deduced from the treasures you receive without opening a chest, as the treasure hunt shares them with the whole team); it is not in your totals. The squad total adds the chests opened or destroyed by your teammates, counted the same way in your replays; a chest opened more than 150 m away from you can be missing, as the replay does not record it.</footer>",
            "<footer>Cuenta los cofres que has abierto o destruido tú mismo, a partir de tus repeticiones, analizadas después de cada partida. Las cajas de munición y de comida no se cuentan. ★★ = cofre dorado de la búsqueda del tesoro, detectado con seguridad. ★ = tesoro recibido de uno de tus cofres (momento exacto), con nombre cuando el objeto se puede identificar. El icono de equipo marca un tesoro encontrado por un compañero (estimación: deducido de los tesoros que recibes sin abrir un cofre, ya que la búsqueda del tesoro los comparte con todo el equipo); no cuenta en tus totales. El total del equipo añade a los tuyos los cofres abiertos o destruidos por tus compañeros, contados igual en tus repeticiones; un cofre abierto a más de 150 m de ti puede faltar, porque la repetición no lo graba.</footer>"));
        sb.Append(script).Append("</main></body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Start of a section the player can fold: its title is a button (with a chevron), <paramref name="right"/> sits at the
    /// right of the title. The page remembers the state in the browser. Ends with <see cref="FoldEnd"/>.
    /// </summary>
    private static string FoldStart(string key, string title, bool open, string info = "", string right = "") =>
        $"""<section class="fold{(open ? "" : " closed")}" data-fold="{key}" data-open="{(open ? 1 : 0)}"><div class="head"><h2 class="pirate"><button type="button" class="foldbtn" aria-expanded="{(open ? "true" : "false")}"><span class="chev" aria-hidden="true"></span>{title}</button>{info}</h2>{right}</div><div class="foldbody">""";

    private const string FoldEnd = "</div></section>";

    /// <summary>A small pencil (edit), drawn with the current text color.</summary>
    public const string PencilIcon = """<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 20h4L19 9l-4-4L4 16z"/><path d="M14 6l4 4"/></svg>""";

    /// <summary>A small on/off switch (same look as the settings of the counter's window).</summary>
    private static string Switch(string id, string label) =>
        $"""<label class="tsw"><input type="checkbox" id="{id}"><span class="trk" aria-hidden="true"></span><span>{label}</span></label>""";

    /// <summary>Folded table controls: the page shows the first rows, "show more" adds some, "show all" / "fold" (script).</summary>
    private static string Pager(string tableId) =>
        $"""<div class="pager" data-for="{tableId}" data-step="10" hidden><span class="count" data-fmt="{H(L.T("{0} sur {1}", "{0} of {1}", "{0} de {1}"))}"></span><button type="button" class="more">{L.T("Afficher 20 de plus", "Show 20 more", "Mostrar 20 más")}</button><button type="button" class="all">{L.T("Tout afficher", "Show all", "Mostrar todo")}</button><button type="button" class="less" hidden>{L.T("Réduire", "Show less", "Reducir")}</button></div>""";

    /// <summary>A small percentage with enough decimals to be meaningful (0.0064 %, 2.8 %, 63 %).</summary>
    private static string Percent(double p)
    {
        var v = p * 100;
        var decimals = v >= 10 ? 1 : v <= 0 ? 0 : Math.Clamp((int)Math.Ceiling(-Math.Log10(v)) + 1, 1, 8);
        var text = v.ToString("F" + decimals, Fr);
        return L.Code == "en" ? text + "%" : text + " %";
    }

    private static string Info(string text) =>
        $"""<span class="info" tabindex="0" role="note" aria-label="{H(text)}">i<span class="infotip">{H(text)}</span></span>""";

    /// <summary>
    /// "Your chances": for each kind of treasure, the chance of having obtained it at least once with the chests opened
    /// so far (1 − (1 − p)^n).
    /// </summary>
    private static string ChancesSection(int chests, IReadOnlyList<MatchRecord> matches)
    {
        var now = DateTime.UtcNow;
        if (now >= TreasureCatalog.Weeks[^1].To.AddDays(14))
        {
            return ""; // the hunt is over: odds and prizes no longer apply
        }

        var sb = new StringBuilder();
        var outfits = string.Join(" ", TreasureCatalog.Weeks.SelectMany(w => w.Prizes).Where(p => p.Kind == TreasureCatalog.GoldOutfit).Select(p => p.Id));
        // When each counted chest was opened (UTC): a chest of a Treasure Hour counts with the boosted odds.
        var chestTimes = matches.SelectMany(m => m.Chests.Select(c => DateTime.SpecifyKind(m.MatchStartUtc ?? m.AnalyzedAtUtc, DateTimeKind.Utc).ToUniversalTime().AddSeconds(c.TimeSeconds))).ToList();
        var plain = Math.Max(0, chests - chestTimes.Count); // added by hand: time unknown, normal odds
        var boosted = chestTimes.Count(t => TreasureCatalog.Boost(TreasureCatalog.GoldOutfit, t) > 1);
        double Chance(TreasureOdds odds) => odds.Kind == TreasureCatalog.PirateShop
            ? PirateOdds.ChanceFor(chests)
            : chests < chestTimes.Count
            ? TreasureCatalog.AtLeastOnce(odds.OneIn, chests)
            : TreasureCatalog.AtLeastOnce(odds, chestTimes, plain);
        var chancesInfo = L.T(
            $"Chaque coffre a toujours la même chance (par exemple 1 sur 1 562 500 pour la tenue en or) : elle n'augmente pas d'un coffre à l'autre. Ce qui augmente, c'est la chance d'avoir obtenu l'objet au moins une fois sur l'ensemble de tes {chests.ToString("N0", Fr)} coffres (ajoutés à la main compris) : 1 − (1 − 1/X)^N. Les coffres ouverts pendant une heure du trésor (« Treasure Hour », samedi 20h-22h et dimanche 3h-5h, heure de Paris) comptent avec leurs chances augmentées ({boosted.ToString("N0", Fr)} chez toi). Les coffres ouverts avant la chasse au trésor ne donnaient pas ces trésors. Cotes publiées par Epic Games, horaires d'après fortnite.bot.",
            $"Every chest always has the same odds (for example 1 in 1,562,500 for the gold outfit): they do not grow from one chest to the next. What grows is the chance of having obtained the item at least once over all your {chests.ToString("N0", Fr)} chests (those added by hand included): 1 − (1 − 1/X)^N. Chests opened during a Treasure Hour (Saturday 19:00-21:00 and Sunday 02:00-04:00 UTC) count with their boosted odds ({boosted.ToString("N0", Fr)} of yours). Chests opened before the treasure hunt did not give these treasures. Odds published by Epic Games, times from fortnite.bot.",
            $"Cada cofre tiene siempre la misma probabilidad (por ejemplo 1 entre 1.562.500 para el traje de oro): no aumenta de un cofre a otro. Lo que aumenta es la probabilidad de haber obtenido el objeto al menos una vez con todos tus {chests.ToString("N0", Fr)} cofres (incluidos los añadidos a mano): 1 − (1 − 1/X)^N. Los cofres abiertos durante una hora del tesoro («Treasure Hour», sábado 19:00-21:00 y domingo 02:00-04:00 UTC) cuentan con su probabilidad aumentada ({boosted.ToString("N0", Fr)} de los tuyos). Los cofres abiertos antes de la búsqueda del tesoro no daban estos tesoros. Probabilidades publicadas por Epic Games, horarios según fortnite.bot.");
        sb.Append(FoldStart("chances", L.T("Tes chances", "Your chances", "Tus probabilidades"), open: true, Info(chancesInfo)));
        sb.Append($"{TreasureHourBanner()}<div class=\"chances\">");

        string Big(TreasureOdds odds, string items)
        {
            var p = Chance(odds);
            var half = TreasureCatalog.ChestsFor(odds.OneIn, 0.5);
            var left = Math.Max(0, half - chests);
            var width = Math.Clamp(p * 100, 0.6, 100).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            return $"""
                <div class="odds ret" data-items="{items}" data-kind="{odds.Kind}" data-p="{p.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}">{(odds.Kind == TreasureCatalog.PirateShop && OperatingSystem.IsWindows() ? Brand.TintedTokenImg(OverlayColor, 84, "tokenpic") : "")}<div class="k"><span>{H(TreasureCatalog.KindName(odds.Kind))}<span class="boost" hidden></span></span><span class="gotbadge">✓ {L.T("obtenu", "obtained", "obtenido")}</span></div>
                <div class="v num">{(odds.Kind == TreasureCatalog.PirateShop ? PirateOdds.Percent(p) : Percent(p))}</div>
                <div class="l">{L.T($"de chances de l'avoir eu au moins une fois avec tes {chests.ToString("N0", Fr)} coffres", $"chance of having had it at least once with your {chests.ToString("N0", Fr)} chests", $"de probabilidad de haberlo conseguido al menos una vez con tus {chests.ToString("N0", Fr)} cofres")}</div>
                <span class="meter wide"><i style="width:{width}%"></i></span>
                <div class="s sline">{L.T($"1 sur {odds.OneIn.ToString("N0", Fr)} à chaque coffre · une chance sur deux vers {half.ToString("N0", Fr)} coffres", $"1 in {odds.OneIn.ToString("N0", Fr)} per chest · one chance in two at about {half.ToString("N0", Fr)} chests", $"1 entre {odds.OneIn.ToString("N0", Fr)} por cofre · una de cada dos hacia {half.ToString("N0", Fr)} cofres")}{(left > 0 ? L.T($" (encore {left.ToString("N0", Fr)})", $" ({left.ToString("N0", Fr)} to go)", $" (faltan {left.ToString("N0", Fr)})") : "")}</div></div>
                """;
        }
        sb.Append(Big(TreasureCatalog.Odds.First(o => o.Kind == TreasureCatalog.PirateShop), TreasureCatalog.PirateShop));
        sb.Append(Big(TreasureCatalog.Odds.First(o => o.Kind == TreasureCatalog.GoldOutfit), outfits));
        sb.Append("</div>");

        // The other kinds: chance of at least one, and how many of them on average with that many chests.
        sb.Append($"<div class=\"card\" style=\"margin-top:12px\"><table><tr><th>{L.T("Trésor", "Treasure", "Tesoro")}</th><th style=\"text-align:right\">{L.T("Par coffre", "Per chest", "Por cofre")}</th><th style=\"text-align:right\">{L.T("Au moins un", "At least one", "Al menos uno")}</th><th style=\"text-align:right\">{L.T("En moyenne", "On average", "En promedio")}</th></tr>");
        foreach (var odds in TreasureCatalog.Odds.Where(o => o.Kind is not (TreasureCatalog.GoldOutfit or TreasureCatalog.PirateShop)))
        {
            var average = (double)chests / odds.OneIn;
            sb.Append($"<tr data-kind=\"{odds.Kind}\"><td>{H(TreasureCatalog.KindName(odds.Kind))}<span class=\"boost\" hidden></span>{(odds.Weekly ? $" <span class=\"items\">· {L.T("change chaque semaine", "changes every week", "cambia cada semana")}</span>" : "")}</td><td class=\"n num pc\">1 / {odds.OneIn.ToString("N0", Fr)}</td><td class=\"n num\">{Percent(Chance(odds))}</td><td class=\"n num\">{average.ToString(average >= 10 ? "N0" : "N1", Fr)}</td></tr>");
        }
        sb.Append($"</table></div>{FoldEnd}");
        return sb.ToString();
    }

    /// <summary>The collection of this season's treasures, to tick what was obtained (folded by default).</summary>
    private static string CollectionSection(IReadOnlyList<MatchRecord> matches, IReadOnlyList<string> obtained)
    {
        var now = DateTime.UtcNow;
        if (now >= TreasureCatalog.Weeks[^1].To.AddDays(14))
        {
            return ""; // the hunt is over
        }
        var sb = new StringBuilder();
        // Golden chests seen in the replays tick their prize by themselves.
        var ticked = obtained.ToHashSet();
        // A teammate's treasure is the player's too (the hunt gives it to the whole team).
        var seen = matches.SelectMany(m => m.TreasureFinds.Concat(m.TeamTreasureFinds)).Select(f => f.PrizeId).Where(id => id != null).Select(id => id!.ToLowerInvariant()).ToHashSet();
        string Prize(TreasurePrize p)
        {
            var auto = p.PrizeIds.Any(id => seen.Contains(id.ToLowerInvariant()));
            var saved = !auto && ticked.Contains(p.Id);
            var src = TreasureImages.Src(p.Image);
            var img = src != null ? $"""<img src="{H(src)}" alt="" loading="lazy" onerror="this.remove()">""" : "";
            return $"""<label class="prize{(auto ? " got" : "")}"><input type="checkbox" data-prize="{p.Id}"{(auto ? " checked disabled" : saved ? " checked data-saved=\"1\"" : "")}><span class="pimg">{img}</span><span class="pn">{H(p.Name)}</span><span class="pk">{H(TreasureCatalog.KindName(p.Kind))}{(auto ? " · " + L.T("vu dans tes replays", "seen in your replays", "visto en tus repeticiones") : "")}</span></label>""";
        }
        var collectionInfo = L.T(
            "Coche ce que tu as déjà obtenu : c'est enregistré dans ce navigateur seulement. Un trésor sorti d'un coffre doré est coché tout seul quand le compteur le reconnaît. À part les gizmos, on ne peut pas obtenir deux fois le même trésor. Images : fortnite.bot, téléchargées une seule fois par le compteur et gardées dans son dossier de données.",
            "Tick what you already obtained: it is saved in this browser only. A treasure from a golden chest is ticked by itself when the counter recognizes it. Apart from gizmos, the same treasure cannot be obtained twice. Images: fortnite.bot, downloaded once by the counter and kept in its data folder.",
            "Marca lo que ya has conseguido: se guarda solo en este navegador. Un tesoro de un cofre dorado se marca solo cuando el contador lo reconoce. Salvo los artilugios, no se puede conseguir dos veces el mismo tesoro. Imágenes: fortnite.bot, descargadas una sola vez por el contador y guardadas en su carpeta de datos.");
        sb.Append(FoldStart("collection", $"{L.T("Ta collection", "Your collection", "Tu colección")} · <span class=\"num\" id=\"colcount\"></span>", open: false, Info(collectionInfo)));
        sb.Append("<div class=\"card collection\">");
        sb.Append($"<div class=\"wk\"><div class=\"wt\">{L.T("Toute la saison", "All season", "Toda la temporada")}</div><div class=\"prizes\">{Prize(TreasureCatalog.PirateShopAccess)}</div></div>");
        foreach (var week in TreasureCatalog.Weeks)
        {
            var current = now >= week.From && now < week.To;
            var dates = $"{week.From.ToLocalTime().ToString(L.T("d MMM HH:mm", "MMM d HH:mm", "d MMM HH:mm"), Fr)} – {week.To.ToLocalTime().ToString(L.T("d MMM HH:mm", "MMM d HH:mm", "d MMM HH:mm"), Fr)}";
            sb.Append($"<div class=\"wk{(current ? " now" : "")}\"><div class=\"wt\">{L.T("Semaine", "Week", "Semana")} {week.Number} · {H(week.Title)} <span class=\"items\">{dates}{(current ? " · " + L.T("cette semaine", "this week", "esta semana") : "")}</span></div>");
            sb.Append(week.Prizes.Count > 0
                ? $"<div class=\"prizes\">{string.Concat(week.Prizes.Select(Prize))}</div></div>"
                : $"<div class=\"items\" style=\"padding:4px 0 8px\">{L.T("Récompenses pas encore dévoilées : elles arriveront avec une mise à jour du compteur.", "Prizes not revealed yet: they will come with an update of the counter.", "Recompensas aún no reveladas: llegarán con una actualización del contador.")}</div></div>");
        }
        sb.Append("</div>").Append(FoldEnd);
        return sb.ToString();
    }

    /// <summary>
    /// The Treasure Hour banner: drawn by the page script from the schedule, against the clock of the PC, so that it
    /// switches by itself when an hour starts or ends ("live now, until 22:00" / "next one: Saturday 20:00").
    /// </summary>
    private static string TreasureHourBanner()
    {
        var hours = TreasureCatalog.TreasureHours.Select(h => new
        {
            s = new DateTimeOffset(h.StartUtc).ToUnixTimeMilliseconds(),
            e = new DateTimeOffset(h.EndUtc).ToUnixTimeMilliseconds(),
            b = h.Boosts.ToDictionary(kv => kv.Key, kv => kv.Value),
            c = h.Confirmed,
        });
        var odds = TreasureCatalog.Odds.ToDictionary(o => o.Kind, o => o.OneIn);
        var names = new Dictionary<string, string>
        {
            [TreasureCatalog.GoldOutfit] = TreasureCatalog.KindName(TreasureCatalog.GoldOutfit),
            [TreasureCatalog.PirateShop] = TreasureCatalog.KindName(TreasureCatalog.PirateShop),
            [TreasureCatalog.VehicleDecal] = TreasureCatalog.KindName(TreasureCatalog.VehicleDecal),
        };
        var text = new
        {
            live = L.T("Heure du trésor en cours, jusqu'à {0}", "Treasure Hour live now, until {0}", "Hora del tesoro en curso, hasta las {0}"),
            next = L.T("Prochaine heure du trésor : {0}", "Next Treasure Hour: {0}", "Próxima hora del tesoro: {0}"),
            expected = L.T("prévue, pas encore annoncée", "expected, not announced yet", "prevista, aún no anunciada"),
            perChest = L.T("1 sur {0} par coffre", "1 in {0} per chest", "1 entre {0} por cofre"),
            boostPerChest = L.T("1 sur {0} à chaque coffre pendant l'heure du trésor", "1 in {0} per chest during the Treasure Hour", "1 entre {0} por cofre durante la hora del tesoro"),
            boostHalf = L.T(" · une chance sur deux dans {0} coffres de plus à ce rythme", " · one chance in two within {0} more chests at this rate", " · una de cada dos en {0} cofres más a este ritmo"),
            locale = L.Culture.Name,
        };
        // The default encoder escapes < > & as <...: the JSON cannot close the <script> element it sits in.
        var json = System.Text.Json.JsonSerializer.Serialize(new { hours, odds, names, text });
        return $$"""
            <div class="thour" id="thour" hidden></div>
            <script type="application/json" id="thdata">{{json}}</script>
            <script>(function(){
            var d=JSON.parse(document.getElementById('thdata').textContent),el=document.getElementById('thour');
            function f(n){return Math.round(n).toLocaleString(d.text.locale)}
            function t(ms,withDay){var o=withDay?{weekday:'long',day:'numeric',month:'short',hour:'2-digit',minute:'2-digit'}:{hour:'2-digit',minute:'2-digit'};return new Date(ms).toLocaleString(d.text.locale,o)}
            function boosts(h,live){return Object.keys(h.b).map(function(k){return '<span><b>'+d.names[k]+' ×'+h.b[k].toLocaleString(d.text.locale)+'</b>'+(live?' · '+d.text.perChest.replace('{0}',f(d.odds[k]/h.b[k])):'')+'</span>'}).join('')}
            function draw(){
              var now=Date.now(),cur=d.hours.filter(function(h){return now>=h.s&&now<h.e})[0],nxt=d.hours.filter(function(h){return h.s>now}).sort(function(a,b){return a.s-b.s})[0];
              if(cur){el.className='thour live';el.innerHTML='<div class="tt">⚡ '+d.text.live.replace('{0}',t(cur.e,false))+'</div><div class="tb">'+boosts(cur,true)+'</div>';el.hidden=false}
              else if(nxt){el.className='thour';el.innerHTML='<div class="tt">'+d.text.next.replace('{0}',t(nxt.s,true))+(nxt.c?'':' <span class="exp">('+d.text.expected+')</span>')+'</div><div class="tb">'+boosts(nxt,false)+'</div>';el.hidden=false}
              else el.hidden=true;
              boost(cur);
            }
            // During a Treasure Hour, the odds of the boosted kinds (cards and table) show the boosted rate, in orange.
            function boost(cur){
              [].forEach.call(document.querySelectorAll('[data-kind]'),function(x){
                var k=x.getAttribute('data-kind'),b=cur&&cur.b[k]>1?cur.b[k]:0,badge=x.querySelector('.boost'),line=x.querySelector('.sline'),pc=x.querySelector('.pc');
                x.classList.toggle('boosted',!!b);
                if(badge){badge.hidden=!b;badge.textContent=b?'⚡ ×'+b.toLocaleString(d.text.locale):''}
                if(line){
                  if(!line.hasAttribute('data-n'))line.setAttribute('data-n',line.innerHTML);
                  if(b){var one=d.odds[k]/b,p=+x.getAttribute('data-p'),s=d.text.boostPerChest.replace('{0}',f(one));
                    if(p<0.5)s+=d.text.boostHalf.replace('{0}',f(Math.ceil(Math.log(0.5/(1-p))/Math.log(1-1/one))));
                    line.innerHTML='<span class="boostv">'+s+'</span>'}
                  else line.innerHTML=line.getAttribute('data-n');
                }
                if(pc){if(!pc.hasAttribute('data-n'))pc.setAttribute('data-n',pc.innerHTML);pc.innerHTML=b?'<span class="boostv">1 / '+f(d.odds[k]/b)+'</span>':pc.getAttribute('data-n')}
              });
            }
            draw();document.addEventListener('DOMContentLoaded',draw);setInterval(draw,30000);
            })();</script>
            """;
    }

    /// <summary>
    /// The rarest treasures (Pirate Shop Access, gold outfits) that other players got in the player's matches, from
    /// golden chests opened within reach of the replay. Experimental: always shown, even empty, to be checked with
    /// friends before it is trusted.
    /// </summary>
    private static string SeenSection(IReadOnlyList<MatchRecord> matches)
    {
        var rows = matches
            .SelectMany(m => m.SeenPrizes.Select(s => (m, s, Prize: TreasureCatalog.Find(s.PrizeId))))
            .Where(r => r.Prize?.Kind is TreasureCatalog.PirateShop or TreasureCatalog.GoldOutfit)
            .OrderByDescending(r => When(r.m)).ThenBy(r => r.s.TimeSeconds)
            .ToList();
        var info = L.T(
            "Un coffre doré est envoyé à tous les joueurs à environ 150 m, avec le trésor et le joueur qui l'a ouvert : ces trésors-là ont été vus dans tes replays. Un joueur plus loin de toi n'y figure pas.",
            "A golden chest is sent to every player within about 150 m, with the treasure and the player who opened it: these were seen in your replays. A player farther away from you is not in them.",
            "Un cofre dorado se envía a todos los jugadores a unos 150 m, con el tesoro y el jugador que lo abrió: estos se vieron en tus repeticiones. Un jugador más lejos de ti no aparece.");
        var sb = new StringBuilder();
        sb.Append(FoldStart("seen", $"{L.T("Ils l'ont eu dans tes parties", "They got it in your matches", "Lo consiguieron en tus partidas")} · <span class=\"num\">{rows.Count}</span>", open: false,
            $" <span class=\"exper\">{L.T("expérimental", "experimental", "experimental")}</span>{Info(info)}"));
        sb.Append("<div class=\"card\">");
        if (rows.Count == 0)
        {
            sb.Append($"<p class=\"items\" style=\"margin:8px 0\">{L.T("Aucun pour l'instant : quand un joueur à moins de 150 m de toi décroche l'accès à la boutique pirate ou une tenue en or, il apparaît ici avec la partie, son pseudo et le moment.", "None yet: when a player within 150 m of you gets the Pirate Shop Access or a gold outfit, they show up here with the match, their name and the moment.", "Ninguno por ahora: cuando un jugador a menos de 150 m de ti consigue el acceso a la tienda pirata o un traje de oro, aparece aquí con la partida, su nombre y el momento.")}</p></div>{FoldEnd}");
            return sb.ToString();
        }
        sb.Append($"<table id=\"t-seen\"><tr><th>{L.T("Partie", "Match", "Partida")}</th><th>{L.T("Joueur", "Player", "Jugador")}</th><th>{L.T("Trésor", "Treasure", "Tesoro")}</th><th>{L.T("Moment", "Time", "Momento")}</th></tr>");
        foreach (var (m, s, prize) in rows)
        {
            var name = prize!.Kind == TreasureCatalog.PirateShop ? TreasureCatalog.KindName(prize.Kind) : prize.Name;
            sb.Append($"<tr class=\"r\"><td class=\"nw num items\">{L.DateTimeShort(When(m))}</td><td><b>{H(s.Player)}</b></td><td><span class=\"gold\">★★</span> {H(name)}</td><td class=\"nw num\">{Pretty.Clock(s.TimeSeconds)}</td></tr>");
        }
        sb.Append($"</table>{Pager("t-seen")}</div>{FoldEnd}");
        return sb.ToString();
    }

    /// <summary>"i" next to the squad total: why the teammates' count is lower than what they really opened.</summary>
    private static string SquadInfo
    {
        get
        {
            var text = L.T(
                "Pourquoi ce nombre est bas ? Il vient de TES replays. Fortnite n'y enregistre que ce qui se passe à environ 150 m autour de toi : un coffre ouvert par un coéquipier plus loin n'y figure pas et ne peut pas être compté. Rien non plus après ton départ de la partie (élimination puis retour au salon). Plus vous lootez ensemble, plus le nombre est juste. Les coffres comptés, eux, le sont avec les mêmes règles que les tiens.",
                "Why is this number low? It comes from YOUR replays. Fortnite only records what happens within about 150 m of you: a chest a teammate opens farther away is not in it and cannot be counted. Nothing either once you leave the match (eliminated, back to the lobby). The more you loot together, the closer it gets. The chests that are counted are counted with the same rules as yours.",
                "¿Por qué este número es bajo? Viene de TUS repeticiones. Fortnite solo graba lo que pasa a unos 150 m a tu alrededor: un cofre que un compañero abre más lejos no está en ella y no se puede contar. Tampoco nada después de que salgas de la partida (eliminado, de vuelta al lobby). Cuanto más looteéis juntos, más exacto es. Los cofres contados se cuentan con las mismas reglas que los tuyos.");
            return $"""<span class="info" tabindex="0" role="note" aria-label="{H(text)}">i<span class="infotip">{H(text)}</span></span>""";
        }
    }

    private static string VehicleWarning => L.T(
        "Fortnite n'a pas enregistré ce que ton véhicule a détruit dans ce replay : du butin de coffres a été vu le long de ton trajet sans aucun coffre enregistré, ces coffres ne peuvent pas être comptés. Désactive « Enregistrer des rediffusions de haute qualité » dans Fortnite (Paramètres → Jeu → Rediffusions) puis relance le jeu.",
        "Fortnite did not record what your vehicle destroyed in this replay: chest loot was seen along your drive with no chest recorded, so those chests cannot be counted. Turn off \"Record High Quality Replays\" in Fortnite (Settings → Game → Replays), then restart the game.",
        "Fortnite no grabó lo que destruyó tu vehículo en esta repetición: se vio botín de cofres a lo largo de tu trayecto sin ningún cofre grabado, así que esos cofres no se pueden contar. Desactiva «Grabar repeticiones de alta calidad» en Fortnite (Ajustes → Juego → Repeticiones) y reinicia el juego.");

    /// <summary>Messages of content.json for these days (text only, escaped).</summary>
    private static string NoticeCards() => string.Concat(TreasureCatalog.ActiveNotices(DateTime.UtcNow)
        .Select(n => $"<div class=\"notice{(n.Level == "warning" ? " warning" : "")}\">{(n.TitleIn(L.Code) is { Length: > 0 } title ? $"<b>{H(title)}</b><br>" : "")}{H(n.TextIn(L.Code))}{(n.Link != null ? $"<br><a href=\"{H(n.Link)}\" target=\"_blank\" rel=\"noopener\">{L.T("En savoir plus", "More info", "Más información")}</a>" : "")}</div>"));

    private static string LoadingCard(ScanProgress? p)
    {
        if (p == null)
        {
            return "";
        }
        var pct = p.Total > 0 ? (int)Math.Round(100.0 * p.Done / p.Total) : 0;
        return L.T(
            $"""<div class="loading"><div class="spin"></div><div class="grow"><b>Analyse de tes replays… {p.Done} / {p.Total}</b><div class="pbar"><i style="width:{pct}%"></i></div><span class="muted">Cette page se met à jour toute seule : les chiffres ci-dessous augmentent au fil de la lecture des replays.</span></div></div>""",
            $"""<div class="loading"><div class="spin"></div><div class="grow"><b>Analyzing your replays… {p.Done} / {p.Total}</b><div class="pbar"><i style="width:{pct}%"></i></div><span class="muted">This page updates by itself: the numbers below grow as the replays are read.</span></div></div>""",
            $"""<div class="loading"><div class="spin"></div><div class="grow"><b>Analizando tus repeticiones… {p.Done} / {p.Total}</b><div class="pbar"><i style="width:{pct}%"></i></div><span class="muted">Esta página se actualiza sola: las cifras de abajo aumentan a medida que se leen las repeticiones.</span></div></div>""");
    }

    private static string ProfilesLine(IReadOnlyList<Profile> profiles, string player)
    {
        var others = profiles.Where(p => p.Name != player).Select(p => H(p.Name)).ToList();
        if (others.Count == 0)
        {
            return "";
        }
        return $"""<div class="profiles muted">{L.T("Autres profils sur ce PC : ", "Other profiles on this PC: ", "Otros perfiles en este PC: ")}{string.Join(", ", others)} · {L.T("« Changer de profil » dans le compteur", "\"Switch profile\" in the counter", "«Cambiar de perfil» en el contador")}</div>""";
    }

    /// <summary>Style of the OBS page: the one chosen for the overlay ("hud" or "classic").</summary>
    public static volatile string OverlayStyle = "jet";

    /// <summary>The accent of the report follows the HUD color.</summary>
    private static string AccentCss => ":root{--sea:" + Brand.NormalizeColor(OverlayColor) + "}";

    /// <summary>Color of the fighter-jet HUD on the OBS page.</summary>
    public static volatile string OverlayColor = Brand.DefaultHudColor;

    /// <summary>Animations of the jet HUD on the OBS page.</summary>
    public static volatile bool OverlayAnimations = true;

    /// <summary>What the overlay shows right now (also written by <see cref="WriteAll"/>, so that the OBS page never goes back).</summary>
    private static volatile OverlayView? LastOverlay;

    /// <summary>Rewrites only the OBS overlay page (called on every live update).</summary>
    public static void WriteOverlay(Store store, OverlayView view)
    {
        LastOverlay = view;
        try
        {
            WriteAtomic(store.OverlayPath, Overlay(view));
        }
        catch
        {
            // the page is a convenience: never stop the tracker for it
        }
    }

    public static string Overlay(OverlayView v) => OverlayStyle switch { "classic" => OverlayClassic(v), "hud" => OverlayHud(v), _ => OverlayJet(v) };

    private static (string Text, bool Live) OverlayState(OverlayView v)
    {
        if (v.CurrentLive && v.Current is { } cur && cur != "…")
        {
            return ($"{L.T("En direct", "Live", "En directo")}  +{cur}", true);
        }
        if (v.CurrentLive)
        {
            return (L.T("En partie", "In a match", "En partida"), true);
        }
        return ($"{L.T("Dernière", "Last", "Última")}  {v.Last ?? "—"}", false);
    }

    private static string OverlayCoin => Brand.TokenPng != null ? Brand.TokenImg(40) : """<svg viewBox="0 0 28 28" aria-hidden="true"><circle cx="14" cy="14" r="12" fill="#34e6c4" stroke="#070a12" stroke-width="2.5"/><circle cx="14" cy="14" r="6" fill="#ffc940" stroke="#070a12" stroke-width="2"/></svg>""";
    private const string OverlayChestUnused = """<svg viewBox="0 0 30 26" aria-hidden="true"><rect x="2" y="10" width="26" height="14" rx="2" fill="#ffc940" stroke="#070a12" stroke-width="2.5"/><path d="M2 12 Q15 -2 28 12" fill="#d6a028" stroke="#070a12" stroke-width="2.5"/><rect x="12.5" y="12" width="5" height="6" rx="1" fill="#070a12"/></svg>""";

    /// <summary>
    /// The HUD overlay as a web page, for OBS ("Browser" source, local file): transparent, figures outlined like the
    /// game's, the same three lines as the overlay on screen. Refreshes itself every 5 seconds.
    /// </summary>
    private static string OverlayHud(OverlayView v)
    {
        var state = OverlayState(v);
        return $$"""
            <!doctype html><html lang="{{L.Code}}"><head><meta charset="utf-8"><meta http-equiv="refresh" content="5">
            <title>Pirate Shop overlay</title>
            <style>
            html,body{margin:0;background:transparent}
            body{padding:10px 14px;display:inline-block;color:#f0f4fa;font-family:Bahnschrift,"Segoe UI",sans-serif;font-weight:600;text-align:right;
                 -webkit-text-stroke:4px #070a12;paint-order:stroke fill}
            .r{display:flex;align-items:center;justify-content:flex-end;gap:10px;line-height:1.15}
            .p{font-size:44px;font-weight:700;color:#34e6c4}
            .p svg,.p img{width:40px;height:40px}
            .t{font-size:22px}
            .t svg{width:24px;height:21px}
            .s{font-size:20px;color:#ffe9a6}
            .s.live{color:#ff4d5e}
            .dot{width:10px;height:10px;border-radius:50%;background:#ff4d5e;box-shadow:0 0 0 2px #070a12}
            .emo{-webkit-text-stroke:0;font-family:"Segoe UI Emoji";font-size:.9em}
            </style></head><body>
            <div class="r p">{{OverlayCoin}}{{H(v.Pirate)}}</div>
            <div class="r t"><span class="emo">🧰</span>{{H(v.Total)}}</div>
            <div class="r s{{(state.Live ? " live" : "")}}">{{(state.Live ? "<i class=\"dot\"></i>" : "")}}{{H(state.Text)}}</div>
            </body></html>
            """;
    }

    /// <summary>The fighter-jet head-up display as a web page, for OBS: phosphor green strokes on a transparent background.</summary>
    private static string OverlayJet(OverlayView v)
    {
        var state = OverlayState(v);
        var caret = Math.Clamp(v.PirateChance / 0.5 * 100, 0, 100).ToString("F2", CultureInfo.InvariantCulture);
        var value = v.Pirate.Replace(" ", "").Replace(" ", "");
        var odds = "1:" + PirateOdds.OneIn.ToString(CultureInfo.InvariantCulture);
        var color = Brand.NormalizeColor(OverlayColor);
        var token = OperatingSystem.IsWindows() ? Brand.TintedTokenImg(color, 40) : "";
        return $$"""
            <!doctype html><html lang="{{L.Code}}"><head><meta charset="utf-8"><meta http-equiv="refresh" content="5">
            <title>Pirate Shop overlay</title>
            <style>
            html,body{margin:0;background:transparent}
            body{display:inline-block;padding:6px}
            .hud{position:relative;min-width:260px;padding:16px 18px;color:{{color}};font:700 14px/1.3 Consolas,"Cascadia Mono",monospace;text-shadow:0 0 1px #070a12,0 0 3px #070a12,0 0 6px {{color}}66}
            .c{position:absolute;width:16px;height:16px;border-color:{{color}};border-style:solid;border-width:0;filter:drop-shadow(0 0 1px #070a12)}
            .c1{top:0;left:0;border-top-width:2px;border-left-width:2px}.c2{top:0;right:0;border-top-width:2px;border-right-width:2px}
            .c3{bottom:0;left:0;border-bottom-width:2px;border-left-width:2px}.c4{bottom:0;right:0;border-bottom-width:2px;border-right-width:2px}
            .row{display:flex;justify-content:space-between;gap:24px}
            .bigrow{display:flex;align-items:center;justify-content:space-between;gap:10px}.bigrow img,.bigrow .sp{width:40px;height:40px;flex:none}
            .big{font-size:36px;text-align:center;margin:4px 0 2px;letter-spacing:.02em}
            .big:before{content:"› ";opacity:.9}.big:after{content:" ‹";opacity:.9}
            .scale{position:relative;height:18px;margin:4px 0 8px;border-bottom:2px solid {{color}};background:repeating-linear-gradient(90deg,{{color}} 0 2px,transparent 2px 20%) bottom/100% 6px no-repeat}
            .caret{position:absolute;bottom:2px;width:0;height:0;border-left:6px solid transparent;border-right:6px solid transparent;border-top:9px solid {{color}};transform:translateX(-6px)}
            .live{color:inherit}
            .emo{font-family:"Segoe UI Emoji";margin-right:6px}
            .hud{text-shadow:0 0 1px #070a12,0 0 2px #070a12,0 0 6px {{color}}aa,0 0 14px {{color}}66}
            .c,.scale,.caret,.bigrow img{filter:drop-shadow(0 0 4px {{color}}aa)}
            {{(OverlayAnimations ? AnimCss : "")}}
            </style></head><body><div class="hud{{(OverlayAnimations ? " anim" : "")}}"><i class="c c1"></i><i class="c c2"></i><i class="c c3"></i><i class="c c4"></i>
            <div class="row"><span>{{L.T("BOUTIQUE PIRATE", "PIRATE SHOP", "TIENDA PIRATA")}}</span><span>{{odds}}</span></div>
            <div class="bigrow">{{token}}<div class="big">{{H(value)}}</div><span class="sp"></span></div>
            <div class="scale"><i class="caret" style="left:{{caret}}%"></i></div>
            <div class="row"><span><span class="emo">🧰</span>{{H(v.Total)}}</span><span class="{{(state.Live ? "live" : "")}}">{{H(state.Text.ToUpperInvariant())}}</span></div>
            </div></body></html>
            """;
    }

    /// <summary>Animations of the jet HUD page (same as the overlay: phosphor flicker, scan sweep, glitch).</summary>
    private const string AnimCss = """
        .anim{animation:flick 2.3s infinite steps(1),glitch 6.5s infinite}
        .anim:after{content:"";position:absolute;left:0;right:0;height:14px;top:-14px;background:linear-gradient(transparent,rgba(255,255,255,.12),transparent);animation:sweep 2.6s linear infinite;pointer-events:none;mix-blend-mode:screen}
        @keyframes flick{0%{opacity:1}8%{opacity:.92}9%{opacity:1}47%{opacity:.95}48%{opacity:.78}49%{opacity:1}72%{opacity:.94}}
        @keyframes sweep{to{top:100%}}
        @keyframes glitch{0%,96%,100%{transform:none;clip-path:none}97%{transform:translateX(5px);clip-path:inset(20% 0 55% 0)}98%{transform:translateX(-6px);clip-path:inset(60% 0 12% 0)}99%{transform:translateX(3px)}}
        @media (prefers-reduced-motion:reduce){.anim,.anim:after{animation:none}}
        """;

    /// <summary>The card overlay as a web page, for OBS.</summary>
    private static string OverlayClassic(OverlayView v)
    {
        var state = OverlayState(v);
        var width = Math.Clamp(v.PirateChance * 100, 1.5, 100).ToString("F2", CultureInfo.InvariantCulture);
        return $$"""
            <!doctype html><html lang="{{L.Code}}"><head><meta charset="utf-8"><meta http-equiv="refresh" content="5">
            <title>Pirate Shop overlay</title>
            <style>
            html,body{margin:0;background:transparent;color:#f0f4fa;font:600 15px/1.3 "Segoe UI",system-ui,sans-serif}
            .box{display:inline-block;min-width:220px;padding:12px 16px 12px;border-radius:14px;background:rgba(12,17,28,.9);border:1px solid #24324c}
            .k{font-size:11px;font-weight:700;letter-spacing:.12em;color:#94a0b8}
            .p{font:700 38px/1.1 Bahnschrift,"Segoe UI",sans-serif;color:#34e6c4;font-variant-numeric:tabular-nums}
            .bar{height:5px;border-radius:3px;background:#202a3e;margin:6px 0 10px;overflow:hidden}.bar i{display:block;height:100%;background:#34e6c4;border-radius:3px}
            .f{display:flex;justify-content:space-between;gap:16px;align-items:center}
            .f span{display:flex;align-items:center;gap:6px}.f svg{width:16px;height:14px}
            .s{color:#94a0b8}.s.live{color:#ff4d5e}
            .dot{width:7px;height:7px;border-radius:50%;background:#ff4d5e}
            .emo{font-family:"Segoe UI Emoji"}
            </style></head><body><div class="box">
            <div class="k">{{L.T("BOUTIQUE PIRATE", "PIRATE SHOP", "TIENDA PIRATA")}}</div>
            <div class="p">{{H(v.Pirate)}}</div>
            <div class="bar"><i style="width:{{width}}%"></i></div>
            <div class="f"><span><span class="emo">🧰</span>{{H(v.Total)}}</span><span class="s{{(state.Live ? " live" : "")}}">{{(state.Live ? "<i class=\"dot\"></i>" : "")}}{{H(state.Text)}}</span></div>
            </div></body></html>
            """;
    }

    public static string Csv(TrackerData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine(L.T("date;mode;joueur;place;joueurs;coffres;coffres_coequipiers;caisses_munitions;tresors;fichier", "date;mode;player;placement;players;chests;teammate_chests;ammo_boxes;treasures;file", "fecha;modo;jugador;puesto;jugadores;cofres;cofres_companeros;cajas_municion;tesoros;archivo"));

        foreach (var m in Profiles.Counted(data).OrderBy(When))
        {
            var t = string.Join(" + ", m.TreasureFinds.Select(f => f.DisplayName));
            sb.AppendLine(string.Join(";",
                When(m).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                Pretty.Playlist(m.Playlist), m.PlayerName, m.Placement, m.TotalPlayers,
                m.ChestsOpened, m.TeamChestsOpened, m.AmmoBoxesOpened, t, m.ReplayFile));
        }
        return sb.ToString();
    }
}
