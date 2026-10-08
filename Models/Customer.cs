namespace InsuranceApi.Models;
    public class Customer
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  //This is set by the server when the customer is created, and it represents the date and time when the customer record was created in UTC format.
}
