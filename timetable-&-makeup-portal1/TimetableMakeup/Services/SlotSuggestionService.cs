namespace TimetableMakeup.Services;

/// <summary>
/// Legacy conflict-check facade delegating to MakeupAvailabilityService until
/// MakeupService is updated to use block checks directly.
/// </summary>
public class SlotSuggestionService
{
    private readonly MakeupAvailabilityService _availabilityService;

    public SlotSuggestionService(MakeupAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    public async Task<(bool HasConflict, string? ConflictReason)> CheckConflictAsync(
        string programId,
        string teacherId,
        string sectionId,
        string roomId,
        DateTime targetDate,
        int slotNumber)
    {
        var (ok, error) = await _availabilityService.CheckBlockAsync(
            programId,
            teacherId,
            sectionId,
            roomId,
            targetDate,
            slotNumber,
            hours: 1);

        return (!ok, error);
    }
}
