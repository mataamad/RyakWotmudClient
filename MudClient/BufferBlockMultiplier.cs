using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient {
    // Repeats a message from one buffer block to multiple
    class BufferBlockMultiplier<T> {
        private readonly BufferBlock<T> _bufferBlock;
        private readonly List<BufferBlock<T>> _multipliedBlocks = new List<BufferBlock<T>>();
        private bool _looped = false;

        public BufferBlockMultiplier(BufferBlock<T> bufferBlock) {
            _bufferBlock = bufferBlock;
        }

        public BufferBlock<T> GetBlock() {
            if (_looped) {
                throw new Exception("Can't get blocks after looping has started");
            }
            var bb = new BufferBlock<T>();
            _multipliedBlocks.Add(bb);
            return bb;
        }

        public void LoopOnNewThread(CancellationToken cancellationToken) {
            if (_looped) {
                throw new Exception("Can't loop twice");
            }

            _looped = true;
            Task.Run(() => Loop(cancellationToken));
        }

        private async Task Loop(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                T data;
                try {
                    data = await _bufferBlock.ReceiveAsync(cancellationToken);
                } catch (OperationCanceledException) {
                    return;
                }
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                foreach (var multipliedBlock in _multipliedBlocks) {
                    await multipliedBlock.SendAsync(data);
                }
            }
        }
    }
}
