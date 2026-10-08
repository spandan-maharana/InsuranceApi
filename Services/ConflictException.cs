namespace InsuranceApi.Services;

//A custom error to be thrown, when someone tries to create a customer with an email that already exists in the database.
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message){}
}
