﻿using Singularity.Hazel.Api.Net.Messages;

namespace Singularity.Hazel
{
    public struct DataReceivedEventArgs
    {
        public readonly Connection Sender;

        /// <summary>
        ///     The bytes received from the client.
        /// </summary>
        public readonly IMessageReader Message;

        /// <summary>
        ///     The <see cref="SendOption"/> the data was sent with.
        /// </summary>
        public readonly MessageType Type;
        
        public DataReceivedEventArgs(Connection sender, IMessageReader msg, MessageType type)
        {
            this.Sender = sender;
            this.Message = msg;
            this.Type = type;
        }
    }
}
