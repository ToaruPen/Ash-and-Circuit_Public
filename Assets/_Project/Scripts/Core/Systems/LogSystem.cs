using System;
using System.Collections.Generic;

namespace AshNCircuit.Core.Systems
{
    /// <summary>
    /// メッセージログの管理と出力を行うシステムクラス。
    /// Core 層では Unity に依存せず、購読者にメッセージを通知する。
    /// UnityIntegration 層で Debug.Log や UI への反映を行う想定。
    /// </summary>
    public class LogSystem
    {
        private readonly List<string> _messages = new List<string>();
        private readonly List<MessageId> _loggedMessageIds = new List<MessageId>();
        private readonly List<LoggedMessageById> _loggedMessagesById = new List<LoggedMessageById>();

        public event Action<string>? OnMessageLogged;
        public event Action<MessageId>? OnMessageLoggedById;

        public IReadOnlyList<string> Messages => _messages;
        public IReadOnlyList<MessageId> LoggedMessageIds => _loggedMessageIds;
        public IReadOnlyList<LoggedMessageById> LoggedMessagesById => _loggedMessagesById;

        public readonly struct LoggedMessageById
        {
            private readonly object[]? _args;

            public MessageId Id { get; }
            public IReadOnlyList<object> Args => _args ?? Array.Empty<object>();

            public LoggedMessageById(MessageId id, object[]? args)
            {
                Id = id;
                _args = args == null ? null : (object[])args.Clone();
            }
        }

        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _messages.Add(message);
            OnMessageLogged?.Invoke(message);
        }

        /// <summary>
        /// メッセージIDと引数からテンプレートを引き、ログとして出力する。
        /// </summary>
        public void LogById(MessageId id, params object[] args)
        {
            var message = MessageCatalog.Format(id, args);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _loggedMessageIds.Add(id);
            _loggedMessagesById.Add(new LoggedMessageById(id, args));
            OnMessageLoggedById?.Invoke(id);
            Log(message);
        }

        /// <summary>
        /// ターン開始時のログを出力する。
        /// </summary>
        public void LogTurnStart(int turnNumber)
        {
            LogById(MessageId.TurnStart, turnNumber);
        }

        /// <summary>
        /// ターン終了時のログを出力する。
        /// </summary>
        public void LogTurnEnd(int turnNumber)
        {
            LogById(MessageId.TurnEnd, turnNumber);
        }

        /// <summary>
        /// RULE P-01: 矢が炎をくぐり燃え上がったときのログ。
        /// </summary>
        public void LogArrowIgnited()
        {
            LogById(MessageId.RuleP01ArrowIgnited);
        }

        /// <summary>
        /// RULE E-01: 木が炎上したときのログ。
        /// </summary>
        public void LogTreeIgnited()
        {
            LogById(MessageId.RuleE01TreeIgnited);
        }

        /// <summary>
        /// RULE E-01: 木が燃え尽きたときのログ。
        /// </summary>
        public void LogTreeBurnedOut()
        {
            LogById(MessageId.RuleE01TreeBurnedOut);
        }
    }
}
