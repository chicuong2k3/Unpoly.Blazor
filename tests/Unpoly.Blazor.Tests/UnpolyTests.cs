using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Unpoly.Blazor.Tests;

/// <summary>
/// Request builders shared by the test classes.
///
/// DefaultHttpContext leaves Method empty and UpNotModified only answers safe methods, so
/// GET is set explicitly -- omitting it once made ten unrelated checks fail.
/// </summary>
internal static class Req
{
    public static HttpContext Ctx(string? target = null, string? version = "3.10.2")
    {
        var c = new DefaultHttpContext();
        if (version is not null) c.Request.Headers["X-Up-Version"] = version;
        if (target is not null) c.Request.Headers["X-Up-Target"] = target;
        return c;
    }

    public static HttpContext Cond(string? ifNoneMatch = null, string? ifModifiedSince = null)
    {
        // DefaultHttpContext leaves Method empty, and UpNotModified only answers safe methods.
        var c = new DefaultHttpContext();
        c.Request.Method = "GET";
        if (ifNoneMatch is not null) c.Request.Headers.IfNoneMatch = ifNoneMatch;
        if (ifModifiedSince is not null) c.Request.Headers.IfModifiedSince = ifModifiedSince;
        return c;
    }

    public static HttpContext Val(string? validate = null, string? target = null, string? failTarget = null)
    {
        var c = new DefaultHttpContext();
        c.Request.Headers["X-Up-Version"] = "3.10.2";
        if (validate is not null) c.Request.Headers["X-Up-Validate"] = validate;
        if (target is not null) c.Request.Headers["X-Up-Target"] = target;
        if (failTarget is not null) c.Request.Headers["X-Up-Fail-Target"] = failTarget;
        return c;
    }

    public static HttpContext Layer(string? mode = null, string? originMode = null,
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
}

public class TargetingTests
{
    /// <summary>PHASE A</summary>
    [Fact]
    public void FragmentTargeting()
    {
        Assert.True(!Req.Ctx(version: null).IsUnpoly(), "a plain request is not an Unpoly request");
        Assert.True(Req.Ctx().IsUnpoly(), "X-Up-Version marks an Unpoly request");

        Assert.True(Req.Ctx(".content").IsUpFragment(), "an ordinary target means a fragment request");
        Assert.True(!Req.Ctx("body").IsUpFragment(), "target body means a full page");
        Assert.True(!Req.Ctx(":main").IsUpFragment(), ":main means a full page");
        Assert.True(!Req.Ctx().IsUpFragment(), "no target means a full page");
        Assert.True(!Req.Ctx(version: null, target: ".content").IsUpFragment(), "not an Unpoly request means a full page");

        // The trap: X-Up-Target is a LIST, not a single selector.
        // Without this check the header and footer vanish from full page loads, silently.
        Assert.True(!Req.Ctx("body, .flash").IsUpFragment(), "a list containing body is still a full page");
        Assert.True(Req.Ctx(".content, .flash").IsUpFragment(), "a list of plain fragments is a fragment request");
        Assert.True(Req.Ctx(".content , .flash").UpTargets().Length == 2, "a whitespace-padded list splits correctly");
        Assert.True(Req.Ctx(".content , .flash").UpTargets()[1] == ".flash", "split entries are trimmed");

        // Targets carry modifiers: :before :after :maybe :content. They change how the match is
        // applied, not what is matched, so classification must look past them.
        // 📖 https://unpoly.com/targeting-fragments
        Assert.True(Req.Ctx(".tasks:after").IsUpFragment(), ":after on a plain selector is still a fragment");
        Assert.True(!Req.Ctx("body:after").IsUpFragment(), ":after on body appends into body, so full page");
        Assert.True(!Req.Ctx(":main:content").IsUpFragment(), ":content on :main is still the main region");
        Assert.True(Req.Ctx(".flash:maybe").IsUpFragment(), ":maybe marks a target optional, not whole-page");
        Assert.True(!Req.Ctx(".list:after, body").IsUpFragment(), "body anywhere in a modified list wins");

        Assert.True(Req.Ctx(":none").WantsNothing(), ":none means the client wants no content");
        Assert.True(!Req.Ctx(".content").WantsNothing(), "an ordinary target still wants content");

        var retargeted = new DefaultHttpContext();
        retargeted.UpRetarget(".sidebar");
        Assert.True(retargeted.Response.Headers["X-Up-Target"] == ".sidebar", "UpRetarget writes the header");
    }

