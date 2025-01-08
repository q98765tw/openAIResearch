using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace openAIResearch.DB.Model
{
    public class Document
    {
        [Key]
        public int Id { get; set; } // 主鍵

        public string Content { get; set; } // 原始文本內容

        [Column(TypeName = "vector")] // 對應 PostgreSQL 的 VECTOR 資料類型
        public float[] Embedding { get; set; } // 嵌入向量（EF Core 無原生向量支援，這裡用 float[] 表示）
    }
}
