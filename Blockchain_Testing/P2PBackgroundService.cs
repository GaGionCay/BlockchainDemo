using BlockchainCore;

namespace Blockchain_Testing
{
    public class P2PBackgroundService : IHostedService
    {
        private readonly P2PNode _p2pNode;

        public P2PBackgroundService(P2PNode p2pNode)
        {
            _p2pNode = p2pNode;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _p2pNode.Start(8888);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
