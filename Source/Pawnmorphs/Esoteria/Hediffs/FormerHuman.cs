﻿// FormerHuman.cs created by Iron Wolf for Pawnmorph on 11/27/2019 1:12 PM
// last updated 11/27/2019  1:12 PM

using RimWorld;
using Verse;

namespace Pawnmorph.Hediffs
{
    /// <summary>
    ///     hediff class for the former human hediff
    /// </summary>
    /// <seealso cref="Verse.HediffWithComps" />
    public class FormerHuman : HediffWithComps
    {
        private int? _lastStage;

        /// <summary>Exposes the data.</summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref _lastStage, nameof(_lastStage));
        }
        /// <summary>
        /// called after the hediff is added 
        /// </summary>
        /// <param name="dinfo">The dinfo.</param>
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (pawn.Name == null)
            {
                pawn.Name = new NameSingle(pawn.Label); //make sure they always have a name, is needed for sapients 
            }
        }

        /// <summary>called after the pawn's tick method.</summary>
        public override void PostTick()
        {
            base.PostTick();

            if (_lastStage != CurStageIndex)
            {
                _lastStage = CurStageIndex;
                if (pawn != null)
                    //if the stage changed make sure we check dynamic components 
                {
                    PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
                    //check needs to 
                    pawn.needs?.AddOrRemoveNeedsAsAppropriate();
                }
            }
        }
    }
}