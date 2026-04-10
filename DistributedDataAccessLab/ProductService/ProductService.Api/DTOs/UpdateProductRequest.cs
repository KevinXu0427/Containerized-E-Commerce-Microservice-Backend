using System.ComponentModel.DataAnnotations;

namespace ProductService.Api.DTOs;

public class UpdateProductRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Price { get; set; }
}
