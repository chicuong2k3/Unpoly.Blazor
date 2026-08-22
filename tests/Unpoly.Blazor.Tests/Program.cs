using Microsoft.AspNetCore.Http;
using Unpoly.Blazor;

// Minimal checks, no framework. `dotnet run` prints OK or throws.
// Every phase appends one block here.

int passed = 0;
void Check(bool ok, string what)
{
    if (!ok) throw new Exception($"FAIL: {what}");
    passed++;
}

static HttpContext Ctx(string? target = null, string? version = "3.10.2")
{
    var c = new DefaultHttpContext();
    if (version is not null) c.Request.Headers["X-Up-Version"] = version;
    if (target is not null) c.Request.Headers["X-Up-Target"] = target;
    return c;
}

// ── PHASE A ─────────────────────────────────────────────────────────────

Check(!Ctx(version: null).IsUnpoly(), "a plain request is not an Unpoly request");
Check(Ctx().IsUnpoly(), "X-Up-Version marks an Unpoly request");

Check(Ctx(".content").IsUpFragment(), "an ordinary target means a fragment request");
Check(!Ctx("body").IsUpFragment(), "target body means a full page");
Check(!Ctx(":main").IsUpFragment(), ":main means a full page");
Check(!Ctx().IsUpFragment(), "no target means a full page");
Check(!Ctx(version: null, target: ".content").IsUpFragment(), "not an Unpoly request means a full page");

// The trap: X-Up-Target is a LIST, not a single selector.
// Without this check the header and footer vanish from full page loads, silently.
Check(!Ctx("body, .flash").IsUpFragment(), "a list containing body is still a full page");
Check(Ctx(".content, .flash").IsUpFragment(), "a list of plain fragments is a fragment request");
Check(Ctx(".content , .flash").UpTargets().Length == 2, "a whitespace-padded list splits correctly");
Check(Ctx(".content , .flash").UpTargets()[1] == ".flash", "split entries are trimmed");

// Targets carry modifiers: :before :after :maybe :content. They change how the match is
// applied, not what is matched, so classification must look past them.
// 📖 https://unpoly.com/targeting-fragments
Check(Ctx(".tasks:after").IsUpFragment(), ":after on a plain selector is still a fragment");
Check(!Ctx("body:after").IsUpFragment(), ":after on body appends into body, so full page");
Check(!Ctx(":main:content").IsUpFragment(), ":content on :main is still the main region");
Check(Ctx(".flash:maybe").IsUpFragment(), ":maybe marks a target optional, not whole-page");
Check(!Ctx(".list:after, body").IsUpFragment(), "body anywhere in a modified list wins");

Check(Ctx(":none").WantsNothing(), ":none means the client wants no content");
Check(!Ctx(".content").WantsNothing(), "an ordinary target still wants content");

var retargeted = new DefaultHttpContext();
retargeted.UpRetarget(".sidebar");
Check(retargeted.Response.Headers["X-Up-Target"] == ".sidebar", "UpRetarget writes the header");

// ── PHASE B (partial: Vary) ─────────────────────────────────────────────

var vary = new DefaultHttpContext();
vary.UpVary("X-Up-Target", "X-Up-Version");
Check(vary.Response.Headers.Vary == "X-Up-Target, X-Up-Version", "UpVary writes both header names");

// Must merge, not clobber: compression and content negotiation set Vary too.
var varyMerge = new DefaultHttpContext();
varyMerge.Response.Headers.Vary = "Accept-Encoding";
varyMerge.UpVary("X-Up-Target");
Check(varyMerge.Response.Headers.Vary == "Accept-Encoding, X-Up-Target", "UpVary merges with an existing Vary");

var varyDupe = new DefaultHttpContext();
varyDupe.UpVary("X-Up-Target");
varyDupe.UpVary("x-up-target", "X-Up-Version");
Check(varyDupe.Response.Headers.Vary == "X-Up-Target, X-Up-Version", "UpVary dedupes case-insensitively");

Console.WriteLine($"OK — {passed} checks passed (Phase A + Vary)");
