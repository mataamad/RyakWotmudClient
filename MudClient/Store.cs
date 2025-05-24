using MudClient.Transformers.ParsedOutput;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    internal class SubscribableBuffer<T> {
        private readonly BroadcastBlock<T> _broadcastBlock = new(null);
        internal async Task SendAsync(T item) {
            await _broadcastBlock.SendAsync(item);
        }

        internal void Subscribe(Action<T> callback) {
            var actionBlock = new ActionBlock<T>((T item) => {
                callback(item);
            }, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
            _broadcastBlock.LinkTo(actionBlock);
        }

        internal void SubscribeAsync(Func<T, Task> callback) {
            var actionBlock = new ActionBlock<T>(callback, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
            _broadcastBlock.LinkTo(actionBlock);
        }
    }

    internal static class Store {
        internal readonly static SubscribableBuffer<string> DevText = new();

        internal readonly static SubscribableBuffer<string> TcpReceive = new();

        internal readonly static SubscribableBuffer<string> TcpSend = new();

        /// <summary>
        /// Currently used for aliases that do complex things like take arguments or interact with the map
        /// </summary>
        internal readonly static SubscribableBuffer<string> ComplexAlias = new();

        /// <summary>
        /// Information to show to the user but not send to the mud - e.g. "Map: Multiple matching rooms found."
        /// </summary>
        internal readonly static SubscribableBuffer<string> ClientInfo = new();

        internal readonly static SubscribableBuffer<List<ParsedOutput>> ParsedOutput = new();
    }
}
