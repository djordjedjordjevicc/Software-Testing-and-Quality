public class Clan
{
    public int ID { get; set; }
    public string? Username { get; set; }
    public byte[]? Password { get; set; }
    public byte[]? Salt { get; set; } 
    public string? Email { get; set; }
    public string? Ime { get; set; }
    public string? Prezime { get; set; }
    public string? BrojClanskeKarte { get; set; }
    public bool IsAdmin { get; set; }
}
