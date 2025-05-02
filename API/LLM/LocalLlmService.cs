using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.DataModels;

namespace API.LLM
{
    public class LocalLlmService : ILlmService
    {
        private readonly HttpClient _httpClient;

        public LocalLlmService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Thought> GetThoughFromPrompt(string prompt)
        {
            var requestBody = new
            {
                model = "qwen2.5-0.5b-instruct-mlx",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content =
                            "You analyze user messages and create insightful thoughts with meaningful observations and relevant follow-up reminders."
                    },
                    new
                    {
                        role = "user",
                        content =
                            $"Original message: \"{prompt}\"\n\nCreate a JSON response with the following structure. Be detailed and specific with your observations based on the user's input:\n\n{{\"Content\": {{\"Text\": \"Your message: '{prompt}'. Analysis: [provide brief analysis]\", \"VoiceMemoUrl\": null, \"ImageUrl\": null, \"Links\": [], \"ListItems\": [\"[specific observation about what the user mentioned]\", \"[question or insight related to their message]\", \"[practical insight based on their words]\"]}}, \"Reminders\": [{{\"ReminderId\": \"1\", \"Trigger\": {{\"Type\": \"time\", \"Value\": \"tomorrow\", \"IsRecurring\": false, \"Frequency\": null}}, \"NotificationTime\": \"2023-07-20T10:00:00Z\"}}], \"Interactions\": [{{\"InteractionId\": \"1\", \"Type\": \"initial_analysis\", \"Timestamp\": \"2023-07-19T14:30:00Z\", \"Details\": \"[brief description of this analysis]\"}}], \"Status\": \"active\"}}\n\nEnsure all JSON is properly formatted."
                    }
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("http://127.0.0.1:1234/v1/chat/completions", requestContent);

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response: {responseString}");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseString);
            var content = responseJson
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // Extract and clean the JSON string
            var jsonContent = SanitizeJsonString(content);

            // Create a new thought with default values first
            var thought = new Thought
            {
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Content = new Content
                {
                    Text = $"Original message: \"{prompt}\"", // Default to original message if parsing fails
                    Links = new List<string>(),
                    ListItems = new List<string>()
                },
                Reminders = new List<Reminder>(),
                Interactions = new List<Interaction>(),
                Status = "active"
            };

            try
            {
                // Parse JSON with recovery logic for common issues
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // Extract Content if present
                if (root.TryGetProperty("Content", out var contentElement))
                {
                    if (contentElement.TryGetProperty("Text", out var textElement) &&
                        textElement.ValueKind != JsonValueKind.Null)
                    {
                        thought.Content.Text = textElement.GetString();
                    }

                    // Extract ListItems safely
                    if (contentElement.TryGetProperty("ListItems", out var listItems) &&
                        listItems.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in listItems.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(item.GetString()))
                            {
                                thought.Content.ListItems.Add(item.GetString());
                            }
                        }
                    }
                }

                // Extract Status if present
                if (root.TryGetProperty("Status", out var statusElement) &&
                    statusElement.ValueKind != JsonValueKind.Null)
                {
                    thought.Status = statusElement.GetString();
                }

                // Extract Reminders if present
                if (root.TryGetProperty("Reminders", out var remindersElement) &&
                    remindersElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var reminderElement in remindersElement.EnumerateArray())
                    {
                        var reminder = new Reminder
                        {
                            ReminderId = Guid.NewGuid().ToString(),
                            NotificationTime = DateTime.Now.AddDays(1)
                        };

                        if (reminderElement.TryGetProperty("Trigger", out var triggerElement) &&
                            triggerElement.ValueKind == JsonValueKind.Object)
                        {
                            var trigger = new Trigger();

                            if (triggerElement.TryGetProperty("Type", out var typeElement) &&
                                typeElement.ValueKind == JsonValueKind.String)
                            {
                                trigger.Type = typeElement.GetString();
                            }

                            if (triggerElement.TryGetProperty("Value", out var valueElement) &&
                                valueElement.ValueKind == JsonValueKind.String)
                            {
                                trigger.Value = valueElement.GetString();
                            }

                            if (triggerElement.TryGetProperty("IsRecurring", out var recurringElement) &&
                                recurringElement.ValueKind == JsonValueKind.False ||
                                recurringElement.ValueKind == JsonValueKind.True)
                            {
                                trigger.IsRecurring = recurringElement.GetBoolean();
                            }

                            if (triggerElement.TryGetProperty("Frequency", out var frequencyElement) &&
                                frequencyElement.ValueKind == JsonValueKind.String)
                            {
                                trigger.Frequency = frequencyElement.GetString();
                            }

                            reminder.Trigger = trigger;
                        }

                        if (reminderElement.TryGetProperty("NotificationTime", out var notificationTimeElement) &&
                            notificationTimeElement.ValueKind == JsonValueKind.String)
                        {
                            if (DateTime.TryParse(notificationTimeElement.GetString(), out var notificationTime))
                            {
                                reminder.NotificationTime = notificationTime;
                            }
                        }

                        thought.Reminders.Add(reminder);
                    }
                }

