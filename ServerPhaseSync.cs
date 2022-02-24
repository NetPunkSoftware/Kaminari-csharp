using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;


namespace Kaminari
{
    public class ServerPhaseSync<PQ> where PQ : IProtocolQueues
    {
        public ulong NextTick { get; private set; }
        public ulong MeanTickTime { get; private set; }
        public float Integrator { get; private set; }
        public float AdjustedIntegrator => Integrator + protocol.ServerTimeDiff - MeanTickTime;

        public Action OnEarlyTick;
        public Action OnTick;
        public Action OnLateTick;

        private Protocol<PQ> protocol;
        private ushort lastPacketID;
        private ConcurrentQueue<Action> earlyOneShot;
        private ConcurrentQueue<Action> oneShot;
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
            MeanTickTime = 0;

            while (running)
            {
                ++TickId;
                TickTime = DateTimeExtensions.now();

                while (earlyOneShot.TryDequeue(out var action))
                {
                    action();
                }

                OnEarlyTick?.Invoke();
                OnTick?.Invoke();

                while (oneShot.TryDequeue(out var action))
                {
                    action();
                }

                OnLateTick?.Invoke();

                // Update tick time
                MeanTickTime = DateTimeExtensions.now() - TickTime;
                // MeanTickTime = (ulong)((float)MeanTickTime * 0.9f + (float)(DateTimeExtensions.now() - TickTime) * 0.1f);

                // Wait until next tick, account sever/client diff
                Thread.Sleep((int)Math.Max(1.01f, AdjustedIntegrator));
            }
        }
    }
}
