// In your .NET Core MVC project, e.g., Controllers/ApiController.cs

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Postman.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendRequest([FromBody] RequestModel request)
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
            {
                return BadRequest("Invalid URL format.");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear(); // Clear any default headers to ensure only user-defined are sent

            // Add user-defined headers
            foreach (var header in request.Headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                {
                    // HttpClient.DefaultRequestHeaders.Add can throw for restricted headers like Content-Type
                    // So we handle them separately if they are part of content
                    if (header.Key.ToLower() == "content-type" && request.Body != null)
                    {
                        // Content-Type is set on HttpContent
                        continue;
                    }
                    try
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    catch (System.FormatException)
                    {
                        // Handle cases where a header might be invalid or restricted by HttpClient
                        System.Console.WriteLine($"Warning: Could not add header {header.Key}:{header.Value}. It might be restricted or invalid.");
                    }
                }
            }

            HttpResponseMessage response;
            HttpContent content = null;

            if (!string.IsNullOrEmpty(request.Body))
            {
                // Determine content type from headers or default to application/json
                var contentType = "application/json";
                if (request.Headers.TryGetValue("Content-Type", out var headerContentType))
                {
                    contentType = headerContentType;
                }
                content = new StringContent(request.Body, Encoding.UTF8, contentType);
            }

            var httpRequestMessage = new HttpRequestMessage(new HttpMethod(request.Method), uri);
            if (content != null)
            {
                httpRequestMessage.Content = content;
            }

            try
            {
                response = await client.SendAsync(httpRequestMessage);

                var responseBody = await response.Content.ReadAsStringAsync();

                // You can return a custom object containing status, headers, and body
                return Ok(new
                {
                    StatusCode = (int)response.StatusCode,
                    StatusPhrase = response.ReasonPhrase,
                    Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.First()),
                    Body = responseBody,
                    ContentType = response.Content.Headers.ContentType?.ToString()
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"Request failed: {ex.Message}");
            }
        }
    }

    public class RequestModel
    {
        public string Url { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}