using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MudClient.Extensions {
    public static class DataFlowBlockExtensions {

        public static async Task<TOutput> ReceiveAsyncIgnoreCanceled<TOutput>(this ISourceBlock<TOutput> source, CancellationToken cancellationToken) {
            try {
                return await source.ReceiveAsync(cancellationToken);
            } catch (OperationCanceledException) {
                return default(TOutput);
            }
        }
    }
}
