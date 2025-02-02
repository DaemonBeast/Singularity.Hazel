using System;
using System.Threading.Tasks;

namespace Singularity.Hazel
{
    public enum HazelInternalErrors
    {
        SocketExceptionSend,
        SocketExceptionReceive,
        ReceivedZeroBytes,
        PingsWithoutResponse,
        ReliablePacketWithoutResponse,
        ConnectionDisconnected
    }

    /// <summary>
    ///     Abstract base class for a <see cref="Connection"/> to a remote end point via a network protocol like TCP or UDP.
    /// </summary>
    /// <threadsafety static="true" instance="true"/>
    public abstract class NetworkConnection : Connection
    {
        /// <summary>
        /// An event that gives us a chance to send well-formed disconnect messages to clients when an internal disconnect happens.
        /// </summary>
        public Func<HazelInternalErrors, MessageWriter> OnInternalDisconnect;

        public virtual float AveragePingMs { get; }

        public long GetIP4Address()
        {
            if (IPMode == IPMode.IPv4)
            {
#pragma warning disable 618
                return this.EndPoint.Address.Address;
#pragma warning restore 618
            }
            else
            {
                var bytes = this.EndPoint.Address.GetAddressBytes();
                return BitConverter.ToInt64(bytes, bytes.Length - 8);
            }
        }

        /// <summary>
        ///     Sends a disconnect message to the end point.
        /// </summary>
        protected abstract ValueTask<bool> SendDisconnect(MessageWriter writer);

        /// <summary>
        ///     Called when the socket has been disconnected at the remote host.
        /// </summary>
        protected async ValueTask DisconnectRemote(string reason, MessageReader reader)
        {
            if (await this.SendDisconnect(null))
            {
                try
                {
                    await InvokeDisconnected(reason, reader);
                }
                catch { }
            }

            this.Dispose();
        }

        /// <summary>
        /// Called when socket is disconnected internally
        /// </summary>
        internal async ValueTask DisconnectInternal(HazelInternalErrors error, string reason)
        {
            var handler = this.OnInternalDisconnect;
            if (handler != null)
            {
                MessageWriter messageToRemote = handler(error);
                if (messageToRemote != null)
                {
                    try
                    {
                        await Disconnect(reason, messageToRemote);
                    }
                    finally
                    {
                        messageToRemote.Recycle();
                    }
                }
                else
                {
                    await Disconnect(reason);
                }
            }
            else
            {
                await Disconnect(reason);
            }
        }

        /// <summary>
        ///     Called when the socket has been disconnected locally.
        /// </summary>
        public override async ValueTask Disconnect(string reason, MessageWriter writer = null)
        {
            if (await this.SendDisconnect(writer))
            {
                try
                {
                    await InvokeDisconnected(reason, null);
                }
                catch { }
            }

            this.Dispose();
        }
    }
}
