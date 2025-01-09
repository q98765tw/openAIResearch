using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace openAIResearch.DB.Model
{
    public class Document
    {
        [Key]
        public int Id { get; set; } // 主鍵

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } // 文件名稱

        [Required]
        public string Content { get; set; } // 原始文本內容

        [Column(TypeName = "vector(1536)")] // PostgreSQL VECTOR 資料類型
        public float[] Embedding { get; set; } // 嵌入向量
    }

}
