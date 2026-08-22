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

// ── PHASE B: cache control ──────────────────────────────────────────────

var expire = new DefaultHttpContext();
expire.UpExpireCache("/shop/*");
Check(expire.Response.Headers["X-Up-Expire-Cache"] == "/shop/*", "UpExpireCache writes the URL pattern");

var expireAll = new DefaultHttpContext();
expireAll.UpExpireCache();
Check(expireAll.Response.Headers["X-Up-Expire-Cache"] == "*", "UpExpireCache defaults to everything");

// Unpoly clears the whole cache after any non-GET; "false" is how a POST opts out.
var keep = new DefaultHttpContext();
keep.UpKeepCache();
Check(keep.Response.Headers["X-Up-Expire-Cache"] == "false", "UpKeepCache preserves the cache after a non-GET");

var evict = new DefaultHttpContext();
evict.UpEvictCache("/cart");
Check(evict.Response.Headers["X-Up-Evict-Cache"] == "/cart", "UpEvictCache writes its own header, not Expire");

// ── PHASE B: conditional requests ───────────────────────────────────────

static HttpContext Cond(string? ifNoneMatch = null, string? ifModifiedSince = null)
{
    var c = new DefaultHttpContext();
    if (ifNoneMatch is not null) c.Request.Headers.IfNoneMatch = ifNoneMatch;
    if (ifModifiedSince is not null) c.Request.Headers.IfModifiedSince = ifModifiedSince;
    return c;
}

var firstVisit = Cond();
Check(!firstVisit.UpNotModified("\"v1\""), "no If-None-Match means the client has nothing");
Check(firstVisit.Response.Headers.ETag == "\"v1\"", "the ETag is published even on a 200");

var current = Cond(ifNoneMatch: "\"v1\"");
Check(current.UpNotModified("\"v1\""), "a matching ETag means the client is current");
Check(current.Response.StatusCode == 304, "a current client gets 304");

Check(!Cond(ifNoneMatch: "\"v0\"").UpNotModified("\"v1\""), "a stale ETag still renders");

// A cache may weaken a strong tag in transit, so the W/ prefix must not break the match.
Check(Cond(ifNoneMatch: "W/\"v1\"").UpNotModified("\"v1\""), "a weakened ETag still matches");
Check(Cond(ifNoneMatch: "*").UpNotModified("\"v1\""), "If-None-Match: * matches anything");
Check(Cond(ifNoneMatch: "\"a\", \"v1\"").UpNotModified("\"v1\""), "If-None-Match may be a list");

var t = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
Check(Cond(ifModifiedSince: t.ToString("R")).UpNotModified(lastModified: t), "unchanged since that time means 304");
Check(!Cond(ifModifiedSince: t.ToString("R")).UpNotModified(lastModified: t.AddSeconds(1)), "newer data still renders");

// HTTP dates carry whole seconds. Without truncating, sub-second precision would make
// every comparison miss and the 304 path would silently never fire.
Check(Cond(ifModifiedSince: t.ToString("R")).UpNotModified(lastModified: t.AddMilliseconds(400)),
      "sub-second precision is truncated before comparing");

Console.WriteLine($"OK — {passed} checks passed (Phase A + B)");
