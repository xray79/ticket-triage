using Microsoft.Extensions.Configuration;
using Triage.Application.Providers;
using Triage.Eval;
using Triage.Infrastructure;
using Triage.Infrastructure.Providers;

const double CategoryThreshold = 0.80;
const double PriorityThreshold = 0.60;
const double SummaryThreshold = 0.50;

var providerKey = "local";
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--provider" && i + 1 < args.Length)
        providerKey = args[++i];
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var options = new TriageOptions();
configuration.GetSection(TriageOptions.SectionName).Bind(options);

ITriageLlmClient client = providerKey switch
{
    "local" => new OllamaTriageClient(
        CreateHttpClient(options.Ollama.BaseUrl, TimeSpan.FromSeconds(options.Ollama.TimeoutSeconds)),
        options.Ollama.Model),
    "openai" => new OpenAiTriageClient(
        CreateHttpClient(options.OpenAi.BaseUrl, TimeSpan.FromSeconds(options.OpenAi.TimeoutSeconds)),
        options.OpenAi.ApiKey, options.OpenAi.Model),
    "anthropic" => new AnthropicTriageClient(
        CreateHttpClient(options.Anthropic.BaseUrl, TimeSpan.FromSeconds(options.Anthropic.TimeoutSeconds)),
        options.Anthropic.ApiKey, options.Anthropic.Model),
    "gemini" => new GeminiTriageClient(
        CreateHttpClient(options.Gemini.BaseUrl, TimeSpan.FromSeconds(options.Gemini.TimeoutSeconds)),
        options.Gemini.ApiKey, options.Gemini.Model),
    _ => throw new ArgumentException($"Unknown provider '{providerKey}'. Expected local, openai, anthropic, or gemini.")
};

var samplesPath = Path.Combine(AppContext.BaseDirectory, "samples.json");
var samples = EvalSampleLoader.LoadFromFile(samplesPath);

Console.WriteLine($"Running {samples.Count} eval samples against provider '{providerKey}'...");
Console.WriteLine();

var results = await EvalRunner.RunAsync(samples, client, CancellationToken.None);
var summary = EvalScorer.Aggregate(results);

foreach (var r in results)
{
    var status = r.CategoryMatch && r.PriorityMatch ? "OK  " : "MISS";
    Console.WriteLine(
        $"  [{status}] {r.SampleId,-16} category={(r.CategoryMatch ? "match" : "MISMATCH"),-8} " +
        $"priority={(r.PriorityMatch ? "match" : "MISMATCH"),-8} summaryHitRate={r.SummaryKeywordHitRate:P0}");
}

Console.WriteLine();
Console.WriteLine($"Category accuracy:            {summary.CategoryAccuracy:P0}");
Console.WriteLine($"Priority accuracy:             {summary.PriorityAccuracy:P0}");
Console.WriteLine($"Avg summary keyword hit rate:  {summary.AverageSummaryKeywordHitRate:P0}");
Console.WriteLine();

var passed = summary.CategoryAccuracy >= CategoryThreshold
    && summary.PriorityAccuracy >= PriorityThreshold
    && summary.AverageSummaryKeywordHitRate >= SummaryThreshold;

Console.WriteLine(passed
    ? "PASS — thresholds met."
    : $"FAIL — thresholds not met (category >= {CategoryThreshold:P0}, priority >= {PriorityThreshold:P0}, " +
      $"summary >= {SummaryThreshold:P0}).");

return passed ? 0 : 1;

static HttpClient CreateHttpClient(string baseUrl, TimeSpan timeout) =>
    new() { BaseAddress = new Uri(baseUrl), Timeout = timeout };
