namespace Application.Services.AI.Agents;

public interface IAgentProfileProvider
{
    IReadOnlyList<AgentProfile> GetAll();
    AgentProfile? GetByCodeName(string codeName);
}
