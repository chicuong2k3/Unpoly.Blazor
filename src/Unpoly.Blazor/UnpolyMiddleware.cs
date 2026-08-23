using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Unpoly.Blazor;

/// <summary>The one pipeline step Unpoly.Blazor needs. See <see cref="UseUnpoly"/>.</summary>
public static class UnpolyMiddleware
{
    /// <summary>
    /// Declares that responses depend on Unpoly's request headers, and drops the body of a
    /// 304 response.
    ///
    /// Any endpoint may branch on X-Up-Target (see <see cref="UpChrome"/>), so the whole
    /// pipeline is marked rather than the individual components that happen to branch —
    /// a component that forgets would produce a cache poisoning bug that is invisible in
    /// development and only shows up behind a CDN.
    ///
    /// Register it before UseAntiforgery so the header is set for every response.
    /// 📖 https://unpoly.com/optimizing-responses
    /// </summary>
    public static IApplicationBuilder UseUnpoly(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            ctx.Response.OnStarting(() =>
            {
                // X-Up-Mode and X-Up-Context are here because a response may legitimately
                // differ by them -- different content for an overlay, or content that depends
                // on layer context. Without them two layers share one cache entry.
                // 📖 https://unpoly.com/context
                ctx.UpVary("X-Up-Target", "X-Up-Version", "X-Up-Mode", "X-Up-Context");
                return Task.CompletedTask;
            });

            var body = ctx.Response.Body;
            ctx.Response.Body = new BodylessWhenNotModified(ctx.Response, body);
            try
            {
                await next();
            }
            finally
            {
                ctx.Response.Body = body;
            }
        });
}

/// <summary>
/// Discards writes once the status is 304.
///
/// <see cref="UpResponse.UpNotModified"/> sets the status and its documentation says the
/// caller must then render nothing — but in Blazor static SSR a component cannot decline to
/// render after the fact. OnParametersSet runs, the page renders, and unless every single
/// route wraps its entire markup in a guard, Kestrel throws
/// "Writing to the response body is invalid for responses with status code 304". That
/// surfaces as an unhandled exception after the response has started, so the developer
/// exception page cannot even run — it looks nothing like a caching problem.
///
/// A 304 carries no body by definition (RFC 9110 §15.4.5), so dropping the bytes is what the
/// protocol already requires, and doing it once here covers every route instead of trusting
/// each page to remember. Guarding the markup as well is still worth it where the render is
/// expensive — it saves the work, not just the bytes.
/// </summary>
internal sealed class BodylessWhenNotModified(HttpResponse response, Stream inner) : Stream
{
    private bool Drop => response.StatusCode == StatusCodes.Status304NotModified;

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!Drop) inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (!Drop) inner.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => Drop ? Task.CompletedTask : inner.WriteAsync(buffer, offset, count, ct);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        => Drop ? ValueTask.CompletedTask : inner.WriteAsync(buffer, ct);

    public override void Flush()
    {
        if (!Drop) inner.Flush();
    }

    public override Task FlushAsync(CancellationToken ct)
        => Drop ? Task.CompletedTask : inner.FlushAsync(ct);

    public override bool CanWrite => true;
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
