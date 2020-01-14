﻿// TransformationBase.cs created by Iron Wolf for Pawnmorph on 01/02/2020 12:54 PM
// last updated 01/02/2020  12:54 PM

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pawnmorph.Utilities;
using UnityEngine;
using Verse;

namespace Pawnmorph.Hediffs
{
    /// <summary>abstract base class for all transformation hediffs</summary>
    /// <seealso cref="Pawnmorph.IDescriptiveHediff" />
    /// <seealso cref="Verse.Hediff" />
    public abstract class TransformationBase : HediffWithComps, IDescriptiveHediff
    {
        [NotNull] private readonly Dictionary<BodyPartDef, List<MutationEntry>> _scratchDict =
            new Dictionary<BodyPartDef, List<MutationEntry>>();

        private List<BodyPartRecord> _checkList;
        private int _curIndex;

        /// <summary>
        ///     Gets the description.
        /// </summary>
        /// <value>
        ///     The description.
        /// </value>
        public virtual string Description
        {
            get
            {
                if (CurStage is IDescriptiveStage dStage)
                    return string.IsNullOrEmpty(dStage.DescriptionOverride) ? def.description : dStage.DescriptionOverride;

                return def.description;
            }
        }

        /// <summary>
        /// the expected number of mutations to happen in a single day 
        /// </summary>
        public abstract float MeanMutationsPerDay { get; }

        /// <summary>Gets the available mutations.</summary>
        /// <value>The available mutations.</value>
        [NotNull]
        public abstract IEnumerable<MutationEntry> AllAvailableMutations { get; }


        /// <summary>
        ///     Gets a value indicating whether this instance has finished adding mutations or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance has finished adding mutations; otherwise, <c>false</c>.
        /// </value>
        public bool FinishedAddingMutations
        {
            get
            {
                if (_checkList == null) return false;
                return _checkList.Count == _curIndex;
            }
        }

        /// <summary>Gets the label base.</summary>
        /// <value>The label base.</value>
        public override string LabelBase
        {
            get
            {
                string label; 
                if (CurStage is IDescriptiveStage dStage)
                    label = string.IsNullOrEmpty(dStage.LabelOverride) ? base.LabelBase : dStage.LabelOverride;
                else
                    label = base.LabelBase;

                if(SingleComp != null)
                {
                    label = $"{label}x{SingleComp.stacks}";
                }

                return label; 

            }
        }

        /// <summary>Gets the minimum mutations per check.</summary>
        /// if greater then 1, every-time a mutation is possible don't stop iterating over the parts until a mutable part is found
        /// <value>The minimum mutations per check.</value>
        protected virtual int MinMutationsPerCheck => 1;

        /// <summary>Gets a value indicating whether this instance can reset.</summary>
        /// <value><c>true</c> if this instance can reset; otherwise, <c>false</c>.</value>
        protected virtual bool CanReset => FinishedAddingMutations;

        /// <summary>Exposes the data.</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _checkList, nameof(_checkList), LookMode.BodyPart);
            Scribe_Values.Look(ref _curIndex, nameof(_curIndex));
            if (Scribe.mode == LoadSaveMode.PostLoadInit && _checkList == null)
            {
                ResetMutationOrder();
                ResetPossibleMutations();
            }
        }


        /// <summary>called after this hediff is added to the pawn</summary>
        /// <param name="dinfo">The dinfo.</param>
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            _checkList = new List<BodyPartRecord>();
            FillPartCheckList(_checkList);
           
