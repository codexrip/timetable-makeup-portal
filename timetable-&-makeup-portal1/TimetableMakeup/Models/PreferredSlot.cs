namespace TimetableMakeup.Models;

/// <summary>
/// A teacher-selected preferred date and start slot for a makeup class.
/// </summary>
public class PreferredSlot
{
    public DateTime Date { get; set; }
    public int StartSlot { get; set; }
}
