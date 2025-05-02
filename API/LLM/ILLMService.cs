using API.DataModels;

namespace API.LLM;

public interface ILlmService
{
    Task<Thought> GetThoughFromPrompt(string prompt);
}