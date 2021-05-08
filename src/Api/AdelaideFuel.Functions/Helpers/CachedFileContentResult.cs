using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions
{
    public class CachedFileContentResult : FileContentResult
    {
        private readonly TimeSpan _maxAge;

        public CachedFileContentResult(byte[] fileContents, string contentType, TimeSpan maxAge) : base(fileContents, contentType)
        {
            _maxAge = maxAge;
        }

        public CachedFileContentResult(byte[] fileContents, MediaTypeHeaderValue contentType, TimeSpan maxAge) : base(fileContents, contentType)
        {
            _maxAge = maxAge;
        }

        public override void ExecuteResult(ActionContext context)
        {
            AddCacheControl(context.HttpContext.Response);
            base.ExecuteResult(context);
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            AddCacheControl(context.HttpContext.Response);
            return base.ExecuteResultAsync(context);
        }

        private void AddCacheControl(HttpResponse response)
        {
            response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = _maxAge
            };
        }
    }
}