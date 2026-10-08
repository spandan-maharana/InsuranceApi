using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Dtos;

//Data sent by client
public class CustomerInputDto
{
    //[Required] - Specifies that the property is required and cannot be null or empty.
    //[StringLength(50)] - Specifies the maximum length of the string property (50 characters in this case).
    //[EmailAddress] - Specifies that the property should be a valid email address format.
    //[Phone] - Specifies that the property should be a valid phone number format.
        [Required, StringLength(50)]
        public string FirstName { get; set; } = "";
        [Required, StringLength(50)]
        public string LastName { get; set; } = "";
        [Required] 
        [EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = "";
}

//Data sent to client
public class CustomerDto
{
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public DateTime CreatedAt { get; set; }

}
