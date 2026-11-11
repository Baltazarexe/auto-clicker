namespace AutoClicker.Core;

/// <summary>Sends <paramref name="count"/> down+up pairs at once.</summary>
public interface IClickSender
{
    void Click(ClickButton button, int count);
}
