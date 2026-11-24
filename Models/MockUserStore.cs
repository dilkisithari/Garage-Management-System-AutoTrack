namespace AutoTrack.Models;

/// <summary>A fake user. Replace with ASP.NET Core Identity in the backend phase.</summary>
public class MockUser
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";   // Owner | FrontDesk | Mechanic | Customer
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
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
            var user = Users.FirstOrDefault(u =>
                string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase)
                && u.Password == password
                && u.IsActive);
            return user == null ? null : Clone(user);
        }
    }

    public static List<MockUser> All()
    {
        lock (Sync) { return Users.Select(Clone).OrderBy(u => u.FullName).ToList(); }
    }

    public static MockUser? Get(string username)
    {
        lock (Sync)
        {
            var user = Users.FirstOrDefault(u =>
                string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
            return user == null ? null : Clone(user);
        }
    }

    public static bool Exists(string username)
    {
        lock (Sync)
        {
            return Users.Any(u => string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public static bool Add(MockUser user)
    {
        lock (Sync)
        {
            if (Users.Any(u => string.Equals(u.Username, user.Username.Trim(), StringComparison.OrdinalIgnoreCase)))
                return false;

            user.Username = user.Username.Trim();
            Users.Add(Clone(user));
            return true;
        }
    }

    public static bool UpdateProfile(string username, string fullName, string? phone)
    {
        lock (Sync)
        {
            var user = Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return false;
            user.FullName = fullName.Trim();
            user.Phone = phone?.Trim();
            return true;
        }
    }

    public static bool UpdateRole(string username, string role)
    {
        lock (Sync)
        {
            var user = Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return false;

            if (user.IsActive && user.Role == "Owner" && role != "Owner" &&
                Users.Count(u => u.IsActive && u.Role == "Owner") <= 1)
                return false;

            user.Role = role;
            return true;
        }
    }

    public static bool SetActive(string username, bool isActive)
    {
        lock (Sync)
        {
            var user = Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return false;

            if (!isActive && user.IsActive && user.Role == "Owner" &&
                Users.Count(u => u.IsActive && u.Role == "Owner") <= 1)
                return false;

            user.IsActive = isActive;
            return true;
        }
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

    private static MockUser Clone(MockUser user) => new()
    {
        Username = user.Username,
        Password = user.Password,
        FullName = user.FullName,
        Role = user.Role,
        Phone = user.Phone,
        IsActive = user.IsActive
    };
}
