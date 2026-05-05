using CommunityToolkit.Mvvm.ComponentModel;

namespace AINovel.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}
