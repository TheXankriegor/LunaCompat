using LunaCompatCommon.Messages;
using LunaCompatCommon.Serializer;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public abstract class ModMessageTestBase<T>
    where T : class, IModMessage, new()
{
    #region Public Methods

    [Fact]
    public void Serialize_Deserialize_ModMessage()
    {
        var message = CreateMessage();

        var data = SerializationUtil.Serialize(message);
        var deserialized = SerializationUtil.Deserialize<T>(data);

        AssertEqual(message, deserialized);
    }

    #endregion

    #region Non-Public Methods

    protected abstract T CreateMessage();

    protected abstract void AssertEqual(T expected, T actual);

    #endregion
}
