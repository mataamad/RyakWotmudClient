using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    internal interface ISend<T> {
        internal Task SendAsync(T item);
    }

    internal interface ISubscribe<T> {
        internal void Subscribe(Action<T> callback);
    }

    internal class SubscribableBuffer<T> : ISend<T>, ISubscribe<T> {
        private BroadcastBlock<T> _broadcastBlock = new(null);
        public async Task SendAsync(T item) {
            await _broadcastBlock.SendAsync(item);
        }

        public void Subscribe(Action<T> callback) {
            var actionBlock = new ActionBlock<T>((T item) => {
                callback(item);
            }, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
            _broadcastBlock.LinkTo(actionBlock);
        }

        public void SubscribeAsync(Func<T, Task> callback) {
            var actionBlock = new ActionBlock<T>(callback, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
            _broadcastBlock.LinkTo(actionBlock);
        }
    }

    internal static class Store {
        internal static SubscribableBuffer<string> DevText = new();

        internal static SubscribableBuffer<string> TcpReceive = new();

        internal static SubscribableBuffer<string> TcpSend = new();

        /// <summary>
        /// Currently used for aliases that do complex things like take arguments or interact with the map
        /// </summary>
        internal static SubscribableBuffer<string> ComplexAlias = new();

        /// <summary>
        /// Information to show to the user but not send to the mud - e.g. "Map: Multiple matching rooms found."
        /// </summary>
        internal static SubscribableBuffer<string> ClientInfo = new();

        internal static SubscribableBuffer<List<ParsedOutput>> ParsedOutput = new();
    }
}
