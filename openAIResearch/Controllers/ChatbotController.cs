using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Assistants;
using OpenAI.Files;
using openAIResearch.Files;
using System.ClientModel;

namespace openAIResearch.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly OpenAIClient _client;

        public ChatbotController(IConfiguration configuration)
        {
            // 從設定中讀取 API Key
            string apiKey = configuration["OpenAI:ApiKey"];
            _client = new OpenAIClient("sk-svcacct-7nkcGU_LoUjzKsXRUpD7KkUJIb6HbdIpbm1mBliePW0e8xfUTlYKPvZ9rCSF1IXT3BlbkFJlLFqgaG4zjIuXsjeg7esvY0YARmQtMLf_H-5l4oz-6TieXntoPf6mn1Vbo7C71AA");
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("問題不能為空");
            }

            try { 

                ChatClient client = new(model: "gpt-3.5-turbo", apiKey: "sk-svcacct-7nkcGU_LoUjzKsXRUpD7KkUJIb6HbdIpbm1mBliePW0e8xfUTlYKPvZ9rCSF1IXT3BlbkFJlLFqgaG4zjIuXsjeg7esvY0YARmQtMLf_H-5l4oz-6TieXntoPf6mn1Vbo7C71AA");

                ChatCompletion completion = client.CompleteChat("Answer Today");

                Console.WriteLine($"[ASSISTANT]: {completion.Content[0].Text}");
            

                // 回傳回應內容
                return Ok(new { Answer = completion.Content[0].Text });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
        [HttpPost("uploadFile")]
        public async Task<string> uploadFile()
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
                    Instructions = "You are an assistant that looks up sales data and answers it.",
                    Tools = { new FileSearchToolDefinition(), new CodeInterpreterToolDefinition() },
                    ToolResources = new()
                    {
                        FileSearch = new() { NewVectorStores = { new VectorStoreCreationHelper(new[] { salesFile.Value.Id }) } }
                    }
                };

                Assistant assistant = assistantClient.CreateAssistant("gpt-3.5-turbo", assistantOptions);
                ThreadCreationOptions threadOptions = new()
                {
                    InitialMessages = { "How well did product 113045 sell in February? Answer it." }
                };

                ThreadRun threadRun = assistantClient.CreateThreadAndRun(assistant.Id, threadOptions);

                do
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    threadRun = assistantClient.GetRun(threadRun.ThreadId, threadRun.Id);
                } while (!threadRun.Status.IsTerminal);

                CollectionResult<ThreadMessage> messages = assistantClient.GetMessages(threadRun.ThreadId, new MessageCollectionOptions() { Order = MessageCollectionOrder.Ascending });

                if (messages == null || messages.Count() == 0)
                {
                    return "未收到任何回應，請檢查助手設定或問題。";
                }

                foreach (ThreadMessage message in messages)
                {
                    foreach (MessageContent contentItem in message.Content)
                    {
                        if (message.Role.ToString().Equals("ASSISTANT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(message.Content.ToString()))
                        {
                            Console.WriteLine($"{contentItem.Text}");
                            return contentItem.Text;
                        }
                        
                    }
                }
                return "助手沒回應";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤: {ex.Message}");
                return "發生錯誤，請稍後再試。";
            }
        }

    }

    public class ChatRequest
    {
        public string Question { get; set; }
    }
}
