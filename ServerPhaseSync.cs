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
        public float AdjustedIntegrator => Integrator + protocol.ServerTimeDiff;

        private Protocol<PQ> protocol;
        private ushort lastPacketID;
        private ConcurrentQueue<Action> earlyOneShot;
        private ConcurrentQueue<Action> oneShot;
        private SyncList<Action> onEarlyTick;
        private SyncList<Action> onTick;
        private SyncList<Action> onLateTick;
        private bool running;
        private Thread thread;
        public ushort TickId { get; private set; }
        public ulong TickTime { get; private set; }

        public ServerPhaseSync(Protocol<PQ> protocol)
        {
            // Base values
            TickId = 0;
            NextTick = DateTimeExtensions.now() + 50;
            Integrator = 50;
            this.protocol = protocol;
            
            // No actions yet
            earlyOneShot = new ConcurrentQueue<Action>();
            oneShot = new ConcurrentQueue<Action>();
            onEarlyTick = new SyncList<Action>();
            onTick = new SyncList<Action>();
            onLateTick = new SyncList<Action>();
        }

        public void Start()
        {
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

        public void FixTickId(ushort id)
        {
            TickId = id;
            lastPacketID = id;
        }

        public void EarlyOneShot(Action action)
        {
            earlyOneShot.Enqueue(action);
        }

        public void OneShot(Action action)
        {
            oneShot.Enqueue(action);
        }

        public void RegisterEarlyTickCallback(Action action)
        {
            onEarlyTick.Add(action);
        }


        public void RegisterTickCallback(Action action)
        {
            onTick.Add(action);
        }

        public void RegisterLateTickCallback(Action action)
        {
            onLateTick.Add(action);
        }

        public void ServerPacket(ushort currentID, ushort maxID)
        {
            // Multipackets should not be counted towards PLL
            if (currentID == maxID)
            {
                return;
            }

            // Get current tick time
            ulong time = TickTime + (ulong)Integrator;
        
            // More than one packet in between?
            ushort packetDiff = Overflow.sub(maxID, lastPacketID);
            lastPacketID = maxID;
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
                ++TickId;
                TickTime = DateTimeExtensions.now();

                while (earlyOneShot.TryDequeue(out var action))
                {
                    action();
                }

                foreach (var action in onEarlyTick)
                {
                    action();
                }

                foreach (var action in onTick)
                {
                    action();
                }

                while (oneShot.TryDequeue(out var action))
                {
                    action();
                }

                foreach (var action in onLateTick)
                {
                    action();
                }

                // Wait until next tick, account sever/client diff
                Thread.Sleep((int)Math.Max(1.01f, AdjustedIntegrator));
            }
        }
    }
}
