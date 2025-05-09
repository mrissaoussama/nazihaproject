using System.ComponentModel.DataAnnotations;

namespace nazihaproject.Models
{
    public class Note
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Description { get; set; }
    }
}
