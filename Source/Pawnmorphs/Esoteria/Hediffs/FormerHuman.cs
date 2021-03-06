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

        /// <summary>
        ///     Gets the label in brackets.
        /// </summary>
        /// <value>
        ///     The label in brackets.
        /// </value>
        public override string LabelInBrackets
        {
            get
            {
                if (string.IsNullOrEmpty(_labelCached))
                {
                    SapienceLevel? saLabel = pawn?.GetQuantizedSapienceLevel();
                    _labelCached = saLabel?.GetLabel() ?? "unknown";
                }

                return _labelCached; 
            }
        }


        private string _labelCached;

        /// <summary>Exposes the data.</summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref _lastStage, nameof(_lastStage));
            Scribe_Values.Look(ref _labelCached, nameof(_labelCached));
        }

        /// <summary>
        ///     called after the hediff is added
        /// </summary>
        /// <param name="dinfo">The dinfo.</param>
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (pawn.Name == null)
                pawn.Name = new NameSingle(pawn.Label); //make sure they always have a name, is needed for sapients 
        }

        /// <summary>called after the pawn's tick method.</summary>
        public override void PostTick()
        {
            base.PostTick();

            if (_lastStage != CurStageIndex)
            {
                OnSapienceLevelChanged();
            }

            if (pawn.needs == null) return;//dead pawns don't have needs for some reason 
            
        }


        /// <summary>
        /// Notifies this instance that the sapience level changes.
        /// </summary>
        public void NotifySapienceLevelChanges()
        {
            SapienceLevel? saLabel = pawn?.GetQuantizedSapienceLevel();
            _labelCached = saLabel?.GetLabel() ?? "unknown";
        }

        private void OnSapienceLevelChanged()
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