namespace RangerAi.Infrastructure.Models;

public class ActionRequest
{
    public string handler { get; set; }
    public string action { get; set; }

    public int scale { get; set; }
    public string who { get; set; }
    public string reasoning { get; set; }
    public string original { get; set; }
}
