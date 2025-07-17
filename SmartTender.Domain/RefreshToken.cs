namespace SmartTender.Domain;

public class RefreshToken
{
    public int Id { get; set; }

    public string Token { get; set; }

    public DateTime Created { get; set; }

    public DateTime Expires { get; set; }

    public bool IsUsed { get; set; }

    public bool IsRevoked { get; set; }

    public string AppUserId { get; set; }

    public AppUser AppUser { get; set; }
}