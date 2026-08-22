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
    // DefaultHttpContext leaves Method empty, and UpNotModified only answers safe methods.
    var c = new DefaultHttpContext();
    c.Request.Method = "GET";
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

// ── PHASE C: forms ──────────────────────────────────────────────────────

static HttpContext Val(string? validate = null, string? target = null, string? failTarget = null)
{
    var c = new DefaultHttpContext();
    c.Request.Headers["X-Up-Version"] = "3.10.2";
    if (validate is not null) c.Request.Headers["X-Up-Validate"] = validate;
    if (target is not null) c.Request.Headers["X-Up-Target"] = target;
    if (failTarget is not null) c.Request.Headers["X-Up-Fail-Target"] = failTarget;
    return c;
}

Check(!Val().IsUpValidating(), "no X-Up-Validate means a real submission");
Check(Val("email").IsUpValidating(), "X-Up-Validate marks a validation request");
Check(Val("").IsUpValidating(), "an empty X-Up-Validate is still a validation request");

// Unpoly batches fields into ONE request, separated by spaces — not commas.
// 📖 https://unpoly.com/X-Up-Validate
Check(Val("email").UpValidatingFields() is ["email"], "a single field parses");
Check(Val("email password").UpValidatingFields() is ["email", "password"], "fields split on spaces");
Check(Val("email  password").UpValidatingFields().Length == 2, "repeated spaces do not produce empties");

// :unknown has two causes: the origin was not a field, or the list overflowed
// up.protocol.config.maxHeaderSize. Both mean "validate the whole form".
Check(Val(":unknown").IsUpValidatingUnknown(), ":unknown is recognised");
Check(Val(":unknown").UpValidatingFields().Length == 0, ":unknown names no fields");
Check(!Val("email").IsUpValidatingUnknown(), "a named field is not :unknown");

// Failure swaps X-Up-Fail-Target, but chrome is rendered by the layout before the page has
// picked a status — so both branches must be considered. 📖 https://unpoly.com/failed-responses
Check(Val(target: ".content").IsUpFragment(), "no fail target behaves as before");
Check(Val(target: ".content", failTarget: ".form").IsUpFragment(), "two fragment targets stay a fragment");
Check(!Val(target: ".content", failTarget: "body").IsUpFragment(),
      "a whole-page FAIL target forces chrome, or a 422 body swap arrives with no nav");
Check(Val(target: ".a", failTarget: ".b, .c").UpFailTargets().Length == 2, "fail targets split on commas");

// Conditional requests are for safe methods. Answering 304 to a POST would skip the
// handler, so the submission would silently do nothing.
var post = Cond(ifNoneMatch: "\"v1\"");
post.Request.Method = "POST";
Check(!post.UpNotModified("\"v1\""), "a POST is never answered 304, even with a matching ETag");
Check(post.Response.StatusCode != 304, "and its status is left alone");
Check(post.Response.Headers.ETag.Count == 0, "nor is a version published on a POST");

var head = Cond(ifNoneMatch: "\"v1\"");
head.Request.Method = "HEAD";
Check(head.UpNotModified("\"v1\""), "HEAD is safe, so it still gets 304");

// ── UpChrome.Provides ───────────────────────────────────────────────────
// A client may target something that lives INSIDE the chrome. Without declaring it,
// the chrome is stripped, the target is absent from the response, and the swap finds
// nothing — silently, with no error anywhere.

Check(Val(target: ".content, .site-nav").UpWantsAny(".site-nav"), "a targeted chrome selector is wanted");
Check(!Val(target: ".content").UpWantsAny(".site-nav"), "an untargeted chrome selector is not");
Check(Val(target: ".site-nav:after").UpWantsAny(".site-nav"), "modifiers are stripped before matching");
Check(Val(target: ".a", failTarget: ".site-nav").UpWantsAny(".site-nav"), "the fail branch counts too");
Check(Val(target: ".content").UpWantsAny(".site-nav, .site-header") == false, "a list of provided selectors, none asked for");
Check(Val(target: ".site-header").UpWantsAny(".site-nav, .site-header"), "a list of provided selectors, one asked for");
Check(!Val(target: ".content").UpWantsAny(""), "providing nothing wants nothing");

var plain = new DefaultHttpContext();
plain.Request.Headers["X-Up-Target"] = ".site-nav";
Check(!plain.UpWantsAny(".site-nav"), "a non-Unpoly request wants nothing");

Console.WriteLine($"OK — {passed} checks passed (Phase A + B + C)");
