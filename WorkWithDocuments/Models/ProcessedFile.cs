using System.ComponentModel.DataAnnotations;

namespace WorkWithDocuments.Models
{
    public class ProcessedFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        //here i use byte[] for varbinary(max) V.S
        [Required]
        public byte[] Data { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now; // idk, DateTime.UtcNow or DateTime.Now V.S
    }
}