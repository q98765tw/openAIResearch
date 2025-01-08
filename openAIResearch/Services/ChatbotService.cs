using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Assistants;
using OpenAI.Files;
using OpenAI.Embeddings;
using openAIResearch.Files;
using System.ClientModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using openAIResearch.Services;
using openAIResearch.DB.Model;

namespace openAIResearch.Services
{
    public class ChatbotService
    {
        private readonly AppDbContext _context;
        private readonly string _apiKey;
        private readonly OpenAIClient _client;
        public ChatbotService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            // 從設定中讀取 API Key
            _apiKey = configuration["OpenAI:ApiKey"];
            _client = new OpenAIClient(_apiKey);
        }

        public async Task<string> Ask(string request) 
        {
            ChatClient client = new(model: "gpt-3.5-turbo", apiKey: _apiKey);

            ChatCompletion completion = client.CompleteChat(request);

            Console.WriteLine($"[ASSISTANT]: {completion.Content[0].Text}");

            // 回傳回應內容
            return  completion.Content[0].Text;
        }
        public async Task<string> AskByFile() 
        {
            OpenAIFileClient fileClient = _client.GetOpenAIFileClient();
            AssistantClient assistantClient = _client.GetAssistantClient();
            using Stream document = textJson.GetTextJson();
            var salesFile = fileClient.UploadFile(document, "monthly_sales.json", FileUploadPurpose.Assistants);
            if (salesFile == null)
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

        public async Task AddUser(string name)
        {
            var data = new user()
            {
                name = name,
            };
            await _context.AddAsync(data);
            await _context.SaveChangesAsync();
        }

        public async Task<List<user>> GetUser() 
        { 
            var users = _context.users.ToList();
            return users;
        }

        public async Task UploadFile(IFormFile file)
        {
            // 1. 讀取 .txt 文件內容
            string content;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync();
            }
            EmbeddingClient client = new("text-embedding-3-small", _apiKey);

            // 2. 將內容切分為段落
            var paragraphs = SplitContentToParagraphs(content);
            // 3. 將每個段落發送到 OpenAI，生成向量並存入資料庫
            foreach (var paragraph in paragraphs)
            {
                OpenAIEmbedding embedding = client.GenerateEmbedding(paragraph);
                var vector = embedding.ToFloats().ToArray();

                var document = new Document
                {
                    Content = paragraph,
                    Embedding = vector
                };

                _context.documents.Add(document);
            }

            await _context.SaveChangesAsync();
        }

        // 切分文本為段落的輔助方法
        private IEnumerable<string> SplitContentToParagraphs(string content)
        {
            return content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
