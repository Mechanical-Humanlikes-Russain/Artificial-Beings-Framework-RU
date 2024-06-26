﻿using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using Verse;
using System;

namespace ArtificialBeings
{
    public class JobDriver_FulfillArtificialNeed : JobDriver
    {
        private bool itemFromInventory;

        public const TargetIndex ConsumableIndex = TargetIndex.A;

        public bool ItemFromInventory => itemFromInventory;

        private Thing ConsumableSource => job.GetTarget(ConsumableIndex).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref itemFromInventory, "itemFromInventory", defaultValue: false);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            itemFromInventory = pawn.inventory != null && pawn.inventory.Contains(ConsumableSource);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Faction != null && ConsumableSource.Spawned)
            {
                if (!pawn.Reserve(ConsumableSource, job, 10, Math.Min(job.count, ConsumableSource.Map.reservationManager.CanReserveStack(pawn, ConsumableSource, 10)), null, errorOnFailed))
                {
                    return false;
                }
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => ConsumableSource.Destroyed);
            Toil consume = Toils_FulfillArtificialNeed.ConsumeItem(pawn, ConsumableIndex).FailOn((Toil x) => !ConsumableSource.Spawned && (pawn.carryTracker == null || pawn.carryTracker.CarriedThing != ConsumableSource)).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            foreach (Toil item in PrepareToConsumeToils(consume))
            {
                yield return item;
            }
            yield return consume;
            yield return Toils_FulfillArtificialNeed.FinalizeConsumption(pawn, ConsumableIndex);
        }

        private IEnumerable<Toil> PrepareToConsumeToils(Toil consumeToil)
        {
            if (pawn.RaceProps.ToolUser)
            {
                return PrepareToConsumeToils_ToolUser(consumeToil);
            }
            return PrepareToConsumeToils_NonToolUser();
        }

        private IEnumerable<Toil> PrepareToConsumeToils_ToolUser(Toil consumeToil)
        {
            if (itemFromInventory)
            {
                yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, ConsumableIndex);
            }
            else
            {
                yield return ReserveConsumable();
                Toil gotoToPickup = Toils_Goto.GotoThing(ConsumableIndex, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(ConsumableIndex);
                yield return Toils_Jump.JumpIf(gotoToPickup, () => pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
                yield return Toils_Goto.GotoThing(ConsumableIndex, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(ConsumableIndex);
                yield return Toils_Jump.Jump(consumeToil);
                yield return gotoToPickup;
                yield return Toils_FulfillArtificialNeed.PickupConsumable(ConsumableIndex, pawn);
            }
        }

        private IEnumerable<Toil> PrepareToConsumeToils_NonToolUser()
        {
            yield return ReserveConsumable();
            yield return Toils_Goto.GotoThing(ConsumableIndex, PathEndMode.Touch);
        }

        private Toil ReserveConsumable()
        {
            Toil toil = ToilMaker.MakeToil("ReserveConsumable");
            toil.initAction = delegate
            {
                if (pawn.Faction != null)
                {
                    Thing thing = job.GetTarget(ConsumableIndex).Thing;
                    if (pawn.carryTracker.CarriedThing != thing)
                    {
                        int maxAmountToPickup = 1;
                        if (thing.Spawned && thing.Map != null)
                        {
                            maxAmountToPickup = Math.Min(job.count, thing.Map.reservationManager.CanReserveStack(pawn, thing, 10));
                        }
                        if (maxAmountToPickup != 0)
                        {
                            if (!pawn.Reserve(thing, job, 10, maxAmountToPickup))
                            {
                                Log.Error(string.Concat("[ABF] Artifical need consumable reservation for ", pawn, " on job ", this, " failed, because it could not register thing from ", thing, " - amount: ", maxAmountToPickup));
                                pawn.jobs.EndCurrentJob(JobCondition.Errored);
                            }
                            job.count = maxAmountToPickup;
                        }
                    }
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.atomicWithPrevious = true;
            return toil;
        }
    }
}