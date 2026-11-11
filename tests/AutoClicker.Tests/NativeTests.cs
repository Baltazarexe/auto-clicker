using AutoClicker.Native;

namespace AutoClicker.Tests;

public class NativeTests
{
    [Fact]
    public void Process_opts_out_of_timer_resolution_throttling() =>
        Assert.True(TimerResolution.KeepHighResolutionWhenHidden());
}
