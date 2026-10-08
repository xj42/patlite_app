namespace Patlite.Avalonia.Services;

public interface IUiDispatcher
{
    void Post(Action action);
}
