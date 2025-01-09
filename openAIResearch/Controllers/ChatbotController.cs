using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Assistants;
using OpenAI.Files;
using openAIResearch.Files;
using System.ClientModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace openAIResearch.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly OpenAIClient _client;
        private readonly string _apiKey;

        public ChatbotController(IConfiguration configuration)
        {
            // 從設定中讀取 API Key
            _apiKey = configuration["OpenAI:ApiKey"];
            _client = new OpenAIClient(_apiKey);
        }

        [HttpPost("Ask")]
        public async Task<IActionResult> Ask(string request)
        {
            if (string.IsNullOrWhiteSpace(request))
            {
                return BadRequest("問題不能為空");
            }
            try { 

                ChatClient client = new(model: "gpt-3.5-turbo", apiKey: _apiKey);

                ChatCompletion completion = client.CompleteChat(request);

                Console.WriteLine($"[ASSISTANT]: {completion.Content[0].Text}");
            

                // 回傳回應內容
                return Ok(new { Answer = completion.Content[0].Text });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
        /// <summary>
        /// 內建問題和文件
        /// </summary>
        /// <returns></returns>
        [HttpPost("AskByFile")]
        public async Task<string> AskByFile()
        {
            try
            {
                OpenAIFileClient fileClient = _client.GetOpenAIFileClient();
                AssistantClient assistantClient = _client.GetAssistantClient();
                using Stream document = textJson.GetTextJson();
                var salesFile = fileClient.UploadFile(document, "monthly_sales.json", FileUploadPurpose.Assistants);
                if (salesFile == null )
                {
                    return "檔案上傳失敗，無法建立助手。";
                }

                AssistantCreationOptions assistantOptions = new()
                {
                    Name = "Example: Contoso sales RAG",
                    Instructions = "Please provide concise and brief responses to questions.",
                    Tools = { new FileSearchToolDefinition(), new CodeInterpreterToolDefinition() },
                    ToolResources = new()
                    {
                        FileSearch = new() { NewVectorStores = { new VectorStoreCreationHelper(new[] { salesFile.Value.Id }) } }
                    }
                };

                Assistant assistant = assistantClient.CreateAssistant("gpt-3.5-turbo", assistantOptions);
                return assistant.Name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤: {ex.Message}");
                return "發生錯誤，請稍後再試。";
            }
        }

    }
}
