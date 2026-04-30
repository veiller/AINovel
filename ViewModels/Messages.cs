using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AINovel.ViewModels;

public class GenerationCompletedMessage : ValueChangedMessage<(int AccountId, int CoreId, string Content)>
{
    public GenerationCompletedMessage((int, int, string) value) : base(value) { }
}

public class GenerationFailedMessage : ValueChangedMessage<(int AccountId, int CoreId, string Reason)>
{
    public GenerationFailedMessage((int, int, string) value) : base(value) { }
}

public class ProgressChangedMessage : ValueChangedMessage<(int AccountId, int CoreId, int Progress)>
{
    public ProgressChangedMessage((int, int, int) value) : base(value) { }
}

public class StatusUpdateMessage : ValueChangedMessage<string>
{
    public StatusUpdateMessage(string status) : base(status) { }
}