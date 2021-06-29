using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Kaminari
{
    public class ServerPhaseSync
    {
        public ulong nextTick;
        public float integrator;

        private ushort lastPacketID;
        private List<Action> onTick;
        private bool running;
        private bool tickCalled;
        private float serverDiff;
        private Thread thread;
        public ulong TickTime {
            get; private set;
        }

        public ServerPhaseSync()
        {
            nextTick = DateTimeExtensions.now() + 50;
            integrator = 50;
            
            onTick = new List<Action>();

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

        public void ServerPacket(ushort packetID, float serverDiff)
        {
            this.serverDiff = serverDiff;
            // Get current tick time
            ulong time = TickTime + (ulong)integrator;
        
            // More than one packet in between?
            ushort packetDiff = Overflow.sub(packetID, lastPacketID);
            lastPacketID = packetID;
            if (packetDiff > 1)
            {
                nextTick += (ulong)(integrator * (packetDiff - 1));
            }

            // Phase detector
            float err = (float)((long)time - (long)nextTick);

            // Loop filter
            // integrator = 0.999f * integrator + err;
            float Ki = 1e-3f;
            integrator = Ki * err + integrator;
        
            // NCO
            nextTick = time + (ulong)integrator;
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
                Thread.Sleep((int)(integrator + serverDiff));
            }
        }
    }
}
