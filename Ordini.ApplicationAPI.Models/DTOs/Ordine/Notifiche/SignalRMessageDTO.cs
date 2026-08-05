namespace Ordini.ApplicationAPI.Models.DTOs.Ordine.Notifiche;

public enum eOrdineStatus
{
    NonProcessato = 0,
    NonAccettato = 1,
    Success = 2,
    Fallito = 3
}
public class SignalRMessageDTO
{
    public eOrdineStatus Status { get; set; } = eOrdineStatus.NonProcessato;

    //descrizione
    public string Motivo { get; set; } = string.Empty;
}
