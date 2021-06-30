using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;


namespace Kaminari
{
    public class ServerPhaseSync<PQ> where PQ : IProtocolQueues
    {
        public ulong NextTick { get; private set; }
        public float Integrator { get; private set; }

        private Protocol<PQ> protocol;
        private ushort lastPacketID;
        private List<Action> onTick;
        private bool running;
        private bool tickCalled;
        private Thread thread;
        public ulong TickTime {
            get; private set;
        }

        public ServerPhaseSync(Protocol<PQ> protocol)
        {
            // Base values
            NextTick = DateTimeExtensions.now() + 50;
            Integrator = 50;
            this.protocol = protocol;
            
            // No actions yet
            onTick = new List<Action>();

            // Start
            running = true;
            thread = new Thread(update);
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            thread.Join();
        }

        public void RegisterTickCallback(Action action)
        {
            onTick.Add(action);
        }

        public void ServerPacket(ushort packetID)
        {
            // Get current tick time
            ulong time = TickTime + (ulong)Integrator;
        
            // More than one packet in between?
            ushort packetDiff = Overflow.sub(packetID, lastPacketID);
            lastPacketID = packetID;
            if (packetDiff > 1)
            {
                NextTick += (ulong)(Integrator * (packetDiff - 1));
            }

            // Phase detector
            float err = (float)((long)time - (long)NextTick);

            // Loop filter
            // Integrator = 0.999f * Integrator + err;
            float Ki = 1e-3f;
            Integrator = Ki * err + Integrator;
        
            // NCO
            NextTick = time + (ulong)Integrator;
        }

        private void update()
        {
            while (running)
            {
                TickTime = DateTimeExtensions.now();
                foreach (var action in onTick)
                {
                    action();
                }

                // Wait until next tick, account sever/client diff
                Thread.Sleep((int)(Integrator + (float)protocol.getServerTimeDiff()));
            }
        }
    }
}
