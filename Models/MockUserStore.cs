namespace AutoTrack.Models;

/// <summary>A fake user. Replace with ASP.NET Core Identity in the backend phase.</summary>
public class MockUser
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";   // Owner | FrontDesk | Mechanic | Customer
    public string? Phone { get; set; }
}

public static class MockUserStore
{
    private static readonly object Sync = new();

    private static readonly List<MockUser> Users = new()
    {
        new() { Username = "owner",    Password = "owner123", FullName = "Dilshan Wijesinghe", Role = "Owner" },
        new() { Username = "desk",     Password = "desk123",  FullName = "Nimali Perera",      Role = "FrontDesk" },
        new() { Username = "mechanic", Password = "mech123",  FullName = "Nuwan Perera",       Role = "Mechanic" },
        new() { Username = "customer", Password = "cust123",  FullName = "Kasun Silva",        Role = "Customer" },
    };

    public static MockUser? Validate(string username, string password)
    {
        lock (Sync)
        {
            return Users.FirstOrDefault(u =>
                string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase)
                && u.Password == password);
        }
    }

    public static bool Exists(string username)
    {
        lock (Sync)
        {
            return Users.Any(u => string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public static void Add(MockUser user)
    {
        lock (Sync) { Users.Add(user); }
    }

    public static bool ChangePassword(string username, string oldPassword, string newPassword)
    {
        lock (Sync)
        {
            var user = Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)
                && u.Password == oldPassword);
            if (user == null) return false;
            user.Password = newPassword;
            return true;
        }
    }
}
