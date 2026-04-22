using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

public class CreateProductRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Price must be at least 0.01.")]
    public decimal Price { get; set; }
}