                // Extract Interactions if present
                if (root.TryGetProperty("Interactions", out var interactionsElement) &&
                    interactionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var interactionElement in interactionsElement.EnumerateArray())
                    {
                        var interaction = new Interaction
                        {
                            InteractionId = Guid.NewGuid().ToString(),
                            Timestamp = DateTime.Now
                        };

                        if (interactionElement.TryGetProperty("Type", out var typeElement) &&
                            typeElement.ValueKind == JsonValueKind.String)
                        {
                            interaction.Type = typeElement.GetString();
                        }

                        if (interactionElement.TryGetProperty("Details", out var detailsElement) &&
                            detailsElement.ValueKind == JsonValueKind.String)
                        {
                            interaction.Details = detailsElement.GetString();
                        }

                        if (interactionElement.TryGetProperty("Timestamp", out var timestampElement) &&
                            timestampElement.ValueKind == JsonValueKind.String)
                        {
                            if (DateTime.TryParse(timestampElement.GetString(), out var timestamp))
                            {
                                interaction.Timestamp = timestamp;
                            }
                        }

                        thought.Interactions.Add(interaction);
                    }
                }

                return thought;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error processing JSON: {ex.Message}. Attempting recovery...");

                // Attempt to recover with manual parsing if JSON is malformed
                try
                {
                    thought.Content.Text = ExtractValueBetweenQuotes(jsonContent, "Text");

                    // Extract list items manually
                    var listItems = ExtractListItems(jsonContent);
                    foreach (var item in listItems)
                    {
                        thought.Content.ListItems.Add(item);
                    }

                    // Add a basic reminder and interaction for recovery cases
                    thought.Reminders.Add(new Reminder
                    {
                        ReminderId = Guid.NewGuid().ToString(),
                        NotificationTime = DateTime.Now.AddDays(1),
                        Trigger = new Trigger
                        {
                            Type = "time",
                            Value = "follow-up",
                            IsRecurring = false
                        }
                    });

                    thought.Interactions.Add(new Interaction
                    {
                        InteractionId = Guid.NewGuid().ToString(),
                        Type = "initial_analysis_recovery",
                        Timestamp = DateTime.Now,
                        Details = "Created via error recovery path"
                    });

                    return thought;
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Recovery failed: {fallbackEx.Message}");
                    // Return the thought with default values and original prompt plus a basic interaction
                    thought.Interactions.Add(new Interaction
                    {
                        InteractionId = Guid.NewGuid().ToString(),
                        Type = "failed_analysis",
                        Timestamp = DateTime.Now,
                        Details = "LLM processing failed"
                    });

                    return thought;
                }
            }
        }

        private string SanitizeJsonString(string content)
        {
            // Remove markdown code blocks
            content = content.Replace("```json", "").Replace("```", "");

            // Find the first { and last }
            int start = content.IndexOf('{');
            int end = content.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                content = content.Substring(start, end - start + 1);
            }

            // Fix common JSON formatting errors
            content = content.Replace("\".", "\""); // Fix erroneous period after quote
            content = content.Replace("],\"", "],"); // Fix missing spaces
            content = content.Replace("}\",", "\"},"); // Fix quote positions

            // Fix missing commas between array items
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                "\"\\s*\"",
                "\",\"");

            return content;
        }

        private string ExtractValueBetweenQuotes(string json, string propertyName)
        {
            var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]*)\"";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private List<string> ExtractListItems(string json)
        {
            var result = new List<string>();
            var listItemsStart = json.IndexOf("\"ListItems\"");

            if (listItemsStart > 0)
            {
                var arrayStart = json.IndexOf('[', listItemsStart);
                var arrayEnd = json.IndexOf(']', arrayStart);

                if (arrayStart > 0 && arrayEnd > arrayStart)
                {
                    var arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                    var itemMatches = System.Text.RegularExpressions.Regex.Matches(arrayContent, "\"([^\"]*)\"");

                    foreach (System.Text.RegularExpressions.Match match in itemMatches)
                    {
                        result.Add(match.Groups[1].Value);
                    }
                }
            }

            return result;
        }
    }
}