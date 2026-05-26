using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using LmpCommon.Message;
using LmpCommon.Message.Base;
using LmpCommon.Message.Data;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

using Moq;

using Xunit;

namespace LunaCompatCommon.Tests;

public class SegmentedMessageTests
{
    #region Public Methods

    [Fact]
    public void SegmentedMessage_IsHandledCorrectly()
    {
        var logger = new Mock<ILogger>().Object;

        var factory = (FactoryBase)FormatterServices.GetUninitializedObject(typeof(ServerMessageFactory));
        var handler = new TestMessageHandler(logger, factory);
        var listener = new TestMessageListener(logger);

        var largeMessage = new LargeTestMessage
        {
            LargePayload = new byte[Constants.MaxMessageSize * 2 + 100]
        };

        new Random(1234).NextBytes(largeMessage.LargePayload);

        var interceptedSegments = new List<ModMsgData>();

        handler.Send(largeMessage, interceptedSegments.Add);

        Assert.True(interceptedSegments.Count > 1, "Message should be segmented");

        string finalMessageType = null;
        byte[] finalCombinedBytes = null;

        var handledCount = 0;

        foreach (var segment in interceptedSegments)
        {
            if (listener.HandleSegment(segment.Data, out var messageType, out var combinedBytes))
            {
                handledCount++;
                finalMessageType = messageType;
                finalCombinedBytes = combinedBytes;
            }
        }

        Assert.Equal(1, handledCount);
        Assert.Equal(SerializationUtil.CreatePrefixedModMessageId<LargeTestMessage>(), finalMessageType);

        var deserializedFinal = SerializationUtil.Deserialize<LargeTestMessage>(finalCombinedBytes);
        Assert.Equal(largeMessage.LargePayload, deserializedFinal.LargePayload);
    }

    [Fact]
    public void SmallMessage_IsNotSegmented()
    {
        var logger = new Mock<ILogger>().Object;

        var factory = (FactoryBase)FormatterServices.GetUninitializedObject(typeof(ServerMessageFactory));
        var handler = new TestMessageHandler(logger, factory);

        var smallMessage = new LargeTestMessage
        {
            LargePayload = new byte[100]
        };

        var interceptedSegments = new List<ModMsgData>();

        handler.Send(smallMessage, interceptedSegments.Add);

        Assert.Single(interceptedSegments);
    }

    [Fact]
    public void MultipleMessages_RandomOrder_ReconstructedCorrectly()
    {
        var logger = new Mock<ILogger>().Object;
        var factory = (FactoryBase)FormatterServices.GetUninitializedObject(typeof(ServerMessageFactory));
        var handler = new TestMessageHandler(logger, factory);
        var listener = new TestMessageListener(logger);

        var rng = new Random(12345);
        var originalMessages = new List<LargeTestMessage>();

        for (var i = 0; i < 50; i++)
        {
            var payload = new byte[Constants.MaxMessageSize * rng.Next(2, 5) + 50];
            rng.NextBytes(payload);
            originalMessages.Add(new LargeTestMessage
            {
                LargePayload = payload
            });
        }

        // Intercept all segments from all messages
        var interceptedSegments = new List<ModMsgData>();
        foreach (var msg in originalMessages)
            handler.Send(msg, interceptedSegments.Add);

        Assert.True(interceptedSegments.Count > originalMessages.Count, "Each large message should be segmented into multiple parts");
        var shuffled = interceptedSegments.OrderBy(_ => rng.Next()).ToList();

        var reconstructed = new List<byte[]>();

        foreach (var segment in shuffled)
        {
            if (listener.HandleSegment(segment.Data, out _, out var combinedBytes))
                reconstructed.Add(combinedBytes);
        }

        Assert.Equal(originalMessages.Count, reconstructed.Count);

        var deserializedPayloads = new List<byte[]>();

        foreach (var bytes in reconstructed)
        {
            var deserialized = SerializationUtil.Deserialize<LargeTestMessage>(bytes);
            deserializedPayloads.Add(deserialized.LargePayload);
        }

        var matched = new bool[originalMessages.Count];

        for (var i = 0; i < originalMessages.Count; i++)
        {
            var original = originalMessages[i].LargePayload;
            var found = false;

            for (var j = 0; j < deserializedPayloads.Count; j++)
            {
                if (!matched[j] && original.SequenceEqual(deserializedPayloads[j]))
                {
                    matched[j] = true;
                    found = true;
                    break;
                }
            }

            Assert.True(found, $"Payload for message {i} was not reconstructed correctly");
        }
    }

    #endregion

    #region Nested Types

    public class LargeTestMessage : IModMessage
    {
        #region Properties

        public byte[] LargePayload { get; set; }

        #endregion
    }

    private class TestMessageListener : SegmentedMessageListener
    {
        #region Constructors

        public TestMessageListener(ILogger logger)
            : base(logger)
        {
        }

        #endregion

        #region Public Methods

        public bool HandleSegment(byte[] data, out string messageType, out byte[] combinedBytes)
        {
            return TryHandleSegment(data, out messageType, out combinedBytes);
        }

        #endregion
    }

    private class TestMessageHandler : MessageHandler<TestMessageListener>
    {
        #region Constructors

        public TestMessageHandler(ILogger logger, FactoryBase messageFactory)
            : base(logger, messageFactory)
        {
        }

        #endregion

        #region Public Methods

        public void Send<T>(T message, Action<ModMsgData> sendAction)
            where T : class, IModMessage, new()
        {
            SendMessageInternal(message, sendAction);
        }

        #endregion
    }

    #endregion
}
