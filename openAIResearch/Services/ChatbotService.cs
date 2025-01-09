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
using System.Text;
using Microsoft.EntityFrameworkCore;
using Slack.NetStandard.WebApi.Apps;

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
    
        public async Task<string> AskByFile(string request) 
        {
            AssistantClient assistantClient = _client.GetAssistantClient();
            //反正他文件已經綁死在當時建立助理時，理論上我可以直接放問題問她
            Assistant assistant = assistantClient.GetAssistant("asst_GjTbob68qBfQDtadi7nL59L4");
            ThreadCreationOptions threadOptions = new()
            {
                InitialMessages = { request }
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

        public async Task UploadFile(IFormFile file)
        {
            // 1. 驗證檔案大小
            if (file.Length > 10 * 1024 * 1024) // 10 MB
            {
                throw new InvalidOperationException("File size exceeds the allowed limit.");
            }

            // 2. 讀取檔案內容
            string content;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync();
            }

            EmbeddingClient client = new("text-embedding-3-small", _apiKey);

            // 3. 切分內容為段落
            var paragraphs = SplitContentToParagraphs(content);

            var documents = new List<Document>();

            foreach (var paragraph in paragraphs)
            {
                try
                {
                    // 非同步生成嵌入
                    OpenAIEmbedding embedding = await client.GenerateEmbeddingAsync(paragraph);
                    var vector = embedding.ToFloats().ToArray();

                    // 構建 Document 實體
                    documents.Add(new Document
                    {
                        Name = file.Name,
                        Content = paragraph,
                        Embedding = vector
                    });
                }
                catch (Exception ex)
                {
                    // 記錄錯誤
                    Console.WriteLine($"Error processing paragraph: {ex.Message}");
                }
            }

            // 4. 批量插入資料庫
            if (documents.Any())
            {
                _context.documents.AddRange(documents);
                await _context.SaveChangesAsync();
            }
        }


        //拆段落
        private IEnumerable<string> SplitContentToParagraphs(string content)
        {
            const int maxLength = 1000; // 每段最多 1000 字
            var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Length > maxLength)
                {
                    // 長段落進一步切分
                    for (int i = 0; i < paragraph.Length; i += maxLength)
                    {
                        yield return paragraph.Substring(i, Math.Min(maxLength, paragraph.Length - i));
                    }
                }
                else
                {
                    yield return paragraph.Trim();
                }
            }
        }


        public async Task AddDocument()
        {
            var random = new Random();
            var vector = new StringBuilder("[");
            for (int i = 0; i < 1536; i++)
            {
                vector.Append(random.NextDouble().ToString("F4")); // 保留 4 位小數
                if (i < 1535)
                    vector.Append(", ");
            }
            vector.Append("]");

            // 生成 SQL 語句
            string sql = $@"
            INSERT INTO documents (name, content, embedding)
            VALUES ('Test Document', 'This is a test', '{vector}'::vector);
            ";
        }

        public async Task<string> GetEmbedding()
        {
            EmbeddingClient client = new("text-embedding-3-small", _apiKey);
            //EmbeddingGenerationOptions options = new() { Dimensions = 512 };

            OpenAIEmbedding embedding = client.GenerateEmbedding("test text.");
            ReadOnlyMemory<float> vector = embedding.ToFloats();

            return string.Join(",", vector.ToArray()); // 轉換向量為字串返回
        }
    }
}
