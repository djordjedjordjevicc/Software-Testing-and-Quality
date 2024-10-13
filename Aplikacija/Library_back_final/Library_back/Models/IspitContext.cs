namespace WebTemplate.Models;

public class IspitContext : DbContext
{
    public DbSet<Knjiga> Knjige { get; set; }
    public DbSet<Clan> Clanovi { get; set; }
    public DbSet<Izdavanje> Izdavanja { get; set; }
    public DbSet<Rezervacija> Rezervacije { get; set; }

    public IspitContext(DbContextOptions options) : base(options)
    {
        
    }
}