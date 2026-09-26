using System;
using Npgsql;

class Program
{
    static void Main()
    {
        string[] regions = { "sa-east-1", "us-east-1", "us-west-1", "us-east-2" };
        string password = "Adrianito@2026";
        string projectRef = "oxmjxyywxksuwrcgewdg";
        
        foreach (var region in regions)
        {
            string host = $"aws-0-{region}.pooler.supabase.com";
            string connString = $"Host={host};Port=6543;Database=postgres;Username=postgres.{projectRef};Password={password};SSL Mode=Require;Trust Server Certificate=true;Timeout=5;";
            
            Console.WriteLine($"Testing {host}...");
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    Console.WriteLine($"SUCCESS: Connected to {host}!");
                    return; // Exit on success
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED: {ex.Message}");
            }
        }
        Console.WriteLine("Could not connect to any tested region.");
    }
}