            _curIndex = 0;
        }

        /// <summary>Ticks this instance.</summary>
        public override void Tick()
        {
            HediffStage cStage = CurStage;
            base.Tick();
            if (CurStage != cStage) OnStageChanged(cStage);
            var mtb = 1 / Mathf.Max(0.0001f, MeanMutationsPerDay); 
            if ( pawn.IsHashIntervalTick(60) && Rand.MTBEventOccurs(mtb, 60000, 60)) AddMutations();

            if (pawn.IsHashIntervalTick(10000) && CanReset) ResetMutationOrder();
        }

        /// <summary>Fills the part check list.</summary>
        /// the check list is a list of all parts in the parents body def in the order mutations should be added
        /// <param name="checkList">The check list.</param>
        protected abstract void FillPartCheckList([NotNull] List<BodyPartRecord> checkList);

        /// <summary>Gets the available the mutations from the given stage.</summary>
        /// <param name="currentStage">The current stage.</param>
        /// <returns></returns>
        [NotNull]
        protected virtual IEnumerable<MutationEntry> GetAvailableMutations([NotNull] HediffStage currentStage)
        {
            return AllAvailableMutations;
        }

        /// <summary>Notifies this instance that the available mutations have changed.</summary>
        protected void Notify_AvailableMutationsChanged()
        {
            ResetPossibleMutations();
        }

        /// <summary>
        ///     Called when the stage changes.
        /// </summary>
        /// <param name="lastStage">The last stage.</param>
        protected virtual void OnStageChanged([CanBeNull] HediffStage lastStage)
        {
            ResetPossibleMutations();
            TryGiveTransformations();
            if(CurStage is IExecutableStage execStage)
            {
                execStage.EnteredStage(this); 
            }
        }

        /// <summary>Tries to give transformations</summary>
        protected virtual void TryGiveTransformations()
        {
            if (CurStage == null) return;

            RandUtilities.PushState();

            foreach (var tfGiver in CurStage.GetAllTransformers())
                if (tfGiver.TryTransform(pawn, this))
                    break; //try each one, one by one. break at first one that succeeds  

            RandUtilities.PopState();
        }

        /// <summary>
        /// returns true if there are ny mutations in this stage 
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        protected abstract bool AnyMutationsInStage(HediffStage stage); 

        private void AddMutations()
        {
            
            if (FinishedAddingMutations) return;
            if (!AnyMutationsInStage(CurStage)) return; 
            
            if (_scratchDict.Count == 0)
            {
                ResetPossibleMutations();   
            }
            
            if(_curIndex < _checkList.Count)
                AddPartMutations();
            else
            {
                //add whole body mutations 
                int mutationsAdded=0;
                foreach (MutationEntry mutationEntry in _wholeBodyParts)
                {
                    if (Rand.Value < mutationEntry.addChance)
                    {
                        MutationUtilities.AddMutation(pawn, mutationEntry.mutation);
                        var mutagen = def.GetMutagenDef();
                        mutagen.TryApplyAspects(pawn); 
                        mutationsAdded++; 
                    }
                    if(mutationsAdded >= MinMutationsPerCheck) break;
                }

                if (mutationsAdded > 0)
                    OnMutationsAdded(mutationsAdded); 
            }
            
        }

        /// <summary>
        /// Tries the merge with the other hediff
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public override bool TryMergeWith(Hediff other)
        {
            if (base.TryMergeWith(other))
            {
                var sCompOther = other.TryGetComp<HediffComp_Single>();
                var sComp = SingleComp;
                if (sComp != null && sCompOther != null)
                {
                    sComp.stacks = Mathf.Clamp(sComp.stacks + sCompOther.stacks, 0, sComp.Props.maxStacks); 
                }
                ResetMutationOrder();
                return true; 
            }

            return false; 
        }

        /// <summary>
        /// Gets the single comp for partial transformation hediffs 
        /// </summary>
        /// <value>
        /// The single comp.
        /// </value>
        [CanBeNull]
        protected HediffComp_Single SingleComp => this.TryGetComp<HediffComp_Single>(); 

        /// <summary>
        /// Called when mutations are added the pawn.
        /// </summary>
        /// <param name="mutationsAdded">The mutations added.</param>
        protected virtual void OnMutationsAdded(int mutationsAdded)
        {
            var sComp = SingleComp;
            if(sComp != null)
            {
                sComp.stacks = Mathf.Max(sComp.stacks - mutationsAdded, 0);
                if (sComp.stacks == 0)
                {
                    pawn.health.RemoveHediff(this); 
                }
            }
        }

        private void AddPartMutations()
        {
            var mutationsAdded = 0;
            while (_curIndex < _checkList.Count)
            {
                BodyPartRecord part = _checkList[_curIndex];

                var lst = _scratchDict.TryGetValue(part.def);
                var mutationToAdd = lst?.RandomElementWithFallback();
                if (mutationToAdd != null)
                {
                    if (Rand.Value < mutationToAdd.addChance)
                    {
                        MutationUtilities.AddMutation(pawn, mutationToAdd.mutation, part);
                        var mutagen = def.GetMutagenDef();
                        mutagen.TryApplyAspects(pawn);
                        mutationsAdded++;
                    }
                    else if (mutationToAdd.blocks)
                    {
                        return;
                    }
                }

                _curIndex++; //increment after so mutations can 'block'

                //check if we have added enough mutations to end the loop early 
                if (mutationsAdded >= MinMutationsPerCheck) break;
            }

            if (mutationsAdded > 0)
            {
                OnMutationsAdded(mutationsAdded); 
            }
        }


        private void ResetMutationOrder()
        {
            _checkList = _checkList ?? new List<BodyPartRecord>();
            _checkList.Clear();
            _curIndex = 0;
            FillPartCheckList(_checkList); 
        }

        [NotNull]
        private readonly List<MutationEntry> _wholeBodyParts = new List<MutationEntry>(); 

        private void ResetPossibleMutations()
        {
            try
            {
                IEnumerable<MutationEntry> mutations = GetAvailableMutations(CurStage);
                _scratchDict.Clear();
                foreach (var entry in mutations) //fill a lookup dict 
                foreach (BodyPartDef possiblePart in entry.mutation.parts.MakeSafe())
                    if (_scratchDict.TryGetValue(possiblePart, out var lst))
                    {
                        lst.Add(entry);
                    }
                    else
                    {
                        lst = new List<MutationEntry> {entry};
                        _scratchDict[possiblePart] = lst;
                    }
                _wholeBodyParts.Clear();
                foreach (MutationEntry availableMutation in GetAvailableMutations(CurStage))
                {
                    if (availableMutation.mutation.parts == null)
                    {
                        _wholeBodyParts.Add(availableMutation); 
                    }
                }
            }
            catch (NullReferenceException e)
            {
                throw new NullReferenceException($"null reference exception while trying reset mutations! are all mutations set in {def.defName}?",e);
            }
        }
    }
}