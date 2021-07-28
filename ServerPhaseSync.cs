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
        public float AdjustedIntegrator => Integrator + (float)protocol.getServerTimeDiff() + protocol.getEstimatedRTT() / 2.0f;

        private Protocol<PQ> protocol;
        private ushort lastPacketID;
        private ConcurrentQueue<Action> earlyOneShot;
        private ConcurrentQueue<Action> oneShot;
        private List<Action> onEarlyTick;
        private List<Action> onTick;
        private List<Action> onLateTick;
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
            earlyOneShot = new ConcurrentQueue<Action>();
            oneShot = new ConcurrentQueue<Action>();
            onEarlyTick = new List<Action>();
            onTick = new List<Action>();
            onLateTick = new List<Action>();
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
                Thread.Sleep((int)AdjustedIntegrator);
            }
        }
    }
}
