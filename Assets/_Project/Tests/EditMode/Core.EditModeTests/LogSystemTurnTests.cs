using NUnit.Framework;
using AshNCircuit.Core.Systems;

namespace AshNCircuit.Tests.Core
{
    /// <summary>
    /// ターン開始・終了メッセージの基本挙動を検証するテスト。
    /// </summary>
    public class LogSystemTurnTests
    {
        [Test]
        public void LogTurnStart_EnqueuesFormattedMessage()
        {
            var log = new LogSystem();

            log.LogTurnStart(5);

            Assert.That(log.Messages, Has.Count.EqualTo(1));
            Assert.That(log.LoggedMessagesById, Has.Count.EqualTo(1));
            Assert.That(log.LoggedMessagesById[0].Id, Is.EqualTo(MessageId.TurnStart));
            Assert.That(log.LoggedMessagesById[0].Args.Count, Is.EqualTo(1));
            Assert.That(log.LoggedMessagesById[0].Args[0], Is.EqualTo(5));
        }

        [Test]
        public void LogTurnEnd_EnqueuesFormattedMessage()
        {
            var log = new LogSystem();

            log.LogTurnEnd(3);

            Assert.That(log.Messages, Has.Count.EqualTo(1));
            Assert.That(log.LoggedMessagesById, Has.Count.EqualTo(1));
            Assert.That(log.LoggedMessagesById[0].Id, Is.EqualTo(MessageId.TurnEnd));
            Assert.That(log.LoggedMessagesById[0].Args.Count, Is.EqualTo(1));
            Assert.That(log.LoggedMessagesById[0].Args[0], Is.EqualTo(3));
        }
    }
}
