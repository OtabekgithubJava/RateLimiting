using Microsoft.Extensions.Caching.Memory;

namespace RateLimiting.UI.Middlewares
{
    public class RateLimiter
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        private const int REQUEST_LIMIT = 5;

        public RateLimiter(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var keyRequest = "requests";
            var keyTime = "remaining_time";

            var appends = _cache.Get<int>(keyRequest);
            var time = _cache.Get<DateTime>(keyTime);
            var currentTime = DateTime.UtcNow;

            if (appends == 0 && time == DateTime.MinValue)
            {
                appends = 1;
                time = currentTime;
            }

            else if (time.AddSeconds(30) < currentTime)
            {
                time = currentTime;
                appends = 0;
            }

            else if (time.AddSeconds(30) > currentTime && appends > REQUEST_LIMIT)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("You have ALREADY reached your rate limit!");
                return;
            }

            _cache.Set(keyRequest, ++appends);
            _cache.Set(keyTime, time);

            await _next(context);
        }

    }
}
