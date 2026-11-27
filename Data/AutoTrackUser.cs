using Microsoft.AspNetCore.Identity;

namespace AutoTrack.Data;

public class AutoTrackUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
