using System.Text.Json.Serialization;

public class Rezervacija
{
    public int ID { get; set; }
    public DateTime DatumIzdavanja {get; set; }
    public DateTime DatumVracanja {get; set; }
    [JsonIgnore]
    public DateTime DatumRezervacije { get; set; }
    [JsonIgnore]
    public Knjiga? Knjiga {get; set; }
    public Clan? Clan {get; set; }
}
