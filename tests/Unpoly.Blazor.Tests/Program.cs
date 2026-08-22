using System.Text.Json;
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

// ── PHASE D: layers ─────────────────────────────────────────────────────

static HttpContext Layer(string? mode = null, string? originMode = null,
                         string? context = null, string? failMode = null,
                         string? failContext = null)
{
    var c = new DefaultHttpContext();
    c.Request.Method = "GET";
    c.Request.Headers["X-Up-Version"] = "3.14.3";
    if (mode is not null) c.Request.Headers["X-Up-Mode"] = mode;
    if (originMode is not null) c.Request.Headers["X-Up-Origin-Mode"] = originMode;
    if (failMode is not null) c.Request.Headers["X-Up-Fail-Mode"] = failMode;
    if (context is not null) c.Request.Headers["X-Up-Context"] = context;
    if (failContext is not null) c.Request.Headers["X-Up-Fail-Context"] = failContext;
    return c;
}

Check(!Layer().IsUpOverlay(), "no mode means not an overlay");
Check(!Layer("root").IsUpOverlay(), "the root layer is not an overlay");
Check(Layer("modal").IsUpOverlay(), "modal is an overlay");
Check(Layer("drawer").IsUpOverlay(), "so is drawer");
Check(Layer("modal").UpMode() == "modal", "UpMode reads the target layer");

// Origin differs from mode exactly while an overlay is being OPENED: the link lives on the
// root layer, the response renders into the modal.
Check(Layer("modal", originMode: "root").UpOriginMode() == "root",
      "UpOriginMode reads the layer that issued the request");
Check(Layer(failMode: "root").UpFailMode() == "root", "UpFailMode reads its own header");

// Context is JSON and travels both ways.
var ctxIn = Layer(context: "{\"from\":\"product\",\"n\":3}");
Check(ctxIn.UpContext<Dictionary<string, object>>() is { Count: 2 }, "UpContext parses a JSON object");
Check(Layer().UpContext<Dictionary<string, object>>() is null, "no header means no context");
Check(Layer(context: "{}").UpContext<Dictionary<string, object>>() is null,
      "an empty object means no context");

// The context is client-controlled. Malformed JSON is a bad request, not a 500 in a page
// that merely wanted to read it.
Check(Layer(context: "{not json").UpContext<Dictionary<string, object>>() is null,
      "malformed context degrades to null instead of throwing");
Check(Layer(failContext: "{\"a\":1}").UpFailContext<Dictionary<string, object>>() is { Count: 1 },
      "UpFailContext reads its own header");

// Accept and dismiss are different headers, and must never both be written.
var acc = new DefaultHttpContext();
acc.UpAcceptLayer(new { size = "M" });
Check(acc.Response.Headers["X-Up-Accept-Layer"] == "{\"size\":\"M\"}", "UpAcceptLayer serialises its value");
Check(acc.Response.Headers["X-Up-Dismiss-Layer"].Count == 0, "and does not also dismiss");

var dis = new DefaultHttpContext();
dis.UpDismissLayer("changed mind");
Check(dis.Response.Headers["X-Up-Dismiss-Layer"] == "\"changed mind\"", "UpDismissLayer serialises its reason");

var nul = new DefaultHttpContext();
nul.UpAcceptLayer();
Check(nul.Response.Headers["X-Up-Accept-Layer"] == "null", "accepting with no value sends null");

var open = new DefaultHttpContext();
open.UpOpenLayer();
Check(open.Response.Headers["X-Up-Open-Layer"] == "{}", "UpOpenLayer with no options means defaults");

var openOpts = new DefaultHttpContext();
openOpts.UpOpenLayer(new { mode = "drawer", size = "medium" });
Check(openOpts.Response.Headers["X-Up-Open-Layer"] == "{\"mode\":\"drawer\",\"size\":\"medium\"}",
      "UpOpenLayer passes render options through");

var setCtx = new DefaultHttpContext();
setCtx.UpSetContext(new { lastSize = "M" });
Check(setCtx.Response.Headers["X-Up-Context"] == "{\"lastSize\":\"M\"}", "UpSetContext writes the response header");

// A response that varies by mode or context must say so, or two layers share a cache entry.
var layerVary = new DefaultHttpContext();
layerVary.UpVary("X-Up-Target", "X-Up-Version", "X-Up-Mode", "X-Up-Context");
Check(layerVary.Response.Headers.Vary.ToString().Contains("X-Up-Context"),
      "Vary covers X-Up-Context, or layers with different context share a cache entry");