    /// <summary>UpChrome.Provides</summary>
    [Fact]
    public void ChromeProvides()
    {
        // A client may target something that lives INSIDE the chrome. Without declaring it,
        // the chrome is stripped, the target is absent from the response, and the swap finds
        // nothing — silently, with no error anywhere.

        Assert.True(Req.Val(target: ".content, .site-nav").UpWantsAny(".site-nav"), "a targeted chrome selector is wanted");
        Assert.True(!Req.Val(target: ".content").UpWantsAny(".site-nav"), "an untargeted chrome selector is not");
        Assert.True(Req.Val(target: ".site-nav:after").UpWantsAny(".site-nav"), "modifiers are stripped before matching");
        Assert.True(Req.Val(target: ".a", failTarget: ".site-nav").UpWantsAny(".site-nav"), "the fail branch counts too");
        Assert.True(Req.Val(target: ".content").UpWantsAny(".site-nav, .site-header") == false, "a list of provided selectors, none asked for");
        Assert.True(Req.Val(target: ".site-header").UpWantsAny(".site-nav, .site-header"), "a list of provided selectors, one asked for");
        Assert.True(!Req.Val(target: ".content").UpWantsAny(""), "providing nothing wants nothing");

        var plain = new DefaultHttpContext();
        plain.Request.Headers["X-Up-Target"] = ".site-nav";
        Assert.True(!plain.UpWantsAny(".site-nav"), "a non-Unpoly request wants nothing");
    }

}

public class CachingTests
{
    /// <summary>PHASE B (partial: Vary)</summary>
    [Fact]
    public void VaryHeader()
    {
        var vary = new DefaultHttpContext();
        vary.UpVary("X-Up-Target", "X-Up-Version");
        Assert.True(vary.Response.Headers.Vary == "X-Up-Target, X-Up-Version", "UpVary writes both header names");

        // Must merge, not clobber: compression and content negotiation set Vary too.
        var varyMerge = new DefaultHttpContext();
        varyMerge.Response.Headers.Vary = "Accept-Encoding";
        varyMerge.UpVary("X-Up-Target");
        Assert.True(varyMerge.Response.Headers.Vary == "Accept-Encoding, X-Up-Target", "UpVary merges with an existing Vary");

        var varyDupe = new DefaultHttpContext();
        varyDupe.UpVary("X-Up-Target");
        varyDupe.UpVary("x-up-target", "X-Up-Version");
        Assert.True(varyDupe.Response.Headers.Vary == "X-Up-Target, X-Up-Version", "UpVary dedupes case-insensitively");
    }

    /// <summary>PHASE B: cache control</summary>
    [Fact]
    public void CacheControlHeaders()
    {
        var expire = new DefaultHttpContext();
        expire.UpExpireCache("/shop/*");
        Assert.True(expire.Response.Headers["X-Up-Expire-Cache"] == "/shop/*", "UpExpireCache writes the URL pattern");

        var expireAll = new DefaultHttpContext();
        expireAll.UpExpireCache();
        Assert.True(expireAll.Response.Headers["X-Up-Expire-Cache"] == "*", "UpExpireCache defaults to everything");

        // Unpoly clears the whole cache after any non-GET; "false" is how a POST opts out.
        var keep = new DefaultHttpContext();
        keep.UpKeepCache();
        Assert.True(keep.Response.Headers["X-Up-Expire-Cache"] == "false", "UpKeepCache preserves the cache after a non-GET");

        var evict = new DefaultHttpContext();
        evict.UpEvictCache("/cart");
        Assert.True(evict.Response.Headers["X-Up-Evict-Cache"] == "/cart", "UpEvictCache writes its own header, not Expire");
    }

