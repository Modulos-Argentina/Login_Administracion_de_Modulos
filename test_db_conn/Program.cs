using System;

class Program
{
    static void Main()
    {
        try
        {
            bool isValid = BCrypt.Net.BCrypt.Verify("123456", "$2b$10$HashSimuladoDe123456");
            Console.WriteLine($"Valid: {isValid}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
}
