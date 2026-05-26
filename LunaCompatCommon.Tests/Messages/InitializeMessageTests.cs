using LunaCompatCommon.Messages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class InitializeMessageTests : ModMessageTestBase<InitializeMessage>
{
    #region Non-Public Methods

    protected override InitializeMessage CreateMessage()
    {
        return new InitializeMessage
        {
            Version = "1.0.0"
        };
    }

    protected override void AssertEqual(InitializeMessage expected, InitializeMessage actual)
    {
        Assert.Equal(expected.Version, actual.Version);
    }

    #endregion
}