    /// <summary>PHASE B: conditional requests</summary>
    [Fact]
    public void ConditionalRequests()
    {
        var firstVisit = Req.Cond();
        Assert.True(!firstVisit.UpNotModified("\"v1\""), "no If-None-Match means the client has nothing");
        Assert.True(firstVisit.Response.Headers.ETag == "\"v1\"", "the ETag is published even on a 200");

        var current = Req.Cond(ifNoneMatch: "\"v1\"");
        Assert.True(current.UpNotModified("\"v1\""), "a matching ETag means the client is current");
        Assert.True(current.Response.StatusCode == 304, "a current client gets 304");

        Assert.True(!Req.Cond(ifNoneMatch: "\"v0\"").UpNotModified("\"v1\""), "a stale ETag still renders");

        // A cache may weaken a strong tag in transit, so the W/ prefix must not break the match.
        Assert.True(Req.Cond(ifNoneMatch: "W/\"v1\"").UpNotModified("\"v1\""), "a weakened ETag still matches");
        Assert.True(Req.Cond(ifNoneMatch: "*").UpNotModified("\"v1\""), "If-None-Match: * matches anything");
        Assert.True(Req.Cond(ifNoneMatch: "\"a\", \"v1\"").UpNotModified("\"v1\""), "If-None-Match may be a list");

        var t = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.True(Req.Cond(ifModifiedSince: t.ToString("R")).UpNotModified(lastModified: t), "unchanged since that time means 304");
        Assert.True(!Req.Cond(ifModifiedSince: t.ToString("R")).UpNotModified(lastModified: t.AddSeconds(1)), "newer data still renders");

        // HTTP dates carry whole seconds. Without truncating, sub-second precision would make
        // every comparison miss and the 304 path would silently never fire.
        Assert.True(Req.Cond(ifModifiedSince: t.ToString("R")).UpNotModified(lastModified: t.AddMilliseconds(400)),
              "sub-second precision is truncated before comparing");
    }

}

public class FormTests
{
    /// <summary>PHASE C: forms</summary>
    [Fact]
    public void ValidationAndFailureBranch()
    {
        Assert.True(!Req.Val().IsUpValidating(), "no X-Up-Validate means a real submission");
        Assert.True(Req.Val("email").IsUpValidating(), "X-Up-Validate marks a validation request");
        Assert.True(Req.Val("").IsUpValidating(), "an empty X-Up-Validate is still a validation request");

        // Unpoly batches fields into ONE request, separated by spaces — not commas.
        // 📖 https://unpoly.com/X-Up-Validate
        Assert.True(Req.Val("email").UpValidatingFields() is ["email"], "a single field parses");
        Assert.True(Req.Val("email password").UpValidatingFields() is ["email", "password"], "fields split on spaces");
        Assert.True(Req.Val("email  password").UpValidatingFields().Length == 2, "repeated spaces do not produce empties");

        // :unknown has two causes: the origin was not a field, or the list overflowed
        // up.protocol.config.maxHeaderSize. Both mean "validate the whole form".
        Assert.True(Req.Val(":unknown").IsUpValidatingUnknown(), ":unknown is recognised");
        Assert.True(Req.Val(":unknown").UpValidatingFields().Length == 0, ":unknown names no fields");
        Assert.True(!Req.Val("email").IsUpValidatingUnknown(), "a named field is not :unknown");

        // Failure swaps X-Up-Fail-Target, but chrome is rendered by the layout before the page has
        // picked a status — so both branches must be considered. 📖 https://unpoly.com/failed-responses
        Assert.True(Req.Val(target: ".content").IsUpFragment(), "no fail target behaves as before");
        Assert.True(Req.Val(target: ".content", failTarget: ".form").IsUpFragment(), "two fragment targets stay a fragment");
        Assert.True(!Req.Val(target: ".content", failTarget: "body").IsUpFragment(),
              "a whole-page FAIL target forces chrome, or a 422 body swap arrives with no nav");
        Assert.True(Req.Val(target: ".a", failTarget: ".b, .c").UpFailTargets().Length == 2, "fail targets split on commas");

        // Conditional requests are for safe methods. Answering 304 to a POST would skip the
        // handler, so the submission would silently do nothing.
        var post = Req.Cond(ifNoneMatch: "\"v1\"");
        post.Request.Method = "POST";
        Assert.True(!post.UpNotModified("\"v1\""), "a POST is never answered 304, even with a matching ETag");
        Assert.True(post.Response.StatusCode != 304, "and its status is left alone");
        Assert.True(post.Response.Headers.ETag.Count == 0, "nor is a version published on a POST");

        var head = Req.Cond(ifNoneMatch: "\"v1\"");
        head.Request.Method = "HEAD";
        Assert.True(head.UpNotModified("\"v1\""), "HEAD is safe, so it still gets 304");
    }

}

public class LayerTests
{
    /// <summary>PHASE D: layers</summary>
    [Fact]
    public void LayersAndContext()
    {
        Assert.True(!Req.Layer().IsUpOverlay(), "no mode means not an overlay");
        Assert.True(!Req.Layer("root").IsUpOverlay(), "the root layer is not an overlay");
        Assert.True(Req.Layer("modal").IsUpOverlay(), "modal is an overlay");
        Assert.True(Req.Layer("drawer").IsUpOverlay(), "so is drawer");
        Assert.True(Req.Layer("modal").UpMode() == "modal", "UpMode reads the target layer");

        // Origin differs from mode exactly while an overlay is being OPENED: the link lives on the
        // root layer, the response renders into the modal.
        Assert.True(Req.Layer("modal", originMode: "root").UpOriginMode() == "root",
              "UpOriginMode reads the layer that issued the request");
        Assert.True(Req.Layer(failMode: "root").UpFailMode() == "root", "UpFailMode reads its own header");

        // Context is JSON and travels both ways.
        var ctxIn = Req.Layer(context: "{\"from\":\"product\",\"n\":3}");
        Assert.True(ctxIn.UpContext<Dictionary<string, object>>() is { Count: 2 }, "UpContext parses a JSON object");
        Assert.True(Req.Layer().UpContext<Dictionary<string, object>>() is null, "no header means no context");
        Assert.True(Req.Layer(context: "{}").UpContext<Dictionary<string, object>>() is null,
              "an empty object means no context");

        // The context is client-controlled. Malformed JSON is a bad request, not a 500 in a page
        // that merely wanted to read it.
        Assert.True(Req.Layer(context: "{not json").UpContext<Dictionary<string, object>>() is null,
              "malformed context degrades to null instead of throwing");
        Assert.True(Req.Layer(failContext: "{\"a\":1}").UpFailContext<Dictionary<string, object>>() is { Count: 1 },
              "UpFailContext reads its own header");

        // Accept and dismiss are different headers, and must never both be written.
        var acc = new DefaultHttpContext();
        acc.UpAcceptLayer(new { size = "M" });
        Assert.True(acc.Response.Headers["X-Up-Accept-Layer"] == "{\"size\":\"M\"}", "UpAcceptLayer serialises its value");
        Assert.True(acc.Response.Headers["X-Up-Dismiss-Layer"].Count == 0, "and does not also dismiss");

        var dis = new DefaultHttpContext();
        dis.UpDismissLayer("changed mind");
        Assert.True(dis.Response.Headers["X-Up-Dismiss-Layer"] == "\"changed mind\"", "UpDismissLayer serialises its reason");

        var nul = new DefaultHttpContext();
        nul.UpAcceptLayer();
        Assert.True(nul.Response.Headers["X-Up-Accept-Layer"] == "null", "accepting with no value sends null");

        var open = new DefaultHttpContext();
        open.UpOpenLayer();
        Assert.True(open.Response.Headers["X-Up-Open-Layer"] == "{}", "UpOpenLayer with no options means defaults");

        var openOpts = new DefaultHttpContext();
        openOpts.UpOpenLayer(new { mode = "drawer", size = "medium" });
        Assert.True(openOpts.Response.Headers["X-Up-Open-Layer"] == "{\"mode\":\"drawer\",\"size\":\"medium\"}",
              "UpOpenLayer passes render options through");

        var setCtx = new DefaultHttpContext();
        setCtx.UpSetContext(new { lastSize = "M" });
        Assert.True(setCtx.Response.Headers["X-Up-Context"] == "{\"lastSize\":\"M\"}", "UpSetContext writes the response header");

        // A response that varies by mode or context must say so, or two layers share a cache entry.
        var layerVary = new DefaultHttpContext();
        layerVary.UpVary("X-Up-Target", "X-Up-Version", "X-Up-Mode", "X-Up-Context");
        Assert.True(layerVary.Response.Headers.Vary.ToString().Contains("X-Up-Context"),
              "Vary covers X-Up-Context, or layers with different context share a cache entry");
    }

}

public class HistoryTests
{
    /// <summary>PHASE E: history</summary>
    [Fact]
    public void HistoryHeaders()
    {
        // X-Up-Title is JSON-encoded: the quotes are PART of the header value. Sending it bare is
        // the mistake, so the method encodes rather than trusting the caller.
        // 📖 https://unpoly.com/X-Up-Title
        var title = new DefaultHttpContext();
        title.UpTitle("Playlist browser");
        Assert.True(title.Response.Headers["X-Up-Title"] == "\"Playlist browser\"", "UpTitle keeps the quotes");

        // And it escapes non-ASCII, which matters: an HTTP header carrying raw UTF-8 is not safe.
        var viet = new DefaultHttpContext();
        viet.UpTitle("Đầm");
        var raw = viet.Response.Headers["X-Up-Title"].ToString();
        Assert.True(raw.All(ch => ch < 128), $"UpTitle stays ASCII-safe: {raw}");
        Assert.True(JsonSerializer.Deserialize<string>(raw) == "Đầm", "and still decodes to the original title");

        var loc = new DefaultHttpContext();
        loc.UpLocation("/shop?page=2");
        Assert.True(loc.Response.Headers["X-Up-Location"] == "/shop?page=2", "UpLocation is a plain URL, not JSON");

        var meth = new DefaultHttpContext();
        meth.UpMethod("get");
        Assert.True(meth.Response.Headers["X-Up-Method"] == "GET", "UpMethod is upper-cased");

        // The cookie exists because a full page load carries no Unpoly request to put a header on.
        // Unpoly pops it during boot, so it is single-use.
        var cook = new DefaultHttpContext();
        cook.Request.Method = "POST";
        cook.UpMethodCookie();
        var setCookie = cook.Response.Headers.SetCookie.ToString();
        Assert.True(setCookie.Contains("_up_method=POST"), $"_up_method defaults to the request method: {setCookie}");
        Assert.True(setCookie.Contains("path=/"), "and is set for the whole site");

        var cook2 = new DefaultHttpContext();
        cook2.Request.Method = "GET";
        cook2.UpMethodCookie("PUT");
        Assert.True(cook2.Response.Headers.SetCookie.ToString().Contains("_up_method=PUT"),
              "an explicit method wins over the request method");
    }

}

public class EventTests
{
    /// <summary>PHASE F: server-emitted events</summary>
    [Fact]
    public void ServerEmittedEvents()
    {
        var ev = new DefaultHttpContext();
        ev.UpEmit("cart:changed", new { count = 1 });
        Assert.True(ev.Response.Headers["X-Up-Events"] == "[{\"count\":1,\"type\":\"cart:changed\"}]",
              "UpEmit writes a JSON array with a type");

        // Calling it again must extend the array, not replace it: two things can happen in one
        // response, and the second must not silently erase the first.
        ev.UpEmit("flash:shown");
        var arr = System.Text.Json.Nodes.JsonNode.Parse(ev.Response.Headers["X-Up-Events"].ToString())!.AsArray();
        Assert.True(arr.Count == 2, $"a second UpEmit accumulates: {arr.Count} events");
        Assert.True(arr[1]!["type"]!.GetValue<string>() == "flash:shown", "and keeps them in order");

        var noProps = new DefaultHttpContext();
        noProps.UpEmit("signup:completed");
        Assert.True(noProps.Response.Headers["X-Up-Events"] == "[{\"type\":\"signup:completed\"}]",
              "an event needs nothing but a type");

        // Unpoly states plainly that HTTP headers may only carry US-ASCII. A Vietnamese message in
        // an event payload would otherwise produce an invalid header.
        var viet2 = new DefaultHttpContext();
        viet2.UpEmit("flash:shown", new { text = "Đã thêm vào giỏ" });
        var rawEv = viet2.Response.Headers["X-Up-Events"].ToString();
        Assert.True(rawEv.All(ch => ch < 128), $"UpEmit stays ASCII-safe: {rawEv}");
        Assert.True(System.Text.Json.Nodes.JsonNode.Parse(rawEv)![0]!["text"]!.GetValue<string>() == "Đã thêm vào giỏ",
              "and still decodes to the original text");

        // Events land on the document unless told otherwise.
        var onLayer = new DefaultHttpContext();
        onLayer.UpEmit("cart:changed", new { layer = "current" });
        Assert.True(onLayer.Response.Headers["X-Up-Events"].ToString().Contains("\"layer\":\"current\""),
              "layer: current is passed through so the event lands on the overlay");
    }

}
