using System.Text.Json.Serialization;

public class Izdavanje
{
    public int ID { get; set; }
    
    public Knjiga? Knjiga { get; set; } 

    public Clan? Clan { get; set; } 
    public DateTime DatumIzdavanja { get; set; }
    public DateTime? DatumVracanja { get; set; } 
    
    public Rezervacija? Rezervacija { get; set; }
}
