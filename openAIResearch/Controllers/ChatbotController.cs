using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Assistants;
using OpenAI.Files;
using openAIResearch.Files;
using System.ClientModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using openAIResearch.Services;
using System.Xml.Linq;
using System.ComponentModel;

namespace openAIResearch.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;

        public ChatbotController(IConfiguration configuration,
            ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }
        /// <summary>
        /// 簡易回答
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Ask")]
        public async Task<IActionResult> Ask(string request)
        {
            if (string.IsNullOrWhiteSpace(request))
            {
                return BadRequest("問題不能為空");
            }
            try { 
                var response = _chatbotService.Ask(request);
                // 回傳回應內容
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
        /// <summary>
        /// 建立助手
        /// </summary>
        /// <returns></returns>
        [HttpPost("CreateAssistant")]
        public async Task<IActionResult> CreateAssistant()
        {
            try
            {
                var response = _chatbotService.CreateAssistant();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
        /// <summary>
        /// 回答問題(過去助手包含文件)
        /// </summary>
        /// <returns></returns>
        [HttpPost("AskByFile")]
        public async Task<IActionResult> AskByFile(
            [DefaultValue("How well did product 113045 sell in January? Answer it.")]string request
            )
        {
            try
            {
                var response =await _chatbotService.AskByFile(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
       
        /// <summary>
        /// 丟.txt到後端拆解字段，並且給openAI分析字段，返回並存入documents
        /// </summary>
        /// <returns></returns>
        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                // 檢查文件是否存在且是文本格式
                if (file == null || file.Length == 0 || file.ContentType != "text/plain")
                {
                    return BadRequest("File is missing, empty, or not a valid .txt file.");
                }

                await _chatbotService.UploadFile(file);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
    }
}
