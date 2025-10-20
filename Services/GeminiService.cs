using Newtonsoft.Json;
using System.Text;

namespace Craftmatrix.org.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiService(IConfiguration configuration)
        {
            _apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ?? 
                     throw new ArgumentNullException("GOOGLE_API_KEY environment variable is required");
            _model = configuration["Google:Model"] ?? "gemini-1.5-flash";
            
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        }

        public async Task<string> GenerateFinancialInsightAsync(FinancialData data, string insightType)
        {
            var prompt = BuildPrompt(data, insightType);
            
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.8,
                        maxOutputTokens = 150
                    }
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"v1beta/models/{_model}:generateContent?key={_apiKey}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return ExtractTextFromResponse(responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Unable to generate insight. API Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Unable to generate insight at this time. Please try again later. ({ex.Message})";
            }
        }

        public async Task<string> AnalyzeSpendingPatternsAsync(List<object> transactions, List<object> categories)
        {
            var data = new FinancialData
            {
                Transactions = transactions,
                Categories = categories,
                AnalysisType = "spending_patterns"
            };

            return await GenerateFinancialInsightAsync(data, "spending_analysis");
        }

        public async Task<string> GenerateBudgetAdviceAsync(object budget, List<object> transactions)
        {
            var data = new FinancialData
            {
                Budget = budget,
                Transactions = transactions,
                AnalysisType = "budget_analysis"
            };

            return await GenerateFinancialInsightAsync(data, "budget_advice");
        }

        public async Task<string> CheckBudgetWarningsAsync(List<object> categories, List<object> transactions)
        {
            var data = new FinancialData
            {
                Categories = categories,
                Transactions = transactions,
                AnalysisType = "budget_warnings"
            };

            return await GenerateFinancialInsightAsync(data, "budget_warning");
        }

        public async Task<string> GenerateSavingsAdviceAsync(List<object> wishlist, List<object> transactions, object accounts)
        {
            var data = new FinancialData
            {
                Wishlist = wishlist,
                Transactions = transactions,
                Accounts = accounts,
                AnalysisType = "savings_advice"
            };

            return await GenerateFinancialInsightAsync(data, "savings_tip");
        }

        public async Task<string> AnalyzeFinancialQuestionAsync(string question, List<object> transactions, List<object> categories, object accounts)
        {
            var data = new FinancialData
            {
                Transactions = transactions,
                Categories = categories,
                Accounts = accounts,
                AnalysisType = "general_question",
                UserQuestion = question
            };

            return await GenerateFinancialInsightAsync(data, "general_analysis");
        }

        private string BuildPrompt(FinancialData data, string insightType)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("You are a personal finance AI assistant for SaveTrack, a manual-first financial awareness system.");
            prompt.AppendLine("Provide helpful, actionable insights without recommending automation.");
            prompt.AppendLine("Focus on manual financial awareness and human decision-making.");
            prompt.AppendLine("Keep responses concise (2-3 sentences) and motivational.");
            prompt.AppendLine("Never suggest risky investments or provide specific financial product recommendations.");
            prompt.AppendLine();

            switch (insightType)
            {
                case "spending_analysis":
                    prompt.AppendLine("TASK: Analyze spending patterns and provide insights.");
                    prompt.AppendLine($"TRANSACTION DATA: {JsonConvert.SerializeObject(data.Transactions)}");
                    prompt.AppendLine($"CATEGORIES: {JsonConvert.SerializeObject(data.Categories)}");
                    prompt.AppendLine("Provide insights on spending trends, top categories, and suggestions for better tracking.");
                    break;

                case "budget_advice":
                    prompt.AppendLine("TASK: Provide budget optimization advice.");
                    prompt.AppendLine($"BUDGET DATA: {JsonConvert.SerializeObject(data.Budget)}");
                    prompt.AppendLine($"RECENT TRANSACTIONS: {JsonConvert.SerializeObject(data.Transactions)}");
                    prompt.AppendLine("Suggest improvements to budget allocations and spending awareness.");
                    break;

                case "budget_warning":
                    prompt.AppendLine("TASK: Check for budget overspending and provide warnings.");
                    prompt.AppendLine($"CATEGORIES WITH LIMITS: {JsonConvert.SerializeObject(data.Categories)}");
                    prompt.AppendLine($"RECENT TRANSACTIONS: {JsonConvert.SerializeObject(data.Transactions)}");
                    prompt.AppendLine("Identify categories approaching or exceeding budget limits. Provide gentle warnings and suggestions.");
                    break;

                case "savings_tip":
                    prompt.AppendLine("TASK: Provide personalized savings advice for goals.");
                    prompt.AppendLine($"SAVINGS GOALS: {JsonConvert.SerializeObject(data.Wishlist)}");
                    prompt.AppendLine($"TRANSACTIONS: {JsonConvert.SerializeObject(data.Transactions)}");
                    prompt.AppendLine($"ACCOUNTS: {JsonConvert.SerializeObject(data.Accounts)}");
                    prompt.AppendLine("Suggest ways to reach savings goals based on spending patterns.");
                    break;

                case "general_analysis":
                    prompt.AppendLine("TASK: Answer the user's financial question using their data.");
                    prompt.AppendLine($"USER QUESTION: {data.UserQuestion}");
                    prompt.AppendLine($"TRANSACTIONS: {JsonConvert.SerializeObject(data.Transactions)}");
                    prompt.AppendLine($"CATEGORIES: {JsonConvert.SerializeObject(data.Categories)}");
                    prompt.AppendLine($"ACCOUNTS: {JsonConvert.SerializeObject(data.Accounts)}");
                    prompt.AppendLine("Analyze their financial data to provide a personalized answer to their question.");
                    break;

                default:
                    prompt.AppendLine("TASK: Provide general financial awareness advice.");
                    prompt.AppendLine("Focus on manual tracking benefits and financial mindfulness.");
                    break;
            }

            return prompt.ToString();
        }

        private string ExtractTextFromResponse(string responseJson)
        {
            try
            {
                dynamic response = JsonConvert.DeserializeObject(responseJson);
                
                if (response?.candidates?.Count > 0)
                {
                    var candidate = response.candidates[0];
                    if (candidate?.content?.parts?.Count > 0)
                    {
                        var part = candidate.content.parts[0];
                        if (part?.text != null)
                        {
                            return part.text.ToString();
                        }
                    }
                }
                
                return "I'm having trouble generating insights right now. Please try again later.";
            }
            catch (Exception ex)
            {
                return $"Unable to process financial insight at this time. Error: {ex.Message}";
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class FinancialData
    {
        public List<object>? Transactions { get; set; }
        public List<object>? Categories { get; set; }
        public object? Budget { get; set; }
        public List<object>? Wishlist { get; set; }
        public object? Accounts { get; set; }
        public string AnalysisType { get; set; } = "";
        public string? UserQuestion { get; set; }
    }
}