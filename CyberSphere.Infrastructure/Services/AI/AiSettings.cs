using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Infrastructure.Services.AI
{
    /// <summary>
    /// Bound from appsettings.json → "AI" section.
    /// BaseUrl points at the external Python AI microservice.
    /// </summary>
    public sealed class AiSettings
    {
        public const string SectionName = "AI";

        /// <summary>Base URL of the Python AI microservice. Example: http://python-ai-service</summary>
        public string BaseUrl { get; init; } = "http://python-ai-service";

        /// <summary>HTTP timeout for AI requests in seconds. Default: 60.</summary>
        public int TimeoutSeconds { get; init; } = 60;

        /// <summary>Named HttpClient key used in IHttpClientFactory registration.</summary>
        public const string HttpClientName = "PythonAiService";
    }
}
