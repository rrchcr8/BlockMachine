namespace BlockMachine.Models;

public sealed class BlockMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = "Es hora de descansar";

    public string Body { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
}
