using API.Data;
using API.DataModels;
using API.LLM;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThoughtController : ControllerBase
    {
        private readonly ILlmService _illmService;
        private readonly MongoDbService _mongoDbService;

        public ThoughtController(ILlmService illmService, MongoDbService mongoDbService)
        {
            _illmService = illmService;
            _mongoDbService = mongoDbService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateThought([FromBody] string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return BadRequest("Prompt cannot be empty.");
            }

            try
            {
                // Generate a Thought from the prompt using LLMService
                var thought = await _illmService.GetThoughFromPrompt(prompt);

                // Save the generated Thought to the database
                await _mongoDbService.CreateThoughtAsync(thought);

                return Ok(thought);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}