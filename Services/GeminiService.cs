using Google.Cloud.AIPlatform.V1;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System.Text;

namespace Craftmatrix.org.Services
{
    public class GeminiService
    {
        private readonly PredictionServiceClient _client;
        private readonly string _projectId;
        private readonly string _location;
        private readonly string _model;

        public GeminiService(IConfiguration configuration)
        {
            _projectId = configuration["Google:ProjectId"] ?? "craftmatrix-ai";
            _location = configuration["Google:Location"] ?? "us-central1";
            _model = configuration["Google:Model"] ?? "gemini-1.5-flash";
            
            // Initialize the Prediction Service Client
            _client = PredictionServiceClient.Create();
        }

        public async Task<string> GenerateFinancialInsightAsync(FinancialData data, string insightType)
        {
            var prompt = BuildPrompt(data, insightType);
            var request = CreatePredictionRequest(prompt);
            
            try
            {
                var response = await _client.PredictAsync(request);
                return ExtractTextFromResponse(response);
            }
            catch (Exception ex)
            {
                // Log the error and return a fallback message
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

                default:
                    prompt.AppendLine("TASK: Provide general financial awareness advice.");
                    prompt.AppendLine("Focus on manual tracking benefits and financial mindfulness.");
                    break;
            }

            return prompt.ToString();
        }

        private PredictRequest CreatePredictionRequest(string prompt)
        {
            var endpoint = EndpointName.FromProjectLocationEndpoint(_projectId, _location, _model);
            
            var instance = new Google.Protobuf.WellKnownTypes.Value
            {
                StructValue = new Struct
                {
                    Fields =
                    {
                        ["prompt"] = Google.Protobuf.WellKnownTypes.Value.ForString(prompt),
                        ["max_output_tokens"] = Google.Protobuf.WellKnownTypes.Value.ForNumber(150),
                        ["temperature"] = Google.Protobuf.WellKnownTypes.Value.ForNumber(0.7),
                        ["top_p"] = Google.Protobuf.WellKnownTypes.Value.ForNumber(0.8),
                        ["top_k"] = Google.Protobuf.WellKnownTypes.Value.ForNumber(40)
                    }
                }
            };

            return new PredictRequest
            {
                Endpoint = endpoint.ToString(),
                Instances = { instance }
            };
        }

        private string ExtractTextFromResponse(PredictResponse response)
        {
            try
            {
                if (response.Predictions.Count > 0)
                {
                    var prediction = response.Predictions[0];
                    if (prediction.StructValue.Fields.ContainsKey("content"))
                    {
                        return prediction.StructValue.Fields["content"].StringValue;
                    }
                    
                    // Fallback: try to extract text from any string field
                    foreach (var field in prediction.StructValue.Fields.Values)
                    {
                        if (!string.IsNullOrEmpty(field.StringValue))
                        {
                            return field.StringValue;
                        }
                    }
                }
                
                return "I'm having trouble generating insights right now. Please try again later.";
            }
            catch
            {
                return "Unable to process financial insight at this time.";
            }
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
    }
}