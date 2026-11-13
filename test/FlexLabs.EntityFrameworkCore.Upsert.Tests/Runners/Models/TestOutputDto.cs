namespace FlexLabs.EntityFrameworkCore.Upsert.Tests.Runners.Models;

/// <summary>
/// DTO with a parameterised constructor only — tests NewExpression without Members.
/// </summary>
public class CtorOnlyDto
{
    public CtorOnlyDto(string oldName, int newTotal)
    {
        OldName = oldName;
        NewTotal = newTotal;
    }

    public string OldName { get; }
    public int NewTotal { get; }
}

/// <summary>
/// DTO with a parameterised constructor plus settable properties —
/// tests MemberInitExpression with constructor arguments.
/// </summary>
public class CtorPlusPropsDto
{
    public CtorPlusPropsDto(int id)
    {
        Id = id;
    }

    public int Id { get; }
    public string? Name { get; set; }
    public int Total { get; set; }
}
