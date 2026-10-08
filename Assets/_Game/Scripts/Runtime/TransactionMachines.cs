using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class TransactionCore
    {
        static StationRuntimeState Machine(TransactionState s, TransactionCommand c)
        {
            var m = s.stations.Find(x => x.id == c.target); Require(m != null, "station", "Station không tồn tại.");
            Require(m.kind == "machine" && (string.IsNullOrEmpty(m.requirement) || s.unlocked.Contains(m.requirement)), "requirement", "Máy chưa mở.");
            WriteAccess(Owner(s, m.input), c.actor); WriteAccess(Owner(s, m.output), c.actor); return m;
        }
        int StartMachine(TransactionState s, TransactionCommand c)
        {
            var m = Machine(s, c); var recipe = MachineRecipe(s,m);
            Require(recipe != null && !m.running && !m.progress.broken, "machine", "Máy đang chạy hoặc bị hỏng.");
            Lease(m, c.actor, clock());
            Require(!string.IsNullOrWhiteSpace(c.secondary), "job", "Thiếu job ID.");
            Require(!s.jobIds.Contains(m.id + ":" + c.secondary), "job", "Job ID đã dùng.");
            Require(m.machinePhase == MachinePhase.Ready || m.autonomous && !m.running,"machine","Máy chưa sẵn sàng.");
            Require(Free(s, Owner(s, m.output), recipe.output) >= recipe.yield, "capacity", "Đầu ra đầy.");
            m.escrow="escrow:"+m.id+":"+c.secondary;
            s.owners.Add(new OwnerState{id=m.escrow,actor="simulation",location=m.escrow,kind=OwnerKind.Escrow,capacity=recipe.inputs.Sum(x=>x.count)});
            foreach (var input in recipe.inputs)
            {
                Consume(s, Allocate(s, m.input, input.id, input.count));
                AddStack(s,"job-input:"+m.id+":"+c.secondary+":"+input.id,m.escrow,input.id,input.count);
            }
            m.jobId = c.secondary; m.reservationId = "output:" + m.id + ":" + c.secondary; m.operatorId = c.actor;
            m.batch=RecipeBatchSnapshot.From(recipe,c.actor=="simulation"?m.lastInputActor??"simulation":c.actor);
            s.jobIds.Add(m.id + ":" + c.secondary);
            s.reservations.Add(new ReservationState { id = m.reservationId, holder = c.actor, destination = m.output, item = recipe.output, quantity = recipe.yield, expiresAt = double.MaxValue });
            m.running = true; m.machinePhase = MachinePhase.Operating; m.remaining = recipe.seconds; return 1;
        }
        int AdvanceMachine(TransactionState s, TransactionCommand c)
        {
            var m = Machine(s, c); Require(m.running && !m.progress.broken && m.jobId == c.secondary, "lease", "Sai job hoặc máy đang hỏng.");
            if(m.autonomous)Require(c.actor=="simulation","authority","Chỉ simulation cập nhật máy tự động.");else Lease(m, c.actor, clock());
            Require(double.IsFinite(c.duration) && c.duration > 0, "duration", "Delta không hợp lệ.");
            m.remaining = Math.Max(0, m.remaining - c.duration); return 1;
        }
        static int CompleteMachine(TransactionState s, TransactionCommand c)
        {
            var m = Machine(s, c);
            if (!m.running && m.jobId == c.secondary) return 0;
            Require(m.running && m.jobId == c.secondary && (m.autonomous?c.actor=="simulation":m.operatorId == c.actor) && m.remaining == 0, "machine", "Job chưa sẵn sàng.");
            var recipe=m.batch??RecipeBatchSnapshot.From(Definitions.Recipe(m.definitionId),c.actor);
            var reservation = Reservation(s, m.reservationId); Require(reservation.status == ReservationStatus.Active, "reservation", "Mất chỗ đầu ra.");
            reservation.status = ReservationStatus.Used;
            Require(Free(s, Owner(s, m.output), recipe.output) >= recipe.yield, "capacity", "Đầu ra không còn chỗ.");
            if(!string.IsNullOrEmpty(m.escrow)){s.stacks.RemoveAll(x=>x.owner==m.escrow);s.owners.RemoveAll(x=>x.id==m.escrow);m.escrow=null;}
            AddStack(s, "job-output:" + m.id + ":" + m.jobId, m.output, recipe.output, recipe.yield);
            m.running = false; m.manualRecipe=false;m.machinePhase = MachinePhase.CompletedWaitingPickup; m.batches++; m.workCount++; if(recipe.initiator=="player"){m.playerWorkCount++;m.playerBatches++;} m.remaining = 0; return recipe.yield;
        }
        static StationRuntimeState Station(TransactionState s, string id)
        { var m = s.stations.Find(x => x.id == id); Require(m != null, "station", "Station không tồn tại: " + id); return m; }
        static void CountPlace(TransactionState s, TransactionCommand c)
        {
            TrackHandDelivery(s,c);
            var station = s.stations.Find(x => x.output == c.destination || x.input == c.destination);
            if(station==null)return;
            if(station.kind=="machine" && station.input==c.destination)station.lastInputActor=c.actor;
            if(c.kind==TransactionKind.Place)station.workCount++;
            if(c.actor=="player")station.playerWorkCount++;
        }
        static void Lease(StationRuntimeState m, string actor, double now)
        {
            Require(string.IsNullOrEmpty(m.operatorId) || m.operatorId == actor || now >= m.operatorUntil, "lease", "Trạm đang có người vận hành.");
            m.operatorId = actor; m.operatorUntil = now + .25;
        }
        StationRuntimeState Producer(TransactionState s, TransactionCommand c, bool lease = true)
        {
            var m = Station(s, c.target);
            Require(m.kind == "producer" && (string.IsNullOrEmpty(m.requirement) || s.unlocked.Contains(m.requirement)), "requirement", "Trạm chưa mở.");
            if (lease) { WriteAccess(Owner(s, m.output), c.actor); Lease(m, c.actor, clock()); }
            return m;
        }
    }
}
