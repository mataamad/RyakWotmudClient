using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    public interface ISend<T> {
        public Task SendAsync(T item);
    }

    public interface ISubscribe<T> {
        public void Subscribe(Action<T> callback);
    }

    public class SubscribableBuffer<T> : ISend<T>, ISubscribe<T> {
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

    public static class Store {
        // todo: little worried about the null cloning function, do I need one?
        public static SubscribableBuffer<List<FormattedOutput>> FormattedText = new();

        public static SubscribableBuffer<List<FormattedOutput>> FormattedTextWithoutStatusLine = new();

        public static SubscribableBuffer<string> DevText = new();

        public static SubscribableBuffer<string> TcpReceive = new();

        public static SubscribableBuffer<string> TcpSend = new();

        /// <summary>
        /// Currently used for aliases that do complex things like take arguments or interact with the map
        /// </summary>
        public static SubscribableBuffer<string> ComplexAlias = new();

        /// <summary>
        /// Information to show to the user but not send to the mud - e.g. "Map: Multiple matching rooms found."
        /// </summary>
        public static SubscribableBuffer<string> ClientInfo = new();

        public static SubscribableBuffer<string> StatusLine = new();

        public static SubscribableBuffer<List<ParsedOutput>> ParsedOutput = new();
    }
}
