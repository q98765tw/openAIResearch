using Microsoft.AspNetCore.Mvc;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Assistants;
using OpenAI.Files;
using openAIResearch.Files;
using System.ClientModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using openAIResearch.Services;

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
        /// 內建問題和文件
        /// </summary>
        /// <returns></returns>
        [HttpPost("AskByFile")]
        public async Task<IActionResult> AskByFile()
        {
            try
            {
                var response = _chatbotService.AskByFile();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
        /// <summary>
        /// 測試Add Users
        /// </summary>
        /// <returns></returns>
        [HttpPost("Add")]
        public async Task<IActionResult> AddUser(string name)
        {
            try
            {
                await _chatbotService.AddUser(name);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
        /// <summary>
        /// 拿全部 Users 資料
        /// </summary>
        /// <returns></returns>
        [HttpGet("Get")]
        public async Task<IActionResult> GetUser()
        {
            try
            {
                var response = await _chatbotService.GetUser();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"伺服器錯誤: {ex.Message}");
            }
        }
        /// <summary>
        /// 拿全部 Users 資料
        /// </summary>
        /// <returns></returns>
        [HttpGet("UploadFile")]
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
