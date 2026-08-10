namespace Application.Services.AI.Agents;

public class AgentProfile
{
    public string CodeName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ArabicRole { get; set; } = string.Empty;
    public string ArabicGoal { get; set; } = string.Empty;
    public IReadOnlyList<string> Responsibilities { get; set; } = [];
    public IReadOnlyList<string> AllowedTools { get; set; } = [];
    public IReadOnlyList<string> DataScope { get; set; } = [];
    public IReadOnlyList<string> ForbiddenActions { get; set; } = [];
    public IReadOnlyList<string> HandoffRules { get; set; } = [];
}
