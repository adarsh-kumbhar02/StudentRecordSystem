using System.ComponentModel.DataAnnotations;

namespace StudentRecordSystem.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required]
        [StringLength(30)]
        public string Course { get; set; }

        public string? UserId { get; set; }
    }
}