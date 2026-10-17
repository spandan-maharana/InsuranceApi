using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Dtos;

public class ProductCreateDto
{
    [Required]
    [StringLength(30, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z][A-Za-z0-9_]*$", ErrorMessage = "Code must start with a letter and use only letters, numbers and underscores.")]
    public string? Code { get; set; }
    
    [Required]
    public string? CategoryCode { get; set; }

    [Required]
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string? CoverageLabel { get; set; }

    [Range(0.0001, 100000)]
    public decimal RatePerThousand { get; set; }

    [Range(0, 1000000)]
    public decimal MinimumPremium { get; set; }

    [Range(0, 1000000)]
    public decimal PolicyFee { get; set; }

    [Range(1, 1000000000)]
    public decimal MinCoverage { get; set; }

    [Range(1, 1000000000)]
    public decimal MaxCoverage { get; set; }

    public int SortOrder { get; set; }
}

public class ProductUpdateDto
{
    [Required]
    public string? CategoryCode { get; set; }

    [Required]
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string? CoverageLabel { get; set; }

    [Range(0.0001, 100000)]
    public decimal RatePerThousand { get; set; }

    [Range(0, 1000000)]
    public decimal MinimumPremium { get; set; }

    [Range(0, 1000000)]
    public decimal PolicyFee { get; set; }

    [Range(1, 1000000000)]
    public decimal MinCoverage { get; set; }

    [Range(1, 1000000000)]
    public decimal MaxCoverage { get; set; }

    public int SortOrder { get; set; }
}
public class AdminProductDtos
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string CategoryCode { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string CoverageLabel { get; set; } = "";
    public decimal RatePerThousand { get; set; }
    public decimal MinimumPremium { get; set; }
    public decimal PolicyFee { get; set; }
    public decimal MinCoverage { get; set; }
    public decimal MaxCoverage { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int QuestionCount { get; set; }
}
