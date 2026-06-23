namespace api.Interfaces;

public class CreateUserInterface
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class LoginInterface
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class UpdateUserInterface
{
    public string? Name { get; set; }
    public string? ImageUrl { get; set; }
    public string? Bio { get; set; }
}