// ── PHASE E: history ────────────────────────────────────────────────────

// X-Up-Title is JSON-encoded: the quotes are PART of the header value. Sending it bare is
// the mistake, so the method encodes rather than trusting the caller.
// 📖 https://unpoly.com/X-Up-Title
var title = new DefaultHttpContext();
title.UpTitle("Playlist browser");
Check(title.Response.Headers["X-Up-Title"] == "\"Playlist browser\"", "UpTitle keeps the quotes");

// And it escapes non-ASCII, which matters: an HTTP header carrying raw UTF-8 is not safe.
var viet = new DefaultHttpContext();
viet.UpTitle("Đầm");
var raw = viet.Response.Headers["X-Up-Title"].ToString();
Check(raw.All(ch => ch < 128), $"UpTitle stays ASCII-safe: {raw}");
Check(JsonSerializer.Deserialize<string>(raw) == "Đầm", "and still decodes to the original title");

var loc = new DefaultHttpContext();
loc.UpLocation("/shop?page=2");
Check(loc.Response.Headers["X-Up-Location"] == "/shop?page=2", "UpLocation is a plain URL, not JSON");

var meth = new DefaultHttpContext();
meth.UpMethod("get");
Check(meth.Response.Headers["X-Up-Method"] == "GET", "UpMethod is upper-cased");

// The cookie exists because a full page load carries no Unpoly request to put a header on.
// Unpoly pops it during boot, so it is single-use.
var cook = new DefaultHttpContext();
cook.Request.Method = "POST";
cook.UpMethodCookie();
var setCookie = cook.Response.Headers.SetCookie.ToString();
Check(setCookie.Contains("_up_method=POST"), $"_up_method defaults to the request method: {setCookie}");
Check(setCookie.Contains("path=/"), "and is set for the whole site");

var cook2 = new DefaultHttpContext();
cook2.Request.Method = "GET";
cook2.UpMethodCookie("PUT");
Check(cook2.Response.Headers.SetCookie.ToString().Contains("_up_method=PUT"),
      "an explicit method wins over the request method");

// ── PHASE F: server-emitted events ──────────────────────────────────────

var ev = new DefaultHttpContext();
ev.UpEmit("cart:changed", new { count = 1 });
Check(ev.Response.Headers["X-Up-Events"] == "[{\"count\":1,\"type\":\"cart:changed\"}]",
      "UpEmit writes a JSON array with a type");

// Calling it again must extend the array, not replace it: two things can happen in one
// response, and the second must not silently erase the first.
ev.UpEmit("flash:shown");
var arr = System.Text.Json.Nodes.JsonNode.Parse(ev.Response.Headers["X-Up-Events"].ToString())!.AsArray();
Check(arr.Count == 2, $"a second UpEmit accumulates: {arr.Count} events");
Check(arr[1]!["type"]!.GetValue<string>() == "flash:shown", "and keeps them in order");

var noProps = new DefaultHttpContext();
noProps.UpEmit("signup:completed");
Check(noProps.Response.Headers["X-Up-Events"] == "[{\"type\":\"signup:completed\"}]",
      "an event needs nothing but a type");

// Unpoly states plainly that HTTP headers may only carry US-ASCII. A Vietnamese message in
// an event payload would otherwise produce an invalid header.
var viet2 = new DefaultHttpContext();
viet2.UpEmit("flash:shown", new { text = "Đã thêm vào giỏ" });
var rawEv = viet2.Response.Headers["X-Up-Events"].ToString();
Check(rawEv.All(ch => ch < 128), $"UpEmit stays ASCII-safe: {rawEv}");
Check(System.Text.Json.Nodes.JsonNode.Parse(rawEv)![0]!["text"]!.GetValue<string>() == "Đã thêm vào giỏ",
      "and still decodes to the original text");

// Events land on the document unless told otherwise.
var onLayer = new DefaultHttpContext();
onLayer.UpEmit("cart:changed", new { layer = "current" });
Check(onLayer.Response.Headers["X-Up-Events"].ToString().Contains("\"layer\":\"current\""),
      "layer: current is passed through so the event lands on the overlay");

Console.WriteLine($"OK — {passed} checks passed (Phase A + B + C + D + E + F)");
