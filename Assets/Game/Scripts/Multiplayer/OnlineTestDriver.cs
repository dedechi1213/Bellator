#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Collections;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;

namespace Bellator.Multiplayer
{
    // Uses the transport's current simulator layer, not the obsolete DebugSimulator property.
    public sealed class OnlineTestDriver : INetworkStreamDriverConstructor
    {
        public void CreateDriver(UnityTransport transport, out NetworkDriver driver, out NetworkPipeline unreliableFragmentedPipeline,
            out NetworkPipeline unreliableSequencedFragmentedPipeline, out NetworkPipeline reliableSequencedPipeline)
        {
            var settings=transport.GetDefaultNetworkSettings();
            settings.WithNetworkSimulatorParameters(sendPacketLossPercent:3,sendDelayMS:75,sendJitterMS:20);
            driver=NetworkDriver.Create(new UDPNetworkInterface(),settings); settings.Dispose();
            transport.GetDefaultPipelineConfigurations(out NativeArray<NetworkPipelineStageId> a,out NativeArray<NetworkPipelineStageId> b,out NativeArray<NetworkPipelineStageId> c);
            unreliableFragmentedPipeline=driver.CreatePipeline(a);
            unreliableSequencedFragmentedPipeline=driver.CreatePipeline(b);
            reliableSequencedPipeline=driver.CreatePipeline(c);
            a.Dispose(); b.Dispose(); c.Dispose();
        }
    }
}
#endif
